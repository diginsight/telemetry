#nullable enable
namespace Diginsight;

/// <summary>
///     Provides an equality comparer for tuples customizable with individual equality comparers for each component.
/// </summary>
/// <typeparam name="T1">The type of the 1st element of the tuple.</typeparam>
public class TupleEqualityComparer<T1>
    : IEqualityComparer<ValueTuple<T1>>, IEqualityComparer<Tuple<T1>>
{
    private readonly IEqualityComparer<T1> c1;

    /// <summary>
    /// Initializes a new instance of the <see cref="TupleEqualityComparer{T1}"/> class.
    /// </summary>
    /// <param name="c1">The equality comparer to use for the 1st element of the tuple.</param>
    public TupleEqualityComparer(
        IEqualityComparer<T1>? c1 = null
    )
    {
        this.c1 = c1 ?? EqualityComparer<T1>.Default;
    }

    /// <inheritdoc />
    public bool Equals(ValueTuple<T1> x, ValueTuple<T1> y)
    {
        return c1.Equals(x.Item1, y.Item1);
    }

    /// <inheritdoc />
    public int GetHashCode(ValueTuple<T1> obj)
    {
        T1 o1 = obj.Item1;
        HashCode hashCode = new();
        hashCode.Add(o1, c1);
        return hashCode.ToHashCode();
    }

    /// <inheritdoc />
    public bool Equals(Tuple<T1>? x, Tuple<T1>? y)
    {
        if (x is null && y is null)
        {
            return true;
        }
        if (x is null || y is null)
        {
            return false;
        }
        return c1.Equals(x.Item1, y.Item1);
    }

    /// <inheritdoc />
    public int GetHashCode(Tuple<T1> obj)
    {
        T1 o1 = obj.Item1;
        HashCode hashCode = new();
        hashCode.Add(o1, c1);
        return hashCode.ToHashCode();
    }
}

/// <summary>
///     Provides an equality comparer for tuples customizable with individual equality comparers for each component.
/// </summary>
/// <typeparam name="T1">The type of the 1st element of the tuple.</typeparam>
/// <typeparam name="T2">The type of the 2nd element of the tuple.</typeparam>
public class TupleEqualityComparer<T1, T2>
    : IEqualityComparer<(T1, T2)>, IEqualityComparer<Tuple<T1, T2>>
{
    private readonly IEqualityComparer<T1> c1;
    private readonly IEqualityComparer<T2> c2;

    /// <summary>
    /// Initializes a new instance of the <see cref="TupleEqualityComparer{T1, T2}"/> class.
    /// </summary>
    /// <param name="c1">The equality comparer to use for the 1st element of the tuple.</param>
    /// <param name="c2">The equality comparer to use for the 2nd element of the tuple.</param>
    public TupleEqualityComparer(
        IEqualityComparer<T1>? c1 = null,
        IEqualityComparer<T2>? c2 = null
    )
    {
        this.c1 = c1 ?? EqualityComparer<T1>.Default;
        this.c2 = c2 ?? EqualityComparer<T2>.Default;
    }

    /// <inheritdoc />
    public bool Equals((T1, T2) x, (T1, T2) y)
    {
        return c1.Equals(x.Item1, y.Item1)
            && c2.Equals(x.Item2, y.Item2);
    }

    /// <inheritdoc />
    public int GetHashCode((T1, T2) obj)
    {
        (T1 o1, T2 o2) = obj;
        HashCode hashCode = new();
        hashCode.Add(o1, c1);
        hashCode.Add(o2, c2);
        return hashCode.ToHashCode();
    }

    /// <inheritdoc />
    public bool Equals(Tuple<T1, T2>? x, Tuple<T1, T2>? y)
    {
        if (x is null && y is null)
        {
            return true;
        }
        if (x is null || y is null)
        {
            return false;
        }
        return c1.Equals(x.Item1, y.Item1)
            && c2.Equals(x.Item2, y.Item2);
    }

    /// <inheritdoc />
    public int GetHashCode(Tuple<T1, T2> obj)
    {
        (T1 o1, T2 o2) = obj;
        HashCode hashCode = new();
        hashCode.Add(o1, c1);
        hashCode.Add(o2, c2);
        return hashCode.ToHashCode();
    }
}

/// <summary>
///     Provides an equality comparer for tuples customizable with individual equality comparers for each component.
/// </summary>
/// <typeparam name="T1">The type of the 1st element of the tuple.</typeparam>
/// <typeparam name="T2">The type of the 2nd element of the tuple.</typeparam>
/// <typeparam name="T3">The type of the 3rd element of the tuple.</typeparam>
public class TupleEqualityComparer<T1, T2, T3>
    : IEqualityComparer<(T1, T2, T3)>, IEqualityComparer<Tuple<T1, T2, T3>>
{
    private readonly IEqualityComparer<T1> c1;
    private readonly IEqualityComparer<T2> c2;
    private readonly IEqualityComparer<T3> c3;

    /// <summary>
    /// Initializes a new instance of the <see cref="TupleEqualityComparer{T1, T2, T3}"/> class.
    /// </summary>
    /// <param name="c1">The equality comparer to use for the 1st element of the tuple.</param>
    /// <param name="c2">The equality comparer to use for the 2nd element of the tuple.</param>
    /// <param name="c3">The equality comparer to use for the 3rd element of the tuple.</param>
    public TupleEqualityComparer(
        IEqualityComparer<T1>? c1 = null,
        IEqualityComparer<T2>? c2 = null,
        IEqualityComparer<T3>? c3 = null
    )
    {
        this.c1 = c1 ?? EqualityComparer<T1>.Default;
        this.c2 = c2 ?? EqualityComparer<T2>.Default;
        this.c3 = c3 ?? EqualityComparer<T3>.Default;
    }

    /// <inheritdoc />
    public bool Equals((T1, T2, T3) x, (T1, T2, T3) y)
    {
        return c1.Equals(x.Item1, y.Item1)
            && c2.Equals(x.Item2, y.Item2)
            && c3.Equals(x.Item3, y.Item3);
    }

    /// <inheritdoc />
    public int GetHashCode((T1, T2, T3) obj)
    {
        (T1 o1, T2 o2, T3 o3) = obj;
        HashCode hashCode = new();
        hashCode.Add(o1, c1);
        hashCode.Add(o2, c2);
        hashCode.Add(o3, c3);
        return hashCode.ToHashCode();
    }

    /// <inheritdoc />
    public bool Equals(Tuple<T1, T2, T3>? x, Tuple<T1, T2, T3>? y)
    {
        if (x is null && y is null)
        {
            return true;
        }
        if (x is null || y is null)
        {
            return false;
        }
        return c1.Equals(x.Item1, y.Item1)
            && c2.Equals(x.Item2, y.Item2)
            && c3.Equals(x.Item3, y.Item3);
    }

    /// <inheritdoc />
    public int GetHashCode(Tuple<T1, T2, T3> obj)
    {
        (T1 o1, T2 o2, T3 o3) = obj;
        HashCode hashCode = new();
        hashCode.Add(o1, c1);
        hashCode.Add(o2, c2);
        hashCode.Add(o3, c3);
        return hashCode.ToHashCode();
    }
}

/// <summary>
///     Provides an equality comparer for tuples customizable with individual equality comparers for each component.
/// </summary>
/// <typeparam name="T1">The type of the 1st element of the tuple.</typeparam>
/// <typeparam name="T2">The type of the 2nd element of the tuple.</typeparam>
/// <typeparam name="T3">The type of the 3rd element of the tuple.</typeparam>
/// <typeparam name="T4">The type of the 4th element of the tuple.</typeparam>
public class TupleEqualityComparer<T1, T2, T3, T4>
    : IEqualityComparer<(T1, T2, T3, T4)>, IEqualityComparer<Tuple<T1, T2, T3, T4>>
{
    private readonly IEqualityComparer<T1> c1;
    private readonly IEqualityComparer<T2> c2;
    private readonly IEqualityComparer<T3> c3;
    private readonly IEqualityComparer<T4> c4;

    /// <summary>
    /// Initializes a new instance of the <see cref="TupleEqualityComparer{T1, T2, T3, T4}"/> class.
    /// </summary>
    /// <param name="c1">The equality comparer to use for the 1st element of the tuple.</param>
    /// <param name="c2">The equality comparer to use for the 2nd element of the tuple.</param>
    /// <param name="c3">The equality comparer to use for the 3rd element of the tuple.</param>
    /// <param name="c4">The equality comparer to use for the 4th element of the tuple.</param>
    public TupleEqualityComparer(
        IEqualityComparer<T1>? c1 = null,
        IEqualityComparer<T2>? c2 = null,
        IEqualityComparer<T3>? c3 = null,
        IEqualityComparer<T4>? c4 = null
    )
    {
        this.c1 = c1 ?? EqualityComparer<T1>.Default;
        this.c2 = c2 ?? EqualityComparer<T2>.Default;
        this.c3 = c3 ?? EqualityComparer<T3>.Default;
        this.c4 = c4 ?? EqualityComparer<T4>.Default;
    }

    /// <inheritdoc />
    public bool Equals((T1, T2, T3, T4) x, (T1, T2, T3, T4) y)
    {
        return c1.Equals(x.Item1, y.Item1)
            && c2.Equals(x.Item2, y.Item2)
            && c3.Equals(x.Item3, y.Item3)
            && c4.Equals(x.Item4, y.Item4);
    }

    /// <inheritdoc />
    public int GetHashCode((T1, T2, T3, T4) obj)
    {
        (T1 o1, T2 o2, T3 o3, T4 o4) = obj;
        HashCode hashCode = new();
        hashCode.Add(o1, c1);
        hashCode.Add(o2, c2);
        hashCode.Add(o3, c3);
        hashCode.Add(o4, c4);
        return hashCode.ToHashCode();
    }

    /// <inheritdoc />
    public bool Equals(Tuple<T1, T2, T3, T4>? x, Tuple<T1, T2, T3, T4>? y)
    {
        if (x is null && y is null)
        {
            return true;
        }
        if (x is null || y is null)
        {
            return false;
        }
        return c1.Equals(x.Item1, y.Item1)
            && c2.Equals(x.Item2, y.Item2)
            && c3.Equals(x.Item3, y.Item3)
            && c4.Equals(x.Item4, y.Item4);
    }

    /// <inheritdoc />
    public int GetHashCode(Tuple<T1, T2, T3, T4> obj)
    {
        (T1 o1, T2 o2, T3 o3, T4 o4) = obj;
        HashCode hashCode = new();
        hashCode.Add(o1, c1);
        hashCode.Add(o2, c2);
        hashCode.Add(o3, c3);
        hashCode.Add(o4, c4);
        return hashCode.ToHashCode();
    }
}

/// <summary>
///     Provides an equality comparer for tuples customizable with individual equality comparers for each component.
/// </summary>
/// <typeparam name="T1">The type of the 1st element of the tuple.</typeparam>
/// <typeparam name="T2">The type of the 2nd element of the tuple.</typeparam>
/// <typeparam name="T3">The type of the 3rd element of the tuple.</typeparam>
/// <typeparam name="T4">The type of the 4th element of the tuple.</typeparam>
/// <typeparam name="T5">The type of the 5th element of the tuple.</typeparam>
public class TupleEqualityComparer<T1, T2, T3, T4, T5>
    : IEqualityComparer<(T1, T2, T3, T4, T5)>, IEqualityComparer<Tuple<T1, T2, T3, T4, T5>>
{
    private readonly IEqualityComparer<T1> c1;
    private readonly IEqualityComparer<T2> c2;
    private readonly IEqualityComparer<T3> c3;
    private readonly IEqualityComparer<T4> c4;
    private readonly IEqualityComparer<T5> c5;

    /// <summary>
    /// Initializes a new instance of the <see cref="TupleEqualityComparer{T1, T2, T3, T4, T5}"/> class.
    /// </summary>
    /// <param name="c1">The equality comparer to use for the 1st element of the tuple.</param>
    /// <param name="c2">The equality comparer to use for the 2nd element of the tuple.</param>
    /// <param name="c3">The equality comparer to use for the 3rd element of the tuple.</param>
    /// <param name="c4">The equality comparer to use for the 4th element of the tuple.</param>
    /// <param name="c5">The equality comparer to use for the 5th element of the tuple.</param>
    public TupleEqualityComparer(
        IEqualityComparer<T1>? c1 = null,
        IEqualityComparer<T2>? c2 = null,
        IEqualityComparer<T3>? c3 = null,
        IEqualityComparer<T4>? c4 = null,
        IEqualityComparer<T5>? c5 = null
    )
    {
        this.c1 = c1 ?? EqualityComparer<T1>.Default;
        this.c2 = c2 ?? EqualityComparer<T2>.Default;
        this.c3 = c3 ?? EqualityComparer<T3>.Default;
        this.c4 = c4 ?? EqualityComparer<T4>.Default;
        this.c5 = c5 ?? EqualityComparer<T5>.Default;
    }

    /// <inheritdoc />
    public bool Equals((T1, T2, T3, T4, T5) x, (T1, T2, T3, T4, T5) y)
    {
        return c1.Equals(x.Item1, y.Item1)
            && c2.Equals(x.Item2, y.Item2)
            && c3.Equals(x.Item3, y.Item3)
            && c4.Equals(x.Item4, y.Item4)
            && c5.Equals(x.Item5, y.Item5);
    }

    /// <inheritdoc />
    public int GetHashCode((T1, T2, T3, T4, T5) obj)
    {
        (T1 o1, T2 o2, T3 o3, T4 o4, T5 o5) = obj;
        HashCode hashCode = new();
        hashCode.Add(o1, c1);
        hashCode.Add(o2, c2);
        hashCode.Add(o3, c3);
        hashCode.Add(o4, c4);
        hashCode.Add(o5, c5);
        return hashCode.ToHashCode();
    }

    /// <inheritdoc />
    public bool Equals(Tuple<T1, T2, T3, T4, T5>? x, Tuple<T1, T2, T3, T4, T5>? y)
    {
        if (x is null && y is null)
        {
            return true;
        }
        if (x is null || y is null)
        {
            return false;
        }
        return c1.Equals(x.Item1, y.Item1)
            && c2.Equals(x.Item2, y.Item2)
            && c3.Equals(x.Item3, y.Item3)
            && c4.Equals(x.Item4, y.Item4)
            && c5.Equals(x.Item5, y.Item5);
    }

    /// <inheritdoc />
    public int GetHashCode(Tuple<T1, T2, T3, T4, T5> obj)
    {
        (T1 o1, T2 o2, T3 o3, T4 o4, T5 o5) = obj;
        HashCode hashCode = new();
        hashCode.Add(o1, c1);
        hashCode.Add(o2, c2);
        hashCode.Add(o3, c3);
        hashCode.Add(o4, c4);
        hashCode.Add(o5, c5);
        return hashCode.ToHashCode();
    }
}

/// <summary>
///     Provides an equality comparer for tuples customizable with individual equality comparers for each component.
/// </summary>
/// <typeparam name="T1">The type of the 1st element of the tuple.</typeparam>
/// <typeparam name="T2">The type of the 2nd element of the tuple.</typeparam>
/// <typeparam name="T3">The type of the 3rd element of the tuple.</typeparam>
/// <typeparam name="T4">The type of the 4th element of the tuple.</typeparam>
/// <typeparam name="T5">The type of the 5th element of the tuple.</typeparam>
/// <typeparam name="T6">The type of the 6th element of the tuple.</typeparam>
public class TupleEqualityComparer<T1, T2, T3, T4, T5, T6>
    : IEqualityComparer<(T1, T2, T3, T4, T5, T6)>, IEqualityComparer<Tuple<T1, T2, T3, T4, T5, T6>>
{
    private readonly IEqualityComparer<T1> c1;
    private readonly IEqualityComparer<T2> c2;
    private readonly IEqualityComparer<T3> c3;
    private readonly IEqualityComparer<T4> c4;
    private readonly IEqualityComparer<T5> c5;
    private readonly IEqualityComparer<T6> c6;

    /// <summary>
    /// Initializes a new instance of the <see cref="TupleEqualityComparer{T1, T2, T3, T4, T5, T6}"/> class.
    /// </summary>
    /// <param name="c1">The equality comparer to use for the 1st element of the tuple.</param>
    /// <param name="c2">The equality comparer to use for the 2nd element of the tuple.</param>
    /// <param name="c3">The equality comparer to use for the 3rd element of the tuple.</param>
    /// <param name="c4">The equality comparer to use for the 4th element of the tuple.</param>
    /// <param name="c5">The equality comparer to use for the 5th element of the tuple.</param>
    /// <param name="c6">The equality comparer to use for the 6th element of the tuple.</param>
    public TupleEqualityComparer(
        IEqualityComparer<T1>? c1 = null,
        IEqualityComparer<T2>? c2 = null,
        IEqualityComparer<T3>? c3 = null,
        IEqualityComparer<T4>? c4 = null,
        IEqualityComparer<T5>? c5 = null,
        IEqualityComparer<T6>? c6 = null
    )
    {
        this.c1 = c1 ?? EqualityComparer<T1>.Default;
        this.c2 = c2 ?? EqualityComparer<T2>.Default;
        this.c3 = c3 ?? EqualityComparer<T3>.Default;
        this.c4 = c4 ?? EqualityComparer<T4>.Default;
        this.c5 = c5 ?? EqualityComparer<T5>.Default;
        this.c6 = c6 ?? EqualityComparer<T6>.Default;
    }

    /// <inheritdoc />
    public bool Equals((T1, T2, T3, T4, T5, T6) x, (T1, T2, T3, T4, T5, T6) y)
    {
        return c1.Equals(x.Item1, y.Item1)
            && c2.Equals(x.Item2, y.Item2)
            && c3.Equals(x.Item3, y.Item3)
            && c4.Equals(x.Item4, y.Item4)
            && c5.Equals(x.Item5, y.Item5)
            && c6.Equals(x.Item6, y.Item6);
    }

    /// <inheritdoc />
    public int GetHashCode((T1, T2, T3, T4, T5, T6) obj)
    {
        (T1 o1, T2 o2, T3 o3, T4 o4, T5 o5, T6 o6) = obj;
        HashCode hashCode = new();
        hashCode.Add(o1, c1);
        hashCode.Add(o2, c2);
        hashCode.Add(o3, c3);
        hashCode.Add(o4, c4);
        hashCode.Add(o5, c5);
        hashCode.Add(o6, c6);
        return hashCode.ToHashCode();
    }

    /// <inheritdoc />
    public bool Equals(Tuple<T1, T2, T3, T4, T5, T6>? x, Tuple<T1, T2, T3, T4, T5, T6>? y)
    {
        if (x is null && y is null)
        {
            return true;
        }
        if (x is null || y is null)
        {
            return false;
        }
        return c1.Equals(x.Item1, y.Item1)
            && c2.Equals(x.Item2, y.Item2)
            && c3.Equals(x.Item3, y.Item3)
            && c4.Equals(x.Item4, y.Item4)
            && c5.Equals(x.Item5, y.Item5)
            && c6.Equals(x.Item6, y.Item6);
    }

    /// <inheritdoc />
    public int GetHashCode(Tuple<T1, T2, T3, T4, T5, T6> obj)
    {
        (T1 o1, T2 o2, T3 o3, T4 o4, T5 o5, T6 o6) = obj;
        HashCode hashCode = new();
        hashCode.Add(o1, c1);
        hashCode.Add(o2, c2);
        hashCode.Add(o3, c3);
        hashCode.Add(o4, c4);
        hashCode.Add(o5, c5);
        hashCode.Add(o6, c6);
        return hashCode.ToHashCode();
    }
}

/// <summary>
///     Provides an equality comparer for tuples customizable with individual equality comparers for each component.
/// </summary>
/// <typeparam name="T1">The type of the 1st element of the tuple.</typeparam>
/// <typeparam name="T2">The type of the 2nd element of the tuple.</typeparam>
/// <typeparam name="T3">The type of the 3rd element of the tuple.</typeparam>
/// <typeparam name="T4">The type of the 4th element of the tuple.</typeparam>
/// <typeparam name="T5">The type of the 5th element of the tuple.</typeparam>
/// <typeparam name="T6">The type of the 6th element of the tuple.</typeparam>
/// <typeparam name="T7">The type of the 7th element of the tuple.</typeparam>
public class TupleEqualityComparer<T1, T2, T3, T4, T5, T6, T7>
    : IEqualityComparer<(T1, T2, T3, T4, T5, T6, T7)>, IEqualityComparer<Tuple<T1, T2, T3, T4, T5, T6, T7>>
{
    private readonly IEqualityComparer<T1> c1;
    private readonly IEqualityComparer<T2> c2;
    private readonly IEqualityComparer<T3> c3;
    private readonly IEqualityComparer<T4> c4;
    private readonly IEqualityComparer<T5> c5;
    private readonly IEqualityComparer<T6> c6;
    private readonly IEqualityComparer<T7> c7;

    /// <summary>
    /// Initializes a new instance of the <see cref="TupleEqualityComparer{T1, T2, T3, T4, T5, T6, T7}"/> class.
    /// </summary>
    /// <param name="c1">The equality comparer to use for the 1st element of the tuple.</param>
    /// <param name="c2">The equality comparer to use for the 2nd element of the tuple.</param>
    /// <param name="c3">The equality comparer to use for the 3rd element of the tuple.</param>
    /// <param name="c4">The equality comparer to use for the 4th element of the tuple.</param>
    /// <param name="c5">The equality comparer to use for the 5th element of the tuple.</param>
    /// <param name="c6">The equality comparer to use for the 6th element of the tuple.</param>
    /// <param name="c7">The equality comparer to use for the 7th element of the tuple.</param>
    public TupleEqualityComparer(
        IEqualityComparer<T1>? c1 = null,
        IEqualityComparer<T2>? c2 = null,
        IEqualityComparer<T3>? c3 = null,
        IEqualityComparer<T4>? c4 = null,
        IEqualityComparer<T5>? c5 = null,
        IEqualityComparer<T6>? c6 = null,
        IEqualityComparer<T7>? c7 = null
    )
    {
        this.c1 = c1 ?? EqualityComparer<T1>.Default;
        this.c2 = c2 ?? EqualityComparer<T2>.Default;
        this.c3 = c3 ?? EqualityComparer<T3>.Default;
        this.c4 = c4 ?? EqualityComparer<T4>.Default;
        this.c5 = c5 ?? EqualityComparer<T5>.Default;
        this.c6 = c6 ?? EqualityComparer<T6>.Default;
        this.c7 = c7 ?? EqualityComparer<T7>.Default;
    }

    /// <inheritdoc />
    public bool Equals((T1, T2, T3, T4, T5, T6, T7) x, (T1, T2, T3, T4, T5, T6, T7) y)
    {
        return c1.Equals(x.Item1, y.Item1)
            && c2.Equals(x.Item2, y.Item2)
            && c3.Equals(x.Item3, y.Item3)
            && c4.Equals(x.Item4, y.Item4)
            && c5.Equals(x.Item5, y.Item5)
            && c6.Equals(x.Item6, y.Item6)
            && c7.Equals(x.Item7, y.Item7);
    }

    /// <inheritdoc />
    public int GetHashCode((T1, T2, T3, T4, T5, T6, T7) obj)
    {
        (T1 o1, T2 o2, T3 o3, T4 o4, T5 o5, T6 o6, T7 o7) = obj;
        HashCode hashCode = new();
        hashCode.Add(o1, c1);
        hashCode.Add(o2, c2);
        hashCode.Add(o3, c3);
        hashCode.Add(o4, c4);
        hashCode.Add(o5, c5);
        hashCode.Add(o6, c6);
        hashCode.Add(o7, c7);
        return hashCode.ToHashCode();
    }

    /// <inheritdoc />
    public bool Equals(Tuple<T1, T2, T3, T4, T5, T6, T7>? x, Tuple<T1, T2, T3, T4, T5, T6, T7>? y)
    {
        if (x is null && y is null)
        {
            return true;
        }
        if (x is null || y is null)
        {
            return false;
        }
        return c1.Equals(x.Item1, y.Item1)
            && c2.Equals(x.Item2, y.Item2)
            && c3.Equals(x.Item3, y.Item3)
            && c4.Equals(x.Item4, y.Item4)
            && c5.Equals(x.Item5, y.Item5)
            && c6.Equals(x.Item6, y.Item6)
            && c7.Equals(x.Item7, y.Item7);
    }

    /// <inheritdoc />
    public int GetHashCode(Tuple<T1, T2, T3, T4, T5, T6, T7> obj)
    {
        (T1 o1, T2 o2, T3 o3, T4 o4, T5 o5, T6 o6, T7 o7) = obj;
        HashCode hashCode = new();
        hashCode.Add(o1, c1);
        hashCode.Add(o2, c2);
        hashCode.Add(o3, c3);
        hashCode.Add(o4, c4);
        hashCode.Add(o5, c5);
        hashCode.Add(o6, c6);
        hashCode.Add(o7, c7);
        return hashCode.ToHashCode();
    }
}

/// <summary>
///     Provides an equality comparer for tuples customizable with individual equality comparers for each component.
/// </summary>
/// <typeparam name="T1">The type of the 1st element of the tuple.</typeparam>
/// <typeparam name="T2">The type of the 2nd element of the tuple.</typeparam>
/// <typeparam name="T3">The type of the 3rd element of the tuple.</typeparam>
/// <typeparam name="T4">The type of the 4th element of the tuple.</typeparam>
/// <typeparam name="T5">The type of the 5th element of the tuple.</typeparam>
/// <typeparam name="T6">The type of the 6th element of the tuple.</typeparam>
/// <typeparam name="T7">The type of the 7th element of the tuple.</typeparam>
/// <typeparam name="T8">The type of the 8th element of the tuple.</typeparam>
public class TupleEqualityComparer<T1, T2, T3, T4, T5, T6, T7, T8>
    : IEqualityComparer<(T1, T2, T3, T4, T5, T6, T7, T8)>
{
    private readonly IEqualityComparer<T1> c1;
    private readonly IEqualityComparer<T2> c2;
    private readonly IEqualityComparer<T3> c3;
    private readonly IEqualityComparer<T4> c4;
    private readonly IEqualityComparer<T5> c5;
    private readonly IEqualityComparer<T6> c6;
    private readonly IEqualityComparer<T7> c7;
    private readonly IEqualityComparer<T8> c8;

    /// <summary>
    /// Initializes a new instance of the <see cref="TupleEqualityComparer{T1, T2, T3, T4, T5, T6, T7, T8}"/> class.
    /// </summary>
    /// <param name="c1">The equality comparer to use for the 1st element of the tuple.</param>
    /// <param name="c2">The equality comparer to use for the 2nd element of the tuple.</param>
    /// <param name="c3">The equality comparer to use for the 3rd element of the tuple.</param>
    /// <param name="c4">The equality comparer to use for the 4th element of the tuple.</param>
    /// <param name="c5">The equality comparer to use for the 5th element of the tuple.</param>
    /// <param name="c6">The equality comparer to use for the 6th element of the tuple.</param>
    /// <param name="c7">The equality comparer to use for the 7th element of the tuple.</param>
    /// <param name="c8">The equality comparer to use for the 8th element of the tuple.</param>
    public TupleEqualityComparer(
        IEqualityComparer<T1>? c1 = null,
        IEqualityComparer<T2>? c2 = null,
        IEqualityComparer<T3>? c3 = null,
        IEqualityComparer<T4>? c4 = null,
        IEqualityComparer<T5>? c5 = null,
        IEqualityComparer<T6>? c6 = null,
        IEqualityComparer<T7>? c7 = null,
        IEqualityComparer<T8>? c8 = null
    )
    {
        this.c1 = c1 ?? EqualityComparer<T1>.Default;
        this.c2 = c2 ?? EqualityComparer<T2>.Default;
        this.c3 = c3 ?? EqualityComparer<T3>.Default;
        this.c4 = c4 ?? EqualityComparer<T4>.Default;
        this.c5 = c5 ?? EqualityComparer<T5>.Default;
        this.c6 = c6 ?? EqualityComparer<T6>.Default;
        this.c7 = c7 ?? EqualityComparer<T7>.Default;
        this.c8 = c8 ?? EqualityComparer<T8>.Default;
    }

    /// <inheritdoc />
    public bool Equals((T1, T2, T3, T4, T5, T6, T7, T8) x, (T1, T2, T3, T4, T5, T6, T7, T8) y)
    {
        return c1.Equals(x.Item1, y.Item1)
            && c2.Equals(x.Item2, y.Item2)
            && c3.Equals(x.Item3, y.Item3)
            && c4.Equals(x.Item4, y.Item4)
            && c5.Equals(x.Item5, y.Item5)
            && c6.Equals(x.Item6, y.Item6)
            && c7.Equals(x.Item7, y.Item7)
            && c8.Equals(x.Item8, y.Item8);
    }

    /// <inheritdoc />
    public int GetHashCode((T1, T2, T3, T4, T5, T6, T7, T8) obj)
    {
        (T1 o1, T2 o2, T3 o3, T4 o4, T5 o5, T6 o6, T7 o7, T8 o8) = obj;
        HashCode hashCode = new();
        hashCode.Add(o1, c1);
        hashCode.Add(o2, c2);
        hashCode.Add(o3, c3);
        hashCode.Add(o4, c4);
        hashCode.Add(o5, c5);
        hashCode.Add(o6, c6);
        hashCode.Add(o7, c7);
        hashCode.Add(o8, c8);
        return hashCode.ToHashCode();
    }

}

/// <summary>
///     Provides an equality comparer for tuples customizable with individual equality comparers for each component.
/// </summary>
/// <typeparam name="T1">The type of the 1st element of the tuple.</typeparam>
/// <typeparam name="T2">The type of the 2nd element of the tuple.</typeparam>
/// <typeparam name="T3">The type of the 3rd element of the tuple.</typeparam>
/// <typeparam name="T4">The type of the 4th element of the tuple.</typeparam>
/// <typeparam name="T5">The type of the 5th element of the tuple.</typeparam>
/// <typeparam name="T6">The type of the 6th element of the tuple.</typeparam>
/// <typeparam name="T7">The type of the 7th element of the tuple.</typeparam>
/// <typeparam name="T8">The type of the 8th element of the tuple.</typeparam>
/// <typeparam name="T9">The type of the 9th element of the tuple.</typeparam>
public class TupleEqualityComparer<T1, T2, T3, T4, T5, T6, T7, T8, T9>
    : IEqualityComparer<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>
{
    private readonly IEqualityComparer<T1> c1;
    private readonly IEqualityComparer<T2> c2;
    private readonly IEqualityComparer<T3> c3;
    private readonly IEqualityComparer<T4> c4;
    private readonly IEqualityComparer<T5> c5;
    private readonly IEqualityComparer<T6> c6;
    private readonly IEqualityComparer<T7> c7;
    private readonly IEqualityComparer<T8> c8;
    private readonly IEqualityComparer<T9> c9;

    /// <summary>
    /// Initializes a new instance of the <see cref="TupleEqualityComparer{T1, T2, T3, T4, T5, T6, T7, T8, T9}"/> class.
    /// </summary>
    /// <param name="c1">The equality comparer to use for the 1st element of the tuple.</param>
    /// <param name="c2">The equality comparer to use for the 2nd element of the tuple.</param>
    /// <param name="c3">The equality comparer to use for the 3rd element of the tuple.</param>
    /// <param name="c4">The equality comparer to use for the 4th element of the tuple.</param>
    /// <param name="c5">The equality comparer to use for the 5th element of the tuple.</param>
    /// <param name="c6">The equality comparer to use for the 6th element of the tuple.</param>
    /// <param name="c7">The equality comparer to use for the 7th element of the tuple.</param>
    /// <param name="c8">The equality comparer to use for the 8th element of the tuple.</param>
    /// <param name="c9">The equality comparer to use for the 9th element of the tuple.</param>
    public TupleEqualityComparer(
        IEqualityComparer<T1>? c1 = null,
        IEqualityComparer<T2>? c2 = null,
        IEqualityComparer<T3>? c3 = null,
        IEqualityComparer<T4>? c4 = null,
        IEqualityComparer<T5>? c5 = null,
        IEqualityComparer<T6>? c6 = null,
        IEqualityComparer<T7>? c7 = null,
        IEqualityComparer<T8>? c8 = null,
        IEqualityComparer<T9>? c9 = null
    )
    {
        this.c1 = c1 ?? EqualityComparer<T1>.Default;
        this.c2 = c2 ?? EqualityComparer<T2>.Default;
        this.c3 = c3 ?? EqualityComparer<T3>.Default;
        this.c4 = c4 ?? EqualityComparer<T4>.Default;
        this.c5 = c5 ?? EqualityComparer<T5>.Default;
        this.c6 = c6 ?? EqualityComparer<T6>.Default;
        this.c7 = c7 ?? EqualityComparer<T7>.Default;
        this.c8 = c8 ?? EqualityComparer<T8>.Default;
        this.c9 = c9 ?? EqualityComparer<T9>.Default;
    }

    /// <inheritdoc />
    public bool Equals((T1, T2, T3, T4, T5, T6, T7, T8, T9) x, (T1, T2, T3, T4, T5, T6, T7, T8, T9) y)
    {
        return c1.Equals(x.Item1, y.Item1)
            && c2.Equals(x.Item2, y.Item2)
            && c3.Equals(x.Item3, y.Item3)
            && c4.Equals(x.Item4, y.Item4)
            && c5.Equals(x.Item5, y.Item5)
            && c6.Equals(x.Item6, y.Item6)
            && c7.Equals(x.Item7, y.Item7)
            && c8.Equals(x.Item8, y.Item8)
            && c9.Equals(x.Item9, y.Item9);
    }

    /// <inheritdoc />
    public int GetHashCode((T1, T2, T3, T4, T5, T6, T7, T8, T9) obj)
    {
        (T1 o1, T2 o2, T3 o3, T4 o4, T5 o5, T6 o6, T7 o7, T8 o8, T9 o9) = obj;
        HashCode hashCode = new();
        hashCode.Add(o1, c1);
        hashCode.Add(o2, c2);
        hashCode.Add(o3, c3);
        hashCode.Add(o4, c4);
        hashCode.Add(o5, c5);
        hashCode.Add(o6, c6);
        hashCode.Add(o7, c7);
        hashCode.Add(o8, c8);
        hashCode.Add(o9, c9);
        return hashCode.ToHashCode();
    }

}

/// <summary>
///     Provides an equality comparer for tuples customizable with individual equality comparers for each component.
/// </summary>
/// <typeparam name="T1">The type of the 1st element of the tuple.</typeparam>
/// <typeparam name="T2">The type of the 2nd element of the tuple.</typeparam>
/// <typeparam name="T3">The type of the 3rd element of the tuple.</typeparam>
/// <typeparam name="T4">The type of the 4th element of the tuple.</typeparam>
/// <typeparam name="T5">The type of the 5th element of the tuple.</typeparam>
/// <typeparam name="T6">The type of the 6th element of the tuple.</typeparam>
/// <typeparam name="T7">The type of the 7th element of the tuple.</typeparam>
/// <typeparam name="T8">The type of the 8th element of the tuple.</typeparam>
/// <typeparam name="T9">The type of the 9th element of the tuple.</typeparam>
/// <typeparam name="T10">The type of the 10th element of the tuple.</typeparam>
public class TupleEqualityComparer<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
    : IEqualityComparer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>
{
    private readonly IEqualityComparer<T1> c1;
    private readonly IEqualityComparer<T2> c2;
    private readonly IEqualityComparer<T3> c3;
    private readonly IEqualityComparer<T4> c4;
    private readonly IEqualityComparer<T5> c5;
    private readonly IEqualityComparer<T6> c6;
    private readonly IEqualityComparer<T7> c7;
    private readonly IEqualityComparer<T8> c8;
    private readonly IEqualityComparer<T9> c9;
    private readonly IEqualityComparer<T10> c10;

    /// <summary>
    /// Initializes a new instance of the <see cref="TupleEqualityComparer{T1, T2, T3, T4, T5, T6, T7, T8, T9, T10}"/> class.
    /// </summary>
    /// <param name="c1">The equality comparer to use for the 1st element of the tuple.</param>
    /// <param name="c2">The equality comparer to use for the 2nd element of the tuple.</param>
    /// <param name="c3">The equality comparer to use for the 3rd element of the tuple.</param>
    /// <param name="c4">The equality comparer to use for the 4th element of the tuple.</param>
    /// <param name="c5">The equality comparer to use for the 5th element of the tuple.</param>
    /// <param name="c6">The equality comparer to use for the 6th element of the tuple.</param>
    /// <param name="c7">The equality comparer to use for the 7th element of the tuple.</param>
    /// <param name="c8">The equality comparer to use for the 8th element of the tuple.</param>
    /// <param name="c9">The equality comparer to use for the 9th element of the tuple.</param>
    /// <param name="c10">The equality comparer to use for the 10th element of the tuple.</param>
    public TupleEqualityComparer(
        IEqualityComparer<T1>? c1 = null,
        IEqualityComparer<T2>? c2 = null,
        IEqualityComparer<T3>? c3 = null,
        IEqualityComparer<T4>? c4 = null,
        IEqualityComparer<T5>? c5 = null,
        IEqualityComparer<T6>? c6 = null,
        IEqualityComparer<T7>? c7 = null,
        IEqualityComparer<T8>? c8 = null,
        IEqualityComparer<T9>? c9 = null,
        IEqualityComparer<T10>? c10 = null
    )
    {
        this.c1 = c1 ?? EqualityComparer<T1>.Default;
        this.c2 = c2 ?? EqualityComparer<T2>.Default;
        this.c3 = c3 ?? EqualityComparer<T3>.Default;
        this.c4 = c4 ?? EqualityComparer<T4>.Default;
        this.c5 = c5 ?? EqualityComparer<T5>.Default;
        this.c6 = c6 ?? EqualityComparer<T6>.Default;
        this.c7 = c7 ?? EqualityComparer<T7>.Default;
        this.c8 = c8 ?? EqualityComparer<T8>.Default;
        this.c9 = c9 ?? EqualityComparer<T9>.Default;
        this.c10 = c10 ?? EqualityComparer<T10>.Default;
    }

    /// <inheritdoc />
    public bool Equals((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10) x, (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10) y)
    {
        return c1.Equals(x.Item1, y.Item1)
            && c2.Equals(x.Item2, y.Item2)
            && c3.Equals(x.Item3, y.Item3)
            && c4.Equals(x.Item4, y.Item4)
            && c5.Equals(x.Item5, y.Item5)
            && c6.Equals(x.Item6, y.Item6)
            && c7.Equals(x.Item7, y.Item7)
            && c8.Equals(x.Item8, y.Item8)
            && c9.Equals(x.Item9, y.Item9)
            && c10.Equals(x.Item10, y.Item10);
    }

    /// <inheritdoc />
    public int GetHashCode((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10) obj)
    {
        (T1 o1, T2 o2, T3 o3, T4 o4, T5 o5, T6 o6, T7 o7, T8 o8, T9 o9, T10 o10) = obj;
        HashCode hashCode = new();
        hashCode.Add(o1, c1);
        hashCode.Add(o2, c2);
        hashCode.Add(o3, c3);
        hashCode.Add(o4, c4);
        hashCode.Add(o5, c5);
        hashCode.Add(o6, c6);
        hashCode.Add(o7, c7);
        hashCode.Add(o8, c8);
        hashCode.Add(o9, c9);
        hashCode.Add(o10, c10);
        return hashCode.ToHashCode();
    }

}

/// <summary>
///     Provides an equality comparer for tuples customizable with individual equality comparers for each component.
/// </summary>
/// <typeparam name="T1">The type of the 1st element of the tuple.</typeparam>
/// <typeparam name="T2">The type of the 2nd element of the tuple.</typeparam>
/// <typeparam name="T3">The type of the 3rd element of the tuple.</typeparam>
/// <typeparam name="T4">The type of the 4th element of the tuple.</typeparam>
/// <typeparam name="T5">The type of the 5th element of the tuple.</typeparam>
/// <typeparam name="T6">The type of the 6th element of the tuple.</typeparam>
/// <typeparam name="T7">The type of the 7th element of the tuple.</typeparam>
/// <typeparam name="T8">The type of the 8th element of the tuple.</typeparam>
/// <typeparam name="T9">The type of the 9th element of the tuple.</typeparam>
/// <typeparam name="T10">The type of the 10th element of the tuple.</typeparam>
/// <typeparam name="T11">The type of the 11th element of the tuple.</typeparam>
public class TupleEqualityComparer<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
    : IEqualityComparer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>
{
    private readonly IEqualityComparer<T1> c1;
    private readonly IEqualityComparer<T2> c2;
    private readonly IEqualityComparer<T3> c3;
    private readonly IEqualityComparer<T4> c4;
    private readonly IEqualityComparer<T5> c5;
    private readonly IEqualityComparer<T6> c6;
    private readonly IEqualityComparer<T7> c7;
    private readonly IEqualityComparer<T8> c8;
    private readonly IEqualityComparer<T9> c9;
    private readonly IEqualityComparer<T10> c10;
    private readonly IEqualityComparer<T11> c11;

    /// <summary>
    /// Initializes a new instance of the <see cref="TupleEqualityComparer{T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11}"/> class.
    /// </summary>
    /// <param name="c1">The equality comparer to use for the 1st element of the tuple.</param>
    /// <param name="c2">The equality comparer to use for the 2nd element of the tuple.</param>
    /// <param name="c3">The equality comparer to use for the 3rd element of the tuple.</param>
    /// <param name="c4">The equality comparer to use for the 4th element of the tuple.</param>
    /// <param name="c5">The equality comparer to use for the 5th element of the tuple.</param>
    /// <param name="c6">The equality comparer to use for the 6th element of the tuple.</param>
    /// <param name="c7">The equality comparer to use for the 7th element of the tuple.</param>
    /// <param name="c8">The equality comparer to use for the 8th element of the tuple.</param>
    /// <param name="c9">The equality comparer to use for the 9th element of the tuple.</param>
    /// <param name="c10">The equality comparer to use for the 10th element of the tuple.</param>
    /// <param name="c11">The equality comparer to use for the 11th element of the tuple.</param>
    public TupleEqualityComparer(
        IEqualityComparer<T1>? c1 = null,
        IEqualityComparer<T2>? c2 = null,
        IEqualityComparer<T3>? c3 = null,
        IEqualityComparer<T4>? c4 = null,
        IEqualityComparer<T5>? c5 = null,
        IEqualityComparer<T6>? c6 = null,
        IEqualityComparer<T7>? c7 = null,
        IEqualityComparer<T8>? c8 = null,
        IEqualityComparer<T9>? c9 = null,
        IEqualityComparer<T10>? c10 = null,
        IEqualityComparer<T11>? c11 = null
    )
    {
        this.c1 = c1 ?? EqualityComparer<T1>.Default;
        this.c2 = c2 ?? EqualityComparer<T2>.Default;
        this.c3 = c3 ?? EqualityComparer<T3>.Default;
        this.c4 = c4 ?? EqualityComparer<T4>.Default;
        this.c5 = c5 ?? EqualityComparer<T5>.Default;
        this.c6 = c6 ?? EqualityComparer<T6>.Default;
        this.c7 = c7 ?? EqualityComparer<T7>.Default;
        this.c8 = c8 ?? EqualityComparer<T8>.Default;
        this.c9 = c9 ?? EqualityComparer<T9>.Default;
        this.c10 = c10 ?? EqualityComparer<T10>.Default;
        this.c11 = c11 ?? EqualityComparer<T11>.Default;
    }

    /// <inheritdoc />
    public bool Equals((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11) x, (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11) y)
    {
        return c1.Equals(x.Item1, y.Item1)
            && c2.Equals(x.Item2, y.Item2)
            && c3.Equals(x.Item3, y.Item3)
            && c4.Equals(x.Item4, y.Item4)
            && c5.Equals(x.Item5, y.Item5)
            && c6.Equals(x.Item6, y.Item6)
            && c7.Equals(x.Item7, y.Item7)
            && c8.Equals(x.Item8, y.Item8)
            && c9.Equals(x.Item9, y.Item9)
            && c10.Equals(x.Item10, y.Item10)
            && c11.Equals(x.Item11, y.Item11);
    }

    /// <inheritdoc />
    public int GetHashCode((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11) obj)
    {
        (T1 o1, T2 o2, T3 o3, T4 o4, T5 o5, T6 o6, T7 o7, T8 o8, T9 o9, T10 o10, T11 o11) = obj;
        HashCode hashCode = new();
        hashCode.Add(o1, c1);
        hashCode.Add(o2, c2);
        hashCode.Add(o3, c3);
        hashCode.Add(o4, c4);
        hashCode.Add(o5, c5);
        hashCode.Add(o6, c6);
        hashCode.Add(o7, c7);
        hashCode.Add(o8, c8);
        hashCode.Add(o9, c9);
        hashCode.Add(o10, c10);
        hashCode.Add(o11, c11);
        return hashCode.ToHashCode();
    }

}

/// <summary>
///     Provides an equality comparer for tuples customizable with individual equality comparers for each component.
/// </summary>
/// <typeparam name="T1">The type of the 1st element of the tuple.</typeparam>
/// <typeparam name="T2">The type of the 2nd element of the tuple.</typeparam>
/// <typeparam name="T3">The type of the 3rd element of the tuple.</typeparam>
/// <typeparam name="T4">The type of the 4th element of the tuple.</typeparam>
/// <typeparam name="T5">The type of the 5th element of the tuple.</typeparam>
/// <typeparam name="T6">The type of the 6th element of the tuple.</typeparam>
/// <typeparam name="T7">The type of the 7th element of the tuple.</typeparam>
/// <typeparam name="T8">The type of the 8th element of the tuple.</typeparam>
/// <typeparam name="T9">The type of the 9th element of the tuple.</typeparam>
/// <typeparam name="T10">The type of the 10th element of the tuple.</typeparam>
/// <typeparam name="T11">The type of the 11th element of the tuple.</typeparam>
/// <typeparam name="T12">The type of the 12th element of the tuple.</typeparam>
public class TupleEqualityComparer<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
    : IEqualityComparer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>
{
    private readonly IEqualityComparer<T1> c1;
    private readonly IEqualityComparer<T2> c2;
    private readonly IEqualityComparer<T3> c3;
    private readonly IEqualityComparer<T4> c4;
    private readonly IEqualityComparer<T5> c5;
    private readonly IEqualityComparer<T6> c6;
    private readonly IEqualityComparer<T7> c7;
    private readonly IEqualityComparer<T8> c8;
    private readonly IEqualityComparer<T9> c9;
    private readonly IEqualityComparer<T10> c10;
    private readonly IEqualityComparer<T11> c11;
    private readonly IEqualityComparer<T12> c12;

    /// <summary>
    /// Initializes a new instance of the <see cref="TupleEqualityComparer{T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12}"/> class.
    /// </summary>
    /// <param name="c1">The equality comparer to use for the 1st element of the tuple.</param>
    /// <param name="c2">The equality comparer to use for the 2nd element of the tuple.</param>
    /// <param name="c3">The equality comparer to use for the 3rd element of the tuple.</param>
    /// <param name="c4">The equality comparer to use for the 4th element of the tuple.</param>
    /// <param name="c5">The equality comparer to use for the 5th element of the tuple.</param>
    /// <param name="c6">The equality comparer to use for the 6th element of the tuple.</param>
    /// <param name="c7">The equality comparer to use for the 7th element of the tuple.</param>
    /// <param name="c8">The equality comparer to use for the 8th element of the tuple.</param>
    /// <param name="c9">The equality comparer to use for the 9th element of the tuple.</param>
    /// <param name="c10">The equality comparer to use for the 10th element of the tuple.</param>
    /// <param name="c11">The equality comparer to use for the 11th element of the tuple.</param>
    /// <param name="c12">The equality comparer to use for the 12th element of the tuple.</param>
    public TupleEqualityComparer(
        IEqualityComparer<T1>? c1 = null,
        IEqualityComparer<T2>? c2 = null,
        IEqualityComparer<T3>? c3 = null,
        IEqualityComparer<T4>? c4 = null,
        IEqualityComparer<T5>? c5 = null,
        IEqualityComparer<T6>? c6 = null,
        IEqualityComparer<T7>? c7 = null,
        IEqualityComparer<T8>? c8 = null,
        IEqualityComparer<T9>? c9 = null,
        IEqualityComparer<T10>? c10 = null,
        IEqualityComparer<T11>? c11 = null,
        IEqualityComparer<T12>? c12 = null
    )
    {
        this.c1 = c1 ?? EqualityComparer<T1>.Default;
        this.c2 = c2 ?? EqualityComparer<T2>.Default;
        this.c3 = c3 ?? EqualityComparer<T3>.Default;
        this.c4 = c4 ?? EqualityComparer<T4>.Default;
        this.c5 = c5 ?? EqualityComparer<T5>.Default;
        this.c6 = c6 ?? EqualityComparer<T6>.Default;
        this.c7 = c7 ?? EqualityComparer<T7>.Default;
        this.c8 = c8 ?? EqualityComparer<T8>.Default;
        this.c9 = c9 ?? EqualityComparer<T9>.Default;
        this.c10 = c10 ?? EqualityComparer<T10>.Default;
        this.c11 = c11 ?? EqualityComparer<T11>.Default;
        this.c12 = c12 ?? EqualityComparer<T12>.Default;
    }

    /// <inheritdoc />
    public bool Equals((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12) x, (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12) y)
    {
        return c1.Equals(x.Item1, y.Item1)
            && c2.Equals(x.Item2, y.Item2)
            && c3.Equals(x.Item3, y.Item3)
            && c4.Equals(x.Item4, y.Item4)
            && c5.Equals(x.Item5, y.Item5)
            && c6.Equals(x.Item6, y.Item6)
            && c7.Equals(x.Item7, y.Item7)
            && c8.Equals(x.Item8, y.Item8)
            && c9.Equals(x.Item9, y.Item9)
            && c10.Equals(x.Item10, y.Item10)
            && c11.Equals(x.Item11, y.Item11)
            && c12.Equals(x.Item12, y.Item12);
    }

    /// <inheritdoc />
    public int GetHashCode((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12) obj)
    {
        (T1 o1, T2 o2, T3 o3, T4 o4, T5 o5, T6 o6, T7 o7, T8 o8, T9 o9, T10 o10, T11 o11, T12 o12) = obj;
        HashCode hashCode = new();
        hashCode.Add(o1, c1);
        hashCode.Add(o2, c2);
        hashCode.Add(o3, c3);
        hashCode.Add(o4, c4);
        hashCode.Add(o5, c5);
        hashCode.Add(o6, c6);
        hashCode.Add(o7, c7);
        hashCode.Add(o8, c8);
        hashCode.Add(o9, c9);
        hashCode.Add(o10, c10);
        hashCode.Add(o11, c11);
        hashCode.Add(o12, c12);
        return hashCode.ToHashCode();
    }

}

/// <summary>
///     Provides an equality comparer for tuples customizable with individual equality comparers for each component.
/// </summary>
/// <typeparam name="T1">The type of the 1st element of the tuple.</typeparam>
/// <typeparam name="T2">The type of the 2nd element of the tuple.</typeparam>
/// <typeparam name="T3">The type of the 3rd element of the tuple.</typeparam>
/// <typeparam name="T4">The type of the 4th element of the tuple.</typeparam>
/// <typeparam name="T5">The type of the 5th element of the tuple.</typeparam>
/// <typeparam name="T6">The type of the 6th element of the tuple.</typeparam>
/// <typeparam name="T7">The type of the 7th element of the tuple.</typeparam>
/// <typeparam name="T8">The type of the 8th element of the tuple.</typeparam>
/// <typeparam name="T9">The type of the 9th element of the tuple.</typeparam>
/// <typeparam name="T10">The type of the 10th element of the tuple.</typeparam>
/// <typeparam name="T11">The type of the 11th element of the tuple.</typeparam>
/// <typeparam name="T12">The type of the 12th element of the tuple.</typeparam>
/// <typeparam name="T13">The type of the 13th element of the tuple.</typeparam>
public class TupleEqualityComparer<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>
    : IEqualityComparer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>
{
    private readonly IEqualityComparer<T1> c1;
    private readonly IEqualityComparer<T2> c2;
    private readonly IEqualityComparer<T3> c3;
    private readonly IEqualityComparer<T4> c4;
    private readonly IEqualityComparer<T5> c5;
    private readonly IEqualityComparer<T6> c6;
    private readonly IEqualityComparer<T7> c7;
    private readonly IEqualityComparer<T8> c8;
    private readonly IEqualityComparer<T9> c9;
    private readonly IEqualityComparer<T10> c10;
    private readonly IEqualityComparer<T11> c11;
    private readonly IEqualityComparer<T12> c12;
    private readonly IEqualityComparer<T13> c13;

    /// <summary>
    /// Initializes a new instance of the <see cref="TupleEqualityComparer{T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13}"/> class.
    /// </summary>
    /// <param name="c1">The equality comparer to use for the 1st element of the tuple.</param>
    /// <param name="c2">The equality comparer to use for the 2nd element of the tuple.</param>
    /// <param name="c3">The equality comparer to use for the 3rd element of the tuple.</param>
    /// <param name="c4">The equality comparer to use for the 4th element of the tuple.</param>
    /// <param name="c5">The equality comparer to use for the 5th element of the tuple.</param>
    /// <param name="c6">The equality comparer to use for the 6th element of the tuple.</param>
    /// <param name="c7">The equality comparer to use for the 7th element of the tuple.</param>
    /// <param name="c8">The equality comparer to use for the 8th element of the tuple.</param>
    /// <param name="c9">The equality comparer to use for the 9th element of the tuple.</param>
    /// <param name="c10">The equality comparer to use for the 10th element of the tuple.</param>
    /// <param name="c11">The equality comparer to use for the 11th element of the tuple.</param>
    /// <param name="c12">The equality comparer to use for the 12th element of the tuple.</param>
    /// <param name="c13">The equality comparer to use for the 13th element of the tuple.</param>
    public TupleEqualityComparer(
        IEqualityComparer<T1>? c1 = null,
        IEqualityComparer<T2>? c2 = null,
        IEqualityComparer<T3>? c3 = null,
        IEqualityComparer<T4>? c4 = null,
        IEqualityComparer<T5>? c5 = null,
        IEqualityComparer<T6>? c6 = null,
        IEqualityComparer<T7>? c7 = null,
        IEqualityComparer<T8>? c8 = null,
        IEqualityComparer<T9>? c9 = null,
        IEqualityComparer<T10>? c10 = null,
        IEqualityComparer<T11>? c11 = null,
        IEqualityComparer<T12>? c12 = null,
        IEqualityComparer<T13>? c13 = null
    )
    {
        this.c1 = c1 ?? EqualityComparer<T1>.Default;
        this.c2 = c2 ?? EqualityComparer<T2>.Default;
        this.c3 = c3 ?? EqualityComparer<T3>.Default;
        this.c4 = c4 ?? EqualityComparer<T4>.Default;
        this.c5 = c5 ?? EqualityComparer<T5>.Default;
        this.c6 = c6 ?? EqualityComparer<T6>.Default;
        this.c7 = c7 ?? EqualityComparer<T7>.Default;
        this.c8 = c8 ?? EqualityComparer<T8>.Default;
        this.c9 = c9 ?? EqualityComparer<T9>.Default;
        this.c10 = c10 ?? EqualityComparer<T10>.Default;
        this.c11 = c11 ?? EqualityComparer<T11>.Default;
        this.c12 = c12 ?? EqualityComparer<T12>.Default;
        this.c13 = c13 ?? EqualityComparer<T13>.Default;
    }

    /// <inheritdoc />
    public bool Equals((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13) x, (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13) y)
    {
        return c1.Equals(x.Item1, y.Item1)
            && c2.Equals(x.Item2, y.Item2)
            && c3.Equals(x.Item3, y.Item3)
            && c4.Equals(x.Item4, y.Item4)
            && c5.Equals(x.Item5, y.Item5)
            && c6.Equals(x.Item6, y.Item6)
            && c7.Equals(x.Item7, y.Item7)
            && c8.Equals(x.Item8, y.Item8)
            && c9.Equals(x.Item9, y.Item9)
            && c10.Equals(x.Item10, y.Item10)
            && c11.Equals(x.Item11, y.Item11)
            && c12.Equals(x.Item12, y.Item12)
            && c13.Equals(x.Item13, y.Item13);
    }

    /// <inheritdoc />
    public int GetHashCode((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13) obj)
    {
        (T1 o1, T2 o2, T3 o3, T4 o4, T5 o5, T6 o6, T7 o7, T8 o8, T9 o9, T10 o10, T11 o11, T12 o12, T13 o13) = obj;
        HashCode hashCode = new();
        hashCode.Add(o1, c1);
        hashCode.Add(o2, c2);
        hashCode.Add(o3, c3);
        hashCode.Add(o4, c4);
        hashCode.Add(o5, c5);
        hashCode.Add(o6, c6);
        hashCode.Add(o7, c7);
        hashCode.Add(o8, c8);
        hashCode.Add(o9, c9);
        hashCode.Add(o10, c10);
        hashCode.Add(o11, c11);
        hashCode.Add(o12, c12);
        hashCode.Add(o13, c13);
        return hashCode.ToHashCode();
    }

}

/// <summary>
///     Provides an equality comparer for tuples customizable with individual equality comparers for each component.
/// </summary>
/// <typeparam name="T1">The type of the 1st element of the tuple.</typeparam>
/// <typeparam name="T2">The type of the 2nd element of the tuple.</typeparam>
/// <typeparam name="T3">The type of the 3rd element of the tuple.</typeparam>
/// <typeparam name="T4">The type of the 4th element of the tuple.</typeparam>
/// <typeparam name="T5">The type of the 5th element of the tuple.</typeparam>
/// <typeparam name="T6">The type of the 6th element of the tuple.</typeparam>
/// <typeparam name="T7">The type of the 7th element of the tuple.</typeparam>
/// <typeparam name="T8">The type of the 8th element of the tuple.</typeparam>
/// <typeparam name="T9">The type of the 9th element of the tuple.</typeparam>
/// <typeparam name="T10">The type of the 10th element of the tuple.</typeparam>
/// <typeparam name="T11">The type of the 11th element of the tuple.</typeparam>
/// <typeparam name="T12">The type of the 12th element of the tuple.</typeparam>
/// <typeparam name="T13">The type of the 13th element of the tuple.</typeparam>
/// <typeparam name="T14">The type of the 14th element of the tuple.</typeparam>
public class TupleEqualityComparer<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>
    : IEqualityComparer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>
{
    private readonly IEqualityComparer<T1> c1;
    private readonly IEqualityComparer<T2> c2;
    private readonly IEqualityComparer<T3> c3;
    private readonly IEqualityComparer<T4> c4;
    private readonly IEqualityComparer<T5> c5;
    private readonly IEqualityComparer<T6> c6;
    private readonly IEqualityComparer<T7> c7;
    private readonly IEqualityComparer<T8> c8;
    private readonly IEqualityComparer<T9> c9;
    private readonly IEqualityComparer<T10> c10;
    private readonly IEqualityComparer<T11> c11;
    private readonly IEqualityComparer<T12> c12;
    private readonly IEqualityComparer<T13> c13;
    private readonly IEqualityComparer<T14> c14;

    /// <summary>
    /// Initializes a new instance of the <see cref="TupleEqualityComparer{T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14}"/> class.
    /// </summary>
    /// <param name="c1">The equality comparer to use for the 1st element of the tuple.</param>
    /// <param name="c2">The equality comparer to use for the 2nd element of the tuple.</param>
    /// <param name="c3">The equality comparer to use for the 3rd element of the tuple.</param>
    /// <param name="c4">The equality comparer to use for the 4th element of the tuple.</param>
    /// <param name="c5">The equality comparer to use for the 5th element of the tuple.</param>
    /// <param name="c6">The equality comparer to use for the 6th element of the tuple.</param>
    /// <param name="c7">The equality comparer to use for the 7th element of the tuple.</param>
    /// <param name="c8">The equality comparer to use for the 8th element of the tuple.</param>
    /// <param name="c9">The equality comparer to use for the 9th element of the tuple.</param>
    /// <param name="c10">The equality comparer to use for the 10th element of the tuple.</param>
    /// <param name="c11">The equality comparer to use for the 11th element of the tuple.</param>
    /// <param name="c12">The equality comparer to use for the 12th element of the tuple.</param>
    /// <param name="c13">The equality comparer to use for the 13th element of the tuple.</param>
    /// <param name="c14">The equality comparer to use for the 14th element of the tuple.</param>
    public TupleEqualityComparer(
        IEqualityComparer<T1>? c1 = null,
        IEqualityComparer<T2>? c2 = null,
        IEqualityComparer<T3>? c3 = null,
        IEqualityComparer<T4>? c4 = null,
        IEqualityComparer<T5>? c5 = null,
        IEqualityComparer<T6>? c6 = null,
        IEqualityComparer<T7>? c7 = null,
        IEqualityComparer<T8>? c8 = null,
        IEqualityComparer<T9>? c9 = null,
        IEqualityComparer<T10>? c10 = null,
        IEqualityComparer<T11>? c11 = null,
        IEqualityComparer<T12>? c12 = null,
        IEqualityComparer<T13>? c13 = null,
        IEqualityComparer<T14>? c14 = null
    )
    {
        this.c1 = c1 ?? EqualityComparer<T1>.Default;
        this.c2 = c2 ?? EqualityComparer<T2>.Default;
        this.c3 = c3 ?? EqualityComparer<T3>.Default;
        this.c4 = c4 ?? EqualityComparer<T4>.Default;
        this.c5 = c5 ?? EqualityComparer<T5>.Default;
        this.c6 = c6 ?? EqualityComparer<T6>.Default;
        this.c7 = c7 ?? EqualityComparer<T7>.Default;
        this.c8 = c8 ?? EqualityComparer<T8>.Default;
        this.c9 = c9 ?? EqualityComparer<T9>.Default;
        this.c10 = c10 ?? EqualityComparer<T10>.Default;
        this.c11 = c11 ?? EqualityComparer<T11>.Default;
        this.c12 = c12 ?? EqualityComparer<T12>.Default;
        this.c13 = c13 ?? EqualityComparer<T13>.Default;
        this.c14 = c14 ?? EqualityComparer<T14>.Default;
    }

    /// <inheritdoc />
    public bool Equals((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14) x, (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14) y)
    {
        return c1.Equals(x.Item1, y.Item1)
            && c2.Equals(x.Item2, y.Item2)
            && c3.Equals(x.Item3, y.Item3)
            && c4.Equals(x.Item4, y.Item4)
            && c5.Equals(x.Item5, y.Item5)
            && c6.Equals(x.Item6, y.Item6)
            && c7.Equals(x.Item7, y.Item7)
            && c8.Equals(x.Item8, y.Item8)
            && c9.Equals(x.Item9, y.Item9)
            && c10.Equals(x.Item10, y.Item10)
            && c11.Equals(x.Item11, y.Item11)
            && c12.Equals(x.Item12, y.Item12)
            && c13.Equals(x.Item13, y.Item13)
            && c14.Equals(x.Item14, y.Item14);
    }

    /// <inheritdoc />
    public int GetHashCode((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14) obj)
    {
        (T1 o1, T2 o2, T3 o3, T4 o4, T5 o5, T6 o6, T7 o7, T8 o8, T9 o9, T10 o10, T11 o11, T12 o12, T13 o13, T14 o14) = obj;
        HashCode hashCode = new();
        hashCode.Add(o1, c1);
        hashCode.Add(o2, c2);
        hashCode.Add(o3, c3);
        hashCode.Add(o4, c4);
        hashCode.Add(o5, c5);
        hashCode.Add(o6, c6);
        hashCode.Add(o7, c7);
        hashCode.Add(o8, c8);
        hashCode.Add(o9, c9);
        hashCode.Add(o10, c10);
        hashCode.Add(o11, c11);
        hashCode.Add(o12, c12);
        hashCode.Add(o13, c13);
        hashCode.Add(o14, c14);
        return hashCode.ToHashCode();
    }

}

