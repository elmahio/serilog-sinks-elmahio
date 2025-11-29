using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Serilog.Sinks.ElmahIo.AspNetCore.Net10.Pages
{
    public class IndexModel(ILogger<IndexModel> logger) : PageModel
    {
        public void OnGet()
        {
            logger.LogWarning("Request to the frontpage");
        }
    }
}
