#if NET
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Diginsight.Logging;

/// <summary>
/// Represents an interpolated string handler that builds a log message only when the target log level is enabled.
/// </summary>
[InterpolatedStringHandler]
public readonly struct LogInterpolatedStringHandler
{
    private readonly ILogger logger;
    private readonly LogLevel logLevel;
    private readonly StringBuilder? builder;
    private readonly ICollection<object?>? arguments;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogInterpolatedStringHandler" /> struct for the specified logger and log level.
    /// </summary>
    /// <param name="literalLength">The total length of the literal portions of the interpolated string.</param>
    /// <param name="formattedCount">The number of interpolation holes in the interpolated string.</param>
    /// <param name="logger">The logger the message is written to.</param>
    /// <param name="logLevel">The level at which the message is logged.</param>
    /// <param name="isEnabled">When this method returns, contains <c>true</c> if the specified log level is enabled and the message should be built; otherwise, <c>false</c>.</param>
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

    /// <summary>
    /// Appends the specified literal text to the interpolated string, escaping brace characters.
    /// </summary>
    /// <param name="value">The literal text to append.</param>
    public void AppendLiteral(string value)
    {
        builder!.Append(value.Replace("{", "{{").Replace("}", "}}"));
    }

    /// <summary>
    /// Appends the specified value to the interpolated string as a structured logging argument.
    /// </summary>
    /// <typeparam name="T">The type of the value to append.</typeparam>
    /// <param name="value">The value to append.</param>
    public void AppendFormatted<T>(T? value)
    {
        builder!.Append(CultureInfo.InvariantCulture, $"{{{arguments!.Count}}}");
        arguments.Add(value);
    }

    /// <summary>
    /// Appends the specified value to the interpolated string with the given alignment.
    /// </summary>
    /// <typeparam name="T">The type of the value to append.</typeparam>
    /// <param name="value">The value to append.</param>
    /// <param name="alignment">The minimum number of characters used to render the value.</param>
    public void AppendFormatted<T>(T? value, int alignment)
    {
        builder!.Append(CultureInfo.InvariantCulture, $"{{{arguments!.Count},{alignment}}}");
        arguments.Add(value);
    }

    /// <summary>
    /// Appends the specified value to the interpolated string with the given alignment and format.
    /// </summary>
    /// <typeparam name="T">The type of the value to append.</typeparam>
    /// <param name="value">The value to append.</param>
    /// <param name="alignment">The minimum number of characters used to render the value.</param>
    /// <param name="format">The format string applied to the value.</param>
    public void AppendFormatted<T>(T? value, int alignment, string format)
    {
        builder!.Append(CultureInfo.InvariantCulture, $"{{{arguments!.Count},{alignment}:{format}}}");
        arguments.Add(value);
    }

    /// <summary>
    /// Appends the specified value to the interpolated string with the given format.
    /// </summary>
    /// <typeparam name="T">The type of the value to append.</typeparam>
    /// <param name="value">The value to append.</param>
    /// <param name="format">The format string applied to the value.</param>
    public void AppendFormatted<T>(T? value, string format)
    {
        builder!.Append(CultureInfo.InvariantCulture, $"{{{arguments!.Count}:{format}}}");
        arguments.Add(value);
    }

    internal void LogIfEnabled(EventId eventId, Exception? exception)
    {
        if (builder is null)
            return;

        FormattableString fs = FormattableStringFactory.Create(builder.ToString(), [ ..arguments! ]);
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
            return FormattableString.GetArguments().Select(static (x, i) => KeyValuePair.Create("Arg" + i, x)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
#endif
