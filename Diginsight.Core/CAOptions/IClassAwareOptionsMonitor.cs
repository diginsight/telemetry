using Microsoft.Extensions.Options;

namespace Diginsight.CAOptions;

public interface IClassAwareOptionsMonitor<out TOptions> : IOptionsMonitor<TOptions>
{
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    TOptions IOptionsMonitor<TOptions>.CurrentValue => Get(null, null);
#endif

    TOptions Get(string? name, Type? @class);

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    TOptions IOptionsMonitor<TOptions>.Get(string? name) => Get(name, null);
#endif

    IDisposable? OnChange(Action<TOptions, string, Type> listener);

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    IDisposable? IOptionsMonitor<TOptions>.OnChange(Action<TOptions, string?> listener)
    {
        return OnChange((options, name, _) => listener(options, name));
    }
#endif
}
