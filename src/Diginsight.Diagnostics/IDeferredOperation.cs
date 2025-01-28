namespace Diginsight.Diagnostics;

public interface IDeferredOperation
{
    bool IsFlushable { get; }

    void Flush();

    void Discard();
}
