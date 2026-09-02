namespace Diginsight.Stringify;

/// <summary>
/// Represents a counter that tracks how many stringification items may still be appended.
/// </summary>
public abstract class AllottedCounter
{
    /// <summary>
    /// Gets an allotted counter that never reaches a count limit.
    /// </summary>
    public static AllottedCounter Unlimited => UnlimitedAllottedCounter.Instance;

    /// <summary>
    /// Decrements the allotted counter.
    /// </summary>
    /// <exception cref="MaxAllottedCountShortCircuit">Thrown when the counter cannot be decremented.</exception>
    public void Decrement()
    {
        if (!TryDecrement())
        {
            throw new MaxAllottedCountShortCircuit();
        }
    }

    /// <summary>
    /// Attempts to decrement the allotted counter.
    /// </summary>
    /// <returns><c>true</c> if the counter was decremented successfully; otherwise, <c>false</c>.</returns>
    public abstract bool TryDecrement();

    /// <summary>
    /// Creates an allotted counter for the specified maximum count.
    /// </summary>
    /// <param name="max">The maximum count.</param>
    /// <returns>The allotted counter.</returns>
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
