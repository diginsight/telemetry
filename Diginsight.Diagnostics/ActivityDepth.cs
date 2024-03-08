using System.Globalization;

namespace Diginsight.Diagnostics;

public readonly struct ActivityDepth
{
    public int Layer { get; }
    public int Local { get; }
    public int Cumulated { get; }

    private ActivityDepth(int layer, int local, int cumulated)
    {
        Layer = layer;
        Local = local;
        Cumulated = cumulated;
    }

    public ActivityDepth GetChild(bool newLayer = false)
    {
        return newLayer || Layer == 0 ? new (Layer + 1, 1, Cumulated + 1) : new (Layer, Local + 1, Cumulated + 1);
    }

    public override string ToString() => string.Format(CultureInfo.InvariantCulture, "{0}.{1}", Layer, Local);
}
