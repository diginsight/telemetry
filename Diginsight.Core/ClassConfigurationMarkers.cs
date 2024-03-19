using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Diginsight;

public static class ClassConfigurationMarkers
{
    private static readonly ConcurrentDictionary<Type, IReadOnlyList<string>> dict = new ();

    // TODO Handle nested types and generic type specificity (type argument count)
    public static IReadOnlyList<string> For(Type @class)
    {
        if (
            @class.IsNested
            || @class.IsArray
            || @class.IsByRef
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            || @class.IsByRefLike
#endif
            || @class.IsGenericParameter
            || @class.IsPointer
#if NET8_0_OR_GREATER
            || @class.IsFunctionPointer
            || @class.IsUnmanagedFunctionPointer
#endif
        )
        {
            throw new ArgumentException("Nested, array, byref, generic parameter and pointer types not supported");
        }

        if (@class.IsConstructedGenericType)
        {
            @class = @class.GetGenericTypeDefinition();
        }

        return dict.GetOrAdd(@class, static c => GetMarkers(c).ToArray());

        static IEnumerable<string> GetMarkers(Type @class)
        {
            string[] namespacePieces = @class.Namespace?.Split('.') ?? Array.Empty<string>();
            IEnumerable<string> namespaceSegments = Enumerable.Range(1, namespacePieces.Length).Select(i => string.Join(".", namespacePieces.Take(i))).Reverse().ToArray();

            var availableShorthands = @class.Assembly.GetCustomAttributes<ClassConfigurationNamespaceShorthandAttribute>().ToDictionary(static x => x.Namespace, static x => x.Shorthand);

            IEnumerable<string> namespaceShorthands = namespaceSegments
                .Select(x => availableShorthands.TryGetValue(x, out string? val) ? val : null)
                .OfType<string>()
                .ToArray();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static string TrimGenericSuffix(string name) => name.IndexOf('`') is > 0 and var idx ? name[..idx] : name;

            if (@class is { Namespace: not null, FullName: { } fullName })
            {
                yield return TrimGenericSuffix(fullName);
            }

            string name = TrimGenericSuffix(@class.Name);

            foreach (string shorthand in namespaceShorthands)
            {
                yield return $"#{shorthand}.{name}";
            }

            yield return name;

            foreach (string segment in namespaceSegments)
            {
                yield return $"{segment}.*";
            }

            foreach (string shorthand in namespaceShorthands)
            {
                yield return $"#{shorthand}.*";
            }

            yield return "";
        }
    }
}
