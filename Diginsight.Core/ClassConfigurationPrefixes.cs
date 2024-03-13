using System.Reflection;

namespace Diginsight;

public static class ClassConfigurationPrefixes
{
    public static IEnumerable<string> For(Type @class)
    {
        return (IEnumerable<string>)typeof(ClassConfigurationPrefixes<>)
            .MakeGenericType(@class)
            .GetProperty(nameof(ClassConfigurationPrefixes<object>.Prefixes), BindingFlags.Public | BindingFlags.Static)!
            .GetValue(null)!;
    }
}

public static class ClassConfigurationPrefixes<TClass>
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly string[] PrefixesCore;

    public static IEnumerable<string> Prefixes => PrefixesCore;

    static ClassConfigurationPrefixes()
    {
        PrefixesCore = GetPrefixes().ToArray();

        static IEnumerable<string> GetPrefixes()
        {
            Type type = typeof(TClass);
            if (type.IsArray || type.IsByRef || type.IsGenericParameter || type.IsGenericType || type.IsPointer)
            {
                throw new ArgumentException("Array, byref, generic and pointer types not supported");
            }

            string[] namespacePieces = (type.Namespace ?? "").Split('.');
            IEnumerable<string> namespaceSegments = Enumerable.Range(1, namespacePieces.Length).Select(i => string.Join(".", namespacePieces.Take(i))).Reverse().ToArray();

            var availableShorthands = type.Assembly.GetCustomAttributes<ClassConfigurationNamespaceShorthandAttribute>().ToDictionary(static x => x.Namespace, static x => x.Shorthand);

            IEnumerable<string> namespaceShorthands = namespaceSegments
                .Select(x => availableShorthands.TryGetValue(x, out string? val) ? val : null)
                .OfType<string>()
                .ToArray();

            if (type.FullName != null)
            {
                yield return $"{type.FullName}.";
            }

            foreach (string shorthand in namespaceShorthands)
            {
                yield return $"#{shorthand}.{type.Name}.";
            }

            yield return $"{type.Name}.";

            foreach (string segment in namespaceSegments)
            {
                yield return $"{segment}.*.";
            }

            foreach (string shorthand in namespaceShorthands)
            {
                yield return $"#{shorthand}.*.";
            }

            yield return "";
        }
    }
}
