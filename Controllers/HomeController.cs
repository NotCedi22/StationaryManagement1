using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StationaryManagement1.Models.ViewModels;

namespace StationaryManagement.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // Check if Remember Me cookies exist
            var rememberedId = Request.Cookies["RememberEmployeeId"];
            var rememberedName = Request.Cookies["RememberEmployeeName"];
            var rememberedRole = Request.Cookies["RememberRoleId"];

            if (!string.IsNullOrEmpty(rememberedId) &&
                !string.IsNullOrEmpty(rememberedName) &&
                !string.IsNullOrEmpty(rememberedRole))
            {
                ViewData["RememberMessage"] = $"Welcome back, {rememberedName}! (RoleId: {rememberedRole})";
            }
            else
            {
                ViewData["RememberMessage"] = "No Remember Me cookie found. Please log in.";
            }

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
