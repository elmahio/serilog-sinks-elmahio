﻿using Elmah.Io.Client;
using Serilog.Core;
using Serilog.Events;
using System;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Serilog.Sinks.ElmahIo
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    /// <summary>
    /// Provides ElmahIoSink with configurable options
    /// </summary>
    /// <remarks>
    /// Creates a new options instance. Period will be set to 2 seconds and
    /// BatchPostingLimit to 50 unless set manually afterwards.
    /// </remarks>
    public class ElmahIoSinkOptions(string apiKey, Guid logId)
    {
        /// <summary>
        /// An API key able to write messages to elmah.io (enable the Messages - Write permission).
        /// </summary>
        public string ApiKey { get; set; } = apiKey;

        /// <summary>
        /// The ID of the log to store messages from Serilog.
        /// </summary>
        public Guid LogId { get; set; } = logId;

        /// <summary>
        /// An application name to put on all log messages.
        /// </summary>
        public string Application { get; set; }

        /// <summary>
        /// Callback executed on each log message. Additional properties can be set on the provided message.
        /// </summary>
        public Action<CreateMessage> OnMessage { get; set; }

        /// <summary>
        /// Callback executed when something goes wrong during communication with elmah.io.
        /// </summary>
        public Action<CreateMessage, Exception> OnError { get; set; }

        /// <summary>
        /// Callback used to filter one or more log message. If returning true from the provided func, the log messages will not be logged.
        /// </summary>
        public Func<CreateMessage, bool> OnFilter { get; set; }

        /// <summary>
        /// Register an action to be called before creating an installation. Use the OnInstallation
        /// action to decorate installations with additional information related to your environment.
        /// </summary>
        public Action<CreateInstallation> OnInstallation { get; set; }

        ///<summary>
        /// Supplies culture-specific formatting information, or null.
        /// </summary>
        public IFormatProvider FormatProvider { get; set; }

        ///<summary>
        /// The maximum number of events to post in a single batch. Defaults to: 50.
        /// </summary>
        public int BatchPostingLimit { get; set; } = 50;

        ///<summary>
        /// The time to wait between checking for event batches. Defaults to 2 seconds.
        /// </summary>
        public TimeSpan Period { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// The minimum log event level required in order to write an event to the sink.
        /// </summary>
        public LogEventLevel? MinimumLogEventLevel { get; set; }

        /// <summary>
        /// A switch allowing the pass-through minimum level to be changed at runtime.
        /// </summary>
        public LoggingLevelSwitch LevelSwitch { get; set; }
    }
}
