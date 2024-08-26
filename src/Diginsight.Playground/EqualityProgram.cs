using Diginsight.Equality;

namespace Diginsight.Playground;

internal static class EqualityProgram
{
    private static void Main()
    {
        //FlexibleEqualityComparer.ContractAccessor
        //    .GetOrAdd<string>(static tc => tc.SetComparerBehavior(typeof(StringComparer), nameof(StringComparer.OrdinalIgnoreCase)));

        Foo foo1 = new("313", "Foo", "paperino");
        Bar bar2 = new(313, "foo", "paperino");
        Console.WriteLine(FlexibleEqualityComparer.Instance.Equals(foo1, bar2));
    }

    private sealed record Foo(
        string Prop1,
        [property: ComparerEquatableMember(typeof(StringComparer), nameof(StringComparer.OrdinalIgnoreCase))]
        string Prop2,
        string Prop3
    );

    [ProxyEquatableObject(nameof(ToFoo))]
    private sealed record Bar(int Prop1, string Prop2, string Prop3)
    {
        public Foo ToFoo() => new (Prop1.ToStringInvariant(), Prop2, Prop3);
    }
}
