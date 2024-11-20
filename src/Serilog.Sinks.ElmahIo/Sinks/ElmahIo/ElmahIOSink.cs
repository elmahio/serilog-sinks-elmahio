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
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Elmah.Io.Client;
using Newtonsoft.Json.Linq;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Serilog.Sinks.ElmahIo
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    /// <summary>
    /// Writes log events to the elmah.io service.
    /// </summary>
    public class ElmahIoSink : IBatchedLogEventSink
    {
        private static readonly string _assemblyVersion = typeof(ElmahIoSink).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
        private static readonly string _serilogAssemblyVersion = typeof(Log).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;

        readonly ElmahIoSinkOptions _options;
        private IElmahioAPI _client;

        /// <summary>
        /// Construct a sink that saves logs to the specified storage account.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0290:Use primary constructor", Justification = "Don't want to use primary constructors when there are more than one.")]
        public ElmahIoSink(ElmahIoSinkOptions options)
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
        public async Task EmitBatchAsync(IEnumerable<LogEvent> batch)
        {
            if (batch == null || !batch.Any())
                return;

            var messages = new List<CreateMessage>();

            foreach (var logEvent in batch)
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
                    CorrelationId = CorrelationId(logEvent),
                    Category = Category(logEvent),
                    ServerVariables = ServerVariables(logEvent),
                    Cookies = Cookies(logEvent),
                    Form = Form(logEvent),
                    QueryString = QueryString(logEvent),
                };

                messages.Add(message);
            }

            EnsureClient();

            try
            {
                await _client
                    .Messages
                    .CreateBulkAndNotifyAsync(_options.LogId, messages)
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Debugging.SelfLog.WriteLine("Caught exception while emitting to sink: {0}", e);
            }
        }

        /// <inheritdoc />
        public Task OnEmptyBatchAsync()
        {
            return Task.FromResult(0);
        }

        private static string UserAgent()
        {
            return new StringBuilder()
                .Append(new ProductInfoHeaderValue(new ProductHeaderValue("Serilog.Sinks.ElmahIo", _assemblyVersion)).ToString())
                .Append(" ")
                .Append(new ProductInfoHeaderValue(new ProductHeaderValue("Serilog", _serilogAssemblyVersion)).ToString())
                .ToString();
        }

        private static string Category(LogEvent logEvent)
        {
            var category = String(logEvent, "category");
            if (!string.IsNullOrWhiteSpace(category)) return category;
            return String(logEvent, "sourcecontext");
        }

        private static string CorrelationId(LogEvent logEvent)
        {
            return String(logEvent, "correlationid");
        }

        private static IList<Item> ServerVariables(LogEvent logEvent)
        {
            var serverVariables = Items(logEvent, "servervariables") ?? [];

            if (!serverVariables.Exists(sv => sv.Key.Equals("User-Agent", StringComparison.OrdinalIgnoreCase)))
            {
                // Look for user agent in properties
                var userAgent = String(logEvent, "HttpRequestUserAgent");
                if (!string.IsNullOrWhiteSpace(userAgent)) serverVariables.Add(new Item("User-Agent", userAgent));
            }

            if (!serverVariables.Exists(sv => sv.Key.Equals("CLIENT-IP", StringComparison.OrdinalIgnoreCase)
                || sv.Key.Equals("CLIENT_IP", StringComparison.OrdinalIgnoreCase)
                || sv.Key.Equals("HTTP-CLIENT-IP", StringComparison.OrdinalIgnoreCase)
                || sv.Key.Equals("HTTP_CLIENT_IP", StringComparison.OrdinalIgnoreCase)))
            {
                // Look for user agent in properties
                var clientIp = String(logEvent, "HttpRequestClientHostIP");
                if (!string.IsNullOrWhiteSpace(clientIp)) serverVariables.Add(new Item("Client-IP", clientIp));
            }

            return serverVariables;
        }

        private static IList<Item> Cookies(LogEvent logEvent)
        {
            return Items(logEvent, "cookies");
        }

        private static IList<Item> Form(LogEvent logEvent)
        {
            return Items(logEvent, "form");
        }

        private static IList<Item> QueryString(LogEvent logEvent)
        {
            var queryString = Items(logEvent, "querystring") ?? [];
            if (queryString.Count > 0) return queryString;

            var httpRequestUrl = String(logEvent, "HttpRequestUrl");
            if (!string.IsNullOrWhiteSpace(httpRequestUrl) && Uri.TryCreate(httpRequestUrl, UriKind.Absolute, out Uri result) && !string.IsNullOrWhiteSpace(result.Query))
            {
                queryString.AddRange(result
                    .Query
                    .TrimStart('?')
                    .Split(['&'], StringSplitOptions.RemoveEmptyEntries)
                    .Select(s =>
                    {
                        var splitted = s.Split('=');
                        var item = new Item();
                        if (splitted.Length > 0) item.Key = splitted[0];
                        if (splitted.Length > 1) item.Value = splitted[1];
                        return item;
                    })
                    .ToList());
            }

            return queryString;
        }

        private static int? StatusCode(LogEvent logEvent)
        {
            var statusCode = String(logEvent, "statuscode");
            if (string.IsNullOrWhiteSpace(statusCode)) return null;
            if (!int.TryParse(statusCode, out int code)) return null;
            return code;
        }

        private static string Url(LogEvent logEvent)
        {
            var url = String(logEvent, "url");
            if (!string.IsNullOrWhiteSpace(url)) return url;
            var httpRequestUrl = String(logEvent, "HttpRequestUrl");
            if (!string.IsNullOrWhiteSpace(httpRequestUrl) && Uri.TryCreate(httpRequestUrl, UriKind.Absolute, out Uri result)) return result.AbsolutePath;

            return null;
        }

        private static string Version(LogEvent logEvent)
        {
            return String(logEvent, "version");
        }

        private static string Method(LogEvent logEvent)
        {
            var method = String(logEvent, "method");
            if (!string.IsNullOrWhiteSpace(method)) return method;
            var httpRequestType = String(logEvent, "HttpRequestType");
            if (!string.IsNullOrWhiteSpace(httpRequestType) && Uri.TryCreate(httpRequestType, UriKind.Relative, out _)) return httpRequestType;

            return null;
        }

        private string Application(LogEvent logEvent)
        {
            var application = String(logEvent, "application");
            if (!string.IsNullOrWhiteSpace(application)) return application;
            return _options.Application;
        }

        private static string Source(LogEvent logEvent)
        {
            var source = String(logEvent, "source");
            if (!string.IsNullOrWhiteSpace(source)) return source;
            return logEvent.Exception?.GetBaseException().Source;
        }

        private static string Hostname(LogEvent logEvent)
        {
            var hostname = String(logEvent, "hostname");
            if (!string.IsNullOrWhiteSpace(hostname)) return hostname;

            var machineName = Environment.MachineName;
            if (!string.IsNullOrWhiteSpace(machineName)) return machineName;

            var computerName = Environment.GetEnvironmentVariable("COMPUTERNAME");
            if (!string.IsNullOrWhiteSpace(computerName)) return computerName;
            return null;
        }

        private static string User(LogEvent logEvent)
        {
            var user = String(logEvent, "user");
            if (!string.IsNullOrWhiteSpace(user)) return user;
            var userName = String(logEvent, "UserName");
            if (!string.IsNullOrWhiteSpace(userName)) return userName;
            return ClaimsPrincipal.Current?.Identity?.Name;
        }

        private static string Type(LogEvent logEvent)
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
                data.AddRange(logEvent.Exception.ToDataList());
            }

            data.AddRange(logEvent.Properties.SelectMany(p => Properties(p)));

            return data;
        }

        static List<Item> Properties(KeyValuePair<string, LogEventPropertyValue> keyValue)
        {
            if (keyValue.Value == null)
            {
                return
                [
                    new Item { Key = keyValue.Key, Value = null }
                ];
            }

            // Handle simple things like strings and integers
            if (keyValue.Value is ScalarValue scalarValue)
            {
                return
                [
                    new Item { Key = keyValue.Key, Value = scalarValue.Value?.ToString() }
                ];
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

            return
            [
                new Item { Key = keyValue.Key, Value = keyValue.Value.ToString()}
            ];
        }

        static string LevelToSeverity(LogEvent logEvent)
        {
            return logEvent.Level switch
            {
                LogEventLevel.Debug => Severity.Debug.ToString(),
                LogEventLevel.Error => Severity.Error.ToString(),
                LogEventLevel.Fatal => Severity.Fatal.ToString(),
                LogEventLevel.Verbose => Severity.Verbose.ToString(),
                LogEventLevel.Warning => Severity.Warning.ToString(),
                _ => Severity.Information.ToString(),
            };
        }

        static string String(LogEvent logEvent, string name)
        {
            if (logEvent == null || logEvent.Properties == null || logEvent.Properties.Count == 0) return null;
            if (!logEvent.Properties.Keys.Any(key => key.Equals(name, StringComparison.OrdinalIgnoreCase))) return null;

            var property = logEvent.Properties.First(prop => prop.Key.Equals(name, StringComparison.OrdinalIgnoreCase));
            var properties = Properties(property);
            return string.Join(", ", properties.Select(p => p.Value));
        }

        private static List<Item> Items(LogEvent logEvent, string keyName)
        {
            if (logEvent == null || logEvent.Properties == null || logEvent.Properties.Count == 0) return [];
            if (!logEvent.Properties.Keys.Any(key => key.Equals(keyName, StringComparison.OrdinalIgnoreCase))) return [];

            var property = logEvent.Properties.First(prop => prop.Key.Equals(keyName, StringComparison.OrdinalIgnoreCase));
            if (property.Value is not DictionaryValue dictionaryValue) return [];

            return dictionaryValue
                .Elements
                .Select(element => element.ToItem())
                .ToList();
        }

        internal void CreateInstallation()
        {
            try
            {
                var logger = new LoggerInfo
                {
                    Type = "Serilog.Sinks.ElmahIo",
                    Properties =
                    [
                        new Item("FormatProvider", _options.FormatProvider?.GetType().FullName ?? ""),
                        new Item("BatchPostingLimit", _options.BatchPostingLimit.ToString()),
                        new Item("LevelSwitch", _options.LevelSwitch?.ToString() ?? ""),
                        new Item("MinimumLogEventLevel", _options.MinimumLogEventLevel?.ToString() ?? ""),
                        new Item("Period", _options.Period.ToString()),
                    ],
                    ConfigFiles = [],
                    Assemblies =
                    [
                        new AssemblyInfo { Name = "Serilog.Sinks.ElmahIo", Version = _assemblyVersion },
                        new AssemblyInfo { Name = "Elmah.Io.Client", Version = typeof(IElmahioAPI).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version },
                        new AssemblyInfo { Name = "Serilog", Version = _serilogAssemblyVersion }
                    ],
                };

                var installation = new CreateInstallation
                {
                    Type = ApplicationInfoHelper.GetApplicationType(),
                    Name = _options.Application,
                    Loggers = [logger]
                };

                var location = GetType().Assembly.Location;
                var currentDirectory = Path.GetDirectoryName(location);

                var appsettingsFilePath = Path.Combine(currentDirectory, "appsettings.json");
                if (File.Exists(appsettingsFilePath))
                {
                    var appsettingsContent = File.ReadAllText(appsettingsFilePath);
                    var appsettingsObject = JObject.Parse(appsettingsContent);
                    if (appsettingsObject.TryGetValue("Serilog", out JToken serilogSection))
                    {
                        logger.ConfigFiles.Add(new ConfigFile
                        {
                            Name = Path.GetFileName(appsettingsFilePath),
                            Content = new JObject { { "Serilog", serilogSection.DeepClone() } }.ToString(),
                            ContentType = "application/json"
                        });
                    }
                }

                EnsureClient();

                _client.Installations.Create(_options.LogId.ToString(), installation);
            }
            catch (Exception e)
            {
                Debugging.SelfLog.WriteLine("Caught exception while creating installation: {0}", e);
                // We don't want to crash the entire application if the installation fails. Carry on.
            }
        }

        private void EnsureClient()
        {
            if (_client == null)
            {
                var api = ElmahioAPI.Create(_options.ApiKey, new ElmahIoOptions
                {
                    Timeout = new TimeSpan(0, 0, 30),
                    UserAgent = UserAgent(),
                });

                api.Messages.OnMessageFilter += (sender, args) =>
                {
                    var filter = _options.OnFilter?.Invoke(args.Message);
                    if (filter.HasValue && filter.Value)
                    {
                        args.Filter = true;
                    }
                };
                api.Messages.OnMessage += (sender, args) =>
                {
                    _options.OnMessage?.Invoke(args.Message);
                };
                api.Messages.OnMessageFail += (sender, args) =>
                {
                    _options.OnError?.Invoke(args.Message, args.Error);
                };
                _client = api;
            }
        }
    }
}
