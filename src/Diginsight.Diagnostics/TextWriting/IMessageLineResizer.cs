namespace Diginsight.Diagnostics.TextWriting;

internal interface IMessageLineResizer
{
    IEnumerable<string> Resize(IEnumerable<string> lines);
}
