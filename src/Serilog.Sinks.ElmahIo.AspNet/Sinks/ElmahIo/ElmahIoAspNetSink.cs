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
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using Elmah.Io.Client;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.ElmahIo.AspNet
{
    /// <summary>
    /// Writes log events to the elmah.io service.
    /// </summary>
    public class ElmahIoAspNetSink : ILogEventSink
    {
        readonly IFormatProvider _formatProvider;
        readonly Elmah.Io.Client.ILogger _logger;

        /// <summary>
        /// Construct a sink that saves logs to the specified storage account.
        /// </summary>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="logId">The log id as found on the elmah.io website.</param>
        public ElmahIoAspNetSink(IFormatProvider formatProvider, Guid logId)
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
        public ElmahIoAspNetSink(IFormatProvider formatProvider, Elmah.Io.Client.ILogger logger)
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
                Type = Type(logEvent),
                Hostname = Environment.MachineName,
            };

            var httpContext = HttpContext.Current;

            if (httpContext != null)
            {
                message.Cookies = Cookies(httpContext);
                message.Form = Form(httpContext);
                message.QueryString = QueryString(httpContext);
                message.ServerVariables = ServerVariables(httpContext);
                message.StatusCode = StatusCode(httpContext);
                message.Url = Url(httpContext);
                message.User = User(httpContext);
            }

            _logger.Log(message);
        }

        private string User(HttpContext httpContext)
        {
            return httpContext.User != null ? httpContext.User.Identity.Name : null;
        }

        private string Url(HttpContext httpContext)
        {
            return httpContext.Request.Url.AbsoluteUri;
        }

        private string Type(LogEvent logEvent)
        {
            return logEvent.Exception == null ? null : logEvent.Exception.GetType().FullName;
        }

        private int? StatusCode(HttpContext httpContext)
        {
            return httpContext.Response.StatusCode;
        }

        private List<Item> ServerVariables(HttpContext httpContext)
        {
            return NameValueCollectionToItems(httpContext.Request.ServerVariables);
        }

        private List<Item> QueryString(HttpContext httpContext)
        {
            return NameValueCollectionToItems(httpContext.Request.QueryString);
        }

        private List<Item> Form(HttpContext httpContext)
        {
            return NameValueCollectionToItems(httpContext.Request.Form);
        }

        private List<Item> NameValueCollectionToItems(NameValueCollection values)
        {
            return values
                .AllKeys
                .Select(c => new Item { Key = c, Value = values[c] })
                .ToList();
        }

        private List<Item> Cookies(HttpContext httpContext)
        {
            var cookies = httpContext.Request.Cookies;
            return cookies
                .AllKeys
                .Where(key => cookies[key] != null)
                .Select(key => new Item {Key = key, Value = cookies[key].Value})
                .ToList();
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
