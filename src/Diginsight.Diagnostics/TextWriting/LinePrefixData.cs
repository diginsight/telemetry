using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Diginsight.Diagnostics.TextWriting;

public readonly ref struct LinePrefixData
{
    private static class CustomPropertyNames
    {
        public const string LastLogTimestamp = nameof(LastLogTimestamp);
        public const string LastWasStart = nameof(LastWasStart);
    }

    private static DateTimeOffset? globalPrevTimestamp;

    public DateTimeOffset Timestamp { get; }
    public LogLevel LogLevel { get; }
    public string Category { get; }
    public bool IsActivity { get; }
    public double? Duration { get; }
    public DateTimeOffset? PrevTimestamp { get; }
    public bool LastWasStart { get; }
    public Activity? Activity { get; }

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
