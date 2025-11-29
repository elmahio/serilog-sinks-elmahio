// Copyright 2014 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#pragma warning disable S125 // Sections of code should not be commented out
using System;
using System.Collections.Generic;
using Serilog.Context;
using Serilog.Events;

namespace Serilog.Sinks.ElmahIo.Console.Net481
{
    public static class Program
    {
        static void Main()
        {
            Log.Logger =
                new LoggerConfiguration()
                    .Enrich.WithProperty("Version", "1.2.3")
                    .Enrich.FromLogContext()
                    .WriteTo.ElmahIo(new ElmahIoSinkOptions("API_KEY", new Guid("LOG_ID"))
                    {
                        // As default, everything is logged. You can set a minimum log level using the following code:
                        MinimumLogEventLevel = LogEventLevel.Information,

                        // Decorate all messages with an application name
                        //Application = "MyApp",

                        // The elmah.io sink bulk upload log messages. To change the default behavior, change one or both of the following properties:
                        //BatchPostingLimit = 50,
                        //Period = TimeSpan.FromSeconds(2),

                        // To decorate all log messages with a general variable or to get a callback every time a message is logged, implement the OnMessage action:
                        //OnMessage = msg =>
                        //{
                        //    msg.Data.Add(new Elmah.Io.Client.Item("Hello", "World"));
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
                    })
                    .CreateLogger();

            Log.Information("First log message from Serilog");

            using (LogContext.PushProperty("User", "Arnold Schwarzenegger"))
            {
                Log.Error("This is a log message with a {TypeOfProperty} message", "structured");
            }

            try
            {
                var i = 0;
                var result = 42 / i;
                System.Console.WriteLine(result);
            }
            catch (Exception e)
            {
                Log.Error(e, "Some exception");
            }

            Log.Information("A message with {Type} {Hostname} {Application} {User} {Source} {Method} {Version} {Url}, {StatusCode}, {ServerVariables}, {Cookies}, {Form} and {QueryString}",
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
            Log.CloseAndFlush();
        }
    }
}
#pragma warning restore S125 // Sections of code should not be commented out
