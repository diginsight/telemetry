using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace OpenTelemetry.Metrics; 

public sealed class TimerMark : IDisposable
{
    private readonly Histogram<double> histogram;
    private readonly ICollection<KeyValuePair<string, object?>> tags;
    private readonly Stopwatch sw = new();

    private IDisposable? stopper;
    private bool committed;

    public bool DisableCommit { get; set; }

    public double ElapsedMilliseconds => sw.Elapsed.TotalMilliseconds;

    internal TimerMark(Histogram<double> histogram, IEnumerable<KeyValuePair<string, object?>> tags)
    {
        this.histogram = histogram;
        this.tags = tags.ToList();
    }

    // ReSharper disable once ParameterHidesMember
    public void AddTags(params KeyValuePair<string, object?>[] tags)
    {
        foreach (var tag in tags)
        {
            this.tags.Add(tag);
        }
    }

    public void AddTag(string key, object value)
    {
        tags.Add(KeyValuePair.Create(key, (object?)value));
    }

    public void AddTag(KeyValuePair<string, object?> tag)
    {
        tags.Add(tag);
    }

    public IDisposable Start()
    {
        if (stopper is not null)
        {
            return stopper;
        }

        sw.Start();
        return stopper = new Stopper(this);
    }

    public void Dispose()
    {
        Commit();
    }

    private void Stop()
    {
        sw.Stop();
    }

    private void Commit()
    {
        Stop();
        if (DisableCommit || committed)
            return;

        committed = true;

        histogram.Record(ElapsedMilliseconds, tags.ToArray());
    }

    private sealed class Stopper : IDisposable
    {
        private readonly TimerMark mark;

        public Stopper(TimerMark mark)
        {
            this.mark = mark;
        }

        public void Dispose()
        {
            mark.Stop();
        }
    }
}
