#if NET7_0_OR_GREATER
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Diginsight.Logging;

[InterpolatedStringHandler]
public readonly struct LogInterpolatedStringHandler
{
    private readonly ILogger logger;
    private readonly LogLevel logLevel;
    private readonly StringBuilder? builder;
    private readonly ICollection<object?>? arguments;

    public LogInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, LogLevel logLevel, out bool isEnabled)
    {
        this.logger = logger;
        this.logLevel = logLevel;

        isEnabled = logger.IsEnabled(logLevel);
        if (isEnabled)
        {
            builder = new StringBuilder(literalLength);
            arguments = new List<object?>();
        }
        else
        {
            builder = null;
            arguments = null;
        }
    }

    public void AppendLiteral(string value)
    {
        builder!.Append(value.Replace("{", "{{").Replace("}", "}}"));
    }

    public void AppendFormatted<T>(T? value)
    {
        builder!.Append(CultureInfo.InvariantCulture, $"{{{arguments!.Count}}}");
        arguments.Add(value);
    }

    public void AppendFormatted<T>(T? value, int alignment)
    {
        builder!.Append(CultureInfo.InvariantCulture, $"{{{arguments!.Count},{alignment}}}");
        arguments.Add(value);
    }

    public void AppendFormatted<T>(T? value, int alignment, string format)
    {
        builder!.Append(CultureInfo.InvariantCulture, $"{{{arguments!.Count},{alignment}:{format}}}");
        arguments.Add(value);
    }

    public void AppendFormatted<T>(T? value, string format)
    {
        builder!.Append(CultureInfo.InvariantCulture, $"{{{arguments!.Count}:{format}}}");
        arguments.Add(value);
    }

    internal void LogIfEnabled(EventId eventId, Exception? exception)
    {
        if (builder is null)
            return;

        FormattableString fs = FormattableStringFactory.Create(builder.ToString(), arguments!.ToArray());
        logger.Log(logLevel, eventId, new CompositeMessage(fs), exception, static (x, _) => x.FormattableString.ToString());
    }

    private readonly struct CompositeMessage : IEnumerable<KeyValuePair<string, object?>>
    {
        public FormattableString FormattableString { get; }

        public CompositeMessage(FormattableString fs)
        {
            FormattableString = fs;
        }

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            return FormattableString.GetArguments().Select(static (x, i) => new KeyValuePair<string, object?>("Arg" + i, x)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
#endif
