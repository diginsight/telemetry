namespace Diginsight.Strings;

public abstract class AllottingCounter
{
    public void Decrement()
    {
        if (!TryDecrement())
        {
            throw new MaxAllottedItemsShortCircuit();
        }
    }

    protected abstract bool TryDecrement();
}
