namespace Diginsight.Strings;

public sealed class AlreadySeenShortCircuit : ShortCircuit
{
    public int DepthDelta { get; }

    public AlreadySeenShortCircuit(int depthDelta)
    {
        DepthDelta = depthDelta;
    }
}
