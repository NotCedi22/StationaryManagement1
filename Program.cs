using Microsoft.EntityFrameworkCore;
using StationaryManagement1.Data;
using StationaryManagement1.Models;
using StationaryManagement1.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// Database - Supports environment variables for portability
// Connection string priority: Environment Variable > appsettings.json > Default
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? Environment.GetEnvironmentVariable("StationeryDB_ConnectionString")
    ?? "Server=(localdb)\\mssqllocaldb;Database=StationeryDB;Trusted_Connection=True;TrustServerCertificate=True";

builder.Services.AddDbContext<AppDBContext>(options =>
    options.UseSqlServer(connectionString)
);

// Services
builder.Services.AddScoped<NotificationService>();

// Session services (1 HOUR EXPIRATION)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);   // 🔥 1 hour
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // for localhost HTTP/HTTPS
    // For production-only HTTPS cookies, switch to:
    // options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Antiforgery cookies also HTTPS-only
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // dev-friendly
    // For production-only HTTPS cookies, switch to:
    // options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Allow session access
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// -----------------------------------------
// APPLY PENDING MIGRATIONS AND FIX MISSING COLUMNS
// MUST RUN BEFORE ANY DATABASE QUERIES
// -----------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDBContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        db.Database.Migrate();
        logger.LogInformation("Migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Migration failed, attempting to add missing columns directly.");
        
        // Try to add missing columns directly via SQL
        try
        {
            var connection = db.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
            }
            
            // Check and add EmployeeNumber column
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Employees]') AND name = 'EmployeeNumber')
                    BEGIN
                        ALTER TABLE [dbo].[Employees] ADD [EmployeeNumber] nvarchar(max) NULL;
                    END";
                command.ExecuteNonQuery();
            }
            
            // Check and add ReportsToRoleId column
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Roles]') AND name = 'ReportsToRoleId')
                    BEGIN
                        ALTER TABLE [dbo].[Roles] ADD [ReportsToRoleId] int NULL;
                    END";
                command.ExecuteNonQuery();
            }
            
            // Create index for ReportsToRoleId if it doesn't exist
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Roles_ReportsToRoleId' AND object_id = OBJECT_ID(N'[dbo].[Roles]'))
                    BEGIN
                        CREATE INDEX [IX_Roles_ReportsToRoleId] ON [dbo].[Roles] ([ReportsToRoleId]);
                    END";
                command.ExecuteNonQuery();
            }
            
            // Add foreign key constraint if it doesn't exist
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Roles_Roles_ReportsToRoleId')
                    BEGIN
                        ALTER TABLE [dbo].[Roles]
                        ADD CONSTRAINT [FK_Roles_Roles_ReportsToRoleId] 
                        FOREIGN KEY ([ReportsToRoleId]) 
                        REFERENCES [dbo].[Roles] ([RoleId]) 
                        ON DELETE NO ACTION;
                    END";
                command.ExecuteNonQuery();
            }
            
            logger.LogInformation("Missing columns added successfully via direct SQL.");
        }
        catch (Exception sqlEx)
        {
            logger.LogError(sqlEx, "Failed to add missing columns. Please run the SQL script manually: Database (File)/AddMissingColumns.sql");
            throw; // Re-throw to prevent app from starting with invalid database
        }
    }
}

// Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Sessions before auth/remember-me helper
app.UseSession();

// Restore session from remember-me cookies if empty
// This middleware validates and restores user session from remember-me cookies
app.Use(async (context, next) =>
{
    // Only restore if session is empty
    if (context.Session.GetInt32("EmployeeId") == null)
    {
        var userIdCookie = context.Request.Cookies["RememberEmployeeId"];
        var nameCookie = context.Request.Cookies["RememberEmployeeName"];
        var roleCookie = context.Request.Cookies["RememberRoleId"];

        // Validate all cookies are present and parseable
        if (!string.IsNullOrEmpty(userIdCookie) &&
            !string.IsNullOrEmpty(nameCookie) &&
            !string.IsNullOrEmpty(roleCookie) &&
            int.TryParse(userIdCookie, out var uid) &&
            int.TryParse(roleCookie, out var rid))
        {
            // Validate user still exists and is active (security check)
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDBContext>();
            var user = await db.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == uid && e.IsActive);
            
            if (user != null && user.RoleId == rid)
            {
                // User is valid and active, restore session
                context.Session.SetInt32("EmployeeId", uid);
                context.Session.SetString("EmployeeName", user.Name); // Use current name from DB
                context.Session.SetInt32("RoleId", rid);
            }
            else
            {
                // User no longer exists or is inactive, clear cookies
                context.Response.Cookies.Delete("RememberEmployeeId");
                context.Response.Cookies.Delete("RememberEmployeeName");
                context.Response.Cookies.Delete("RememberRoleId");
            }
        }
    }

    await next.Invoke();
});

app.UseAuthentication();
app.UseAuthorization();

// normal MVC routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

// -----------------------------------------
// SEED ADMIN USER IF DATABASE IS EMPTY
// -----------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDBContext>();

    if (!db.Employees.Any())
    {
        var adminRole = db.Roles.FirstOrDefault(r => r.RoleName == "Admin");

        if (adminRole != null)
        {
            // Check if admin already exists
            var existingAdmin = db.Employees.FirstOrDefault(e => e.Email == "admin@example.com");
            if (existingAdmin == null)
            {
                db.Employees.Add(new Employee
                {
                    Name = "Admin",
                    Email = "admin@example.com",
                    EmployeeNumber = "1", // Assign employee number 1 for admin
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    RoleId = adminRole.RoleId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                });

                db.SaveChanges();
            }
            else
            {
                // Ensure existing admin has correct password and employee number
                if (string.IsNullOrEmpty(existingAdmin.EmployeeNumber))
                {
                    existingAdmin.EmployeeNumber = "1";
                }
                // Re-hash password to ensure it's correct
                existingAdmin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123");
                existingAdmin.IsActive = true;
                db.SaveChanges();
            }
        }
    }

    // -----------------------------------------
    // ASSIGN EMPLOYEE NUMBERS TO EXISTING USERS WITHOUT ONE
    // -----------------------------------------
    var employeesWithoutNumber = db.Employees
        .Where(e => string.IsNullOrEmpty(e.EmployeeNumber))
        .OrderBy(e => e.EmployeeId)
        .ToList();

    if (employeesWithoutNumber.Any())
    {
        var usedNumbers = db.Employees
            .Where(e => !string.IsNullOrEmpty(e.EmployeeNumber))
            .Select(e => e.EmployeeNumber)
            .ToList();

        var usedInts = usedNumbers
            .Where(n => !string.IsNullOrEmpty(n) && int.TryParse(n, out _))
            .Select(n => int.Parse(n!))
            .Where(n => n >= 1 && n <= 1000)
            .OrderBy(n => n)
            .ToList();

        int nextNumber = 1;
        foreach (var emp in employeesWithoutNumber)
        {
            // Find next available number
            while (usedInts.Contains(nextNumber) && nextNumber <= 1000)
            {
                nextNumber++;
            }

            if (nextNumber <= 1000)
            {
                emp.EmployeeNumber = nextNumber.ToString();
                usedInts.Add(nextNumber);
                nextNumber++;
            }
        }

        db.SaveChanges();
    }
}

app.Run();
