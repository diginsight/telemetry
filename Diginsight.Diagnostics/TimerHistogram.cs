using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public sealed class TimerHistogram
{
    public Histogram<double> Underlying { get; }

    public TimerHistogram(Meter meter, string name, string? unit = "ms", string? description = null)
    {
        Underlying = meter.CreateHistogram<double>(name, unit, description);
    }

    public TimerLap CreateLap(params Tag[] tags) => CoreCreateLap(tags, false);

    public TimerLap StartLap(params Tag[] tags) => CoreCreateLap(tags, true);

    public TimerLap CreateLap(Tags tags) => CoreCreateLap(tags, false);

    public TimerLap StartLap(Tags tags) => CoreCreateLap(tags, true);

    private TimerLap CoreCreateLap(Tags tags, bool start)
    {
        TimerLap lap = new (Underlying, tags);
        if (start)
        {
            _ = lap.Start();
        }

        return lap;
    }
}
