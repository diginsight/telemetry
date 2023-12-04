namespace Diginsight.CAOptions;

public sealed class ClassAwareOptionsFactory<TOptions> : IClassAwareOptionsFactory<TOptions>
    where TOptions : class
{
    public TOptions Create(string name, Type? type)
    {
        throw new NotImplementedException();
    }
}
