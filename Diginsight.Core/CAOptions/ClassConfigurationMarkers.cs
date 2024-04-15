using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Diginsight.CAOptions;

public static class ClassConfigurationMarkers
{
    private static readonly ConcurrentDictionary<Type, IReadOnlyList<string>> Markers = new ();
    private static readonly Regex GenericSuffixRegex = new Regex(@"`\d+");
    private static readonly IReadOnlyList<string> NoClassMarkers = new[] { "" };

    public static IReadOnlyList<string> For(Type @class)
    {
        if (@class == ClassAwareOptions.NoClass)
        {
            return NoClassMarkers;
        }

        if (
            @class.IsArray
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
            throw new ArgumentException("Array, byref, generic parameter and pointer types not supported");
        }

        if (@class.IsConstructedGenericType)
        {
            @class = @class.GetGenericTypeDefinition();
        }

        return Markers.GetOrAdd(@class, static c => CalculateMarkers(c).ToArray());

        static IEnumerable<string> CalculateMarkers(Type @class)
        {
            string[] namespacePieces = @class.Namespace?.Split('.') ?? [ ];
            IEnumerable<string> namespaceSegments = Enumerable.Range(1, namespacePieces.Length)
                .Select(i => string.Join(".", namespacePieces.Take(i)))
                .Reverse()
                .ToArray();

            IReadOnlyDictionary<string, string> availableShorthands = @class.Assembly
                .GetCustomAttributes<ClassConfigurationNamespaceShorthandAttribute>()
                .ToDictionary(static x => x.Namespace, static x => x.Shorthand);

            IEnumerable<string> namespaceShorthands = namespaceSegments
                .Select(x => availableShorthands.TryGetValue(x, out string? val) ? val : null)
                .OfType<string>()
                .ToArray();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static string CleanGeneric(string x) => GenericSuffixRegex.Replace(x, "");

            if (@class is { Namespace: not null, FullName: { } fullName })
            {
                yield return CleanGeneric(fullName).Replace('+', '.');
            }

            static (string Name, string? NestedName) CalculateNames(Type @class)
            {
                string name = CleanGeneric(@class.Name);
                if (!@class.IsNested)
                {
                    return (name, null);
                }

                string nestedName = name;
                for (Type? c = @class.DeclaringType; c is not null; c = c.DeclaringType)
                {
                    nestedName = $"{CleanGeneric(c.Name)}.{nestedName}";
                }

                return (name, nestedName);
            }

            (string name, string? nestedName) = CalculateNames(@class);

            foreach (string shorthand in namespaceShorthands)
            {
                yield return $"#{shorthand}.{nestedName ?? name}";
            }

            if (nestedName is not null)
            {
                yield return nestedName;
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
