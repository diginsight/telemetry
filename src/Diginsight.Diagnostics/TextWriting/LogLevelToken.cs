using Microsoft.Extensions.Logging;
using Pastel;
using System.Diagnostics;
using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

public sealed class LogLevelToken : ILineToken
{
    private int? length;

    public int? Length
    {
        get => length;
        set => length = value is < 1 or > 5 ? throw new ArgumentOutOfRangeException(nameof(Length), "Must be null or in the range 1-5") : value;
    }

    internal int? LengthUnsafe
    {
        set => length = value;
    }

    public void Apply(ref MutableLineDescriptor lineDescriptor)
    {
        lineDescriptor.Appenders.Add(Appender.For(length));
    }

    public ILineToken Clone() => new LogLevelToken() { LengthUnsafe = length };

    internal sealed class Appender : IPrefixTokenAppender
    {
        private static readonly Appender?[] Instances = new Appender?[5];

        private readonly int desiredLength;

        private Appender(int desiredLength)
        {
            this.desiredLength = desiredLength;
        }

        internal static Appender For(int? length)
        {
            int finalLength = length ?? 4;
            return Instances[finalLength - 1] ??= new Appender(finalLength);
        }

        public void Append(StringBuilder sb, ref int length, in LinePrefixData linePrefixData, bool useColor)
        {
            Append(sb, linePrefixData.LogLevel, useColor);
            length += desiredLength;
        }

        private void Append(StringBuilder sb, LogLevel logLevel, bool useColor)
        {
            (string logLevelStr, ConsoleColor logLevelColor) = logLevel switch
            {
                LogLevel.Trace => (desiredLength switch
                {
                    1 => "T",
                    2 => "TR",
                    3 => "TRC",
                    4 => "TRCE",
                    _ => "TRACE",
                }, ConsoleColor.Gray),
                LogLevel.Debug => (desiredLength switch
                {
                    1 => "D",
                    2 => "DB",
                    3 => "DBG",
                    4 => "DBUG",
                    _ => "DEBUG",
                }, ConsoleColor.DarkCyan),
                LogLevel.Information => (desiredLength switch
                {
                    1 => "I",
                    2 => "IN",
                    3 => "INF",
                    4 => "INFO",
                    _ => "INFOR",
                }, ConsoleColor.Green),
                LogLevel.Warning => (desiredLength switch
                {
                    1 => "W",
                    2 => "WR",
                    3 => "WRN",
                    4 => "WARN",
                    _ => "WARNG",
                }, ConsoleColor.Yellow),
                LogLevel.Error => (desiredLength switch
                {
                    1 => "E",
                    2 => "ER",
                    3 => "ERR",
                    4 => "EROR",
                    _ => "ERROR",
                }, ConsoleColor.Red),
                LogLevel.Critical => (desiredLength switch
                {
                    1 => "C",
                    2 => "CR",
                    3 => "CRT",
                    4 => "CRTC",
                    _ => "CRTCL",
                }, ConsoleColor.Magenta),
                LogLevel.None => throw new InvalidOperationException($"Unexpected {nameof(LogLevel)}"),
                _ => throw new UnreachableException($"Unrecognized {nameof(LogLevel)}"),
            };

            sb.Append(useColor ? logLevelStr.Pastel(logLevelColor) : logLevelStr);
        }
    }
}
