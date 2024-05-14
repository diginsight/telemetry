using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public static class DiginsightDefaults
{
    private static readonly ActivitySource FallbackActivitySource = new ("Default");
    private static readonly Meter FallbackMeter = new ("Default");

    private static ActivitySource activitySource = FallbackActivitySource;
    private static Meter meter = FallbackMeter;

    [AllowNull]
    public static ActivitySource ActivitySource
    {
        get => activitySource;
        set => activitySource = value ?? FallbackActivitySource;
    }

    [AllowNull]
    public static Meter Meter
    {
        get => meter;
        set => meter = value ?? FallbackMeter;
    }
}
