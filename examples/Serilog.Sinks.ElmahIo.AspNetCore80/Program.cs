using Serilog;
using Serilog.Events;
using Serilog.Sinks.ElmahIo;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.ElmahIo(new ElmahIoSinkOptions("API_KEY", new Guid("LOG_ID"))
    {
        // As default, everything is logged. You can set a minimum log level using the following code:
        MinimumLogEventLevel = LogEventLevel.Information,

        // Decorate all messages with an application name
        //Application = "ASP.NET Core 8.0",

        // The elmah.io sink bulk upload log messages. To change the default behavior, change one or both of the following properties:
        //BatchPostingLimit = 50,
        //Period = TimeSpan.FromSeconds(2),

        // To decorate all log messages with a general variable or to get a callback every time a message is logged, implement the OnMessage action:
        //OnMessage = msg =>
        //{
        //    msg.Data.Add(new Elmah.Io.Client.Item("Network", "Skynet"));
        //},

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
    }));

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
