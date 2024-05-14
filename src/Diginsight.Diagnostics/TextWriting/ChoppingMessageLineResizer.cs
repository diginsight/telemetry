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
        return lines.Select(l => l.Length <= maxLength ? l : $"{l[..(maxLength - 1)]}…");
    }
}
