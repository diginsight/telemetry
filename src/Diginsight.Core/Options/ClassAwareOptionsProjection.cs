using Microsoft.Extensions.Options;

namespace Diginsight.Options;

public sealed class ClassAwareOptionsProjection<TOptions> : IOptions<TOptions>
    where TOptions : class
{
    private readonly IClassAwareOptions<TOptions> underlying;
    private readonly Type @class;

    public TOptions Value => underlying.Get(@class);

    public ClassAwareOptionsProjection(IClassAwareOptions<TOptions> underlying, Type @class)
    {
        this.underlying = underlying;
        this.@class = @class;
    }
}
