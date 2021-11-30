using System.Web.Mvc;

namespace Serilog.Sinks.ElmahIo.AspNet.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            Log.Error("Request to the frontpage");
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}