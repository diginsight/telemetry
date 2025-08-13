using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Diginsight;

/// <summary>
/// Provides extension methods for inspecting characteristics of <see cref="Type" /> instances.
/// </summary>
/// <remarks>
/// For example, this class includes methods for determining whether a type is anonymous,
/// implements certain interfaces, or can be assigned from another type considering generic type definitions.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class TypeExtensions
{
    private static readonly IDictionary<Type, bool> AnonymousCache = new Dictionary<Type, bool>();

    /// <summary>
    /// Gets the closure of the specified type, i.e. its base types and, optionally, its interfaces.
    /// </summary>
    /// <param name="type">The type to get the closure for.</param>
    /// <param name="includeSelf">Whether to include the type itself in the closure.</param>
    /// <param name="includeInterfaces">Whether to include the interfaces implemented by the type in the closure.</param>
    /// <returns>A lazy enumerable of types representing the closure of the specified type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the type is <c>null</c>.</exception>
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

    /// <summary>
    /// Determines whether <paramref name="target" /> can be assigned from <paramref name="source" />, considering generic type definitions.
    /// </summary>
    /// <param name="target">The target type.</param>
    /// <param name="source">The source type.</param>
    /// <returns><c>true</c> if <paramref name="target" /> can be assigned from <paramref name="source" />; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either input is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source" /> is an open generic type.</exception>
    public static bool IsGenericAssignableFrom(this Type target, Type source)
    {
        if (target is null)
            throw new ArgumentNullException(nameof(target));
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        if (source.IsGenericTypeDefinition)
        {
            throw new ArgumentException("Cannot assign from an open generic type", nameof(source));
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

    /// <summary>
    /// Gets the generic arguments of the specified type, viewed as instantiation of the target generic type definition.
    /// </summary>
    /// <param name="self">The type to get the generic arguments for.</param>
    /// <param name="target">The target generic type definition.</param>
    /// <returns>A lazy enumerable of <see cref="T:System.Type[]" /> representing the generic arguments of the specified type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="target" /> is not a generic type definition.</exception>
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

    /// <summary>
    /// Determines whether the specified type is an anonymous type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns><c>true</c> if <paramref name="type" /> is an anonymous type; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type" /> is <c>null</c>.</exception>
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

    /// <summary>
    /// Determines whether the specified type implements <see cref="IEnumerable{T}" /> and which is the enumerated type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="tInner">When this method returns <c>true</c>, contains the enumerated type; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if the <paramref name="type" /> implements <see cref="IEnumerable{T}" />; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type" /> is <c>null</c>.</exception>
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

    /// <summary>
    /// Determines whether the specified type implements <see cref="IAsyncEnumerable{T}" /> and which is the enumerated type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="tInner">When this method returns <c>true</c>, contains the enumerated type; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if the <paramref name="type" /> implements <see cref="IAsyncEnumerable{T}" />; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type" /> is <c>null</c>.</exception>
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

    /// <summary>
    /// Determines whether the specified type is <see cref="KeyValuePair{TKey, TValue}" /> and which are the key and value types.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="tKey">When this method returns <c>true</c>, contains the key type; otherwise, <c>null</c>.</param>
    /// <param name="tValue">When this method returns <c>true</c>, contains the value type; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if the <paramref name="type" /> is <see cref="KeyValuePair{TKey, TValue}" />; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type" /> is <c>null</c>.</exception>
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

    /// <summary>
    /// Determines whether the specified type implements <see cref="IEnumerable{T}" /> of <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}" /> and which are the enumerated key and value types.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="tKey">When this method returns <c>true</c>, contains the enumerated key type; otherwise, <c>null</c>.</param>
    /// <param name="tValue">When this method returns <c>true</c>, contains the enumerated value type; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if the <paramref name="type" /> implements <see cref="IEnumerable{T}" /> of <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}" />; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type" /> is <c>null</c>.</exception>
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
    /// <summary>
    /// Determines whether the specified members have the same metadata definition.
    /// </summary>
    /// <param name="m1">The first member to compare.</param>
    /// <param name="m2">The second member to compare.</param>
    /// <returns><c>true</c> if the members have the same metadata definition; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either member is <c>null</c>.</exception>
    public static bool HasSameMetadataDefinitionAs(this MemberInfo m1, MemberInfo m2)
    {
        if (m1 is null)
            throw new ArgumentNullException(nameof(m1));
        if (m2 is null)
            throw new ArgumentNullException(nameof(m2));

        return m1.MetadataToken == m2.MetadataToken && ReferenceEquals(m1.Module, m2.Module);
    }
#endif
}
