namespace Diginsight.Strings;

public abstract class AllottingCounter
{
    public void Decrement()
    {
        if (!TryDecrement())
        {
            throw new MaxAllottedCountShortCircuit();
        }
    }

    protected abstract bool TryDecrement();
}
