using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Diginsight.Diagnostics.TextWriting;

/// <summary>
/// Represents the data used to append a text-writing line prefix.
/// </summary>
public readonly ref struct LinePrefixData
{
    private static class CustomPropertyNames
    {
        /// <summary>
        /// The custom property name used for the last log timestamp.
        /// </summary>
        public const string LastLogTimestamp = nameof(LastLogTimestamp);

        /// <summary>
        /// The custom property name used for the last activity start flag.
        /// </summary>
        public const string LastWasStart = nameof(LastWasStart);
    }

    private static DateTimeOffset? globalPrevTimestamp;

    /// <summary>
    /// Gets the log timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the log level.
    /// </summary>
    public LogLevel LogLevel { get; }

    /// <summary>
    /// Gets the log category.
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// Gets a value indicating whether the line represents activity lifecycle output.
    /// </summary>
    public bool IsActivity { get; }

    /// <summary>
    /// Gets the activity duration in milliseconds.
    /// </summary>
    public double? Duration { get; }

    /// <summary>
    /// Gets the previous log timestamp.
    /// </summary>
    public DateTimeOffset? PrevTimestamp { get; }

    /// <summary>
    /// Gets a value indicating whether the previous activity lifecycle output was a start entry.
    /// </summary>
    public bool LastWasStart { get; }

    /// <summary>
    /// Gets the current activity.
    /// </summary>
    public Activity? Activity { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LinePrefixData" /> struct.
    /// </summary>
    /// <param name="timestamp">The log timestamp.</param>
    /// <param name="logLevel">The log level.</param>
    /// <param name="category">The log category.</param>
    /// <param name="isActivity">Whether the line represents activity lifecycle output.</param>
    /// <param name="duration">The activity duration.</param>
    /// <param name="activity">The current activity.</param>
    /// <exception cref="InvalidOperationException">Thrown when activity custom properties contain invalid data.</exception>
    public LinePrefixData(DateTimeOffset timestamp, LogLevel logLevel, string category, bool isActivity, TimeSpan? duration, Activity? activity)
    {
        double? durationMsec = duration?.TotalMilliseconds;

        DateTimeOffset? prevTimestamp;
        bool lastWasStart;
        {
            if (activity is null)
            {
                prevTimestamp = globalPrevTimestamp;
            }
            else
            {
                prevTimestamp = activity.GetCustomProperty(CustomPropertyNames.LastLogTimestamp) switch
                {
                    DateTimeOffset dto => dto,
                    null => activity.Parent?.GetCustomProperty(CustomPropertyNames.LastLogTimestamp) switch
                    {
                        DateTimeOffset dto => dto,
                        null => globalPrevTimestamp,
                        _ => throw new InvalidOperationException("Invalid last log timestamp in activity"),
                    },
                    _ => throw new InvalidOperationException("Invalid last log timestamp in activity"),
                };
            }

            if (activity is not null)
            {
                lastWasStart = activity.GetCustomProperty(CustomPropertyNames.LastWasStart) switch
                {
                    bool b => b,
                    null => false,
                    _ => throw new InvalidOperationException($"Invalid '{CustomPropertyNames.LastWasStart}' in activity"),
                };
                activity.SetCustomProperty(CustomPropertyNames.LastWasStart, isActivity && durationMsec is null);

                activity.SetCustomProperty(CustomPropertyNames.LastLogTimestamp, timestamp);
                if (durationMsec is not null)
                {
                    if (activity.Parent is { } parent)
                    {
                        parent.SetCustomProperty(CustomPropertyNames.LastLogTimestamp, timestamp);
                    }
                    else
                    {
                        globalPrevTimestamp = timestamp;
                    }
                }
            }
            else
            {
                lastWasStart = false;
                globalPrevTimestamp = timestamp;
            }
        }

        Timestamp = timestamp;
        LogLevel = logLevel;
        Category = category;
        IsActivity = isActivity;
        Duration = durationMsec;
        PrevTimestamp = prevTimestamp;
        LastWasStart = lastWasStart;
        Activity = activity;
    }
}
