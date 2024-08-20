using System.Diagnostics;
using System.Reflection;
using ArgumentException = System.ArgumentException;

namespace Diginsight.Equality;

public sealed class AttributedEqualityComparer : IEqualityComparer<object>
{
    private readonly IEqualityTypeContractAccessor contractAccessor = null!;

    public new bool Equals(object? obj1, object? obj2)
    {
        if (ReferenceEquals(obj1, obj2))
        {
            return true;
        }

        if (obj1 is null || obj2 is null)
        {
            return false;
        }

        Type type1 = obj1.GetType();
        Type type2 = obj2.GetType();

        IEquatableObjectDescriptor FindTypeDescriptor(Type type, string paramName)
        {
            foreach (Type t in type.GetClosure())
            {
                if (contractAccessor.TryGet(t) is { Behavior: not null } typeContract)
                {
                    return typeContract;
                }

                EquatableObjectAttribute[] attributes = t.GetCustomAttributes<EquatableObjectAttribute>().Take(2).ToArray();
                switch (attributes)
                {
                    case [ ]:
                        break;

                    case [ var attribute ]:
                        return attribute;

                    case [ _, _ ]:
                        throw new ArgumentException($"Multiple {nameof(EquatableObjectAttribute)}s applied to type {t}", paramName);
                }
            }

            return EqualityTypeContract.Empty;
        }

        IEquatableObjectDescriptor eod1 = FindTypeDescriptor(type1, nameof(obj1));
        IEquatableObjectDescriptor eod2 = FindTypeDescriptor(type2, nameof(obj2));

        EqualityBehavior b1 = eod1.Behavior;
        EqualityBehavior b2 = eod2.Behavior;
        if (b1 != b2)
        {
            throw new ArgumentException($"Inputs have different equality behaviors ({b1:G} vs {b2:G})");
        }

        switch (b1)
        {
            case EqualityBehavior.Attributed:
                throw new NotImplementedException();

            case EqualityBehavior.Comparer:
            {
                IComparerEquatableObjectDescriptor ceod1 = (IComparerEquatableObjectDescriptor)eod1;
                IComparerEquatableObjectDescriptor ceod2 = (IComparerEquatableObjectDescriptor)eod2;

                throw new NotImplementedException();
            }

            case EqualityBehavior.Default:
                return obj1.Equals(obj2);

            case EqualityBehavior.Forbidden:
            case EqualityBehavior.Identity:
                return false;

            case EqualityBehavior.Proxy:
            {
                IProxyEquatableObjectDescriptor peod1 = (IProxyEquatableObjectDescriptor)eod1;
                IProxyEquatableObjectDescriptor peod2 = (IProxyEquatableObjectDescriptor)eod2;

                throw new NotImplementedException();
            }

            default:
                throw new UnreachableException($"Unrecognized {nameof(EqualityBehavior)}");
        }
    }

    public int GetHashCode(object obj)
    {
        throw new NotImplementedException();
    }
}
