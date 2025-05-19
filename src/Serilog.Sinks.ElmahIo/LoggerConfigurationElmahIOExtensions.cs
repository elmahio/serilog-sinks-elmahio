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
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Sinks.ElmahIo;
using Serilog.Sinks.PeriodicBatching;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Serilog
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    /// <summary>
    /// Adds the WriteTo.ElmahIo() extension method to <see cref="LoggerConfiguration"/>.
    /// </summary>
    public static class LoggerConfigurationElmahIoExtensions
    {
        /// <summary>
        /// Adds a sink that writes log events to elmah.io. If not specified through options,
        /// every level are logged to elmah.io. It is recommended to log warnings and up only.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="options">The options (like API key and log ID) to use when setting up the sink.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration ElmahIo(
            this LoggerSinkConfiguration loggerConfiguration,
            ElmahIoSinkOptions options)
        {
            if (loggerConfiguration == null) throw new ArgumentNullException(nameof(loggerConfiguration));

            var elmahIoSink = new ElmahIoSink(options);

            var batchingOptions = new PeriodicBatchingSinkOptions
            {
                BatchSizeLimit = options.BatchPostingLimit,
                Period = options.Period,
            };

            var batchingSink = new PeriodicBatchingSink(elmahIoSink, batchingOptions);

            elmahIoSink.CreateInstallation();

            return loggerConfiguration.Sink(
                batchingSink,
                restrictedToMinimumLevel: options.MinimumLogEventLevel ?? LevelAlias.Minimum,
                levelSwitch: options.LevelSwitch);
        }

        /// <summary>
        /// Adds a sink that writes log events to elmah.io. This overload accepts logId as a string and
        /// should be used from packages not supporting ElmahIoSinkOptions (like when configuring
        /// through appsettings.json files or similar.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="apiKey">An API key from the organization containing the log.</param>
        /// <param name="logId">The log ID as found on the elmah.io website.</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink. Set to Verbose by default.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
#pragma warning disable S1133 // Deprecated code should be removed
        [Obsolete("This method is intended for configuration through appsettings.json or similar only. From C# you should always call the overload accepting ElmahIoSinkOptions.")]
#pragma warning restore S1133 // Deprecated code should be removed
        public static LoggerConfiguration ElmahIo(
            this LoggerSinkConfiguration loggerConfiguration,
            string apiKey,
            string logId,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            IFormatProvider formatProvider = null)
        {
            return ElmahIo(loggerConfiguration, new ElmahIoSinkOptions(apiKey, new Guid(logId))
            {
                MinimumLogEventLevel = restrictedToMinimumLevel,
                FormatProvider = formatProvider,
            });
        }
    }
}
