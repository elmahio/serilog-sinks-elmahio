using System;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Serilog.Sinks.ElmahIo.AspNet.Example
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            Log.Logger =
                new LoggerConfiguration()
                    .Enrich.WithProperty("Hello", "World")
                    .Enrich.FromLogContext()
                    .WriteTo.ElmahIoAspNet(new Guid("8bd4e199-1229-405b-9bc9-cf52397ff44f"))
                    .CreateLogger();

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}
