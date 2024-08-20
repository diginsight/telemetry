using System.Reflection;
using ArgumentException = System.ArgumentException;

namespace Diginsight.Equality;

public sealed class AttributedEqualityComparer : IEqualityComparer<object>
{
    public new bool Equals(object? obj1, object? obj2)
    {
        if (obj1 is null)
            return obj2 is null;

        if (obj2 is null)
            return false;

        Type type1 = obj1.GetType();
        Type type2 = obj2.GetType();

        static IEquatableObjectDescriptor? FindAttribute(Type type)
        {
            foreach (Type t in type.GetClosure())
            {
                EquatableObjectAttribute[] attributes = t.GetCustomAttributes<EquatableObjectAttribute>().Take(2).ToArray();
                switch (attributes)
                {
                    case [ ]:
                        break;

                    case [ var attribute ]:
                        return attribute;

                    case [ _, _ ]:
                        throw new ArgumentException($"Multiple {nameof(EquatableObjectAttribute)}s applied to type {t}");
                }
            }

            return null;
        }

        throw new NotImplementedException();
    }

    public int GetHashCode(object obj)
    {
        throw new NotImplementedException();
    }
}
