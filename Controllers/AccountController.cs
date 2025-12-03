using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StationaryManagement.Data;
using StationaryManagement.Models;
using System.Security.Cryptography;
using System.Text;

namespace StationaryManagement.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDBContext _context;

        public AccountController(AppDBContext context)
        {
            _context = context;
        }

        // ----------------------
        // REGISTER (GET)
        // ----------------------
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // ----------------------
        // REGISTER (POST)
        // ----------------------
        [HttpPost]
        public async Task<IActionResult> Register(Employee model, string password)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (await _context.Employees.AnyAsync(e => e.Email == model.Email))
            {
                ModelState.AddModelError("", "Email already exists");
                return View(model);
            }

            model.PasswordHash = HashPassword(password);
            model.CreatedAt = DateTime.UtcNow;
            model.IsActive = true;

            _context.Employees.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }


        // ----------------------
        // LOGIN (GET)
        // ----------------------
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }


        // ----------------------
        // LOGIN (POST)
        // ----------------------
        [HttpPost]
        public IActionResult Login(string email, string password, bool rememberMe)
        {
            var user = _context.Employees
                .FirstOrDefault(e => e.Email == email && e.PasswordHash == password);

            if (user == null)
            {
                TempData["Error"] = "Invalid email or password.";
                return RedirectToAction("Login");
            }

            if (!user.IsActive)
            {
                TempData["Error"] = "Your account is deactivated.";
                return RedirectToAction("Login");
            }

            // Save short session normally
            HttpContext.Session.SetInt32("EmployeeId", user.EmployeeId);
            HttpContext.Session.SetString("EmployeeName", user.Name);

            // ✔ Remember Me: Save cookie for 30 days
            if (rememberMe)
            {
                Response.Cookies.Append("RememberEmployeeId", user.EmployeeId.ToString(), new CookieOptions
                {
                    Expires = DateTime.UtcNow.AddDays(30),
                    HttpOnly = true,
                    Secure = true,
                    IsEssential = true
                });

                Response.Cookies.Append("RememberEmployeeName", user.Name, new CookieOptions
                {
                    Expires = DateTime.UtcNow.AddDays(30),
                    HttpOnly = true,
                    Secure = true,
                    IsEssential = true
                });
            }

            TempData["Success"] = "Login successful!";
            return RedirectToAction("Index", "Home");
        }



        // ----------------------
        // LOGOUT
        // ----------------------
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }


        // ----------------------
        // CHANGE PASSWORD (GET)
        // ----------------------
        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (HttpContext.Session.GetInt32("EmployeeId") == null)
                return RedirectToAction("Login");

            return View();
        }


        // ----------------------
        // CHANGE PASSWORD (POST)
        // ----------------------
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword)
        {
            int? employeeId = HttpContext.Session.GetInt32("EmployeeId");

            if (employeeId == null)
                return RedirectToAction("Login");

            var employee = await _context.Employees.FindAsync(employeeId);

            if (employee == null || employee.PasswordHash != HashPassword(currentPassword))
            {
                ModelState.AddModelError("", "Current password is incorrect.");
                return View();
            }

            employee.PasswordHash = HashPassword(newPassword);
            employee.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            ViewBag.Success = "Password changed successfully!";
            return View();
        }


        // ----------------------
        // HASH PASSWORD
        // ----------------------
        private string HashPassword(string password)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }
    }
}
