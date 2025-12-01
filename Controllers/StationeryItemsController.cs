using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        public async Task<IActionResult> Index()
        {
            var appDBContext = _context.StationeryItems.Include(s => s.Category);
            return View(await appDBContext.ToListAsync());
        }

        // GET: StationeryItems/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stationeryItem = await _context.StationeryItems
                .Include(s => s.Category)
                .FirstOrDefaultAsync(m => m.ItemId == id);
            if (stationeryItem == null)
            {
                return NotFound();
            }

            return View(stationeryItem);
        }

        // GET: StationeryItems/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryId");
            return View();
        }

        // POST: StationeryItems/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ItemId,ItemName,Description,CategoryId,UnitCost,CurrentStock,ImagePath,CreatedAt,ModifiedAt")] StationeryItem stationeryItem)
        {
            if (ModelState.IsValid)
            {
                _context.Add(stationeryItem);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryId", stationeryItem.CategoryId);
            return View(stationeryItem);
        }

        // GET: StationeryItems/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stationeryItem = await _context.StationeryItems.FindAsync(id);
            if (stationeryItem == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryId", stationeryItem.CategoryId);
            return View(stationeryItem);
        }

        // POST: StationeryItems/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ItemId,ItemName,Description,CategoryId,UnitCost,CurrentStock,ImagePath,CreatedAt,ModifiedAt")] StationeryItem stationeryItem)
        {
            if (id != stationeryItem.ItemId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(stationeryItem);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StationeryItemExists(stationeryItem.ItemId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryId", stationeryItem.CategoryId);
            return View(stationeryItem);
        }

        // GET: StationeryItems/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stationeryItem = await _context.StationeryItems
                .Include(s => s.Category)
                .FirstOrDefaultAsync(m => m.ItemId == id);
            if (stationeryItem == null)
            {
                return NotFound();
            }

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
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool StationeryItemExists(int id)
        {
            return _context.StationeryItems.Any(e => e.ItemId == id);
        }
    }
}
