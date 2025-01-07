using Microsoft.Extensions.Options;

namespace Diginsight.Options;

/// <summary>
/// Interface for monitoring class-aware options.
/// </summary>
/// <typeparam name="TOptions">The type of options being monitored.</typeparam>
public interface IClassAwareOptionsMonitor<out TOptions> : IOptionsMonitor<TOptions>
{
#if NET || NETSTANDARD2_1_OR_GREATER
    TOptions IOptionsMonitor<TOptions>.CurrentValue => Get(null, null);
#endif

    /// <summary>
    /// Gets the options for the specified name and class.
    /// </summary>
    /// <param name="name">The name of the options instance.</param>
    /// <param name="class">The class of the options instance.</param>
    /// <returns>The options instance.</returns>
    TOptions Get(string? name, Type? @class);

#if NET || NETSTANDARD2_1_OR_GREATER
    TOptions IOptionsMonitor<TOptions>.Get(string? name) => Get(name, null);
#endif

    /// <summary>
    /// Registers a listener to be called whenever options change.
    /// </summary>
    /// <param name="listener">The action to be invoked when options change.</param>
    /// <returns>A disposable object that can be used to unregister the listener.</returns>
    IDisposable? OnChange(Action<TOptions, string, Type> listener);

#if NET || NETSTANDARD2_1_OR_GREATER
    IDisposable? IOptionsMonitor<TOptions>.OnChange(Action<TOptions, string?> listener)
    {
        return OnChange((options, name, _) => listener(options, name));
    }
#endif
}
