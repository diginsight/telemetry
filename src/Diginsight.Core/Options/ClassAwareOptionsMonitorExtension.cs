using Microsoft.Extensions.Options;

namespace Diginsight.Options;

/// <summary>
/// Adapts an <see cref="IOptionsMonitor{TOptions}" /> or an <see cref="IClassAwareOptions{TOptions}" /> to the <see cref="IClassAwareOptionsMonitor{TOptions}" /> contract.
/// </summary>
/// <typeparam name="TOptions">The type of options being monitored.</typeparam>
public sealed class ClassAwareOptionsMonitorExtension<TOptions> : IClassAwareOptionsMonitor<TOptions>
    where TOptions : class
{
    private readonly IOptionsMonitor<TOptions>? underlying1;
    private readonly IClassAwareOptions<TOptions>? underlying2;

    /// <inheritdoc />
    public TOptions CurrentValue => underlying1 is { } underlying ? underlying.CurrentValue : underlying2!.Value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassAwareOptionsMonitorExtension{TOptions}" /> class wrapping the specified options monitor.
    /// </summary>
    /// <param name="underlying">The options monitor to adapt.</param>
    public ClassAwareOptionsMonitorExtension(IOptionsMonitor<TOptions> underlying)
    {
        underlying1 = underlying;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassAwareOptionsMonitorExtension{TOptions}" /> class wrapping the specified class-aware options.
    /// </summary>
    /// <param name="underlying">The class-aware options to adapt.</param>
    public ClassAwareOptionsMonitorExtension(IClassAwareOptions<TOptions> underlying)
    {
        underlying2 = underlying;
    }

    TOptions IOptionsMonitor<TOptions>.Get(string? name) => underlying1 is { } underlying ? underlying.Get(name) : underlying2!.Get(null);

    /// <inheritdoc />
    public TOptions Get(string? name, Type? @class) => underlying1 is { } underlying ? underlying.Get(name) : underlying2!.Get(@class);

    /// <inheritdoc />
    public IDisposable? OnChange(Action<TOptions, string?> listener) => underlying1?.OnChange(listener);

    /// <inheritdoc />
    public IDisposable? OnChange(Action<TOptions, string, Type> listener) => underlying1?.OnChange(
        (o, n) => listener(o, n ?? Microsoft.Extensions.Options.Options.DefaultName, ClassAwareOptions.NoClass)
    );
}
