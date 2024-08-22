using Diginsight.Equality;

namespace Diginsight.Playground;

internal static class EqualityProgram
{
    private static void Main()
    {
        FlexibleEqualityComparer.ContractAccessor
            .GetOrAdd<string>(static tc => tc.SetComparerBehavior(typeof(StringComparer), nameof(StringComparer.OrdinalIgnoreCase)));

        Foo foo1 = new("313", 42);
        Bar bar2 = new(313, 42);
        Console.WriteLine(FlexibleEqualityComparer.Instance.Equals(foo1, bar2));
    }

    private sealed record Foo(string Prop1, int Prop2);

    [ProxyEquatableObject(nameof(ToFoo))]
    private sealed record Bar(int Prop1, int Prop2)
    {
        public Foo ToFoo() => new (Prop1.ToStringInvariant(), Prop2);
    }
}
