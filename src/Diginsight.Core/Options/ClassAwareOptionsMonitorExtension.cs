using Microsoft.Extensions.Options;

namespace Diginsight.Options;

public sealed class ClassAwareOptionsMonitorExtension<TOptions> : IClassAwareOptionsMonitor<TOptions>
    where TOptions : class
{
    private readonly IOptionsMonitor<TOptions>? underlying1;
    private readonly IClassAwareOptions<TOptions>? underlying2;

    public TOptions CurrentValue => underlying1 is { } underlying ? underlying.CurrentValue : underlying2!.Value;

    public ClassAwareOptionsMonitorExtension(IOptionsMonitor<TOptions> underlying)
    {
        underlying1 = underlying;
    }

    public ClassAwareOptionsMonitorExtension(IClassAwareOptions<TOptions> underlying)
    {
        underlying2 = underlying;
    }

    TOptions IOptionsMonitor<TOptions>.Get(string? name) => underlying1 is { } underlying ? underlying.Get(name) : underlying2!.Get(null);

    public TOptions Get(string? name, Type? @class) => underlying1 is { } underlying ? underlying.Get(name) : underlying2!.Get(@class);

    public IDisposable? OnChange(Action<TOptions, string?> listener) => underlying1?.OnChange(listener);

    public IDisposable? OnChange(Action<TOptions, string, Type> listener) => underlying1?.OnChange(
        (o, n) => listener(o, n ?? Microsoft.Extensions.Options.Options.DefaultName, ClassAwareOptions.NoClass)
    );
}
