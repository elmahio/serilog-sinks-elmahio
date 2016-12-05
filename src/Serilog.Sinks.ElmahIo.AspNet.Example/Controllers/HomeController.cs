using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Serilog.Sinks.ElmahIo.AspNet.Example.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            Log.Information("Calling Index on HomeController");
            return View();
        }

        public ActionResult About()
        {
            Log.Error(new ApplicationException(), "An error happened");
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