using Microsoft.EntityFrameworkCore;
using StationaryManagement.Data;
using StationaryManagement.Models;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// Database
builder.Services.AddDbContext<AppDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Session services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Allow session access in views/controllers
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

app.UseAuthentication();  // optional but OK
app.UseAuthorization();

app.UseSession();  // MUST be before routing

// Routing
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
                PasswordHash = "admin123",  // Or hashed later
                RoleId = adminRole.RoleId,
                IsActive = true
            });

            db.SaveChanges();
        }
    }
}

app.Run();
