using System.Reflection;

namespace Diginsight;

public static class ClassConfigurationMarkers
{
    public static IReadOnlyList<string> For(Type @class)
    {
        return (IReadOnlyList<string>)typeof(ClassConfigurationMarkers<>)
            .MakeGenericType(@class)
            .GetProperty(nameof(ClassConfigurationMarkers<object>.Markers), BindingFlags.Public | BindingFlags.Static)!
            .GetValue(null)!;
    }
}

public static class ClassConfigurationMarkers<TClass>
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly string[] MarkersCore;

    public static IReadOnlyList<string> Markers => MarkersCore;

    static ClassConfigurationMarkers()
    {
        MarkersCore = GetMarkers().ToArray();

        static IEnumerable<string> GetMarkers()
        {
            Type type = typeof(TClass);
            if (type.IsArray || type.IsByRef || type.IsGenericParameter || type.IsGenericType || type.IsPointer)
            {
                throw new ArgumentException("Array, byref, generic and pointer types not supported");
            }

            string[] namespacePieces = type.Namespace?.Split('.') ?? Array.Empty<string>();
            IEnumerable<string> namespaceSegments = Enumerable.Range(1, namespacePieces.Length).Select(i => string.Join(".", namespacePieces.Take(i))).Reverse().ToArray();

            var availableShorthands = type.Assembly.GetCustomAttributes<ClassConfigurationNamespaceShorthandAttribute>().ToDictionary(static x => x.Namespace, static x => x.Shorthand);

            IEnumerable<string> namespaceShorthands = namespaceSegments
                .Select(x => availableShorthands.TryGetValue(x, out string? val) ? val : null)
                .OfType<string>()
                .ToArray();

            if (type is { Namespace: not null, FullName: not null })
            {
                yield return type.FullName;
            }

            foreach (string shorthand in namespaceShorthands)
            {
                yield return $"#{shorthand}.{type.Name}";
            }

            yield return type.Name;

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
