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
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Elmah.Io.Client;
using Elmah.Io.Client.Models;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.ElmahIo
{
    /// <summary>
    /// Writes log events to the elmah.io service.
    /// </summary>
    public class ElmahIoSink : PeriodicBatchingSink
    {
#if DOTNETCORE
        internal static string _assemblyVersion = typeof(ElmahIoSink).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
#else
        internal static string _assemblyVersion = typeof(ElmahIoSink).Assembly.GetName().Version.ToString();
#endif

        readonly ElmahIoSinkOptions _options;
        readonly IElmahioAPI _client;

        /// <summary>
        /// Construct a sink that saves logs to the specified storage account.
        /// </summary>
        public ElmahIoSink(ElmahIoSinkOptions options)
            : base(options.BatchPostingLimit, options.Period)
        {
            _options = options;
        }

        /// <summary>
        /// Construct a sink that saves logs to the specified logger. The purpose of this
        /// constructor is to re-use an existing client from ELMAH or similar.
        /// </summary>
        public ElmahIoSink(ElmahIoSinkOptions options, IElmahioAPI client)
            : this(options)
        {
            _client = client;
        }

        /// <inheritdoc />
        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            if (events == null || !events.Any())
                return;

            var client = _client;
            if (_client == null)
            {
                ElmahioAPI api = (ElmahioAPI)ElmahioAPI.Create(_options.ApiKey);
                api.HttpClient.Timeout = new TimeSpan(0, 0, 30);
                api.HttpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue("Serilog.Sinks.ElmahIo", _assemblyVersion)));
                api.Messages.OnMessage += (sender, args) =>
                {
                    _options.OnMessage?.Invoke(args.Message);
                };
                api.Messages.OnMessageFail += (sender, args) =>
                {
                    _options.OnError?.Invoke(args.Message, args.Error);
                };
                client = api;
            }

            var messages = new List<CreateMessage>();

            foreach (var logEvent in events)
            {
                var message = new CreateMessage
                {
                    Title = logEvent.RenderMessage(_options.FormatProvider),
                    TitleTemplate = logEvent.MessageTemplate?.Text,
                    Severity = LevelToSeverity(logEvent),
                    DateTime = logEvent.Timestamp.DateTime.ToUniversalTime(),
                    Detail = logEvent.Exception?.ToString(),
                    Data = PropertiesToData(logEvent),
                    Type = Type(logEvent),
                    Hostname = Hostname(logEvent),
                    Application = Application(logEvent),
                    User = User(logEvent),
                    Source = Source(logEvent),
                    Method = Method(logEvent),
                    Version = Version(logEvent),
                    Url = Url(logEvent),
                    StatusCode = StatusCode(logEvent),
                    ServerVariables = ServerVariables(logEvent),
                    Cookies = Cookies(logEvent),
                    Form = Form(logEvent),
                    QueryString = QueryString(logEvent),
                };

                if (_options.OnFilter != null && _options.OnFilter(message))
                {
                    continue;
                }

                messages.Add(message);
            }

            try
            {
                await client
                    .Messages
                    .CreateBulkAndNotifyAsync(_options.LogId, messages)
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Debugging.SelfLog.WriteLine("Caught exception while emitting to sink: {0}", e);
            }
        }

        private IList<Item> ServerVariables(LogEvent logEvent)
        {
            return Items(logEvent, "servervariables");
        }

        private IList<Item> Cookies(LogEvent logEvent)
        {
            return Items(logEvent, "cookies");
        }

        private IList<Item> Form(LogEvent logEvent)
        {
            return Items(logEvent, "form");
        }

        private IList<Item> QueryString(LogEvent logEvent)
        {
            return Items(logEvent, "querystring");
        }

        private int? StatusCode(LogEvent logEvent)
        {
            var statusCode = String(logEvent, "statuscode");
            if (string.IsNullOrWhiteSpace(statusCode)) return null;
            if (!int.TryParse(statusCode, out int code)) return null;
            return code;
        }

        private string Url(LogEvent logEvent)
        {
            return String(logEvent, "url");
        }

        private string Version(LogEvent logEvent)
        {
            return String(logEvent, "version");
        }

        private string Method(LogEvent logEvent)
        {
            return String(logEvent, "method");
        }

        private string Application(LogEvent logEvent)
        {
            return String(logEvent, "application");
        }

        private string Source(LogEvent logEvent)
        {
            var source = String(logEvent, "source");
            if (!string.IsNullOrWhiteSpace(source)) return source;
            return logEvent.Exception?.GetBaseException().Source;
        }

        private string Hostname(LogEvent logEvent)
        {
            var hostname = String(logEvent, "hostname");
            if (!string.IsNullOrWhiteSpace(hostname)) return hostname;
#if !DOTNETCORE
            return Environment.MachineName;
#else
            return Environment.GetEnvironmentVariable("COMPUTERNAME");
#endif
        }

        private string User(LogEvent logEvent)
        {
            var user = String(logEvent, "user");
            if (!string.IsNullOrWhiteSpace(user)) return user;
#if !DOTNETCORE
            return Thread.CurrentPrincipal?.Identity?.Name;
#else
            return ClaimsPrincipal.Current?.Identity?.Name;
#endif
        }

        private string Type(LogEvent logEvent)
        {
            var type = String(logEvent, "type");
            if (!string.IsNullOrWhiteSpace(type)) return type;
            return logEvent.Exception?.GetBaseException().GetType().FullName;
        }

        static List<Item> PropertiesToData(LogEvent logEvent)
        {
            var data = new List<Item>();
            if (logEvent.Exception != null)
            {
                data.AddRange(
                    logEvent.Exception.Data.Keys.Cast<object>()
                        .Select(key => new Item { Key = key.ToString(), Value = logEvent.Exception.Data[key].ToString() }));
            }

            data.AddRange(logEvent.Properties.SelectMany(p => Properties(p)));

            return data;
        }

        static List<Item> Properties(KeyValuePair<string, LogEventPropertyValue> keyValue)
        {
            if (keyValue.Value == null)
            {
                return new List<Item>
                {
                    new Item { Key = keyValue.Key, Value = null }
                };
            }

            // Handle simple things like strings and integers
            if (keyValue.Value is ScalarValue scalarValue)
            {
                return new List<Item>
                {
                    new Item { Key = keyValue.Key, Value = scalarValue.Value?.ToString() }
                };
            }

            // Handle dictionary types
            if (keyValue.Value is DictionaryValue dictionaryValue)
            {
                return dictionaryValue
                    .Elements
                    .SelectMany(element => Properties(new KeyValuePair<string, LogEventPropertyValue>($"{keyValue.Key}.{element.Key}", element.Value)))
                    .ToList();
            }

            // Handle complext objects
            if (keyValue.Value is StructureValue structureValue)
            {
                return structureValue
                    .Properties
                    .SelectMany(property => Properties(new KeyValuePair<string, LogEventPropertyValue>($"{keyValue.Key}.{property.Name}", property.Value)))
                    .ToList();
            }

            return new List<Item>
            {
                new Item { Key = keyValue.Key, Value = keyValue.Value.ToString()}
            };
        }

        static string LevelToSeverity(LogEvent logEvent)
        {
            switch (logEvent.Level)
            {
                case LogEventLevel.Debug:
                    return Severity.Debug.ToString();
                case LogEventLevel.Error:
                    return Severity.Error.ToString();
                case LogEventLevel.Fatal:
                    return Severity.Fatal.ToString();
                case LogEventLevel.Verbose:
                    return Severity.Verbose.ToString();
                case LogEventLevel.Warning:
                    return Severity.Warning.ToString();
                default:
                    return Severity.Information.ToString();
            }
        }

        static string String(LogEvent logEvent, string name)
        {
            if (logEvent == null || logEvent.Properties == null || !logEvent.Properties.Any()) return null;
            if (!logEvent.Properties.Keys.Any(key => key.ToLower().Equals(name.ToLower()))) return null;

            var property = logEvent.Properties.First(prop => prop.Key.ToLower().Equals(name.ToLower()));
            var properties = Properties(property);
            return string.Join(", ", properties.Select(p => p.Value));
        }

        private IList<Item> Items(LogEvent logEvent, string keyName)
        {
            if (logEvent == null || logEvent.Properties == null || !logEvent.Properties.Any()) return null;
            if (!logEvent.Properties.Keys.Any(key => key.ToLower().Equals(keyName))) return null;

            var property = logEvent.Properties.First(prop => prop.Key.ToLower().Equals(keyName));
            if (!(property.Value is DictionaryValue dictionaryValue)) return null;

            return dictionaryValue
                .Elements
                .Select(element => element.ToItem())
                .ToList();
        }
    }
}
