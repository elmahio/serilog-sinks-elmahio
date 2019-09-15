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

using System;
using System.Collections.Generic;
using Serilog.Context;
using Serilog.Events;

namespace Serilog.Sinks.ElmahIo.Example
{
    public class Program
    {
        static void Main(string[] args)
        {
            Log.Logger =
                new LoggerConfiguration()
                    .Enrich.WithProperty("Version", "1.2.3")
                    .Enrich.FromLogContext()
                    .WriteTo.ElmahIo(new ElmahIoSinkOptions("API_KEY", new Guid("LOG_ID"))
                    {
                        BatchPostingLimit = 50,
                        MinimumLogEventLevel = LogEventLevel.Information,
                        Period = TimeSpan.FromSeconds(2),
                        OnMessage = msg =>
                        {
                            msg.Data.Add(new Elmah.Io.Client.Models.Item("Hello", "World"));
                        },
                        OnFilter = msg =>
                        {
                            return msg.StatusCode == 404;
                        },
                        OnError = (msg, ex) =>
                        {
                            Console.Error.WriteLine(ex.Message);
                        }
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
            Log.CloseAndFlush();
        }
    }
}
