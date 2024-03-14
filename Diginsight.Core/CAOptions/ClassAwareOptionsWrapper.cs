namespace Diginsight.CAOptions;

public sealed class ClassAwareOptionsWrapper<TOptions, TClass> : IClassAwareOptions<TOptions, TClass>
    where TOptions : class
{
    public TOptions Value { get; }

    public ClassAwareOptionsWrapper(TOptions value)
    {
        Value = value;
    }
}
