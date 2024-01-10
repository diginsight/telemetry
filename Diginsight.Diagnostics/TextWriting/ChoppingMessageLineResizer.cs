namespace Diginsight.Diagnostics.TextWriting;

internal sealed class ChoppingMessageLineResizer : IMessageLineResizer
{
    private readonly int maxLength;

    public ChoppingMessageLineResizer(int maxLength)
    {
        this.maxLength = maxLength;
    }

    public IEnumerable<string> Resize(IEnumerable<string> lines)
    {
        return lines.Select(
            l => l.Length <= maxLength
                ? l
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                : $"{l[..(maxLength - 1)]}…"
#else
                : $"{l.Substring(0, maxLength - 1)}…"
#endif
        );
    }
}
