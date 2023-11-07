using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public sealed class TimerHistogram
{
    private readonly Histogram<double> histogram;

    public TimerHistogram(Meter meter, string name, string? unit = "ms", string? description = null)
    {
        histogram = meter.CreateHistogram<double>(name, unit, description);
    }

    public TimerMark CreateMark(params Tag[] tags) => CoreCreateMark(tags, false);

    public TimerMark StartMark(params Tag[] tags) => CoreCreateMark(tags, true);

    public TimerMark CreateMark(Tags tags) => CoreCreateMark(tags, false);

    public TimerMark StartMark(Tags tags) => CoreCreateMark(tags, true);

    private TimerMark CoreCreateMark(Tags tags, bool start)
    {
        TimerMark mark = new (histogram, tags);
        if (start)
        {
            _ = mark.Start();
        }

        return mark;
    }
}
