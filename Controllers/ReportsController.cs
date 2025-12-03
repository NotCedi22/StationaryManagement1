using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StationaryManagement.Data;

public class ReportsController : Controller
{
    private readonly AppDBContext _context;

    public ReportsController(AppDBContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var report = await _context.FrequentItems.ToListAsync();
        return View(report);
    }
}
