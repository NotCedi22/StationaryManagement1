using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using StationaryManagement1.Data;
using StationaryManagement1.Models.Filters;

namespace StationaryManagement1.Controllers
{
    [RequireLogin]
    public class AdminController : Controller
    {
        private readonly AppDBContext _context;

        public AdminController(AppDBContext context) => _context = context;

        private bool IsAdmin()
        {
            return HttpContext.Session.GetInt32("RoleId") == 1;
        }

        // GET: Admin/HashPasswords
        public IActionResult HashPasswords()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View();
        }

        // POST: Admin/HashPasswords
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HashPasswordsConfirmed()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var employees = _context.Employees.ToList();

            foreach (var emp in employees)
            {
                if (!string.IsNullOrWhiteSpace(emp.PasswordHash))
                {
                    string plainPassword = emp.PasswordHash;

                    // Assign known passwords
                    if (emp.Email.Contains("admin", StringComparison.OrdinalIgnoreCase))
                        plainPassword = "admin123";
                    else if (emp.RoleId == 2) // Employee
                        plainPassword = "pass123";
                    else if (emp.RoleId == 3) // Manager
                        plainPassword = "manager123";

                    emp.PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);
                }
            }

            _context.SaveChanges();
            ViewBag.Message = "All passwords hashed successfully!";
            return View("HashPasswords");
        }
    }
}
