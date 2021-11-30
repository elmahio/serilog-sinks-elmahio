using System.Web;
using System.Web.Mvc;

namespace Serilog.Sinks.ElmahIo.AspNet
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
