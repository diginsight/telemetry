using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

public sealed class TimerLap : IDisposable
{
    private readonly Histogram<double> histogram;
    private readonly ICollection<Tag> tags;
    private readonly StrongBox<double>? elapsedMillisecondsBox;
    private readonly Stopwatch sw = new ();

    private IDisposable? stopper;
    private bool committed;

    public bool DisableCommit { get; set; }

    public double ElapsedMilliseconds => sw.Elapsed.TotalMilliseconds;

    internal TimerLap(Histogram<double> histogram, Tags tags, StrongBox<double>? elapsedMillisecondsBox)
    {
        this.histogram = histogram;
        this.tags = tags.ToList();

        if (elapsedMillisecondsBox is not null)
        {
            elapsedMillisecondsBox.Value = double.NaN;
        }
        this.elapsedMillisecondsBox = elapsedMillisecondsBox;
    }

    public void AddTags([SuppressMessage("ReSharper", "ParameterHidesMember")] params Tag[] tags)
    {
        foreach (var tag in tags)
        {
            this.tags.Add(tag);
        }
    }

    public void AddTag(string key, object value)
    {
        tags.Add(new Tag(key, (object?)value));
    }

    public void AddTag(Tag tag)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Stop()
    {
        sw.Stop();

        if (elapsedMillisecondsBox is { Value: double.NaN })
        {
            elapsedMillisecondsBox.Value = ElapsedMilliseconds;
        }
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
        private readonly TimerLap lap;

        public Stopper(TimerLap lap)
        {
            this.lap = lap;
        }

        public void Dispose()
        {
            lap.Stop();
        }
    }
}
