using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StationaryManagement1.Data;
using StationaryManagement1.Models.ViewModels;

public class ReportsController : Controller
{
    private readonly AppDBContext _context;

    public ReportsController(AppDBContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var roleId = HttpContext.Session.GetInt32("RoleId");
        if (roleId != 1 && roleId != 2) // Admin or Manager
            return RedirectToAction("AccessDenied", "Account");

        // Total cost across all items (use current item unit cost if available, else stored unit cost)
        var totalSpentAll = await _context.RequestItems
            .Include(ri => ri.Item)
            .SumAsync(ri => ri.Quantity * (ri.Item != null ? ri.Item.UnitCost : ri.UnitCost));
        totalSpentAll = totalSpentAll == 0 ? 1 : totalSpentAll; // avoid divide-by-zero

        var itemSummaries = await _context.RequestItems
            .Include(ri => ri.Item)
            .Include(ri => ri.Request)
            .GroupBy(ri => new { ri.ItemId, ri.Item!.ItemName })
            .Select(g => new ItemCostReportViewModel
            {
                ItemId = g.Key.ItemId,
                ItemName = g.Key.ItemName,
                UnitCost = g.Select(x => x.Item != null ? x.Item.UnitCost : x.UnitCost).FirstOrDefault(),
                TotalRequested = g.Sum(x => x.Quantity),
                HeadCount = g.Select(x => x.Request!.EmployeeId).Distinct().Count(),
                TotalSpent = g.Sum(x => x.Quantity * (x.Item != null ? x.Item.UnitCost : x.UnitCost)),
                PercentOfTotal = 0,   // set below
                CumulativeCost = 0    // set below
            })
            .OrderByDescending(x => x.TotalSpent)
            .ToListAsync();

        decimal cumulative = 0;
        foreach (var item in itemSummaries)
        {
            item.PercentOfTotal = Math.Round((item.TotalSpent / totalSpentAll) * 100, 2);
            cumulative += item.TotalSpent;
            item.CumulativeCost = cumulative;
        }

        ViewBag.TotalSpentAll = totalSpentAll;
        return View(itemSummaries);
    }
}
