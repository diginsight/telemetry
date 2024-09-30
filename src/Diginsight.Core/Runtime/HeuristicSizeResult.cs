using System.Runtime.CompilerServices;

namespace Diginsight.Runtime;

public readonly ref struct HeuristicSizeResult
{
    public long Sz { get; }
    public bool Fxd { get; private init; }
    public Exception? Exc { get; }

    public HeuristicSizeResult(long sz, Exception exc)
        : this(sz, false, exc) { }

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

    public static HeuristicSizeResult operator +(HeuristicSizeResult r1, HeuristicSizeResult r2)
    {
        return new HeuristicSizeResult(
            SafeAdd(r1.Sz, r2.Sz),
            r1.Fxd && r2.Fxd,
            r1.Exc is { } exc1 ? r2.Exc is { } exc2 ? new AggregateException(exc1, exc2) : exc1 : r2.Exc
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HeuristicSizeResult operator +(HeuristicSizeResult r, Exception e) => new HeuristicSizeResult(0, e) + r;

    public static HeuristicSizeResult operator ~(HeuristicSizeResult r) => r.Fxd ? r with { Fxd = false } : r;
}
