namespace Diginsight.Stringify;

public sealed class AlreadySeenShortCircuit : ShortCircuit
{
    public object Subject { get; }
    public int DepthDelta { get; }

    public AlreadySeenShortCircuit(object subject, int depthDelta)
    {
        Subject = subject;
        DepthDelta = depthDelta;
    }
}
