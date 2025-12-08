using Microsoft.AspNetCore.Mvc;
using StationaryManagement1.Data;

namespace StationaryManagement1.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDBContext _context;

        public AdminController(AppDBContext context) => _context = context;

        // GET: Admin/HashPasswords
        public IActionResult HashPasswords()
        {
            return View();
        }

        // POST: Admin/HashPasswords
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HashPasswordsConfirmed()
        {
            var employees = _context.Employees.ToList();

            foreach (var emp in employees)
            {
                if (!string.IsNullOrWhiteSpace(emp.PasswordHash))
                {
                    string plainPassword = emp.PasswordHash;

                    // Assign known passwords for specific users
                    if (emp.Email.ToLower().Contains("admin"))
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
            return View();
        }
    }
}
