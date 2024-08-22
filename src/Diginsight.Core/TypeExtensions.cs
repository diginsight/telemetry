using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Diginsight;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class TypeExtensions
{
    private static readonly IEnumerable<Type> FixedBannedTypes =
    [
        typeof(Thread),
        typeof(CancellationToken),
        typeof(CancellationTokenSource),
        typeof(MarshalByRefObject),
#if NET
        typeof(TaskCompletionSource),
#endif
        typeof(TaskCompletionSource<>),
    ];

    private static readonly IMemoryCache BannedTypesCache = new MemoryCache(
        Options.Create(new MemoryCacheOptions() { SizeLimit = 2000 })
    );

    private static readonly IDictionary<Type, bool> AnonymousCache = new Dictionary<Type, bool>();

    public static IEnumerable<Type> GetClosure(this Type type, bool includeSelf = true, bool includeInterfaces = true)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));

        Type? currentType = includeSelf ? type : type.BaseType;
        while (currentType is not null)
        {
            yield return currentType;
            currentType = currentType.BaseType;
        }

        if (!includeInterfaces)
            yield break;

        foreach (Type @interface in type.GetInterfaces())
        {
            yield return @interface;
        }
    }

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

    public static bool IsIEnumerable(this Type type, [NotNullWhen(true)] out Type? tInner)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));

        foreach (Type interfaceType in type.GetInterfaces())
        {
            if (!interfaceType.IsGenericType || interfaceType.GetGenericTypeDefinition() != typeof(IEnumerable<>))
                continue;

            tInner = interfaceType.GetGenericArguments()[0];
            return true;
        }

        tInner = null;
        return false;
    }

    public static bool IsIAsyncEnumerable(this Type type, [NotNullWhen(true)] out Type? tInner)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));

        foreach (Type interfaceType in type.GetInterfaces())
        {
            if (!interfaceType.IsGenericType || interfaceType.GetGenericTypeDefinition() != typeof(IAsyncEnumerable<>))
                continue;

            tInner = interfaceType.GetGenericArguments()[0];
            return true;
        }

        tInner = null;
        return false;
    }

    public static bool IsKeyValuePair(this Type type, [NotNullWhen(true)] out Type? tKey, [NotNullWhen(true)] out Type? tValue)
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

    public static bool IsIEnumerableOfKeyValuePair(this Type type, [NotNullWhen(true)] out Type? tKey, [NotNullWhen(true)] out Type? tValue)
    {
        foreach (Type interfaceType in type.GetInterfaces())
        {
            if (interfaceType.IsIEnumerable(out Type? innerType) && innerType.IsKeyValuePair(out tKey, out tValue))
            {
                return true;
            }
        }

        tKey = null;
        tValue = null;
        return false;
    }

#if !(NET || NETSTANDARD2_1_OR_GREATER)
    public static bool HasSameMetadataDefinitionAs(this MemberInfo m1, MemberInfo m2)
    {
        if (m1 is null)
            throw new ArgumentNullException(nameof(m1));
        if (m2 is null)
            throw new ArgumentNullException(nameof(m2));

        return m1.MetadataToken == m2.MetadataToken && ReferenceEquals(m1.Module, m2.Module);
    }
#endif

    internal static bool IsBanned(this Type type)
    {
        static bool IsAwaitable(Type type)
        {
            return type.GetMethod("GetAwaiter", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, [ ]) is { IsGenericMethod: false } method
                && IsAwaiter(method.ReturnType);
        }

        static bool IsAwaiter(Type type)
        {
            return typeof(INotifyCompletion).IsAssignableFrom(type)
                && type.GetProperty("IsCompleted", BindingFlags.Public | BindingFlags.Instance, null, typeof(bool), Type.EmptyTypes, [ ]) is not null
                && type.GetMethod("GetResult", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, [ ]) is { IsGenericMethod: false };
        }

        static bool IsEnumerator(Type type)
        {
            return (typeof(IEnumerator).IsAssignableFrom(type) || typeof(IEnumerator<>).IsGenericAssignableFrom(type))
                && !(typeof(IEnumerable).IsAssignableFrom(type) || typeof(IEnumerable<>).IsGenericAssignableFrom(type));
        }

        static bool IsAsyncStateMachine(Type type)
        {
            return (typeof(IAsyncStateMachine).IsAssignableFrom(type) || typeof(IAsyncEnumerator<>).IsGenericAssignableFrom(type))
                && !typeof(IAsyncEnumerable<>).IsGenericAssignableFrom(type);
        }

        return BannedTypesCache.GetOrCreate(
            type,
            e =>
            {
                e.SlidingExpiration = TimeSpan.FromMinutes(30);
                e.Size = 1;

                return FixedBannedTypes.Any(x => x.IsGenericAssignableFrom(type))
                    || IsAwaitable(type)
                    || IsAwaiter(type)
                    || IsEnumerator(type)
                    || IsAsyncStateMachine(type);
            }
        );
    }
}
