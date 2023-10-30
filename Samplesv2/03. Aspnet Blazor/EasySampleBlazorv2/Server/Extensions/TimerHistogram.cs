using System.Diagnostics.Metrics;

namespace OpenTelemetry.Metrics; 

public sealed class TimerHistogram
{
    private readonly Histogram<double> histogram;

    public TimerHistogram(Meter meter, string name, string? unit = "ms", string? description = null)
    {
        histogram = meter.CreateHistogram<double>(name, unit, description);
    }

    public TimerMark CreateMark(params KeyValuePair<string, object?>[] tags) => CoreCreateMark(tags, false);

    public TimerMark StartMark(params KeyValuePair<string, object?>[] tags) => CoreCreateMark(tags, true);

    public TimerMark CreateMark(IEnumerable<KeyValuePair<string, object?>> tags) => CoreCreateMark(tags, false);

    public TimerMark StartMark(IEnumerable<KeyValuePair<string, object?>> tags) => CoreCreateMark(tags, true);

    private TimerMark CoreCreateMark(IEnumerable<KeyValuePair<string, object?>> tags, bool start)
    {
        TimerMark mark = new(histogram, tags);
        if (start) { _ = mark.Start(); }
        return mark;
    }
}
