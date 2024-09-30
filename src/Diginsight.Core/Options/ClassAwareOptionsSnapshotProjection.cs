using Microsoft.Extensions.Options;

namespace Diginsight.Options;

public sealed class ClassAwareOptionsSnapshotProjection<TOptions> : IOptionsSnapshot<TOptions>
    where TOptions : class
{
    private readonly IClassAwareOptionsSnapshot<TOptions> underlying;
    private readonly Type @class;

    public TOptions Value => Get(null);

    public ClassAwareOptionsSnapshotProjection(IClassAwareOptionsSnapshot<TOptions> underlying, Type @class)
    {
        this.underlying = underlying;
        this.@class = @class;
    }

    public TOptions Get(string? name) => underlying.Get(name, @class);
}
