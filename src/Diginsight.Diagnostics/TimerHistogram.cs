using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

/// <summary>
/// Represents a histogram used to record elapsed time measurements.
/// </summary>
public sealed class TimerHistogram
{
    /// <summary>
    /// Gets the underlying histogram instrument.
    /// </summary>
    public Histogram<double> Underlying { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TimerHistogram" /> class.
    /// </summary>
    /// <param name="meter">The meter used to create the histogram.</param>
    /// <param name="name">The histogram name.</param>
    /// <param name="unit">The histogram unit.</param>
    /// <param name="description">The histogram description.</param>
    public TimerHistogram(Meter meter, string name, string? unit = "ms", string? description = null)
    {
        Underlying = meter.CreateHistogram<double>(name, unit, description);
    }

    /// <summary>
    /// Creates a timer lap with elapsed time output and tags.
    /// </summary>
    /// <param name="elapsedMillisecondsBox">The box that receives the elapsed milliseconds.</param>
    /// <param name="tags">The tags to record with the measurement.</param>
    /// <returns>The created timer lap.</returns>
    /// <remarks>
    /// The box is initialized to <c>double.NaN</c> when the lap is created and receives the total elapsed milliseconds when the lap is first stopped; later stops leave the value unchanged.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TimerLap CreateLap(StrongBox<double> elapsedMillisecondsBox, params Tag[] tags) => CoreCreateLap(tags, false, elapsedMillisecondsBox);

    /// <summary>
    /// Creates and starts a timer lap with elapsed time output and tags.
    /// </summary>
    /// <param name="elapsedMillisecondsBox">The box that receives the elapsed milliseconds.</param>
    /// <param name="tags">The tags to record with the measurement.</param>
    /// <returns>The created timer lap.</returns>
    /// <remarks>
    /// The box is initialized to <c>double.NaN</c> when the lap is created and receives the total elapsed milliseconds when the lap is first stopped; later stops leave the value unchanged.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TimerLap StartLap(StrongBox<double> elapsedMillisecondsBox, params Tag[] tags) => CoreCreateLap(tags, true, elapsedMillisecondsBox);

    /// <summary>
    /// Creates a timer lap with tags.
    /// </summary>
    /// <param name="tags">The tags to record with the measurement.</param>
    /// <returns>The created timer lap.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TimerLap CreateLap(params Tag[] tags) => CoreCreateLap(tags, false, null);

    /// <summary>
    /// Creates and starts a timer lap with tags.
    /// </summary>
    /// <param name="tags">The tags to record with the measurement.</param>
    /// <returns>The created timer lap.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TimerLap StartLap(params Tag[] tags) => CoreCreateLap(tags, true, null);

    /// <summary>
    /// Creates a timer lap with elapsed time output and tags.
    /// </summary>
    /// <param name="elapsedMillisecondsBox">The box that receives the elapsed milliseconds.</param>
    /// <param name="tags">The tags to record with the measurement.</param>
    /// <returns>The created timer lap.</returns>
    /// <remarks>
    /// The box is initialized to <c>double.NaN</c> when the lap is created and receives the total elapsed milliseconds when the lap is first stopped; later stops leave the value unchanged.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TimerLap CreateLap(StrongBox<double> elapsedMillisecondsBox, Tags tags) => CoreCreateLap(tags, false, elapsedMillisecondsBox);

    /// <summary>
    /// Creates and starts a timer lap with elapsed time output and tags.
    /// </summary>
    /// <param name="elapsedMillisecondsBox">The box that receives the elapsed milliseconds.</param>
    /// <param name="tags">The tags to record with the measurement.</param>
    /// <returns>The created timer lap.</returns>
    /// <remarks>
    /// The box is initialized to <c>double.NaN</c> when the lap is created and receives the total elapsed milliseconds when the lap is first stopped; later stops leave the value unchanged.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TimerLap StartLap(StrongBox<double> elapsedMillisecondsBox, Tags tags) => CoreCreateLap(tags, true, elapsedMillisecondsBox);

    /// <summary>
    /// Creates a timer lap with tags.
    /// </summary>
    /// <param name="tags">The tags to record with the measurement.</param>
    /// <returns>The created timer lap.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TimerLap CreateLap(Tags tags) => CoreCreateLap(tags, false, null);

    /// <summary>
    /// Creates and starts a timer lap with tags.
    /// </summary>
    /// <param name="tags">The tags to record with the measurement.</param>
    /// <returns>The created timer lap.</returns>
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
