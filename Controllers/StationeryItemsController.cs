using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StationaryManagement1.Data;
using StationaryManagement1.Models;
using StationaryManagement1.Models.ViewModels;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace StationaryManagement1.Controllers
{
    public class StationeryItemsController : Controller
    {
        private readonly AppDBContext _context;
        private readonly string _imageFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");

        public StationeryItemsController(AppDBContext context)
        {
            _context = context;

            // Ensure image folder exists so uploads don't fail at runtime
            if (!Directory.Exists(_imageFolder))
            {
                Directory.CreateDirectory(_imageFolder);
            }
        }

        // GET: StationeryItems - UPDATED WITH AVAILABILITY a
        public async Task<IActionResult> Index(int? categoryId)
        {
            // Get current user and role
            var currentUserId = GetCurrentUserId();
            var currentUser = await _context.Employees
                .Include(e => e.Role)
                .ThenInclude(r => r!.RoleThreshold) // Role can be null; suppress nullable warning
                .FirstOrDefaultAsync(e => e.EmployeeId == currentUserId);

            var currentUserRole = GetCurrentUserRole();

            ViewBag.CurrentUserRole = currentUserRole;

            // Pass eligibility info to view
            if (currentUser?.Role?.RoleThreshold != null)
            {
                ViewBag.MaxPurchaseAmount = currentUser.Role.RoleThreshold!.MaxAmount;

                // Calculate current month usage
                var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var currentMonthSpending = await _context.StationeryRequests
                    .Where(r => r.EmployeeId == currentUserId
                        && r.RequestDate >= startOfMonth
                        && (r.Status == "Approved" || r.Status == "Pending"))
                    .SumAsync(r => r.TotalCost);

                ViewBag.CurrentMonthSpending = currentMonthSpending;
                ViewBag.RemainingBudget = currentUser.Role.RoleThreshold.MaxAmount - currentMonthSpending;
            }

            // Fetch all categories for dropdown
            var categories = await _context.Categories.ToListAsync();
            ViewData["Categories"] = new SelectList(categories, "CategoryId", "CategoryName", categoryId);

            // Query items with availability calculation
            var itemsQuery = _context.StationeryItems.Include(i => i.Category).AsQueryable();

            if (categoryId.HasValue)
                itemsQuery = itemsQuery.Where(i => i.CategoryId == categoryId.Value);

            var items = await itemsQuery.ToListAsync();

            // Create ViewModels with availability info
            var itemViewModels = new List<StationeryItemViewModel>();

            foreach (var item in items)
            {
                // Calculate reserved quantity (from pending and approved requests)
                var reservedQty = await _context.RequestItems
                    .Include(ri => ri.Request)
                    .Where(ri => ri.ItemId == item.ItemId
                        && ri.Request != null
                        && (ri.Request.Status == "Pending" || ri.Request.Status == "Approved"))
                    .SumAsync(ri => ri.Quantity);

                var availableStock = item.CurrentStock - reservedQty;

                itemViewModels.Add(new StationeryItemViewModel
                {
                    Item = item,
                    AvailableStock = availableStock > 0 ? availableStock : 0,
                    ReservedStock = reservedQty
                });
            }

            return View(itemViewModels);
        }

        // GET: StationeryItems/Details/5 - UPDATED WITH AVAILABILITY
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var stationeryItem = await _context.StationeryItems
                .Include(s => s.Category)
                .FirstOrDefaultAsync(m => m.ItemId == id);

            if (stationeryItem == null) return NotFound();

            // Calculate availability
            var reservedQty = await _context.RequestItems
                .Include(ri => ri.Request)
                .Where(ri => ri.ItemId == id
                    && ri.Request != null
                    && (ri.Request.Status == "Pending" || ri.Request.Status == "Approved"))
                .SumAsync(ri => ri.Quantity);

            var availableStock = stationeryItem.CurrentStock - reservedQty;

            // Pass current user's role to the view
            ViewBag.CurrentUserRole = GetCurrentUserRole();
            ViewBag.AvailableStock = availableStock > 0 ? availableStock : 0;
            ViewBag.ReservedStock = reservedQty;

            return View(stationeryItem);
        }

        // GET: StationeryItems/Eligibility - NEW ACTION FOR FEATURE 2
        public async Task<IActionResult> Eligibility()
        {
            var currentUserId = GetCurrentUserId();
            var currentUser = await _context.Employees
                .Include(e => e.Role)
                .ThenInclude(r => r!.RoleThreshold) // Role can be null; suppress nullable warning
                .FirstOrDefaultAsync(e => e.EmployeeId == currentUserId);

            if (currentUser?.Role?.RoleThreshold == null)
            {
                ViewBag.ErrorMessage = "No purchase limit configured for your role.";
                return View();
            }

            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            // Get current month's approved and pending requests
            var currentMonthRequests = await _context.StationeryRequests
                .Where(r => r.EmployeeId == currentUserId
                    && r.RequestDate >= startOfMonth
                    && (r.Status == "Approved" || r.Status == "Pending"))
                .ToListAsync();

            var totalSpent = currentMonthRequests.Where(r => r.Status == "Approved").Sum(r => r.TotalCost);
            var pendingAmount = currentMonthRequests.Where(r => r.Status == "Pending").Sum(r => r.TotalCost);

            var role = currentUser.Role;
            var threshold = role?.RoleThreshold;

            ViewBag.RoleName = role?.RoleName;
            ViewBag.MaxAmount = threshold?.MaxAmount ?? 0;
            ViewBag.TotalSpent = totalSpent;
            ViewBag.PendingAmount = pendingAmount;
            ViewBag.AvailableBudget = (threshold?.MaxAmount ?? 0) - totalSpent - pendingAmount;
            ViewBag.CurrentMonth = startOfMonth.ToString("MMMM yyyy");

            return View();
        }

        // GET: StationeryItems/Create
        public IActionResult Create()
        {
            PopulateCategories();
            return View();
        }

        // POST: StationeryItems/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StationeryItem stationeryItem, IFormFile? ImageFile)
        {
            if (ModelState.IsValid)
            {
                // Handle image upload
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var fileName = Path.GetFileName(ImageFile.FileName);
                    var filePath = Path.Combine(_imageFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                        await ImageFile.CopyToAsync(stream);

                    stationeryItem.ImagePath = fileName;
                }

                _context.Add(stationeryItem);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            PopulateCategories(stationeryItem.CategoryId);
            return View(stationeryItem);
        }

        // GET: StationeryItems/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var stationeryItem = await _context.StationeryItems.FindAsync(id);
            if (stationeryItem == null) return NotFound();

            PopulateCategories(stationeryItem.CategoryId);
            return View(stationeryItem);
        }

        // POST: StationeryItems/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, StationeryItem stationeryItem, IFormFile? ImageFile)
        {
            if (id != stationeryItem.ItemId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Handle new image upload
                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        var fileName = Path.GetFileName(ImageFile.FileName);
                        var filePath = Path.Combine(_imageFolder, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                            await ImageFile.CopyToAsync(stream);

                        stationeryItem.ImagePath = fileName;
                    }

                    _context.Update(stationeryItem);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.StationeryItems.Any(e => e.ItemId == stationeryItem.ItemId))
                        return NotFound();
                    else
                        throw;
                }

                return RedirectToAction(nameof(Index));
            }

            PopulateCategories(stationeryItem.CategoryId);
            return View(stationeryItem);
        }

        // GET: StationeryItems/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.StationeryItems.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        // POST: StationeryItems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.StationeryItems.FindAsync(id);
            if (item != null)
            {
                _context.StationeryItems.Remove(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool StationeryItemExists(int id) =>
            _context.StationeryItems.Any(e => e.ItemId == id);

        private void PopulateCategories(int? selectedCategory = null)
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", selectedCategory);
        }

        private int GetCurrentUserId()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var claim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId");
                if (claim != null && int.TryParse(claim.Value, out int id))
                    return id;
            }
            return 0;
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
    }
}