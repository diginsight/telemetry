namespace Diginsight.Diagnostics.TextWriting;

public interface ILineTokenParser
{
    string TokenName { get; }

    ILineToken Parse(ReadOnlySpan<char> tokenDetailSpan);
}
