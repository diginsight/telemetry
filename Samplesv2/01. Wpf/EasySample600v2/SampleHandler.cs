using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace EasySample600v2
{
    public class SampleLogger
    {
        public LogLevel EnabledLevel { get; init; } = LogLevel.Error;

        //public void LogDebug(LogLevel level, string msg)
        //{
        //    if (EnabledLevel < level) return;
        //    Console.WriteLine(msg);
        //}
        //public void LogMessage(LogLevel level, TraceLoggerInterpolatedStringHandler builder)
        //{
        //    if (EnabledLevel < level) return;
        //    Console.WriteLine(builder.GetFormattedText());
        //}
        //public void LogDebug(LogLevel level, [InterpolatedStringHandlerArgument("", "level")] TraceLoggerInterpolatedStringHandler builder)
        //{
        //    if (EnabledLevel < level) return;
        //    Console.WriteLine(builder.GetFormattedText());
        //}
    }


}
