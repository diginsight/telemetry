using System.Runtime.CompilerServices;

namespace Diginsight.Runtime;

/// <summary>
/// Represents the result of a heuristic size calculation.
/// </summary>
public readonly ref struct HeuristicSizeResult
{
    /// <summary>
    /// Gets the size.
    /// </summary>
    public long Sz { get; }

    /// <summary>
    /// Gets a value indicating whether the size is fixed.
    /// </summary>
    /// <remarks>
    /// A size is considered fixed if it is guaranteed to be the same for all instances of the object.
    /// </remarks>
    public bool Fxd { get; private init; }

    /// <summary>
    /// Gets the exception, if any, that occurred during the size calculation.
    /// </summary>
    public Exception? Exc { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HeuristicSizeResult" /> struct with the specified size and exception.
    /// </summary>
    /// <param name="sz">The size.</param>
    /// <param name="exc">The exception.</param>
    public HeuristicSizeResult(long sz, Exception exc)
        : this(sz, false, exc) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="HeuristicSizeResult" /> struct with the specified size and an optional fixed flag.
    /// </summary>
    /// <param name="sz">The size.</param>
    /// <param name="fxd">A value indicating whether the size is fixed.</param>
    public HeuristicSizeResult(long sz, bool fxd = false)
        : this(sz, fxd, null) { }

    private HeuristicSizeResult(long sz, bool fxd, Exception? exc)
    {
        Sz = sz;
        Fxd = fxd;
        Exc = exc;
    }

    private static long SafeAdd(long l1, long l2)
    {
        try
        {
            return checked(l1 + l2);
        }
        catch (OverflowException)
        {
            return long.MaxValue;
        }
    }

    /// <summary>
    /// Adds two <see cref="HeuristicSizeResult" /> instances.
    /// </summary>
    /// <remarks>
    /// The returned instance will be fixed only if both input instances are fixed.
    /// </remarks>
    /// <param name="r1">The first result.</param>
    /// <param name="r2">The second result.</param>
    /// <returns>The sum of the two results.</returns>
    public static HeuristicSizeResult operator +(HeuristicSizeResult r1, HeuristicSizeResult r2)
    {
        return new HeuristicSizeResult(
            SafeAdd(r1.Sz, r2.Sz),
            r1.Fxd && r2.Fxd,
            r1.Exc is { } exc1 ? r2.Exc is { } exc2 ? new AggregateException(exc1, exc2) : exc1 : r2.Exc
        );
    }

    /// <summary>
    /// Adds an exception to an <see cref="HeuristicSizeResult" />.
    /// </summary>
    /// <param name="r">The result.</param>
    /// <param name="e">The exception.</param>
    /// <returns>The result with the exception added.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HeuristicSizeResult operator +(HeuristicSizeResult r, Exception e) => new HeuristicSizeResult(0, e) + r;

    /// <summary>
    /// Clears the fixed flag of a <see cref="HeuristicSizeResult" />.
    /// </summary>
    /// <param name="r">The result.</param>
    /// <returns>The result with the fixed flag cleared.</returns>
    public static HeuristicSizeResult operator ~(HeuristicSizeResult r) => r.Fxd ? r with { Fxd = false } : r;
}
