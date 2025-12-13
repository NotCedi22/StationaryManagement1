using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StationaryManagement1.Data;
using StationaryManagement1.Models;
using StationaryManagement1.Models.Filters;
using StationaryManagement1.Models.ViewModels;

namespace StationaryManagement1.Controllers
{
    [RequireLogin]
    public class StationeryItemsController : Controller
    {
        private readonly AppDBContext _context;
        private readonly string _imageFolder =
            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");

        public StationeryItemsController(AppDBContext context)
        {
            _context = context;

            if (!Directory.Exists(_imageFolder))
                Directory.CreateDirectory(_imageFolder);
        }

        // ========================= INDEX =========================
        public async Task<IActionResult> Index(int? categoryId)
        {
            var currentUserId = GetCurrentUserId();

            var currentUser = await _context.Employees
                .Include(e => e.Role)
                .ThenInclude(r => r!.RoleThreshold)
                .FirstOrDefaultAsync(e => e.EmployeeId == currentUserId);

            ViewBag.CurrentUserRole = GetCurrentUserRole();

            if (currentUser?.Role?.RoleThreshold != null)
            {
                var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

                var currentMonthSpending = await _context.StationeryRequests
                    .Where(r => r.EmployeeId == currentUserId
                        && r.RequestDate >= startOfMonth
                        && (r.Status == "Approved" || r.Status == "Pending"))
                    .SumAsync(r => r.TotalCost);

                ViewBag.MaxPurchaseAmount = currentUser.Role.RoleThreshold.MaxAmount;
                ViewBag.CurrentMonthSpending = currentMonthSpending;
                ViewBag.RemainingBudget =
                    currentUser.Role.RoleThreshold.MaxAmount - currentMonthSpending;
            }

            ViewData["Categories"] = new SelectList(
                await _context.Categories.ToListAsync(),
                "CategoryId", "CategoryName", categoryId);

            var itemsQuery = _context.StationeryItems.Include(i => i.Category).AsQueryable();
            if (categoryId.HasValue)
                itemsQuery = itemsQuery.Where(i => i.CategoryId == categoryId);

            var items = await itemsQuery.ToListAsync();
            var vm = new List<StationeryItemViewModel>();

            foreach (var item in items)
            {
                var reserved = await _context.RequestItems
                    .Include(r => r.Request)
                    .Where(r => r.ItemId == item.ItemId &&
                                r.Request != null &&
                                (r.Request.Status == "Pending" || r.Request.Status == "Approved"))
                    .SumAsync(r => r.Quantity);

                vm.Add(new StationeryItemViewModel
                {
                    Item = item,
                    ReservedStock = reserved,
                    AvailableStock = Math.Max(item.CurrentStock - reserved, 0)
                });
            }

            return View(vm);
        }

        // ========================= DETAILS =========================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var item = await _context.StationeryItems
                .Include(i => i.Category)
                .FirstOrDefaultAsync(i => i.ItemId == id);

            if (item == null) return NotFound();

            ViewBag.CurrentUserRole = GetCurrentUserRole();
            return View(item);
        }

        // ========================= CREATE =========================
        public IActionResult Create()
        {
            if (HttpContext.Session.GetInt32("RoleId") != 1)
                return RedirectToAction("AccessDenied", "Account");

            PopulateCategories();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StationeryItem item, IFormFile? ImageFile)
        {
            if (HttpContext.Session.GetInt32("RoleId") != 1)
                return RedirectToAction("AccessDenied", "Account");

            if (!ModelState.IsValid)
            {
                PopulateCategories(item.CategoryId);
                return View(item);
            }

            await SaveImage(item, ImageFile);

            _context.Add(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ========================= EDIT =========================
        public async Task<IActionResult> Edit(int? id)
        {
            if (HttpContext.Session.GetInt32("RoleId") != 1)
                return RedirectToAction("AccessDenied", "Account");

            if (id == null) return NotFound();

            var item = await _context.StationeryItems.FindAsync(id);
            if (item == null) return NotFound();

            PopulateCategories(item.CategoryId);
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, StationeryItem item, IFormFile? ImageFile)
        {
            if (HttpContext.Session.GetInt32("RoleId") != 1)
                return RedirectToAction("AccessDenied", "Account");

            if (id != item.ItemId) return NotFound();

            if (!ModelState.IsValid)
            {
                PopulateCategories(item.CategoryId);
                return View(item);
            }

            await SaveImage(item, ImageFile);

            _context.Update(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ========================= DELETE =========================
        public async Task<IActionResult> Delete(int? id)
        {
            if (HttpContext.Session.GetInt32("RoleId") != 1)
                return RedirectToAction("AccessDenied", "Account");

            if (id == null) return NotFound();

            var item = await _context.StationeryItems.FindAsync(id);
            if (item == null) return NotFound();

            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (HttpContext.Session.GetInt32("RoleId") != 1)
                return RedirectToAction("AccessDenied", "Account");

            var item = await _context.StationeryItems.FindAsync(id);
            if (item != null)
            {
                _context.StationeryItems.Remove(item);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // ========================= HELPERS =========================
        private async Task SaveImage(StationeryItem item, IFormFile? image)
        {
            if (image == null || image.Length == 0) return;

            var fileName = Path.GetFileName(image.FileName);
            var path = Path.Combine(_imageFolder, fileName);

            using var stream = new FileStream(path, FileMode.Create);
            await image.CopyToAsync(stream);

            item.ImagePath = fileName;
        }

        private void PopulateCategories(int? selected = null)
        {
            ViewData["CategoryId"] =
                new SelectList(_context.Categories, "CategoryId", "CategoryName", selected);
        }

        private int GetCurrentUserId() =>
            HttpContext.Session.GetInt32("EmployeeId") ?? 0;

        private string GetCurrentUserRole() =>
            HttpContext.Session.GetInt32("RoleId") switch
            {
                1 => "Admin",
                2 => "Manager",
                3 => "Employee",
                _ => "Guest"
            };
    }
}
