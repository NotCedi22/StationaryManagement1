using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StationaryManagement1.Data;
using StationaryManagement1.Models.Filters;
using StationaryManagement1.Services;


namespace StationaryManagement1.Controllers;

public class AccountController(AppDBContext context, NotificationService notificationService) : Controller
{
    private readonly AppDBContext _context = context;
    private readonly NotificationService _notificationService = notificationService;

        // REGISTER - Admin only (no approval needed, direct creation)
        [RequireLogin]
        [HttpGet] 
        public async Task<IActionResult> Register()
        {
            // Check if user is admin
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 1)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // Get all roles for admin to select
            ViewBag.Roles = await _context.Roles.ToListAsync();
            return View();
        }

        [RequireLogin]
        [HttpPost]
        public async Task<IActionResult> Register(StationaryManagement1.Models.Employee model, string password)
        {
            // Check if user is admin
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 1)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                ModelState.AddModelError("", "Password must be at least 6 characters long.");
                ViewBag.Roles = await _context.Roles.ToListAsync();
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Roles = await _context.Roles.ToListAsync();
                return View(model);
            }

            // Check if email already exists
            if (await _context.Employees.AnyAsync(e => e.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email already exists.");
                ViewBag.Roles = await _context.Roles.ToListAsync();
                return View(model);
            }

            // Auto-assign Employee Number: Find the lowest available number (1-1000)
            var usedNumbers = await _context.Employees
                .Where(e => !string.IsNullOrEmpty(e.EmployeeNumber))
                .Select(e => e.EmployeeNumber)
                .ToListAsync();

            // Convert to integers and find available number
            var usedInts = usedNumbers
                .Where(n => !string.IsNullOrEmpty(n) && int.TryParse(n, out _))
                .Select(n => int.Parse(n!))
                .Where(n => n >= 1 && n <= 1000)
                .OrderBy(n => n)
                .ToList();

            int? assignedNumber = null;
            for (int i = 1; i <= 1000; i++)
            {
                if (!usedInts.Contains(i))
                {
                    assignedNumber = i;
                    break;
                }
            }

            if (!assignedNumber.HasValue)
            {
                ModelState.AddModelError("", "Sorry, all employee numbers (1-1000) are currently in use.");
                ViewBag.Roles = await _context.Roles.ToListAsync();
                return View(model);
            }

            // Set values - admin can choose role
            model.EmployeeNumber = assignedNumber.Value.ToString();
            model.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            model.CreatedAt = DateTime.UtcNow;
            model.IsActive = true; // Active immediately, no approval needed

            _context.Employees.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Employee created successfully! Employee Number: {assignedNumber.Value}, Email: {model.Email}";
            return RedirectToAction("Index", "Employees");
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
            if (user == null)
            {
                TempData["Error"] = "Invalid email or password.";
                return RedirectToAction("Login");
            }

            if (string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                TempData["Error"] = "Invalid email or password.";
                return RedirectToAction("Login");
            }

            // No longer checking for pending approval since registration is automatic

            if (!user.IsActive)
            {
                TempData["Error"] = "Your account is deactivated.";
                return RedirectToAction("Login");
            }

            // Save session values
            HttpContext.Session.SetInt32("EmployeeId", user.EmployeeId);
            HttpContext.Session.SetString("EmployeeName", user.Name);
            HttpContext.Session.SetInt32("RoleId", user.RoleId);

            // REMEMBER ME (store cookies) - Cross-browser compatible
            if (rememberMe)
            {
                var isHttps = Request.IsHttps;
                var cookieOptions = new CookieOptions
                {
                    Expires = DateTime.UtcNow.AddDays(30),
                    HttpOnly = true, // Prevents JavaScript access for security
                    Secure = isHttps, // HTTPS in production; HTTP for local dev
                    IsEssential = true, // Required for GDPR compliance
                    Path = "/",
                    SameSite = SameSiteMode.Lax // Works across browsers (Chrome, Firefox, Safari, Edge)
                };

                Response.Cookies.Append("RememberEmployeeId", user.EmployeeId.ToString(), cookieOptions);
                Response.Cookies.Append("RememberEmployeeName", user.Name, cookieOptions);
                Response.Cookies.Append("RememberRoleId", user.RoleId.ToString(), cookieOptions);
            }
            else
            {
                // Clear remember-me cookies if user unchecks the box
                Response.Cookies.Delete("RememberEmployeeId");
                Response.Cookies.Delete("RememberEmployeeName");
                Response.Cookies.Delete("RememberRoleId");
            }

            TempData["Success"] = "Login successful!";
            return RedirectToAction("Index", "Home");
        }

        // LOGOUT
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            
            // Clear remember-me cookies with same options for cross-browser compatibility
            var cookieOptions = new CookieOptions
            {
                Expires = DateTime.UtcNow.AddDays(-1), // Expire immediately
                HttpOnly = true,
                Secure = Request.IsHttps,
                IsEssential = true,
                Path = "/",
                SameSite = SameSiteMode.Lax
            };
            
            Response.Cookies.Append("RememberEmployeeId", "", cookieOptions);
            Response.Cookies.Append("RememberEmployeeName", "", cookieOptions);
            Response.Cookies.Append("RememberRoleId", "", cookieOptions);
            
            return RedirectToAction("Login");
        }

        // CHANGE PASSWORD
        [RequireLogin]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [RequireLogin]
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

            await _notificationService.NotifyAsync(user.EmployeeId, user.SuperiorId, null, "Your account password was changed.");
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
