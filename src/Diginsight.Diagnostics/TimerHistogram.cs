using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

public sealed class TimerHistogram
{
    public Histogram<double> Underlying { get; }

    public TimerHistogram(Meter meter, string name, string? unit = "ms", string? description = null)
    {
        Underlying = meter.CreateHistogram<double>(name, unit, description);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TimerLap CreateLap(StrongBox<double> elapsedMillisecondsBox, params Tag[] tags) => CoreCreateLap(tags, false, elapsedMillisecondsBox);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TimerLap StartLap(StrongBox<double> elapsedMillisecondsBox, params Tag[] tags) => CoreCreateLap(tags, true, elapsedMillisecondsBox);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TimerLap CreateLap(params Tag[] tags) => CoreCreateLap(tags, false, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TimerLap StartLap(params Tag[] tags) => CoreCreateLap(tags, true, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TimerLap CreateLap(StrongBox<double> elapsedMillisecondsBox, Tags tags) => CoreCreateLap(tags, false, elapsedMillisecondsBox);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TimerLap StartLap(StrongBox<double> elapsedMillisecondsBox, Tags tags) => CoreCreateLap(tags, true, elapsedMillisecondsBox);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TimerLap CreateLap(Tags tags) => CoreCreateLap(tags, false, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TimerLap StartLap(Tags tags) => CoreCreateLap(tags, true, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TimerLap CoreCreateLap(Tags tags, bool start, StrongBox<double>? elapsedMillisecondsBox)
    {
        TimerLap lap = new (Underlying, tags, elapsedMillisecondsBox);
        if (start)
        {
            _ = lap.Start();
        }

        return lap;
    }
}
