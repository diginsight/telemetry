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

    public DateTime Timestamp { get; }
    public LogLevel LogLevel { get; }
    public string Category { get; }
    public bool IsActivity { get; }
    public double? Duration { get; }
    public DateTime? PrevTimestamp { get; }
    public bool LastWasStart { get; }
    public Activity? Activity { get; }

    public LinePrefixData(DateTime timestamp, LogLevel logLevel, string category, bool isActivity, TimeSpan? duration, Activity? activity)
    {
        double? durationMsec = duration?.TotalMilliseconds;

        DateTime? prevTimestamp;
        bool lastWasStart;
        {
            if (activity is null)
            {
                prevTimestamp = null;
            }
            else
            {
                prevTimestamp = activity.GetCustomProperty(CustomPropertyNames.LastLogTimestamp) switch
                {
                    DateTime dt => dt,
                    null => activity.Parent?.GetCustomProperty(CustomPropertyNames.LastLogTimestamp) switch
                    {
                        DateTime dt => dt,
                        null => null,
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
                    activity.Parent?.SetCustomProperty(CustomPropertyNames.LastLogTimestamp, timestamp);
                }
            }
            else
            {
                lastWasStart = false;
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
