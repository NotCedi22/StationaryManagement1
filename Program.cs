using Microsoft.EntityFrameworkCore;
using StationaryManagement1.Data;
using StationaryManagement1.Models;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// Database
builder.Services.AddDbContext<AppDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Session services (1 HOUR EXPIRATION)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);   // 🔥 1 hour
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Allow session access
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();  // 🔥 MUST be here BEFORE endpoints

// -----------------------------------------
// AUTO LOGIN FROM COOKIE IF SESSION EMPTY
// -----------------------------------------
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSession();  // session must be BEFORE this middleware

// Restore session from remember-me cookies
app.Use(async (context, next) =>
{
    if (context.Session.GetInt32("EmployeeId") == null)
    {
        var userIdCookie = context.Request.Cookies["RememberEmployeeId"];
        var nameCookie = context.Request.Cookies["RememberEmployeeName"];
        var roleCookie = context.Request.Cookies["RememberRoleId"];

        if (!string.IsNullOrEmpty(userIdCookie) &&
            !string.IsNullOrEmpty(nameCookie) &&
            !string.IsNullOrEmpty(roleCookie))
        {
            context.Session.SetInt32("EmployeeId", int.Parse(userIdCookie));
            context.Session.SetString("EmployeeName", nameCookie);
            context.Session.SetInt32("RoleId", int.Parse(roleCookie));
        }
    }

    await next.Invoke();
});

app.UseRouting();
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
            db.Employees.Add(new Employee
            {
                Name = "Admin",
                Email = "admin@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                RoleId = adminRole.RoleId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            });

            db.SaveChanges();
        }
    }
}

app.Run();
