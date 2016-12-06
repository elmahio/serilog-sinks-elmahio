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
using Serilog.Sinks.ElmahIo.AspNet;

namespace Serilog
{
    /// <summary>
    /// Adds the WriteTo.ElmahIoAspNet() extension method to <see cref="LoggerConfiguration"/>.
    /// </summary>
    public static class LoggerConfigurationElmahIoAspNetExtensions
    {
        /// <summary>
        /// Adds a sink that writes log events including ASP.NET info to the elmah.io webservice. 
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="logId">The log id as found on the elmah.io website.</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink. Set to Verbose by default.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration ElmahIoAspNet(
            this LoggerSinkConfiguration loggerConfiguration,
            Guid logId,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            IFormatProvider formatProvider = null)
        {
            if (loggerConfiguration == null) throw new ArgumentNullException("loggerConfiguration");
            return loggerConfiguration.Sink(
                new ElmahIoAspNetSink(formatProvider, logId),
                restrictedToMinimumLevel);
        }

    }
}
