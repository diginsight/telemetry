using Microsoft.Extensions.Options;

namespace Diginsight.Options;

/// <summary>
/// Projects an <see cref="IClassAwareOptionsMonitor{TOptions}" /> onto a fixed class, exposing it as a standard <see cref="IOptionsMonitor{TOptions}" />.
/// </summary>
/// <typeparam name="TOptions">The type of options being monitored.</typeparam>
public sealed class ClassAwareOptionsMonitorProjection<TOptions> : IOptionsMonitor<TOptions>
    where TOptions : class
{
    private readonly IClassAwareOptionsMonitor<TOptions> underlying;
    private readonly Type @class;

    /// <inheritdoc />
    public TOptions CurrentValue => Get(null);

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassAwareOptionsMonitorProjection{TOptions}" /> class for the specified monitor and class.
    /// </summary>
    /// <param name="underlying">The class-aware options monitor to project.</param>
    /// <param name="class">The class the options are resolved for.</param>
    public ClassAwareOptionsMonitorProjection(IClassAwareOptionsMonitor<TOptions> underlying, Type @class)
    {
        this.underlying = underlying;
        this.@class = @class;
    }

    /// <inheritdoc />
    public TOptions Get(string? name) => underlying.Get(name, @class);

    /// <inheritdoc />
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
