namespace Diginsight.Strings;

public abstract class AllottingCounter
{
    public static AllottingCounter Unlimited => UnlimitedAllottingCounter.Instance;

    public void Decrement()
    {
        if (!TryDecrement())
        {
            throw new MaxAllottedCountShortCircuit();
        }
    }

    public abstract bool TryDecrement();

    public static AllottingCounter Count(int? max)
    {
        return max is { } max0 ? new LimitedAllottingCounter(max0) : Unlimited;
    }

    private sealed class UnlimitedAllottingCounter : AllottingCounter
    {
        public static readonly AllottingCounter Instance = new UnlimitedAllottingCounter();

        private UnlimitedAllottingCounter() { }

        public override bool TryDecrement() => true;
    }

    private sealed class LimitedAllottingCounter : AllottingCounter
    {
        private int current;

        public LimitedAllottingCounter(int max)
        {
            current = max;
        }

        public override bool TryDecrement() => --current >= 0;
    }
}
