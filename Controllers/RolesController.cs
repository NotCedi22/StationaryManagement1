using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StationaryManagement1.Data;
using StationaryManagement1.Models;
using StationaryManagement1.Models.Filters;

namespace StationaryManagement1.Controllers
{
    [RequireLogin]
    public class RolesController : Controller
    {
        private readonly AppDBContext _context;

        public RolesController(AppDBContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetInt32("RoleId") == 1;
        }

        private string GetCurrentUserRole()
        {
            var roleId = HttpContext.Session.GetInt32("RoleId");
            return roleId switch
            {
                1 => "Admin",
                2 => "Manager",
                3 => "Employee",
                _ => "Guest"
            };
        }

        // GET: Roles
        public async Task<IActionResult> Index()
        {
            ViewBag.CurrentUserRole = GetCurrentUserRole();
            return View(await _context.Roles
                .Include(r => r.RoleThreshold)
                .Include(r => r.ReportsTo)
                .ToListAsync());
        }

        // GET: Roles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var role = await _context.Roles
    .Include(r => r.RoleThreshold)
    .FirstOrDefaultAsync(m => m.RoleId == id);
            if (role == null) return NotFound();

            ViewBag.CurrentUserRole = GetCurrentUserRole();
            return View(role);
        }

        // GET: Roles/Create
        public IActionResult Create()
        {
            if (!IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            ViewBag.CurrentUserRole = GetCurrentUserRole();
            return View();
        }

        // POST: Roles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Role role)
        {
            if (!IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            if (!ModelState.IsValid)
                return View(role);

            // Ensure RoleThreshold is linked to Role
            if (role.RoleThreshold != null)
                role.RoleThreshold.RoleId = role.RoleId;

            _context.Add(role);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Roles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            if (id == null) return NotFound();

            var role = await _context.Roles
                .Include(r => r.RoleThreshold) // include RoleThreshold
                .FirstOrDefaultAsync(r => r.RoleId == id);

            if (role == null) return NotFound();

            // Ensure RoleThreshold is not null for the view
            if (role.RoleThreshold == null)
                role.RoleThreshold = new RoleThreshold { MaxAmount = 0 };

            ViewBag.CurrentUserRole = GetCurrentUserRole();
            return View(role);
        }
        // POST: Roles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Role role)
        {
            if (!IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            if (id != role.RoleId) return NotFound();
            if (!ModelState.IsValid) return View(role);

            // Load existing role including RoleThreshold
            var existingRole = await _context.Roles
                .Include(r => r.RoleThreshold)
                .FirstOrDefaultAsync(r => r.RoleId == id);

            if (existingRole == null) return NotFound();

            // Update main fields
            existingRole.RoleName = role.RoleName;
            existingRole.Description = role.Description;
            existingRole.CanApprove = role.CanApprove;

            // Update or create RoleThreshold
            if (existingRole.RoleThreshold == null && role.RoleThreshold != null)
            {
                existingRole.RoleThreshold = new RoleThreshold
                {
                    MaxAmount = role.RoleThreshold.MaxAmount
                };
            }
            else if (existingRole.RoleThreshold != null && role.RoleThreshold != null)
            {
                existingRole.RoleThreshold.MaxAmount = role.RoleThreshold.MaxAmount;
            }

            _context.Update(existingRole);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Roles/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            if (id == null) return NotFound();

            var role = await _context.Roles.FindAsync(id);
            if (role == null) return NotFound();

            ViewBag.CurrentUserRole = GetCurrentUserRole();
            return View(role);
        }

        // POST: Roles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            var role = await _context.Roles.FindAsync(id);
            if (role != null) _context.Roles.Remove(role);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
