namespace Diginsight.Diagnostics.TextWriting;

internal sealed class DepthTokenParser : ILineTokenParser
{
    public string TokenName => "depth";

    public ILineToken Parse(ReadOnlySpan<char> tokenDetailSpan)
    {
        DepthTokenModes modes;
        if (tokenDetailSpan.IsEmpty)
        {
            modes = 0;
        }
        else if (tokenDetailSpan[0] == '|')
        {
            string src = tokenDetailSpan[1..].ToString().ToUpperInvariant();
            modes = src switch
            {
                "L" => DepthTokenModes.Local,
                "YL" => DepthTokenModes.Layer | DepthTokenModes.Local,
                "LC" => DepthTokenModes.Local | DepthTokenModes.Cumulated,
                "YLC" => DepthTokenModes.Layer | DepthTokenModes.Local | DepthTokenModes.Cumulated,
                _ => throw new FormatException("Expected 'L', 'YL', 'LC' or 'YLC' (Y = layer, L = local, C = cumulated)"),
            };
        }
        else
        {
            throw new FormatException("Expected '|' or nothing");
        }

        return new DepthToken() { Modes = modes };
    }
}
