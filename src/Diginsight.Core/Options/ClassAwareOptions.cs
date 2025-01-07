using Microsoft.Extensions.Options;

namespace Diginsight.Options;

/// <summary>
/// Provides utility properties and methods for class-aware options.
/// </summary>
public static class ClassAwareOptions
{
    /// <summary>
    /// A dummy type used to represent "No class" in class-aware options.
    /// </summary>
    public static readonly Type NoClass = typeof(Fake);

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="DependencyInjectionExtensions.AddClassAwareOptions" />
    /// should replace class-agnostic service descriptors (such as <see cref="IOptions{TOptions}" />).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///     This property defaults to <c>true</c>, in order to fully leverage the class-aware options system.
    ///     Unless you really know what you are doing, you should not change this value.
    ///     </para>
    ///     <para>
    ///     Changing the value of this property has no effect after the first call to
    ///     <see cref="DependencyInjectionExtensions.AddClassAwareOptions" />.
    ///     </para>
    /// </remarks>
    public static bool ReplaceClassAgnosticOptions { get; set; } = true;

    /// <summary>
    /// Creates an instance of the specified options type for a given class.
    /// </summary>
    /// <typeparam name="TOptions">The type of options to create.</typeparam>
    /// <param name="optionsFactory">The factory to create the options.</param>
    /// <param name="class">The class for which to create the options.</param>
    /// <returns>An instance of the specified options type.</returns>
    public static TOptions Create<TOptions>(this IClassAwareOptionsFactory<TOptions> optionsFactory, Type @class)
        where TOptions : class
    {
        return optionsFactory.Create(Microsoft.Extensions.Options.Options.DefaultName, @class);
    }

    /// <summary>
    /// Gets an instance of the specified options type for a given class.
    /// </summary>
    /// <typeparam name="TOptions">The type of options to get.</typeparam>
    /// <param name="optionsMonitor">The monitor to get the options from.</param>
    /// <param name="class">The class for which to get the options.</param>
    /// <returns>An instance of the specified options type.</returns>
    public static TOptions Get<TOptions>(this IClassAwareOptionsMonitor<TOptions> optionsMonitor, Type? @class)
        where TOptions : class
    {
        return optionsMonitor.Get(null, @class);
    }

    private readonly struct Fake;
}
