namespace Diginsight.Diagnostics.TextWriting;

internal sealed class NoopMessageLineResizer : IMessageLineResizer
{
    public static readonly IMessageLineResizer Instance = new NoopMessageLineResizer();

    private NoopMessageLineResizer() { }

    public IEnumerable<string> Resize(IEnumerable<string> lines) => lines;
}
