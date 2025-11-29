using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Serilog.Sinks.ElmahIo.AspNetCore.Net10.Pages
{
    public class PrivacyModel(ILogger<PrivacyModel> logger) : PageModel
    {
        public void OnGet()
        {
            try
            {
                var i = 0;
                var result = 42 / i;
                Console.WriteLine(result);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during Privacy");
            }
        }
    }

}
