using Microsoft.Extensions.Options;

namespace Diginsight.CAOptions;

public interface IClassAwareOptionsMonitor<out TOptions> : IOptionsMonitor<TOptions>
    where TOptions : class
{
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    TOptions IOptionsMonitor<TOptions>.CurrentValue => Get(Options.DefaultName, ClassAwareOptions.NoType);
#endif

    TOptions Get(string name, Type @class);

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    TOptions IOptionsMonitor<TOptions>.Get(string? name) => Get(name ?? Options.DefaultName, ClassAwareOptions.NoType);
#endif

    IDisposable? OnChange(Action<TOptions, string, Type> listener);

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    IDisposable? IOptionsMonitor<TOptions>.OnChange(Action<TOptions, string?> listener)
    {
        throw new NotSupportedException("Use the other overload instead");
    }
#endif
}
