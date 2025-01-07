using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Diginsight.Options;

/// <summary>
/// Provides methods to retrieve class configuration markers.
/// </summary>
/// <remarks>
///     <para>Despite the name, class markers also apply to interfaces.</para>
///     <para>
///     The class markers for a type <c>Namespace1.Namespace2.Namespace3.SomeClass</c> are:
///     <list type="number">
///         <item>
///             <description>
///             <c>Namespace1.Namespace2.Namespace3.SomeClass</c> (fully qualified name)
///             </description>
///         </item>
///         <item>
///             <description>
///             <c>SomeClass</c> (simple name)
///             </description>
///         </item>
///         <item>
///             <description>
///             <c>Namespace1.Namespace2.Namespace3.*</c> (cascading namespace wildcards)
///             </description>
///         </item>
///         <item>
///             <description>
///                 <c>Namespace1.Namespace2.*</c>
///             </description>
///         </item>
///         <item>
///             <description>
///                 <c>Namespace1.*</c>
///             </description>
///         </item>
///         <item>
///             <description>
///             empty string
///             </description>
///         </item>
///     </list>
///     </para>
///     <para>
///     The class markers for a nested type <c>NestedClass</c> declared within
///     <c>Namespace1.Namespace2.Namespace3.SomeClass</c> are:
///     <list type="number">
///         <item>
///             <description>
///             <c>Namespace1.Namespace2.Namespace3.SomeClass.NestedClass</c> (fully qualified name)
///             </description>
///         </item>
///         <item>
///             <description>
///             <c>NestedClass</c> (own simple name, without declaring type name)
///             </description>
///         </item>
///         <item>
///             <description>
///             The following entries are as above.
///             </description>
///         </item>
///     </list>
///     </para>
///     <para>The class markers for a generic type are built as if the type was not generic.</para>
/// </remarks>
public static class ClassConfigurationMarkers
{
    private static readonly ConcurrentDictionary<Type, IReadOnlyList<string>> Markers = new ();
    private static readonly Regex GenericSuffixRegex = new (@"`\d+");
    private static readonly IReadOnlyList<string> NoClassMarkers = [ "" ];

    /// <summary>
    /// Gets the configuration markers for a given class type.
    /// </summary>
    /// <param name="class">The class type to get markers for.</param>
    /// <returns>A read-only list of configuration markers.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the class type is not supported
    /// (array, by-ref, generic parameter, pointer).
    /// </exception>
    public static IReadOnlyList<string> For(Type @class)
    {
        if (@class == ClassAwareOptions.NoClass)
        {
            return NoClassMarkers;
        }

        if (
            @class.IsArray
            || @class.IsByRef
#if NET || NETSTANDARD2_1_OR_GREATER
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
