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
using Elmah.Io.Client;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.ElmahIO
{
    /// <summary>
    /// Writes log events to the Elmah.IO service.
    /// </summary>
    public class ElmahIOSink : ILogEventSink
    {
        readonly IFormatProvider _formatProvider;
        readonly Elmah.Io.Client.ILogger _logger;

        /// <summary>
        /// Construct a sink that saves logs to the specified storage account.
        /// </summary>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="logId">The log id as found on the elmah.io website.</param>
        public ElmahIOSink(IFormatProvider formatProvider, Guid logId)
        {
            _formatProvider = formatProvider;
            _logger = new Elmah.Io.Client.Logger(logId);
        }

        /// <summary>
        /// Construct a sink that saves logs to the specified logger. The purpose of this
        /// constructor is to re-use an existing ILogger from ELMAH or similar.
        /// </summary>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="logger">The logger to use.</param>
        public ElmahIOSink(IFormatProvider formatProvider, Elmah.Io.Client.ILogger logger)
        {
            _formatProvider = formatProvider;
            _logger = logger;
        }

        /// <summary>
        /// Emit the provided log event to the sink.
        /// </summary>
        /// <param name="logEvent">The log event to write.</param>
        public void Emit(LogEvent logEvent)
        {
            var message = new Message(logEvent.RenderMessage(_formatProvider))
            {
                Severity = LevelToSeverity(logEvent),
                DateTime = logEvent.Timestamp.DateTime.ToUniversalTime(),
                Detail = logEvent.Exception != null ? logEvent.Exception.ToString() : null,
                Data = PropertiesToData(logEvent),
            };

            _logger.Log(message);
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

            data.AddRange(logEvent.Properties.Select(p => new Item { Key = p.Key, Value = p.Value.ToString() }));
            return data;
        }

        static Severity LevelToSeverity(LogEvent logEvent)
        {
            switch (logEvent.Level)
            {
                case LogEventLevel.Debug:
                    return Severity.Debug;
                case LogEventLevel.Error:
                    return Severity.Error;
                case LogEventLevel.Fatal:
                    return Severity.Fatal;
                case LogEventLevel.Verbose:
                    return Severity.Verbose;
                case LogEventLevel.Warning:
                    return Severity.Warning;
                default:
                    return Severity.Information;
            }
        }
    }
}
