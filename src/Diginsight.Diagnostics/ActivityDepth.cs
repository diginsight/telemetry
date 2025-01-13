using System.Globalization;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

public readonly struct ActivityDepth : IFormattable
{
    public static readonly TraceStateKey DepthTraceStateKey = "diginsightdepth";

    public int Layer { get; }
    public int Local { get; }
    public int Cumulated { get; }
    public int VizLocal { get; }
    public int VizCumulated { get; }

    private ActivityDepth(int layer, int local, int cumulated)
        : this(layer, local, cumulated, local, cumulated) { }

    private ActivityDepth(int layer, int local, int cumulated, int vizLocal, int vizCumulated)
    {
        Layer = layer;
        Local = local;
        Cumulated = cumulated;
        VizLocal = vizLocal;
        VizCumulated = vizCumulated;
    }

    public ActivityDepth MakeRemoteChild()
    {
        return new ActivityDepth(Layer + 1, 1, Cumulated + 1, 1, VizCumulated + 1);
    }

    // TODO Add 'hidden' parameter
    public ActivityDepth MakeLocalChild()
    {
        return Layer == 0
            ? new ActivityDepth(1, 1, 1)
            : new ActivityDepth(Layer, Local + 1, Cumulated + 1, VizLocal + 1, VizCumulated + 1);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString() => ToString(null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ToTraceStateValue() => ToString("T");

    public string ToString(string? format)
    {
        return format?.ToUpperInvariant() switch
        {
            null or "G" or "YL" or "YLV" => string.Format(CultureInfo.InvariantCulture, "{0}.{1}", Layer, VizLocal),
            "YLA" => string.Format(CultureInfo.InvariantCulture, "{0}.{1}", Layer, Local),
            "L" or "LV" => VizLocal.ToString(CultureInfo.InvariantCulture),
            "LA" => Local.ToString(CultureInfo.InvariantCulture),
            "YLC" or "YLCV" => string.Format(CultureInfo.InvariantCulture, "{0}.{1}.{2}", Layer, VizLocal, VizCumulated),
            "YLCA" => string.Format(CultureInfo.InvariantCulture, "{0}.{1}.{2}", Layer, Local, Cumulated),
            "T" => string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}", Layer, Local, Cumulated),
            _ => throw new FormatException($"The '{format}' format string is not supported."),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString(format);
}
