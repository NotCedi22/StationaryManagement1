using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StationaryManagement1.Data;
using StationaryManagement1.Models;
using StationaryManagement1.Models.Filters;
using StationaryManagement1.Services;

namespace StationaryManagement1.Controllers
{
    [RequireLogin]
    public class EmployeesController(AppDBContext context, NotificationService notificationService) : Controller
    {
        private readonly AppDBContext _context = context;
        private readonly NotificationService _notificationService = notificationService;

        // GET: Employees - Admin/Manager only
        public async Task<IActionResult> Index()
        {
            // Check if user is admin or manager
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 1 && roleId != 2)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var employees = await _context.Employees
                .Include(e => e.Role)
                .ToListAsync();
            
            return View(employees);
        }

        // GET: Employees/Details/5 - Admin/Manager can see all, Employees can only see themselves
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var roleId = HttpContext.Session.GetInt32("RoleId");
            var currentUserId = HttpContext.Session.GetInt32("EmployeeId");

            // Employees can only view their own details
            if (roleId == 3 && currentUserId != id)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // Admin and Manager can view all
            if (roleId != 1 && roleId != 2 && roleId != 3)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var employee = await _context.Employees
                .Include(e => e.Role)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null) return NotFound();
            return View(employee);
        }

        // GET: Employees/Create - Admin only
        public IActionResult Create()
        {
            // Check if user is admin
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 1)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            ViewBag.Roles = _context.Roles.ToList();
            return View();
        }

        // POST: Employees/Create - Admin only
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Employee employee, string? Password)
        {
            // Check if user is admin
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 1)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (ModelState.IsValid)
            {
                if (!string.IsNullOrWhiteSpace(Password))
                {
                    // Hash the password using BCrypt
                    employee.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password);
                }

                employee.CreatedAt = DateTime.UtcNow;
                employee.IsActive = true;

                _context.Add(employee);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Roles = _context.Roles.ToList();
            return View(employee);
        }

        // GET: Employees/Edit/5 - Admin only
        public async Task<IActionResult> Edit(int? id)
        {
            // Check if user is admin
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 1)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (id == null) return NotFound();

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            ViewBag.Roles = _context.Roles.ToList();
            return View(employee);
        }

        // POST: Employees/Edit/5 - Admin only
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Employee employee, string? NewPassword)
        {
            // Check if user is admin
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 1)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (id != employee.EmployeeId)
                return NotFound();

            if (ModelState.IsValid)
            {
                var existing = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == id);
                if (existing == null) return NotFound();

                existing.Name = employee.Name;
                existing.Email = employee.Email;
                existing.RoleId = employee.RoleId;
                existing.IsActive = employee.IsActive;

                if (!string.IsNullOrWhiteSpace(NewPassword))
                {
                    existing.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
                }

                existing.ModifiedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Roles = _context.Roles.ToList();
            return View(employee);
        }

        // GET: Employees/Delete/5 - Admin only
        public async Task<IActionResult> Delete(int? id)
        {
            // Check if user is admin
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 1)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (id == null) return NotFound();

            var employee = await _context.Employees
                .Include(e => e.Role)
                .FirstOrDefaultAsync(m => m.EmployeeId == id);
            if (employee == null) return NotFound();

            return View(employee);
        }

        // POST: Employees/Delete/5 - Admin only
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Check if user is admin
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 1)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var employee = await _context.Employees
                .Include(e => e.Subordinates)
                .Include(e => e.StationeryRequests)
                .Include(e => e.Notifications)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee != null)
            {
                // Remove related entities
                _context.StationeryRequests.RemoveRange(employee.StationeryRequests);
                _context.Notifications.RemoveRange(employee.Notifications);

                // Reassign subordinates
                foreach (var sub in employee.Subordinates)
                    sub.SuperiorId = null;

                // Hard delete employee
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.EmployeeId == id);
        }
    }
}
