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
using System.Security.Claims;
using System.Threading;
using Elmah.Io.Client;
using Elmah.Io.Client.Models;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.ElmahIo
{
    /// <summary>
    /// Writes log events to the elmah.io service.
    /// </summary>
    public class ElmahIoSink : ILogEventSink
    {
        readonly IFormatProvider _formatProvider;
        readonly Guid _logId;
        readonly IElmahioAPI _client;

        /// <summary>
        /// Construct a sink that saves logs to the specified storage account.
        /// </summary>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="apiKey">An API key from the organization containing the log.</param>
        /// <param name="logId">The log ID as found on the elmah.io website.</param>
        public ElmahIoSink(IFormatProvider formatProvider, string apiKey, Guid logId)
        {
            _formatProvider = formatProvider;
            _logId = logId;
            _client = ElmahioAPI.Create(apiKey);
        }

        /// <summary>
        /// Construct a sink that saves logs to the specified logger. The purpose of this
        /// constructor is to re-use an existing client from ELMAH or similar.
        /// </summary>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="client">The client to use.</param>
        public ElmahIoSink(IFormatProvider formatProvider, IElmahioAPI client)
        {
            _formatProvider = formatProvider;
            _client = client;
        }

        /// <summary>
        /// Emit the provided log event to the sink.
        /// </summary>
        /// <param name="logEvent">The log event to write.</param>
        public void Emit(LogEvent logEvent)
        {
            try
            {
                var message = new CreateMessage
                {
                    Title = logEvent.RenderMessage(_formatProvider),
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
                };

                _client.Messages.CreateAndNotify(_logId, message);
            }
            catch (Exception e)
            {
                Debugging.SelfLog.WriteLine("Caught exception while emitting to sink: {0}", e);
                throw;
            }
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
    }
}
