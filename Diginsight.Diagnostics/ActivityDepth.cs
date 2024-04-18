using System.Globalization;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

public readonly struct ActivityDepth : IFormattable
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString() => ToString(null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ToTraceStateValue() => ToString("T");

    public string ToString(string? format)
    {
        return format?.ToUpperInvariant() switch
        {
            null or "G" or "YL" => string.Format(CultureInfo.InvariantCulture, "{0}.{1}", Layer, Local),
            "L" => Layer.ToString(CultureInfo.InvariantCulture),
            "YLC" => string.Format(CultureInfo.InvariantCulture, "{0}.{1}.{2}", Layer, Local, Cumulated),
            "T" => string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}", Layer, Local, Cumulated),
            _ => throw new FormatException($"The '{format}' format string is not supported."),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString(format);
}
