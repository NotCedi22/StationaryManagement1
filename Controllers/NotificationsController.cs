using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StationaryManagement.Data;

public class NotificationsController : Controller
{
    private readonly AppDBContext _context;

    public NotificationsController(AppDBContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var notifications = await _context.Notifications
            .Include(n => n.Employee)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
        return View(notifications);
    }
}
