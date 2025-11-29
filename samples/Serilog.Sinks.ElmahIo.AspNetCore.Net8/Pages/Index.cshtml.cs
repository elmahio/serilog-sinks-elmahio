using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Serilog.Sinks.ElmahIo.AspNetCore.Net8.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            _logger.LogWarning("Request to the frontpage");
        }
    }
}
