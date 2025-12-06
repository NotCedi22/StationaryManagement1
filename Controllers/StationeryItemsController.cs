using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StationaryManagement1.Data;
using StationaryManagement1.Models;

namespace StationaryManagement1.Controllers
{
    public class StationeryItemsController : Controller
    {
        private readonly AppDBContext _context;
        private readonly string _imageFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");

        public StationeryItemsController(AppDBContext context)
        {
            _context = context;
        }

        // GET: StationeryItems
        public async Task<IActionResult> Index(int? categoryId)
        {
            // Get current user and role
            var currentUserId = GetCurrentUserId();
            var currentUser = await _context.Employees.FindAsync(currentUserId);
            var currentUserRole = GetCurrentUserRole(); // Use string role

            ViewBag.CurrentUserRole = currentUserRole; // Pass to view

            // Fetch all categories for dropdown
            var categories = await _context.Categories.ToListAsync();
            ViewData["Categories"] = new SelectList(categories, "CategoryId", "CategoryName", categoryId);

            // Query items
            var itemsQuery = _context.StationeryItems.Include(i => i.Category).AsQueryable();

            if (categoryId.HasValue)
                itemsQuery = itemsQuery.Where(i => i.CategoryId == categoryId.Value);

            var items = await itemsQuery.ToListAsync();
            return View(items);
        }

        // GET: StationeryItems/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var stationeryItem = await _context.StationeryItems
                .Include(s => s.Category)
                .FirstOrDefaultAsync(m => m.ItemId == id);

            if (stationeryItem == null) return NotFound();

            // Pass current user's role to the view
            ViewBag.CurrentUserRole = GetCurrentUserRole();

            return View(stationeryItem);
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
