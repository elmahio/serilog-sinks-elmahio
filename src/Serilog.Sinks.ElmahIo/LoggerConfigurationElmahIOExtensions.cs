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

namespace Serilog
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

            return loggerConfiguration.Sink(
                batchingSink,
                restrictedToMinimumLevel: options.MinimumLogEventLevel ?? LevelAlias.Minimum,
                levelSwitch: options.LevelSwitch);
        }
    }
}
