#pragma warning disable S125 // Sections of code should not be commented out
using Serilog.Context;
using Serilog.Events;
using Serilog.Sinks.ElmahIo;
using Serilog;

Log.Logger =
    new LoggerConfiguration()
        .Enrich.WithProperty("Version", "1.2.3")
        .Enrich.FromLogContext()
        .WriteTo.ElmahIo(new ElmahIoSinkOptions("API_KEY", new Guid("LOG_ID"))
        {
            // As default, everything is logged. You can set a minimum log level using the following code:
            MinimumLogEventLevel = LogEventLevel.Information,

            // Decorate all messages with an application name
            //Application = ".NET 8.0 Console",

            // The elmah.io sink bulk upload log messages. To change the default behavior, change one or both of the following properties:
            //BatchPostingLimit = 50,
            //Period = TimeSpan.FromSeconds(2),

            // To decorate all log messages with a general variable or to get a callback every time a message is logged, implement the OnMessage action:
            OnMessage = msg =>
            {
                msg.Data.Add(new Elmah.Io.Client.Item("Network", "Skynet"));
            },

            // To create client side filtering of what not to log, implement the OnFilter action:
            //OnFilter = msg =>
            //{
            //    return msg.StatusCode == 404;
            //},

            // To get a callback if logging to elmah.io fail, implement the OnError action:
            //OnError = (msg, ex) =>
            //{
            //    Console.Error.WriteLine(ex.Message);
            //}
        })
        .CreateLogger();

Log.Information("First log message from Serilog");

using (LogContext.PushProperty("User", "Arnold Schwarzenegger"))
{
    Log.Error("Hasta la vista, {Name}", "Baby");
}

try
{
    var i = 0;
    var result = 42 / i;
}
catch (Exception e)
{
    Log.Error(e, "Some exception");
}

Log.Information("A message with {type} {hostname} {application} {user} {source} {method} {version} {url}, {statusCode}, {serverVariables}, {cookies}, {form} and {queryString}",
    "custom type",
    "custom hostname",
    "custom application",
    "custom user",
    "custom source",
    "custom method",
    "custom version",
    "custom url",
    500,
    new Dictionary<string, string> { { "REMOTE_ADDR", "1.1.1.1" } },
    new Dictionary<string, string> { { "_ga", "GA1.3.1162527071.1564749318" } },
    new Dictionary<string, string> { { "username", "Arnold" } },
    new Dictionary<string, string> { { "id", "42" } });

// Make sure to emit any batched messages not already sent to elmah.io.
await Log.CloseAndFlushAsync();
#pragma warning restore S125 // Sections of code should not be commented out
