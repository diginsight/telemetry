using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

internal sealed class LogLevelAppender : IPrefixTokenAppender
{
    private static readonly LogLevelAppender?[] Instances = new LogLevelAppender?[5];

    private readonly int length;

    private LogLevelAppender(int length)
    {
        this.length = length;
    }

    public static LogLevelAppender For(int? length)
    {
        return UnsafeFor(length is < 1 or > 5 ? throw new ArgumentOutOfRangeException(nameof(length), "Length must be in the range 1-5") : length);
    }

    internal static LogLevelAppender UnsafeFor(int? length)
    {
        int finalLength = length ?? 4;
        return Instances[finalLength - 1] ??= new LogLevelAppender(finalLength);
    }

    public void Append(StringBuilder sb, in LinePrefixData linePrefixData) => Append(sb, linePrefixData.LogLevel);

    private void Append(StringBuilder sb, LogLevel logLevel)
    {
        string logLevelStr = logLevel switch
        {
            LogLevel.Trace => length switch
            {
                1 => "T",
                2 => "TR",
                3 => "TRC",
                4 => "TRCE",
                _ => "TRACE",
            },
            LogLevel.Debug => length switch
            {
                1 => "D",
                2 => "DB",
                3 => "DBG",
                4 => "DBUG",
                _ => "DEBUG",
            },
            LogLevel.Information => length switch
            {
                1 => "I",
                2 => "IN",
                3 => "INF",
                4 => "INFO",
                _ => "INFOR",
            },
            LogLevel.Warning => length switch
            {
                1 => "W",
                2 => "WR",
                3 => "WRN",
                4 => "WARN",
                _ => "WARNG",
            },
            LogLevel.Error => length switch
            {
                1 => "E",
                2 => "ER",
                3 => "ERR",
                4 => "EROR",
                _ => "ERROR",
            },
            LogLevel.Critical => length switch
            {
                1 => "C",
                2 => "CR",
                3 => "CRT",
                4 => "CRTC",
                _ => "CRTCL",
            },
            LogLevel.None => throw new InvalidOperationException($"Unexpected {nameof(LogLevel)}"),
            _ => throw new UnreachableException($"Unrecognized {nameof(LogLevel)}"),
        };

        sb.Append(logLevelStr);
    }
}
