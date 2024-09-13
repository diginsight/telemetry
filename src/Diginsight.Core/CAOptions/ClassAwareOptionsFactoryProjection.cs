using Microsoft.Extensions.Options;

namespace Diginsight.CAOptions;

public sealed class ClassAwareOptionsFactoryProjection<TOptions> : IOptionsFactory<TOptions>
    where TOptions : class
{
    private readonly IClassAwareOptionsFactory<TOptions> underlying;
    private readonly Type @class;

    public ClassAwareOptionsFactoryProjection(IClassAwareOptionsFactory<TOptions> underlying, Type @class)
    {
        this.underlying = underlying;
        this.@class = @class;
    }

    public TOptions Create(string name) => underlying.Create(name, @class);
}
