namespace Diginsight.Diagnostics.TextWriting;

public sealed class DeltaToken : ILineToken
{
    public static readonly ILineToken Instance = new DeltaToken();

    private DeltaToken() { }
}
