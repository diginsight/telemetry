using Diginsight.Stringify;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

[StringifiableType]
public readonly struct ActivityDepth
{
    public static readonly TraceStateKey TraceStateKey = "diginsightdepth";

    public int Layer { get; }
    public int ActualLocal { get; }
    public int ActualCumulated { get; }
    public int VisualLocal { get; }
    public int VisualCumulated { get; }

    private ActivityDepth(int layer, int actualLocal, int actualCumulated, int visualLocal, int visualCumulated)
    {
        Layer = layer;
        ActualLocal = actualLocal;
        ActualCumulated = actualCumulated;
        VisualLocal = visualLocal;
        VisualCumulated = visualCumulated;
    }

    public ActivityDepth MakeRemoteChild() => new (Layer + 1, 1, ActualCumulated + 1, 1, VisualCumulated + 1);

    public ActivityDepth MakeLocalChild()
    {
        return Layer == 0
            ? new ActivityDepth(1, 1, 1, 1, 1)
            : new ActivityDepth(Layer, ActualLocal + 1, ActualCumulated + 1, VisualLocal + 1, VisualCumulated + 1);
    }

    public ActivityDepth MakeHidden() => new (Layer, ActualLocal, ActualCumulated, VisualLocal - 1, VisualCumulated - 1);

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

    public override string ToString() => this.Stringify();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ToTraceStateValue() =>
        string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}_{3}_{4}", Layer, ActualLocal, ActualCumulated, VisualLocal, VisualCumulated);
}
