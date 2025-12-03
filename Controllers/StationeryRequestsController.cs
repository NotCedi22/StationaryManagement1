using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StationaryManagement.Data;
using StationaryManagement.Models;
using StationaryManagement.Models.ViewModels;


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
            var currentUserId = GetCurrentUserId();
            var currentUserRole = GetCurrentUserRole();

            var query = _context.StationeryRequests
                .Include(r => r.Employee)
                .Include(r => r.Superior)
                .Include(r => r.RequestItems)
                    .ThenInclude(ri => ri.Item)
                .AsQueryable();

            if (currentUserRole == "Employee")
                query = query.Where(r => r.EmployeeId == currentUserId);

            return View(await query.ToListAsync());
        }

        // GET: StationeryRequests/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var request = await _context.StationeryRequests
                .Include(r => r.Employee)
                .Include(r => r.Superior)
                .Include(r => r.RequestItems)
                    .ThenInclude(ri => ri.Item)
                .FirstOrDefaultAsync(r => r.RequestId == id);

            if (request == null) return NotFound();

            // Authorization
            var currentUserId = GetCurrentUserId();
            var currentUserRole = GetCurrentUserRole();
            if (currentUserRole == "Employee" && request.EmployeeId != currentUserId)
                return Forbid();

            return View(request);
        }

        // GET: StationeryRequests/Create
        public IActionResult Create()
        {
            PopulateEmployeesAndSuperiorsDropDown();

            // Pass StationeryItems with stock and cost
            var items = _context.StationeryItems
                .Select(i => new StationeryItemViewModel
                {
                    ItemId = i.ItemId,
                    ItemName = i.ItemName,
                    CurrentStock = i.CurrentStock,
                    UnitCost = i.UnitCost
                }).ToList();

            ViewData["StationeryItemsDetails"] = items;

            return View();
        }

        // POST: StationeryRequests/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StationeryRequest request, Dictionary<int, int>? Quantities)
        {
            var currentUserId = GetCurrentUserId();
            request.EmployeeId = currentUserId;
            request.Status = request.Status = "Pending";


            if (ModelState.IsValid)
            {
                // Calculate total cost based on quantities
                decimal totalCost = 0;
                var requestItems = new List<RequestItem>();

                if (Quantities != null)
                {
                    foreach (var q in Quantities)
                    {
                        var item = await _context.StationeryItems.FindAsync(q.Key);
                        if (item == null) continue;

                        if (q.Value > item.CurrentStock)
                        {
                            ModelState.AddModelError("", $"Quantity for {item.ItemName} exceeds stock.");
                            PopulateEmployeesAndSuperiorsDropDown(request.EmployeeId, request.SuperiorId);
                            var itemsDetails = _context.StationeryItems
                                .Select(i => new StationeryItemViewModel
                                {
                                    ItemId = i.ItemId,
                                    ItemName = i.ItemName,
                                    CurrentStock = i.CurrentStock,
                                    UnitCost = i.UnitCost
                                }).ToList();
                            ViewData["StationeryItemsDetails"] = itemsDetails;
                            return View(request);
                        }

                        requestItems.Add(new RequestItem
                        {
                            ItemId = q.Key,
                            Quantity = q.Value,
                            UnitCost = item.UnitCost
                        });

                        totalCost += q.Value * item.UnitCost;
                    }
                }

                request.TotalCost = totalCost;

                _context.StationeryRequests.Add(request);
                await _context.SaveChangesAsync();

                // Save request items
                foreach (var ri in requestItems)
                {
                    ri.RequestId = request.RequestId;
                    _context.RequestItems.Add(ri);
                }

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            PopulateEmployeesAndSuperiorsDropDown(request.EmployeeId, request.SuperiorId);

            var itemsDetailsFallback = _context.StationeryItems
                .Select(i => new StationeryItemViewModel
                {
                    ItemId = i.ItemId,
                    ItemName = i.ItemName,
                    CurrentStock = i.CurrentStock,
                    UnitCost = i.UnitCost
                }).ToList();
            ViewData["StationeryItemsDetails"] = itemsDetailsFallback;

            return View(request);
        }
        // GET: StationeryRequests/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var request = await _context.StationeryRequests
                .Include(r => r.RequestItems)
                .ThenInclude(ri => ri.Item)
                .FirstOrDefaultAsync(r => r.RequestId == id);

            if (request == null) return NotFound();

            // Populate dropdowns
            PopulateEmployeesAndSuperiorsDropDown(request.EmployeeId, request.SuperiorId);

            // Bring request items into memory
            var requestItems = request.RequestItems.ToList();

            // Use AsEnumerable() to allow null-propagation
            var itemsDetails = _context.StationeryItems
                .AsEnumerable()
                .Select(i => new StationeryItemViewModel
                {
                    ItemId = i.ItemId,
                    ItemName = i.ItemName,
                    CurrentStock = i.CurrentStock,
                    UnitCost = i.UnitCost,
                    SelectedQuantity = requestItems.FirstOrDefault(ri => ri.ItemId == i.ItemId)?.Quantity ?? 0
                }).ToList();

            ViewData["StationeryItemsDetails"] = itemsDetails;

            return View(request);
        }

        // POST: StationeryRequests/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, StationeryRequest request, Dictionary<int, int>? Quantities)
        {
            if (id != request.RequestId)
                return BadRequest();

            if (ModelState.IsValid)
            {
                try
                {
                    decimal totalCost = 0;
                    var requestItems = new List<RequestItem>();

                    if (Quantities != null)
                    {
                        foreach (var q in Quantities)
                        {
                            var item = await _context.StationeryItems.FindAsync(q.Key);
                            if (item == null) continue;

                            if (q.Value > item.CurrentStock)
                            {
                                ModelState.AddModelError("", $"Quantity for {item.ItemName} exceeds stock.");
                                PopulateEmployeesAndSuperiorsDropDown(request.EmployeeId, request.SuperiorId);

                                // Rebuild itemsDetails with SelectedQuantity
                                var itemsDetails = _context.StationeryItems
                                    .AsEnumerable()
                                    .Select(i => new StationeryItemViewModel
                                    {
                                        ItemId = i.ItemId,
                                        ItemName = i.ItemName,
                                        CurrentStock = i.CurrentStock,
                                        UnitCost = i.UnitCost,
                                        SelectedQuantity = Quantities.ContainsKey(i.ItemId) ? Quantities[i.ItemId] : 0
                                    }).ToList();

                                ViewData["StationeryItemsDetails"] = itemsDetails;
                                return View(request);
                            }

                            requestItems.Add(new RequestItem
                            {
                                RequestId = request.RequestId,
                                ItemId = q.Key,
                                Quantity = q.Value,
                                UnitCost = item.UnitCost
                            });

                            totalCost += q.Value * item.UnitCost;
                        }
                    }

                    request.TotalCost = totalCost;

                    // Remove old RequestItems and add updated
                    var oldItems = _context.RequestItems.Where(ri => ri.RequestId == id);
                    _context.RequestItems.RemoveRange(oldItems);
                    await _context.SaveChangesAsync();

                    _context.RequestItems.AddRange(requestItems);

                    // Update request
                    _context.Update(request);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.StationeryRequests.Any(e => e.RequestId == id))
                        return NotFound();
                    else
                        throw;
                }
            }

            PopulateEmployeesAndSuperiorsDropDown(request.EmployeeId, request.SuperiorId);

            // Rebuild itemsDetails for validation errors
            var fallbackItems = _context.StationeryItems
                .AsEnumerable()
                .Select(i => new StationeryItemViewModel
                {
                    ItemId = i.ItemId,
                    ItemName = i.ItemName,
                    CurrentStock = i.CurrentStock,
                    UnitCost = i.UnitCost,
                    SelectedQuantity = Quantities?.ContainsKey(i.ItemId) == true ? Quantities[i.ItemId] : 0
                }).ToList();

            ViewData["StationeryItemsDetails"] = fallbackItems;

            return View(request);
        }

        // POST: StationeryRequests/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var request = await _context.StationeryRequests
                .Include(r => r.RequestItems)
                .FirstOrDefaultAsync(r => r.RequestId == id);

            if (request == null) return NotFound();

            // Optional: only allow deletion if Pending
            if (request.Status != "Pending")
                return BadRequest("Only pending requests can be deleted.");

            // Remove associated request items first
            _context.RequestItems.RemoveRange(request.RequestItems);

            // Remove the request
            _context.StationeryRequests.Remove(request);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        // POST: StationeryRequests/Approve/5
        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var request = await _context.StationeryRequests
                .Include(r => r.RequestItems)
                .ThenInclude(ri => ri.Item)
                .FirstOrDefaultAsync(r => r.RequestId == id);

            if (request == null) return NotFound();

            var role = GetCurrentUserRole();
            if (role != "Manager" && role != "Admin") return Forbid();

            request.Status = request.Status = "Approved";

            request.LastStatusChangedAt = DateTime.UtcNow;

            // Reduce stock
            foreach (var ri in request.RequestItems)
            {
                if (ri.Item != null)
                {
                    ri.Item.CurrentStock -= ri.Quantity;
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: StationeryRequests/Reject/5
        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            var request = await _context.StationeryRequests.FindAsync(id);
            if (request == null) return NotFound();

            var role = GetCurrentUserRole();
            if (role != "Manager" && role != "Admin") return Forbid();

            request.Status = request.Status = "Rejected";

            request.LastStatusChangedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: StationeryRequests/Withdraw/5
        [HttpPost]
        public async Task<IActionResult> Withdraw(int id)
        {
            var request = await _context.StationeryRequests.FindAsync(id);
            if (request == null) return NotFound();

            var currentUserId = GetCurrentUserId();
            if (request.EmployeeId != currentUserId) return Forbid();

            if (request.Status != "Pending")
                return BadRequest("Cannot withdraw request that is already processed.");

            request.Status = "Withdrawn";
            request.LastStatusChangedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Notifications page (Admin/Manager only)
        public async Task<IActionResult> Notifications()
        {
            var role = GetCurrentUserRole();
            if (role != "Manager" && role != "Admin") return Forbid();

            var requests = await _context.StationeryRequests
                .Include(r => r.Employee)
                .Include(r => r.Superior)
                .Where(r => r.Status == "Pending")
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            return View(requests);
        }

        // Combined Employees + Superiors dropdown
        private void PopulateEmployeesAndSuperiorsDropDown(int? selectedEmployee = null, int? selectedSuperior = null)
        {
            var employees = _context.Employees
                .Include(e => e.Role)
                .Where(e => e.Role != null && !e.Role.CanApprove)
                .Select(e => new { e.EmployeeId, DisplayName = e.Name })
                .ToList();

            ViewData["EmployeeId"] = new SelectList(employees, "EmployeeId", "DisplayName", selectedEmployee);

            var superiors = _context.Employees
                .Include(e => e.Role)
                .Where(e => e.Role != null && e.Role.CanApprove)
                .Select(e => new { e.EmployeeId, DisplayName = e.Name })
                .ToList();

            ViewData["SuperiorId"] = new SelectList(superiors, "EmployeeId", "DisplayName", selectedSuperior);
        }

        // Auth helpers (replace with real auth)
        private int GetCurrentUserId() => 1;
        private string GetCurrentUserRole() => "Employee";
    }
}
