using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StationaryManagement.Data;
using StationaryManagement.Models;

namespace StationaryManagement.Controllers
{
    public class StationeryItemsController : Controller
    {
        private readonly AppDBContext _context;

        public StationeryItemsController(AppDBContext context)
        {
            _context = context;
        }

        // GET: StationeryItems
        public async Task<IActionResult> Index(int? categoryId)
        {
            // Get categories for filter dropdown
            ViewData["Categories"] = new SelectList(await _context.Categories.ToListAsync(), "CategoryId", "CategoryName");

            // Get stationery items with category
            var query = _context.StationeryItems.Include(s => s.Category).AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(s => s.CategoryId == categoryId.Value);
            }

            var items = await query.ToListAsync();
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
        public async Task<IActionResult> Create([Bind("ItemId,ItemName,Description,CategoryId,UnitCost,CurrentStock,ImagePath")] StationeryItem stationeryItem)
        {
            if (ModelState.IsValid)
            {
                _context.Add(stationeryItem);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            PopulateCategories(stationeryItem.CategoryId ?? 0);
            return View(stationeryItem);
        }

        // GET: StationeryItems/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var stationeryItem = await _context.StationeryItems.FindAsync(id);
            if (stationeryItem == null) return NotFound();

            PopulateCategories(stationeryItem.CategoryId ?? 0);
            return View(stationeryItem);
        }

        // POST: StationeryItems/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, StationeryItem stationeryItem, IFormFile? ImageFile)
        {
            if (id != stationeryItem.ItemId)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Handle uploaded image
                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        var fileName = Path.GetFileName(ImageFile.FileName);
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await ImageFile.CopyToAsync(stream);
                        }

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

            // Repopulate category dropdown
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", stationeryItem.CategoryId);
            return View(stationeryItem);
        }

        // GET: StationeryItems/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var stationeryItem = await _context.StationeryItems
                .Include(s => s.Category)
                .FirstOrDefaultAsync(m => m.ItemId == id);

            if (stationeryItem == null) return NotFound();

            return View(stationeryItem);
        }

        // POST: StationeryItems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var stationeryItem = await _context.StationeryItems.FindAsync(id);
            if (stationeryItem != null)
            {
                _context.StationeryItems.Remove(stationeryItem);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool StationeryItemExists(int id)
        {
            return _context.StationeryItems.Any(e => e.ItemId == id);
        }

        // -----------------------
        // Helper: Populate category dropdown
        private void PopulateCategories(int? selectedCategory = null)
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", selectedCategory);
        }
    }
}
