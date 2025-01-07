namespace Diginsight.Options;

/// <summary>
/// Represents something that class-awarely configures the <typeparamref name="TOptions" /> type.
/// </summary>
/// <remarks>
/// These are run before all <see cref="IPostConfigureClassAwareOptions{TOptions}" />.
/// </remarks>
/// <typeparam name="TOptions">The options type being configured.</typeparam>
public interface IConfigureClassAwareOptions<in TOptions>
    where TOptions : class
{
    /// <summary>
    /// Invoked to class-awarely configure a <typeparamref name="TOptions" /> instance.
    /// </summary>
    /// <param name="name">The name of the options instance being configured.</param>
    /// <param name="class">The class associated with the options instance.</param>
    /// <param name="options">The options instance to configure.</param>
    void Configure(string name, Type @class, TOptions options);
}
