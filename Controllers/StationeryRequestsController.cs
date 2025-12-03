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
    public class StationeryRequestsController : Controller
    {
        private readonly AppDBContext _context;

        public StationeryRequestsController(AppDBContext context)
        {
            _context = context;
        }

        // GET: StationeryRequests
        public async Task<IActionResult> Index()
        {
            var appDBContext = _context.StationeryRequests
                .Include(s => s.Employee)
                .Include(s => s.Superior)
                .Include(s => s.RequestItems)
                    .ThenInclude(ri => ri.Item); // Load related StationeryItems
            return View(await appDBContext.ToListAsync());
        }

        // GET: StationeryRequests/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var request = await _context.StationeryRequests
                .Include(s => s.Employee)
                .Include(s => s.Superior)
                .Include(s => s.RequestItems)
                    .ThenInclude(ri => ri.Item)
                .FirstOrDefaultAsync(m => m.RequestId == id);

            if (request == null) return NotFound();

            return View(request);
        }

        // GET: StationeryRequests/Create
        public IActionResult Create()
        {
            PopulateEmployeesDropDown();
            PopulateStationeryItemsDropDown();
            return View();
        }

        // POST: StationeryRequests/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StationeryRequest request, int[]? RequestItems)
        {
            if (ModelState.IsValid)
            {
                _context.StationeryRequests.Add(request);
                await _context.SaveChangesAsync();

                // Save RequestItems
                if (RequestItems != null)
                {
                    foreach (var itemId in RequestItems)
                    {
                        var item = await _context.StationeryItems.FindAsync(itemId);
                        _context.RequestItems.Add(new RequestItem
                        {
                            RequestId = request.RequestId,
                            ItemId = itemId,
                            Quantity = 1,
                            UnitCost = item?.UnitCost ?? 0
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }

            // Reload dropdowns if validation fails
            PopulateEmployeesDropDown(request.EmployeeId, request.SuperiorId);
            PopulateStationeryItemsDropDown(RequestItems);
            return View(request);
        }

        // GET: StationeryRequests/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var request = await _context.StationeryRequests
                .Include(r => r.RequestItems)
                .FirstOrDefaultAsync(r => r.RequestId == id);

            if (request == null) return NotFound();

            PopulateEmployeesDropDown(request.EmployeeId, request.SuperiorId);
            PopulateStationeryItemsDropDown(request.RequestItems.Select(ri => ri.ItemId).ToArray());

            return View(request);
        }

        // POST: StationeryRequests/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, StationeryRequest request, int[]? RequestItems)
        {
            if (id != request.RequestId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(request);
                    await _context.SaveChangesAsync();

                    // Update RequestItems
                    var existingItems = _context.RequestItems.Where(ri => ri.RequestId == id);
                    _context.RequestItems.RemoveRange(existingItems);

                    if (RequestItems != null)
                    {
                        foreach (var itemId in RequestItems)
                        {
                            var item = await _context.StationeryItems.FindAsync(itemId);
                            _context.RequestItems.Add(new RequestItem
                            {
                                RequestId = id,
                                ItemId = itemId,
                                Quantity = 1,
                                UnitCost = item?.UnitCost ?? 0
                            });
                        }
                    }
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StationeryRequestExists(request.RequestId)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            PopulateEmployeesDropDown(request.EmployeeId, request.SuperiorId);
            PopulateStationeryItemsDropDown(RequestItems);
            return View(request);
        }

        // GET: StationeryRequests/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var request = await _context.StationeryRequests
                .Include(s => s.Employee)
                .Include(s => s.Superior)
                .FirstOrDefaultAsync(m => m.RequestId == id);

            if (request == null) return NotFound();

            return View(request);
        }

        // POST: StationeryRequests/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var request = await _context.StationeryRequests.FindAsync(id);
            if (request != null)
            {
                var items = _context.RequestItems.Where(ri => ri.RequestId == id);
                _context.RequestItems.RemoveRange(items);
                _context.StationeryRequests.Remove(request);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool StationeryRequestExists(int id)
        {
            return _context.StationeryRequests.Any(e => e.RequestId == id);
        }

        private void PopulateEmployeesDropDown(int? selectedEmployee = null, int? selectedSuperior = null)
        {
            var employees = _context.Employees
                .Select(e => new { e.EmployeeId, DisplayName = $"{e.Name} ({e.EmployeeId})" })
                .ToList();

            ViewData["EmployeeId"] = new SelectList(employees, "EmployeeId", "DisplayName", selectedEmployee);
            ViewData["SuperiorId"] = new SelectList(employees, "EmployeeId", "DisplayName", selectedSuperior);
        }

        private void PopulateStationeryItemsDropDown(int[]? selectedItems = null)
        {
            var items = _context.StationeryItems
                .Include(i => i.Category)
                .Select(i => new
                {
                    i.ItemId,
                    Display = $"{i.ItemName} (Category: {(i.Category != null ? i.Category.CategoryName : "None")})"
                })
                .ToList();

            ViewData["StationeryItems"] = new MultiSelectList(items, "ItemId", "Display", selectedItems);
        }
    }
}
