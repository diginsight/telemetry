using System.Runtime.CompilerServices;

namespace Diginsight.Equality;

public sealed class AttributedEqualityComparer : IEqualityComparer<object>
{
    public static readonly IEqualityComparer<object> Instance = new AttributedEqualityComparer();

    private AttributedEqualityComparer() { }

    public bool Equals(object? o1, object? o2)
    {
        if (ReferenceEquals(o1, o2))
        {
            return true;
        }
        if (o1 is null || o2 is null)
        {
            return false;
        }

        EqualityMode m = GetEqualityMode(o1);
        if (m != GetEqualityMode(o2))
        {
            throw new ArgumentException("Equality modes are different");
        }

        if (m == EqualityMode.Reference)
        {
            return false;
        }

        if (TryByEquatable(o1, o2, m, nameof(o1)) && TryByEquatable(o2, o1, m, nameof(o2)))
        {
            return true;
        }

        if (m == EqualityMode.Default)
        {
            return EqualityComparer<object>.Default.Equals(o1, o2);
        }

        throw new NotImplementedException();
    }

    private static EqualityMode GetEqualityMode(object o)
    {
        throw new NotImplementedException();
    }

    private static bool TryByEquatable(object o1, object o2, EqualityMode m, string n1)
    {
        IReadOnlyDictionary<Type, Func<object, bool>> eed = GetEquatableEquators(o1);
        if (eed.Count > 0)
        {
            if (m != EqualityMode.Default)
            {
                throw new ArgumentException($"Object implements {nameof(IEquatable<object>)}<> but equality mode is not {nameof(EqualityMode.Default)}", n1);
            }

            if (eed.Where(x => x.Key.IsInstanceOfType(o2)).Select(static x => x.Value).Any(x => x(o2)))
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyDictionary<Type, Func<object, bool>> GetEquatableEquators(object o1)
    {
        return o1.GetType().GetGenericArgumentsAs(typeof(IEquatable<>))
            .Select(static t => t[0])
            .ToDictionary(
                static t => t,
                t => new Func<object, bool>(
                    o2 => t.IsInstanceOfType(o2) &&
                        (bool)typeof(IEquatable<>).MakeGenericType(t).GetMethod(nameof(IEquatable<object>.Equals))!.Invoke(o1, [ o2 ])!
                )
            );
    }

    public int GetHashCode(object obj)
    {
        throw new NotImplementedException();
    }
}
