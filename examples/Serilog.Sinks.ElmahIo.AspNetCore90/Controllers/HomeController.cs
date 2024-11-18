using Microsoft.AspNetCore.Mvc;
using Serilog.Sinks.ElmahIo.AspNetCore90.Models;
using System.Diagnostics;

namespace Serilog.Sinks.ElmahIo.AspNetCore90.Controllers
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
            _logger.LogWarning("Request to the frontpage");

            return View();
        }

        public IActionResult Privacy()
        {
            try
            {
                var i = 0;
                var result = 42 / i;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error during Privacy");
            }

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
