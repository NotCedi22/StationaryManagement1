using Microsoft.AspNetCore.Mvc;

namespace StationaryManagement1.Controllers
{
    public class HelpController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}

