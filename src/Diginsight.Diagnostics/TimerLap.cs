using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

/// <summary>
/// Represents an elapsed time measurement that can be recorded to a timer histogram.
/// </summary>
public sealed class TimerLap : IDisposable
{
    private readonly Histogram<double> histogram;
    private readonly ICollection<Tag> tags;
    private readonly StrongBox<double>? elapsedMillisecondsBox;
    private readonly Stopwatch sw = new ();

    private IDisposable? stopper;
    private bool committed;

    /// <summary>
    /// Gets a value indicating whether the elapsed time should not be recorded when this instance is disposed.
    /// </summary>
    public bool DisableCommit { get; set; }

    /// <summary>
    /// Gets the elapsed time in milliseconds.
    /// </summary>
    public double ElapsedMilliseconds => sw.Elapsed.TotalMilliseconds;

    internal TimerLap(Histogram<double> histogram, Tags tags, StrongBox<double>? elapsedMillisecondsBox)
    {
        this.histogram = histogram;
        this.tags = [ ..tags ];

        elapsedMillisecondsBox?.Value = double.NaN;
        this.elapsedMillisecondsBox = elapsedMillisecondsBox;
    }

    /// <summary>
    /// Adds tags to the timer lap.
    /// </summary>
    /// <param name="tags">The tags to add.</param>
    public void AddTags([SuppressMessage("ReSharper", "ParameterHidesMember")] params Tag[] tags)
    {
        foreach (var tag in tags)
        {
            this.tags.Add(tag);
        }
    }

    /// <summary>
    /// Adds a tag to the timer lap.
    /// </summary>
    /// <param name="key">The tag key.</param>
    /// <param name="value">The tag value.</param>
    public void AddTag(string key, object value)
    {
        tags.Add(new Tag(key, (object?)value));
    }

    /// <summary>
    /// Adds a tag to the timer lap.
    /// </summary>
    /// <param name="tag">The tag to add.</param>
    public void AddTag(Tag tag)
    {
        tags.Add(tag);
    }

    /// <summary>
    /// Starts measuring elapsed time.
    /// </summary>
    /// <returns>A disposable that stops the timer lap.</returns>
    public IDisposable Start()
    {
        if (stopper is not null)
        {
            return stopper;
        }

        sw.Start();
        return stopper = new Stopper(this);
    }

    /// <inheritdoc />
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

        histogram.Record(ElapsedMilliseconds, [ ..tags ]);
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
