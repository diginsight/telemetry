namespace Diginsight.Diagnostics.TextWriting;

[Flags]
public enum DepthTokenModes
{
    Layer = 1 << 0,
    Local = 1 << 1,
    Cumulated = 1 << 2,
}
