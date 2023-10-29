using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Diginsight;

public static class TypeExtensions
{
    private static readonly IDictionary<Type, bool> AnonymousCache = new Dictionary<Type, bool>();

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

    public static bool IsAnonymous(this Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));

        lock (((ICollection)AnonymousCache).SyncRoot)
        {
            return AnonymousCache.TryGetValue(type, out bool isAnonymous)
                ? isAnonymous
                : AnonymousCache[type] = IsAnonymousCore();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool IsAnonymousCore()
            {
                return type is { IsClass: true, IsSealed: true, IsNotPublic: true }
                    && type.IsDefined(typeof(CompilerGeneratedAttribute))
                    && type.Name.Contains("AnonymousType");
            }
        }
    }

    internal static bool IsKeyValuePair(this Type type, [NotNullWhen(true)] out Type? tKey, [NotNullWhen(true)] out Type? tValue)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));

        if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(KeyValuePair<,>))
        {
            tKey = null;
            tValue = null;
            return false;
        }

        Type[] typeArgs = type.GetGenericArguments();
        tKey = typeArgs[0];
        tValue = typeArgs[1];
        return true;
    }
}
