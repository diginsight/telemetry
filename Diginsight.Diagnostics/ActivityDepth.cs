using System.Globalization;

namespace Diginsight.Diagnostics;

public readonly struct ActivityDepth
{
    public static readonly TraceStateKey DepthTraceStateKey = "diginsightdepth";

    public int Layer { get; }
    public int Local { get; }
    public int Cumulated { get; }

    private ActivityDepth(int layer, int local, int cumulated)
    {
        Layer = layer;
        Local = local;
        Cumulated = cumulated;
    }

    public ActivityDepth MakeChild(bool remote)
    {
        return remote || Layer == 0
            ? new ActivityDepth(Layer + 1, 1, Cumulated + 1)
            : new ActivityDepth(Layer, Local + 1, Cumulated + 1);
    }

    public static ActivityDepth? FromTraceStateValue(string? traceStateValue)
    {
        return traceStateValue?.Split('_') is [ var rawLayer, var rawLocal, var rawCumulated ]
            && int.TryParse(rawLayer, out int layer)
            && int.TryParse(rawLocal, out int local)
            && int.TryParse(rawCumulated, out int cumulated)
                ? new ActivityDepth(layer, local, cumulated)
                : null;
    }

    public override string ToString() => string.Format(CultureInfo.InvariantCulture, "{0}.{1}", Layer, Local);

    public string ToTraceStateValue() => string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}", Layer, Local, Cumulated);
}
