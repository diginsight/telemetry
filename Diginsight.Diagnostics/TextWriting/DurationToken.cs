namespace Diginsight.Diagnostics.TextWriting;

public sealed class DurationToken : ILineToken
{
    public static readonly ILineToken Instance = new DurationToken();

    private DurationToken() { }
}
