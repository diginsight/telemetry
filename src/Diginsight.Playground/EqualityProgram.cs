using Diginsight.Equality;

namespace Diginsight.Playground;

internal static class EqualityProgram
{
    private static void Main()
    {
        AttributedEqualityComparer.ContractAccessor
            .GetOrAdd<string>(static tc => tc.SetComparerBehavior(typeof(StringComparer), nameof(StringComparer.OrdinalIgnoreCase)));

        Foo foo1 = new("foo", 42);
        Foo foo2 = new("Foo", 42);
        Console.WriteLine(AttributedEqualityComparer.Instance.Equals(foo1, foo2));
    }

    private sealed record Foo(string Prop1, int Prop2);
}
