using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StationaryManagement1.Data;


namespace StationaryManagement1.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDBContext _context;

        public AccountController(AppDBContext context)
        {
            _context = context;
        }

        // REGISTER
        [HttpGet] public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(StationaryManagement1.Models.Employee model, string password)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _context.Employees.AnyAsync(e => e.Email == model.Email))
            {
                ModelState.AddModelError("", "Email already exists.");
                return View(model);
            }

            model.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            model.CreatedAt = DateTime.UtcNow;
            model.IsActive = true;

            _context.Employees.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Registration successful! You can now log in.";
            return RedirectToAction("Login");
        }

        // LOGIN
        [HttpGet] public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                TempData["Error"] = "Email and password are required.";
                return RedirectToAction("Login");
            }

            var user = await _context.Employees.FirstOrDefaultAsync(e => e.Email == email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                TempData["Error"] = "Invalid email or password.";
                return RedirectToAction("Login");
            }

            if (!user.IsActive)
            {
                TempData["Error"] = "Your account is deactivated.";
                return RedirectToAction("Login");
            }

            // Save session values
            HttpContext.Session.SetInt32("EmployeeId", user.EmployeeId);
            HttpContext.Session.SetString("EmployeeName", user.Name);
            HttpContext.Session.SetInt32("RoleId", user.RoleId);

            // REMEMBER ME (store cookies)
            if (rememberMe)
            {
                var isHttps = Request.IsHttps;
                var cookieOptions = new CookieOptions
                {
                    Expires = DateTime.UtcNow.AddDays(30),
                    HttpOnly = true,
                    Secure = isHttps, // use HTTPS in prod; ok on HTTP for local dev
                    IsEssential = true,
                    Path = "/"
                };

                Response.Cookies.Append("RememberEmployeeId", user.EmployeeId.ToString(), cookieOptions);
                Response.Cookies.Append("RememberEmployeeName", user.Name, cookieOptions);
                Response.Cookies.Append("RememberRoleId", user.RoleId.ToString(), cookieOptions);
            }

            TempData["Success"] = "Login successful!";
            return RedirectToAction("Index", "Home");
        }

        // LOGOUT
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            Response.Cookies.Delete("RememberEmployeeId");
            Response.Cookies.Delete("RememberEmployeeName");
            Response.Cookies.Delete("RememberRoleId");
            return RedirectToAction("Login");
        }

        // CHANGE PASSWORD
        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword)
        {
            int? employeeId = HttpContext.Session.GetInt32("EmployeeId");
            if (employeeId == null) return RedirectToAction("Login");

            var user = await _context.Employees.FindAsync(employeeId.Value);
            if (user == null) return RedirectToAction("Login");

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            {
                TempData["Error"] = "Current password is incorrect.";
                return RedirectToAction("ChangePassword");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.ModifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Password changed successfully!";
            return RedirectToAction("Index", "Home");
        }

        public IActionResult TestCookie()
        {
            var cookieOptions = new CookieOptions
            {
                Expires = DateTime.UtcNow.AddDays(30),
                HttpOnly = true,
                Secure = Request.IsHttps,
                IsEssential = true,
                Path = "/"
            };

            Response.Cookies.Append("TestCookie", "HelloWorld", cookieOptions);

            return Content("TestCookie set!");
        }
        // GET: Account/AccessDenied
        public IActionResult AccessDenied()
        {
            // Optional: you can pass a message via ViewData or TempData
            ViewData["Message"] = "You do not have permission to access this page.";
            return View();
        }
    }
}
