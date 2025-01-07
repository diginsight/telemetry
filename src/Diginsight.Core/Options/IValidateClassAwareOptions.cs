using Microsoft.Extensions.Options;

namespace Diginsight.Options;

/// <summary>
/// Represents something that class-awarely validates the <typeparamref name="TOptions" /> type.
/// </summary>
/// <typeparam name="TOptions">The options type being validated.</typeparam>
public interface IValidateClassAwareOptions<in TOptions>
    where TOptions : class
{
    /// <summary>
    /// Invoked to class-awarely validate a <typeparamref name="TOptions" /> instance.
    /// </summary>
    /// <param name="name">The name of the options instance being validated.</param>
    /// <param name="class">The class associated with the options instance.</param>
    /// <param name="options">The options instance to configure.</param>
    /// <returns>The validation result.</returns>
    ValidateOptionsResult Validate(string name, Type @class, TOptions options);
}
