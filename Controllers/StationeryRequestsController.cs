using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StationaryManagement1.Data;
using StationaryManagement1.Models;
using StationaryManagement1.Models.ViewModels;

namespace StationaryManagement1.Controllers
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

            ViewData["CurrentUserId"] = currentUserId;
            ViewData["CurrentUserRole"] = currentUserRole;

            var query = _context.StationeryRequests
                .Include(r => r.Employee)
                .Include(r => r.Superior)
                .Include(r => r.RequestItems)
                    .ThenInclude(ri => ri.Item)
                .AsQueryable();

            if (currentUserRole == "Employee")
                query = query.Where(r => r.EmployeeId == currentUserId);

            var requests = await query.OrderByDescending(r => r.RequestDate).ToListAsync();
            return View(requests);
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

            // Authorization: Employees can only view their own requests
            var currentUserId = GetCurrentUserId();
            var currentUserRole = GetCurrentUserRole();

            if (currentUserRole == "Employee" && request.EmployeeId != currentUserId)
                return Forbid();

            // Pass current user's role to the view for UI purposes (Edit button)
            ViewBag.CurrentUserRole = currentUserRole;

            return View(request);
        }
        // GET: StationeryRequests/Create
        public IActionResult Create()
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return RedirectToAction("Login", "Account");

            PopulateEmployeesAndSuperiorsDropDown(currentUserId);

            // Pass StationeryItems for selection
            var items = _context.StationeryItems
                .Select(i => new StationeryItemViewModel
                {
                    ItemId = i.ItemId,
                    ItemName = i.ItemName,
                    CurrentStock = i.CurrentStock,
                    UnitCost = i.UnitCost,
                    SelectedQuantity = i.CurrentStock > 0 ? 1 : 0
                }).ToList();

            ViewData["StationeryItemsDetails"] = items;
            return View();
        }

        // POST: StationeryRequests/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Dictionary<int, int> Quantities, int SuperiorId, DateTime? FromDate, DateTime? ToDate, string Reason)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return RedirectToAction("Login", "Account");

            if (Quantities == null || !Quantities.Any())
            {
                TempData["Error"] = "Select at least one item.";
                return RedirectToAction(nameof(Create));
            }

            decimal totalCost = 0;
            var requestItems = new List<RequestItem>();

            foreach (var q in Quantities)
            {
                var item = await _context.StationeryItems.FindAsync(q.Key);
                if (item == null) continue;
                if (q.Value <= 0) continue;

                if (q.Value > item.CurrentStock)
                {
                    TempData["Error"] = $"Quantity for {item.ItemName} exceeds stock.";
                    return RedirectToAction(nameof(Create));
                }

                requestItems.Add(new RequestItem
                {
                    ItemId = item.ItemId,
                    Quantity = q.Value,
                    UnitCost = item.UnitCost
                });

                totalCost += q.Value * item.UnitCost;
            }

            if (!requestItems.Any())
            {
                TempData["Error"] = "You must select at least one item.";
                return RedirectToAction(nameof(Create));
            }

            var request = new StationeryRequest
            {
                EmployeeId = currentUserId,
                SuperiorId = SuperiorId,
                FromDate = FromDate,
                ToDate = ToDate,
                Reason = Reason,
                RequestItems = requestItems,
                TotalCost = totalCost,
                Status = "Pending",
                RequestDate = DateTime.UtcNow
            };

            _context.StationeryRequests.Add(request);
            await _context.SaveChangesAsync();

            // Save RequestItems
            foreach (var ri in requestItems)
            {
                ri.RequestId = request.RequestId;
                _context.RequestItems.Add(ri);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Request created successfully!";
            return RedirectToAction(nameof(Index));
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

            var currentUserId = GetCurrentUserId();
            var currentUserRole = GetCurrentUserRole();

            if (currentUserRole == "Employee" && request.EmployeeId != currentUserId)
                return Forbid();

            PopulateEmployeesAndSuperiorsDropDown(request.EmployeeId, request.SuperiorId);

            var itemsDetails = _context.StationeryItems
                .AsEnumerable()
                .Select(i => new StationeryItemViewModel
                {
                    ItemId = i.ItemId,
                    ItemName = i.ItemName,
                    CurrentStock = i.CurrentStock,
                    UnitCost = i.UnitCost,
                    SelectedQuantity = request.RequestItems.FirstOrDefault(ri => ri.ItemId == i.ItemId)?.Quantity ?? 0
                }).ToList();

            ViewData["StationeryItemsDetails"] = itemsDetails;
            return View(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, StationeryRequest model, Dictionary<int, int>? Quantities)
        {
            var request = await _context.StationeryRequests
                .Include(r => r.RequestItems)
                .FirstOrDefaultAsync(r => r.RequestId == id);

            if (request == null) return NotFound();

            var currentUserId = GetCurrentUserId();
            var currentUserRole = GetCurrentUserRole();

            if (currentUserRole == "Employee" && request.EmployeeId != currentUserId)
                return Forbid();

            if (Quantities == null || !Quantities.Any())
            {
                TempData["Error"] = "Select at least one item.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            // Update request details from model
            request.EmployeeId = model.EmployeeId;
            request.SuperiorId = model.SuperiorId;
            request.FromDate = model.FromDate;
            request.ToDate = model.ToDate;
            request.Reason = model.Reason;

            decimal totalCost = 0;
            var newItems = new List<RequestItem>();

            foreach (var q in Quantities)
            {
                var item = await _context.StationeryItems.FindAsync(q.Key);
                if (item == null || q.Value <= 0) continue;
                if (q.Value > item.CurrentStock)
                {
                    TempData["Error"] = $"Quantity for {item.ItemName} exceeds stock.";
                    return RedirectToAction(nameof(Edit), new { id });
                }

                newItems.Add(new RequestItem
                {
                    RequestId = request.RequestId,
                    ItemId = item.ItemId,
                    Quantity = q.Value,
                    UnitCost = item.UnitCost
                });

                totalCost += q.Value * item.UnitCost;
            }

            request.TotalCost = totalCost;

            _context.RequestItems.RemoveRange(request.RequestItems);
            _context.RequestItems.AddRange(newItems);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
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
            if (request.Status != "Pending") return BadRequest("Only pending requests can be deleted.");

            _context.RequestItems.RemoveRange(request.RequestItems);
            _context.StationeryRequests.Remove(request);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: Approve
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

            request.Status = "Approved";
            request.LastStatusChangedAt = DateTime.UtcNow;

            foreach (var ri in request.RequestItems)
            {
                if (ri.Item != null) ri.Item.CurrentStock -= ri.Quantity;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: Reject
        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            var request = await _context.StationeryRequests.FindAsync(id);
            if (request == null) return NotFound();

            var role = GetCurrentUserRole();
            if (role != "Manager" && role != "Admin") return Forbid();

            request.Status = "Rejected";
            request.LastStatusChangedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: Withdraw
        [HttpPost]
        public async Task<IActionResult> Withdraw(int id)
        {
            var request = await _context.StationeryRequests.FindAsync(id);
            if (request == null) return NotFound();

            var currentUserId = GetCurrentUserId();
            if (request.EmployeeId != currentUserId) return Forbid();
            if (request.Status != "Pending") return BadRequest("Cannot withdraw processed request.");

            request.Status = "Withdrawn";
            request.LastStatusChangedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Helpers for dropdowns
        private void PopulateEmployeesAndSuperiorsDropDown(int? selectedEmployee = null, int? selectedSuperior = null)
        {
            var employees = _context.Employees
                .Where(e => e.RoleId == 3) // Only employees
                .Select(e => new { e.EmployeeId, e.Name })
                .ToList();
            ViewData["EmployeeId"] = new SelectList(employees, "EmployeeId", "Name", selectedEmployee);

            var superiors = _context.Employees
                .Where(e => e.RoleId == 1 || e.RoleId == 2) // Admin/Manager
                .Select(e => new { e.EmployeeId, e.Name })
                .ToList();
            ViewData["SuperiorId"] = new SelectList(superiors, "EmployeeId", "Name", selectedSuperior);
        }

        // Auth helpers
        private int GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("EmployeeId") ?? 0;
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
