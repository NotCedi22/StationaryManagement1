using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StationaryManagement1.Data;
using StationaryManagement1.Models.Filters;
using StationaryManagement1.Models.ViewModels;
using System.Diagnostics;

namespace StationaryManagement1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDBContext _context;

        public HomeController(ILogger<HomeController> logger, AppDBContext context)
        {
            _logger = logger;
            _context = context;
        }

        [RequireLogin]
        public async Task<IActionResult> Index()
        {
            // Fetch dashboard statistics
            var itemCount = await _context.StationeryItems.CountAsync();
            var employeeCount = await _context.Employees.CountAsync();
            var pendingRequests = await _context.StationeryRequests
                .CountAsync(r => r.Status == "Pending");

            ViewData["ItemCount"] = itemCount;
            ViewData["EmployeeCount"] = employeeCount;
            ViewData["PendingRequests"] = pendingRequests;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
