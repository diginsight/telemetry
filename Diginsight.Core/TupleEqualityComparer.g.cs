namespace Diginsight;

public class TupleEqualityComparer<T1>
    : IEqualityComparer<ValueTuple<T1>>, IEqualityComparer<Tuple<T1>>
{
    private readonly IEqualityComparer<T1> c1;

    public TupleEqualityComparer(
        IEqualityComparer<T1> c1 = null
    )
    {
        this.c1 = c1 ?? EqualityComparer<T1>.Default;
    }

    public bool Equals(ValueTuple<T1> x, ValueTuple<T1> y)
    {
        return c1.Equals(x.Item1, y.Item1);
    }

    public int GetHashCode(ValueTuple<T1> obj)
    {
        T1 o1 = obj.Item1;
        HashCode hashCode = new();
        hashCode.Add(o1, c1);
        return hashCode.ToHashCode();
    }

    public bool Equals(Tuple<T1> x, Tuple<T1> y)
    {
        return c1.Equals(x.Item1, y.Item1);
    }

    public int GetHashCode(Tuple<T1> obj)
    {
        T1 o1 = obj.Item1;
        HashCode hashCode = new();
        hashCode.Add(o1, c1);
        return hashCode.ToHashCode();
    }
}

public class TupleEqualityComparer<T1, T2>
    : IEqualityComparer<(T1, T2)>, IEqualityComparer<Tuple<T1, T2>>
{
    private readonly IEqualityComparer<T1> c1;
    private readonly IEqualityComparer<T2> c2;

    public TupleEqualityComparer(
        IEqualityComparer<T1> c1 = null, IEqualityComparer<T2> c2 = null
    )
    {
        this.c1 = c1 ?? EqualityComparer<T1>.Default;
        this.c2 = c2 ?? EqualityComparer<T2>.Default;
    }

    public bool Equals((T1, T2) x, (T1, T2) y)
    {
        return c1.Equals(x.Item1, y.Item1) && c2.Equals(x.Item2, y.Item2);
    }

    public int GetHashCode((T1, T2) obj)
    {
        (T1 o1, T2 o2) = obj;
        HashCode hashCode = new();
        hashCode.Add(o1, c1);
        hashCode.Add(o2, c2);
        return hashCode.ToHashCode();
    }

    public bool Equals(Tuple<T1, T2> x, Tuple<T1, T2> y)
    {
        return c1.Equals(x.Item1, y.Item1) && c2.Equals(x.Item2, y.Item2);
    }

    public int GetHashCode(Tuple<T1, T2> obj)
    {
        (T1 o1, T2 o2) = obj;
        HashCode hashCode = new();
        hashCode.Add(o1, c1);
        hashCode.Add(o2, c2);
        return hashCode.ToHashCode();
    }
}

public class TupleEqualityComparer<T1, T2, T3>
    : IEqualityComparer<(T1, T2, T3)>, IEqualityComparer<Tuple<T1, T2, T3>>
{
    private readonly IEqualityComparer<T1> c1;
    private readonly IEqualityComparer<T2> c2;
    private readonly IEqualityComparer<T3> c3;

    public TupleEqualityComparer(
        IEqualityComparer<T1> c1 = null, IEqualityComparer<T2> c2 = null, IEqualityComparer<T3> c3 = null
    )
    {
        this.c1 = c1 ?? EqualityComparer<T1>.Default;
        this.c2 = c2 ?? EqualityComparer<T2>.Default;
        this.c3 = c3 ?? EqualityComparer<T3>.Default;
    }

    public bool Equals((T1, T2, T3) x, (T1, T2, T3) y)
    {
        return c1.Equals(x.Item1, y.Item1) && c2.Equals(x.Item2, y.Item2) && c3.Equals(x.Item3, y.Item3);
    }

    public int GetHashCode((T1, T2, T3) obj)
    {
        (T1 o1, T2 o2, T3 o3) = obj;
        HashCode hashCode = new();
        hashCode.Add(o1, c1);
        hashCode.Add(o2, c2);
        hashCode.Add(o3, c3);
        return hashCode.ToHashCode();
    }

    public bool Equals(Tuple<T1, T2, T3> x, Tuple<T1, T2, T3> y)
    {
        return c1.Equals(x.Item1, y.Item1) && c2.Equals(x.Item2, y.Item2) && c3.Equals(x.Item3, y.Item3);
    }

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

public class TupleEqualityComparer<T1, T2, T3, T4>
    : IEqualityComparer<(T1, T2, T3, T4)>, IEqualityComparer<Tuple<T1, T2, T3, T4>>
{
    private readonly IEqualityComparer<T1> c1;
    private readonly IEqualityComparer<T2> c2;
    private readonly IEqualityComparer<T3> c3;
    private readonly IEqualityComparer<T4> c4;

    public TupleEqualityComparer(
        IEqualityComparer<T1> c1 = null, IEqualityComparer<T2> c2 = null, IEqualityComparer<T3> c3 = null, IEqualityComparer<T4> c4 = null
    )
    {
        this.c1 = c1 ?? EqualityComparer<T1>.Default;
        this.c2 = c2 ?? EqualityComparer<T2>.Default;
        this.c3 = c3 ?? EqualityComparer<T3>.Default;
        this.c4 = c4 ?? EqualityComparer<T4>.Default;
    }

    public bool Equals((T1, T2, T3, T4) x, (T1, T2, T3, T4) y)
    {
        return c1.Equals(x.Item1, y.Item1) && c2.Equals(x.Item2, y.Item2) && c3.Equals(x.Item3, y.Item3) && c4.Equals(x.Item4, y.Item4);
    }

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

    public bool Equals(Tuple<T1, T2, T3, T4> x, Tuple<T1, T2, T3, T4> y)
    {
        return c1.Equals(x.Item1, y.Item1) && c2.Equals(x.Item2, y.Item2) && c3.Equals(x.Item3, y.Item3) && c4.Equals(x.Item4, y.Item4);
    }

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

public class TupleEqualityComparer<T1, T2, T3, T4, T5>
    : IEqualityComparer<(T1, T2, T3, T4, T5)>, IEqualityComparer<Tuple<T1, T2, T3, T4, T5>>
{
    private readonly IEqualityComparer<T1> c1;
    private readonly IEqualityComparer<T2> c2;
    private readonly IEqualityComparer<T3> c3;
    private readonly IEqualityComparer<T4> c4;
    private readonly IEqualityComparer<T5> c5;

    public TupleEqualityComparer(
        IEqualityComparer<T1> c1 = null, IEqualityComparer<T2> c2 = null, IEqualityComparer<T3> c3 = null, IEqualityComparer<T4> c4 = null, IEqualityComparer<T5> c5 = null
    )
    {
        this.c1 = c1 ?? EqualityComparer<T1>.Default;
        this.c2 = c2 ?? EqualityComparer<T2>.Default;
        this.c3 = c3 ?? EqualityComparer<T3>.Default;
        this.c4 = c4 ?? EqualityComparer<T4>.Default;
        this.c5 = c5 ?? EqualityComparer<T5>.Default;
    }

    public bool Equals((T1, T2, T3, T4, T5) x, (T1, T2, T3, T4, T5) y)
    {
        return c1.Equals(x.Item1, y.Item1) && c2.Equals(x.Item2, y.Item2) && c3.Equals(x.Item3, y.Item3) && c4.Equals(x.Item4, y.Item4) && c5.Equals(x.Item5, y.Item5);
    }

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

    public bool Equals(Tuple<T1, T2, T3, T4, T5> x, Tuple<T1, T2, T3, T4, T5> y)
    {
        return c1.Equals(x.Item1, y.Item1) && c2.Equals(x.Item2, y.Item2) && c3.Equals(x.Item3, y.Item3) && c4.Equals(x.Item4, y.Item4) && c5.Equals(x.Item5, y.Item5);
    }

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

public class TupleEqualityComparer<T1, T2, T3, T4, T5, T6>
    : IEqualityComparer<(T1, T2, T3, T4, T5, T6)>, IEqualityComparer<Tuple<T1, T2, T3, T4, T5, T6>>
{
    private readonly IEqualityComparer<T1> c1;
    private readonly IEqualityComparer<T2> c2;
    private readonly IEqualityComparer<T3> c3;
    private readonly IEqualityComparer<T4> c4;
    private readonly IEqualityComparer<T5> c5;
    private readonly IEqualityComparer<T6> c6;

    public TupleEqualityComparer(
        IEqualityComparer<T1> c1 = null, IEqualityComparer<T2> c2 = null, IEqualityComparer<T3> c3 = null, IEqualityComparer<T4> c4 = null, IEqualityComparer<T5> c5 = null, IEqualityComparer<T6> c6 = null
    )
    {
        this.c1 = c1 ?? EqualityComparer<T1>.Default;
        this.c2 = c2 ?? EqualityComparer<T2>.Default;
        this.c3 = c3 ?? EqualityComparer<T3>.Default;
        this.c4 = c4 ?? EqualityComparer<T4>.Default;
        this.c5 = c5 ?? EqualityComparer<T5>.Default;
        this.c6 = c6 ?? EqualityComparer<T6>.Default;
    }

    public bool Equals((T1, T2, T3, T4, T5, T6) x, (T1, T2, T3, T4, T5, T6) y)
    {
        return c1.Equals(x.Item1, y.Item1) && c2.Equals(x.Item2, y.Item2) && c3.Equals(x.Item3, y.Item3) && c4.Equals(x.Item4, y.Item4) && c5.Equals(x.Item5, y.Item5) && c6.Equals(x.Item6, y.Item6);
    }

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

    public bool Equals(Tuple<T1, T2, T3, T4, T5, T6> x, Tuple<T1, T2, T3, T4, T5, T6> y)
    {
        return c1.Equals(x.Item1, y.Item1) && c2.Equals(x.Item2, y.Item2) && c3.Equals(x.Item3, y.Item3) && c4.Equals(x.Item4, y.Item4) && c5.Equals(x.Item5, y.Item5) && c6.Equals(x.Item6, y.Item6);
    }

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

    public TupleEqualityComparer(
        IEqualityComparer<T1> c1 = null, IEqualityComparer<T2> c2 = null, IEqualityComparer<T3> c3 = null, IEqualityComparer<T4> c4 = null, IEqualityComparer<T5> c5 = null, IEqualityComparer<T6> c6 = null, IEqualityComparer<T7> c7 = null
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

    public bool Equals((T1, T2, T3, T4, T5, T6, T7) x, (T1, T2, T3, T4, T5, T6, T7) y)
    {
        return c1.Equals(x.Item1, y.Item1) && c2.Equals(x.Item2, y.Item2) && c3.Equals(x.Item3, y.Item3) && c4.Equals(x.Item4, y.Item4) && c5.Equals(x.Item5, y.Item5) && c6.Equals(x.Item6, y.Item6) && c7.Equals(x.Item7, y.Item7);
    }

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

    public bool Equals(Tuple<T1, T2, T3, T4, T5, T6, T7> x, Tuple<T1, T2, T3, T4, T5, T6, T7> y)
    {
        return c1.Equals(x.Item1, y.Item1) && c2.Equals(x.Item2, y.Item2) && c3.Equals(x.Item3, y.Item3) && c4.Equals(x.Item4, y.Item4) && c5.Equals(x.Item5, y.Item5) && c6.Equals(x.Item6, y.Item6) && c7.Equals(x.Item7, y.Item7);
    }

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

    public TupleEqualityComparer(
        IEqualityComparer<T1> c1 = null, IEqualityComparer<T2> c2 = null, IEqualityComparer<T3> c3 = null, IEqualityComparer<T4> c4 = null, IEqualityComparer<T5> c5 = null, IEqualityComparer<T6> c6 = null, IEqualityComparer<T7> c7 = null, IEqualityComparer<T8> c8 = null
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

    public bool Equals((T1, T2, T3, T4, T5, T6, T7, T8) x, (T1, T2, T3, T4, T5, T6, T7, T8) y)
    {
        return c1.Equals(x.Item1, y.Item1) && c2.Equals(x.Item2, y.Item2) && c3.Equals(x.Item3, y.Item3) && c4.Equals(x.Item4, y.Item4) && c5.Equals(x.Item5, y.Item5) && c6.Equals(x.Item6, y.Item6) && c7.Equals(x.Item7, y.Item7) && c8.Equals(x.Item8, y.Item8);
    }

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

    public TupleEqualityComparer(
        IEqualityComparer<T1> c1 = null, IEqualityComparer<T2> c2 = null, IEqualityComparer<T3> c3 = null, IEqualityComparer<T4> c4 = null, IEqualityComparer<T5> c5 = null, IEqualityComparer<T6> c6 = null, IEqualityComparer<T7> c7 = null, IEqualityComparer<T8> c8 = null, IEqualityComparer<T9> c9 = null
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

    public bool Equals((T1, T2, T3, T4, T5, T6, T7, T8, T9) x, (T1, T2, T3, T4, T5, T6, T7, T8, T9) y)
    {
        return c1.Equals(x.Item1, y.Item1) && c2.Equals(x.Item2, y.Item2) && c3.Equals(x.Item3, y.Item3) && c4.Equals(x.Item4, y.Item4) && c5.Equals(x.Item5, y.Item5) && c6.Equals(x.Item6, y.Item6) && c7.Equals(x.Item7, y.Item7) && c8.Equals(x.Item8, y.Item8) && c9.Equals(x.Item9, y.Item9);
    }

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

    public TupleEqualityComparer(
        IEqualityComparer<T1> c1 = null, IEqualityComparer<T2> c2 = null, IEqualityComparer<T3> c3 = null, IEqualityComparer<T4> c4 = null, IEqualityComparer<T5> c5 = null, IEqualityComparer<T6> c6 = null, IEqualityComparer<T7> c7 = null, IEqualityComparer<T8> c8 = null, IEqualityComparer<T9> c9 = null, IEqualityComparer<T10> c10 = null
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

    public bool Equals((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10) x, (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10) y)
    {
        return c1.Equals(x.Item1, y.Item1) && c2.Equals(x.Item2, y.Item2) && c3.Equals(x.Item3, y.Item3) && c4.Equals(x.Item4, y.Item4) && c5.Equals(x.Item5, y.Item5) && c6.Equals(x.Item6, y.Item6) && c7.Equals(x.Item7, y.Item7) && c8.Equals(x.Item8, y.Item8) && c9.Equals(x.Item9, y.Item9) && c10.Equals(x.Item10, y.Item10);
    }

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

    public TupleEqualityComparer(
        IEqualityComparer<T1> c1 = null, IEqualityComparer<T2> c2 = null, IEqualityComparer<T3> c3 = null, IEqualityComparer<T4> c4 = null, IEqualityComparer<T5> c5 = null, IEqualityComparer<T6> c6 = null, IEqualityComparer<T7> c7 = null, IEqualityComparer<T8> c8 = null, IEqualityComparer<T9> c9 = null, IEqualityComparer<T10> c10 = null, IEqualityComparer<T11> c11 = null
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

    public bool Equals((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11) x, (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11) y)
    {
        return c1.Equals(x.Item1, y.Item1) && c2.Equals(x.Item2, y.Item2) && c3.Equals(x.Item3, y.Item3) && c4.Equals(x.Item4, y.Item4) && c5.Equals(x.Item5, y.Item5) && c6.Equals(x.Item6, y.Item6) && c7.Equals(x.Item7, y.Item7) && c8.Equals(x.Item8, y.Item8) && c9.Equals(x.Item9, y.Item9) && c10.Equals(x.Item10, y.Item10) && c11.Equals(x.Item11, y.Item11);
    }

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

    public TupleEqualityComparer(
        IEqualityComparer<T1> c1 = null, IEqualityComparer<T2> c2 = null, IEqualityComparer<T3> c3 = null, IEqualityComparer<T4> c4 = null, IEqualityComparer<T5> c5 = null, IEqualityComparer<T6> c6 = null, IEqualityComparer<T7> c7 = null, IEqualityComparer<T8> c8 = null, IEqualityComparer<T9> c9 = null, IEqualityComparer<T10> c10 = null, IEqualityComparer<T11> c11 = null, IEqualityComparer<T12> c12 = null
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

    public bool Equals((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12) x, (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12) y)
    {
        return c1.Equals(x.Item1, y.Item1) && c2.Equals(x.Item2, y.Item2) && c3.Equals(x.Item3, y.Item3) && c4.Equals(x.Item4, y.Item4) && c5.Equals(x.Item5, y.Item5) && c6.Equals(x.Item6, y.Item6) && c7.Equals(x.Item7, y.Item7) && c8.Equals(x.Item8, y.Item8) && c9.Equals(x.Item9, y.Item9) && c10.Equals(x.Item10, y.Item10) && c11.Equals(x.Item11, y.Item11) && c12.Equals(x.Item12, y.Item12);
    }

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

    public TupleEqualityComparer(
        IEqualityComparer<T1> c1 = null, IEqualityComparer<T2> c2 = null, IEqualityComparer<T3> c3 = null, IEqualityComparer<T4> c4 = null, IEqualityComparer<T5> c5 = null, IEqualityComparer<T6> c6 = null, IEqualityComparer<T7> c7 = null, IEqualityComparer<T8> c8 = null, IEqualityComparer<T9> c9 = null, IEqualityComparer<T10> c10 = null, IEqualityComparer<T11> c11 = null, IEqualityComparer<T12> c12 = null, IEqualityComparer<T13> c13 = null
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

    public bool Equals((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13) x, (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13) y)
    {
        return c1.Equals(x.Item1, y.Item1) && c2.Equals(x.Item2, y.Item2) && c3.Equals(x.Item3, y.Item3) && c4.Equals(x.Item4, y.Item4) && c5.Equals(x.Item5, y.Item5) && c6.Equals(x.Item6, y.Item6) && c7.Equals(x.Item7, y.Item7) && c8.Equals(x.Item8, y.Item8) && c9.Equals(x.Item9, y.Item9) && c10.Equals(x.Item10, y.Item10) && c11.Equals(x.Item11, y.Item11) && c12.Equals(x.Item12, y.Item12) && c13.Equals(x.Item13, y.Item13);
    }

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

    public TupleEqualityComparer(
        IEqualityComparer<T1> c1 = null, IEqualityComparer<T2> c2 = null, IEqualityComparer<T3> c3 = null, IEqualityComparer<T4> c4 = null, IEqualityComparer<T5> c5 = null, IEqualityComparer<T6> c6 = null, IEqualityComparer<T7> c7 = null, IEqualityComparer<T8> c8 = null, IEqualityComparer<T9> c9 = null, IEqualityComparer<T10> c10 = null, IEqualityComparer<T11> c11 = null, IEqualityComparer<T12> c12 = null, IEqualityComparer<T13> c13 = null, IEqualityComparer<T14> c14 = null
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

    public bool Equals((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14) x, (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14) y)
    {
        return c1.Equals(x.Item1, y.Item1) && c2.Equals(x.Item2, y.Item2) && c3.Equals(x.Item3, y.Item3) && c4.Equals(x.Item4, y.Item4) && c5.Equals(x.Item5, y.Item5) && c6.Equals(x.Item6, y.Item6) && c7.Equals(x.Item7, y.Item7) && c8.Equals(x.Item8, y.Item8) && c9.Equals(x.Item9, y.Item9) && c10.Equals(x.Item10, y.Item10) && c11.Equals(x.Item11, y.Item11) && c12.Equals(x.Item12, y.Item12) && c13.Equals(x.Item13, y.Item13) && c14.Equals(x.Item14, y.Item14);
    }

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

public class TupleEqualityComparer<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
    : IEqualityComparer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>
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
    private readonly IEqualityComparer<T15> c15;

    public TupleEqualityComparer(
        IEqualityComparer<T1> c1 = null, IEqualityComparer<T2> c2 = null, IEqualityComparer<T3> c3 = null, IEqualityComparer<T4> c4 = null, IEqualityComparer<T5> c5 = null, IEqualityComparer<T6> c6 = null, IEqualityComparer<T7> c7 = null, IEqualityComparer<T8> c8 = null, IEqualityComparer<T9> c9 = null, IEqualityComparer<T10> c10 = null, IEqualityComparer<T11> c11 = null, IEqualityComparer<T12> c12 = null, IEqualityComparer<T13> c13 = null, IEqualityComparer<T14> c14 = null, IEqualityComparer<T15> c15 = null
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
        this.c15 = c15 ?? EqualityComparer<T15>.Default;
    }

    public bool Equals((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15) x, (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15) y)
    {
        return c1.Equals(x.Item1, y.Item1) && c2.Equals(x.Item2, y.Item2) && c3.Equals(x.Item3, y.Item3) && c4.Equals(x.Item4, y.Item4) && c5.Equals(x.Item5, y.Item5) && c6.Equals(x.Item6, y.Item6) && c7.Equals(x.Item7, y.Item7) && c8.Equals(x.Item8, y.Item8) && c9.Equals(x.Item9, y.Item9) && c10.Equals(x.Item10, y.Item10) && c11.Equals(x.Item11, y.Item11) && c12.Equals(x.Item12, y.Item12) && c13.Equals(x.Item13, y.Item13) && c14.Equals(x.Item14, y.Item14) && c15.Equals(x.Item15, y.Item15);
    }

    public int GetHashCode((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15) obj)
    {
        (T1 o1, T2 o2, T3 o3, T4 o4, T5 o5, T6 o6, T7 o7, T8 o8, T9 o9, T10 o10, T11 o11, T12 o12, T13 o13, T14 o14, T15 o15) = obj;
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
        hashCode.Add(o15, c15);
        return hashCode.ToHashCode();
    }

}

public class TupleEqualityComparer<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>
    : IEqualityComparer<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16)>
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
    private readonly IEqualityComparer<T15> c15;
    private readonly IEqualityComparer<T16> c16;

    public TupleEqualityComparer(
        IEqualityComparer<T1> c1 = null, IEqualityComparer<T2> c2 = null, IEqualityComparer<T3> c3 = null, IEqualityComparer<T4> c4 = null, IEqualityComparer<T5> c5 = null, IEqualityComparer<T6> c6 = null, IEqualityComparer<T7> c7 = null, IEqualityComparer<T8> c8 = null, IEqualityComparer<T9> c9 = null, IEqualityComparer<T10> c10 = null, IEqualityComparer<T11> c11 = null, IEqualityComparer<T12> c12 = null, IEqualityComparer<T13> c13 = null, IEqualityComparer<T14> c14 = null, IEqualityComparer<T15> c15 = null, IEqualityComparer<T16> c16 = null
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
        this.c15 = c15 ?? EqualityComparer<T15>.Default;
        this.c16 = c16 ?? EqualityComparer<T16>.Default;
    }

    public bool Equals((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16) x, (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16) y)
    {
        return c1.Equals(x.Item1, y.Item1) && c2.Equals(x.Item2, y.Item2) && c3.Equals(x.Item3, y.Item3) && c4.Equals(x.Item4, y.Item4) && c5.Equals(x.Item5, y.Item5) && c6.Equals(x.Item6, y.Item6) && c7.Equals(x.Item7, y.Item7) && c8.Equals(x.Item8, y.Item8) && c9.Equals(x.Item9, y.Item9) && c10.Equals(x.Item10, y.Item10) && c11.Equals(x.Item11, y.Item11) && c12.Equals(x.Item12, y.Item12) && c13.Equals(x.Item13, y.Item13) && c14.Equals(x.Item14, y.Item14) && c15.Equals(x.Item15, y.Item15) && c16.Equals(x.Item16, y.Item16);
    }

    public int GetHashCode((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16) obj)
    {
        (T1 o1, T2 o2, T3 o3, T4 o4, T5 o5, T6 o6, T7 o7, T8 o8, T9 o9, T10 o10, T11 o11, T12 o12, T13 o13, T14 o14, T15 o15, T16 o16) = obj;
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
        hashCode.Add(o15, c15);
        hashCode.Add(o16, c16);
        return hashCode.ToHashCode();
    }

}

