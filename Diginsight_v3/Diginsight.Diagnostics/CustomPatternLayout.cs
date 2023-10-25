using log4net.Core;
using log4net.Layout;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;

namespace Diginsight.Diagnostics
{
    internal class CustomPatternLayout : PatternLayout
    {
        public const string FormatterName = "log4net";

        private readonly IOptionsMonitor<ObservabilityConsoleFormatterOptions> formatterOptionsMonitor;
        public CustomPatternLayout(
            IOptionsMonitor<ObservabilityConsoleFormatterOptions> formatterOptionsMonitor
        )
        {
            this.formatterOptionsMonitor = formatterOptionsMonitor;
        }

        public override void Format(TextWriter writer, LoggingEvent loggingEvent)
        {
            IObservabilityConsoleFormatterOptions formatterOptions = formatterOptionsMonitor.CurrentValue;

            if(!(loggingEvent.MessageObject is Log4NetMessage log4netMessage))
            {
                return;
            }

            ObservabilityTextWriter.Write(
                writer,
                formatterOptions.UseUtcTimestamp ? DateTime.UtcNow : DateTime.Now,
                formatterOptions.TimestampFormat,
                formatterOptions.TimestampCulture is { } cultureName ? CultureInfo.GetCultureInfo(cultureName) : null,
                this.TranslateLogLevel(loggingEvent.Level),
                "category",
                formatterOptions.MaxCategoryLength,
                log4netMessage.Message,
                loggingEvent.ExceptionObject,
                log4netMessage.IsActivity,
                log4netMessage.Duration
            );
        }

        private LogLevel TranslateLogLevel(Level level)
        {
            return level.Value switch
            {
                70000 => LogLevel.Error,
                60000 => LogLevel.Warning,
                40000 => LogLevel.Information,
                30000 => LogLevel.Debug,
                _ => LogLevel.Trace
            };
        }
    }
}
