using Microsoft.Extensions.Options;

namespace Diginsight.Options;

public sealed class ClassAwareOptionsMonitorProjection<TOptions> : IOptionsMonitor<TOptions>
    where TOptions : class
{
    private readonly IClassAwareOptionsMonitor<TOptions> underlying;
    private readonly Type @class;

    public TOptions CurrentValue => Get(null);

    public ClassAwareOptionsMonitorProjection(IClassAwareOptionsMonitor<TOptions> underlying, Type @class)
    {
        this.underlying = underlying;
        this.@class = @class;
    }

    public TOptions Get(string? name) => underlying.Get(name, @class);

    public IDisposable? OnChange(Action<TOptions, string?> listener)
    {
        return underlying.OnChange(
            (o, n, c) =>
            {
                if (c == @class)
                {
                    listener(o, n);
                }
            }
        );
    }
}
