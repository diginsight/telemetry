using Diginsight.Stringify;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

/// <summary>
/// Represents the depth of an activity in the local and distributed activity hierarchy.
/// </summary>
[StringifiableType]
public readonly struct ActivityDepth
{
    /// <summary>
    /// Represents the trace state key used to propagate activity depth.
    /// </summary>
    public static readonly TraceStateKey TraceStateKey = "diginsightdepth";

    /// <summary>
    /// Gets the distributed activity layer.
    /// </summary>
    public int Layer { get; }
    /// <summary>
    /// Gets the actual local depth within the current distributed layer.
    /// </summary>
    public int ActualLocal { get; }
    /// <summary>
    /// Gets the actual cumulated depth across all distributed layers.
    /// </summary>
    public int ActualCumulated { get; }
    /// <summary>
    /// Gets the visual local depth within the current distributed layer.
    /// </summary>
    public int VisualLocal { get; }
    /// <summary>
    /// Gets the visual cumulated depth across all distributed layers.
    /// </summary>
    public int VisualCumulated { get; }

    private ActivityDepth(int layer, int actualLocal, int actualCumulated, int visualLocal, int visualCumulated)
    {
        Layer = layer;
        ActualLocal = actualLocal;
        ActualCumulated = actualCumulated;
        VisualLocal = visualLocal;
        VisualCumulated = visualCumulated;
    }

    /// <summary>
    /// Creates the depth for a remote child activity.
    /// </summary>
    /// <returns>The depth for a remote child activity.</returns>
    public ActivityDepth MakeRemoteChild() => new (Layer + 1, 1, ActualCumulated + 1, 1, VisualCumulated + 1);

    /// <summary>
    /// Creates the depth for a local child activity.
    /// </summary>
    /// <returns>The depth for a local child activity.</returns>
    public ActivityDepth MakeLocalChild()
    {
        return Layer == 0
            ? new ActivityDepth(1, 1, 1, 1, 1)
            : new ActivityDepth(Layer, ActualLocal + 1, ActualCumulated + 1, VisualLocal + 1, VisualCumulated + 1);
    }

    /// <summary>
    /// Creates the depth for an activity hidden from visual activity lifecycle logging.
    /// </summary>
    /// <returns>The depth for a hidden activity.</returns>
    public ActivityDepth MakeHidden() => new (Layer, ActualLocal, ActualCumulated, VisualLocal - 1, VisualCumulated - 1);

    /// <summary>
    /// Parses an activity depth from a trace state value.
    /// </summary>
    /// <param name="traceStateValue">The trace state value to parse.</param>
    /// <returns>The parsed <see cref="ActivityDepth" /> instance, or <c>null</c> if the value is invalid.</returns>
    public static ActivityDepth? FromTraceStateValue(string? traceStateValue)
    {
        return traceStateValue?.Split('_') is [ var rawLayer, var rawActualLocal, var rawActualCumulated, var rawVisualLocal, var rawVisualCumulated ]
            && int.TryParse(rawLayer, out int layer)
            && int.TryParse(rawActualLocal, out int actualLocal)
            && int.TryParse(rawActualCumulated, out int actualCumulated)
            && int.TryParse(rawVisualLocal, out int visualLocal)
            && int.TryParse(rawVisualCumulated, out int visualCumulated)
                ? new ActivityDepth(layer, actualLocal, actualCumulated, visualLocal, visualCumulated)
                : null;
    }

    /// <inheritdoc />
    public override string ToString() => this.Stringify();

    /// <summary>
    /// Converts this depth to the trace state value used for propagation.
    /// </summary>
    /// <returns>The trace state value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ToTraceStateValue() =>
        string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}_{3}_{4}", Layer, ActualLocal, ActualCumulated, VisualLocal, VisualCumulated);
}
