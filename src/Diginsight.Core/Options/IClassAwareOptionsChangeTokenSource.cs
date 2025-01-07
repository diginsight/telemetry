using Microsoft.Extensions.Primitives;

namespace Diginsight.Options;

/// <summary>
/// Used to fetch <see cref="IChangeToken" /> used for tracking class-aware options changes.
/// </summary>
/// <typeparam name="TOptions">The options type being changed.</typeparam>
// ReSharper disable once UnusedTypeParameter
public interface IClassAwareOptionsChangeTokenSource<out TOptions>
{
    /// <summary>
    /// Returns a <see cref="IChangeToken" /> which can be used to register a change notification callback.
    /// </summary>
    /// <returns>Change token.</returns>
    IChangeToken GetChangeToken();

    /// <summary>
    /// The name of the option instance being changed.
    /// </summary>
    string? Name { get; }
}
