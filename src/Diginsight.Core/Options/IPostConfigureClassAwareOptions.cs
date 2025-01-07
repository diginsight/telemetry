namespace Diginsight.Options;

/// <summary>
/// Represents something that class-awarely post-configures the <typeparamref name="TOptions" /> type.
/// </summary>
/// <remarks>
/// These are run after all <see cref="IConfigureClassAwareOptions{TOptions}" />.
/// </remarks>
/// <typeparam name="TOptions">The options type being configured.</typeparam>
public interface IPostConfigureClassAwareOptions<in TOptions>
    where TOptions : class
{
    /// <summary>
    /// Invoked to class-awarely post-configure a <typeparamref name="TOptions" /> instance.
    /// </summary>
    /// <param name="name">The name of the options instance being post-configured.</param>
    /// <param name="class">The class associated with the options instance.</param>
    /// <param name="options">The options instance to post-configure.</param>
    void PostConfigure(string name, Type @class, TOptions options);
}
