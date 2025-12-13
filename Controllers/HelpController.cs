using Microsoft.AspNetCore.Mvc;
using StationaryManagement1.Models.Filters;

namespace StationaryManagement1.Controllers
{
    [RequireLogin]
    public class HelpController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}

