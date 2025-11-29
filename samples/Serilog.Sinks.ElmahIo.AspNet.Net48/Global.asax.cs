using SerilogWeb.Classic;
using System;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Serilog.Sinks.ElmahIo.AspNet.Net48
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            // Request logging disabled for this sample. You can have SerilogWeb.Classic log information messages
            // on all HTTP request by removing the following lines:
            SerilogWebClassic.Configure(cfg => cfg
              .Disable()
            );

            Log.Logger = new LoggerConfiguration()
                // Write log messages to elmah.io:
                .WriteTo.ElmahIo(new ElmahIoSinkOptions("API_KEY", new Guid("LOG_ID")))
                // Use SerilogWeb.Classic to enrich log messages with HTTP contextual information:
                .Enrich.WithHttpRequestClientHostIP()
                .Enrich.WithHttpRequestRawUrl()
                .Enrich.WithHttpRequestType()
                .Enrich.WithHttpRequestUrl()
                .Enrich.WithHttpRequestUserAgent()
                .Enrich.WithUserName(anonymousUsername:null)
                .CreateLogger();

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}
