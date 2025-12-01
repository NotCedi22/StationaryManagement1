using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StationaryManagement.Data;
using StationaryManagement.Models;

namespace StationaryManagement.Controllers
{
    public class EmployeesController : Controller
    {
        private readonly AppDBContext _context;

        public EmployeesController(AppDBContext context)
        {
            _context = context;
        }

        // GET: Employees
        public async Task<IActionResult> Index()
        {
            var employees = await _context.Employees
                .Include(e => e.Role)
                .ToListAsync();
            return View(employees);
        }

        // GET: Employees/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees
                .Include(e => e.Role)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null) return NotFound();

            return View(employee);
        }

        // GET: Employees/Create
        public IActionResult Create()
        {
            ViewBag.Roles = _context.Roles.ToList();
            return View();
        }

        // POST: Employees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Employee employee)
        {
            if (ModelState.IsValid)
            {
                _context.Add(employee);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Roles = _context.Roles.ToList();
            return View(employee);
        }

        // GET: Employees/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            ViewBag.Roles = _context.Roles.ToList();
            return View(employee);
        }
        // POST: Employees/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Employee employee, string? NewPassword)
        {
            if (id != employee.EmployeeId)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Fetch the existing tracked employee
                    var existing = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == id);
                    if (existing == null) return NotFound();

                    // Update only fields that can change
                    existing.Name = employee.Name;
                    existing.Email = employee.Email;
                    existing.RoleId = employee.RoleId;
                    existing.IsActive = employee.IsActive;

                    // Update password ONLY if a new password was provided
                    if (!string.IsNullOrWhiteSpace(NewPassword))
                    {
                        existing.PasswordHash = NewPassword; // Hash if needed
                    }

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeExists(employee.EmployeeId))
                        return NotFound();
                    else
                        throw;
                }

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Roles = _context.Roles.ToList();
            return View(employee);
        }
        // GET: Employees/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees
                .Include(e => e.Role)
                .FirstOrDefaultAsync(m => m.EmployeeId == id);
            if (employee == null) return NotFound();

            return View(employee);
        }

        // POST: Employees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
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
