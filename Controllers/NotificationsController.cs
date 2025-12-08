using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StationaryManagement1.Data;
using StationaryManagement1.Models;

namespace StationaryManagement1.Controllers;

public class NotificationsController(AppDBContext context) : Controller
{
    private readonly AppDBContext _context = context;

    public async Task<IActionResult> Index()
    {
        var currentUserId = HttpContext.Session.GetInt32("EmployeeId");
        if (currentUserId == null) return RedirectToAction("Login", "Account");

        var notifications = await _context.Notifications
            .Include(n => n.Employee)
            .Include(n => n.RelatedRequest).ThenInclude(r => r!.Superior)
            .Where(n => n.EmployeeId == currentUserId.Value)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        foreach (var n in notifications.Where(n => !n.IsRead))
        {
            n.IsRead = true;
        }
        await _context.SaveChangesAsync();

        return View(notifications);
    }

    [HttpGet]
    public async Task<IActionResult> Go(int id)
    {
        var currentUserId = HttpContext.Session.GetInt32("EmployeeId");
        if (currentUserId == null) return RedirectToAction("Login", "Account");

        var notification = await _context.Notifications
            .Include(n => n.RelatedRequest)
            .FirstOrDefaultAsync(n => n.NotificationId == id && n.EmployeeId == currentUserId.Value);

        if (notification == null) return RedirectToAction(nameof(Index));

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }

        if (notification.RelatedRequestId.HasValue)
        {
            return RedirectToAction("Details", "StationeryRequests", new { id = notification.RelatedRequestId.Value });
        }

        return RedirectToAction(nameof(Index));
    }
}
