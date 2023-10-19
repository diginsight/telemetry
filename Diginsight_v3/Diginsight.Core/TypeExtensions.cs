namespace Diginsight;

public static class TypeExtensions
{
    public static bool IsGenericAssignableFrom(this Type target, Type source)
    {
        if (target is null)
            throw new ArgumentNullException(nameof(target));
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        if (source.IsGenericTypeDefinition)
        {
            throw new ArgumentException("cannot assign from an open generic type", nameof(source));
        }
        if (!target.IsGenericTypeDefinition)
        {
            return target.IsAssignableFrom(source);
        }

        if (source.IsInterface && !target.IsInterface)
        {
            return false;
        }

        bool IsTarget(Type other) => other.IsGenericType && other.GetGenericTypeDefinition() == target;

        if (target.IsInterface)
        {
            return IsTarget(source) || source.GetInterfaces().Any(IsTarget);
        }

        Type? current = source;
        while (current is not null)
        {
            if (IsTarget(current))
            {
                return true;
            }
            current = current.BaseType;
        }

        return false;
    }

    public static IEnumerable<Type[]> GetGenericArgumentsAs(this Type self, Type target)
    {
        if (self is null)
            throw new ArgumentNullException(nameof(self));
        if (target is null)
            throw new ArgumentNullException(nameof(target));

        if (!target.IsGenericTypeDefinition)
        {
            throw new ArgumentException("generic type definition expected", nameof(target));
        }

        if (self.IsInterface && !target.IsInterface)
        {
            yield break;
        }

        bool IsTarget(Type other) => other.IsGenericType && other.GetGenericTypeDefinition() == target;

        if (target.IsInterface)
        {
            if (IsTarget(self))
            {
                yield return self.GetGenericArguments();
            }
            else
            {
                foreach (Type current in self.GetInterfaces().Where(IsTarget))
                {
                    yield return current.GetGenericArguments();
                }
            }
        }
        else
        {
            Type? current = self;
            while (current is not null)
            {
                if (IsTarget(current))
                {
                    yield return current.GetGenericArguments();
                }
                current = current.BaseType;
            }
        }
    }
}
