using System.Web.Mvc;

namespace Serilog.Sinks.ElmahIo.AspNet.Net48
{
    public static class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
