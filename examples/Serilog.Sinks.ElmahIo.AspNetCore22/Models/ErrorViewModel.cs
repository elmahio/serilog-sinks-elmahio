using System;

namespace Serilog.Sinks.ElmahIo.AspNetCore22.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}