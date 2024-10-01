namespace Diginsight.Stringify;

public abstract class AllottedCounter
{
    public static AllottedCounter Unlimited => UnlimitedAllottedCounter.Instance;

    public void Decrement()
    {
        if (!TryDecrement())
        {
            throw new MaxAllottedCountShortCircuit();
        }
    }

    public abstract bool TryDecrement();

    public static AllottedCounter Count(int? max)
    {
        return max is { } max0 ? new LimitedAllottedCounter(max0) : Unlimited;
    }

    private sealed class UnlimitedAllottedCounter : AllottedCounter
    {
        public static readonly AllottedCounter Instance = new UnlimitedAllottedCounter();

        private UnlimitedAllottedCounter() { }

        public override bool TryDecrement() => true;
    }

    private sealed class LimitedAllottedCounter : AllottedCounter
    {
        private int current;

        public LimitedAllottedCounter(int max)
        {
            current = max;
        }

        public override bool TryDecrement() => --current >= 0;
    }
}
