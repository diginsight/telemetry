using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Diginsight.Stringify;

/// <summary>
/// Represents a builder for configuring and creating stringify context factories.
/// </summary>
public sealed class StringifyContextFactoryBuilder
{
    /// <summary>
    /// Gets the default stringify context factory builder.
    /// </summary>
    public static StringifyContextFactoryBuilder DefaultBuilder { get; set; } = new ();

    /// <summary>
    /// Gets the default stringify context factory.
    /// </summary>
    [AllowNull]
    [field: MaybeNull]
    public static IStringifyContextFactory DefaultFactory
    {
        get => field ??= DefaultBuilder.Build();
        set;
    }

    /// <summary>
    /// Gets the service collection used by the builder.
    /// </summary>
    public IServiceCollection Services { get; } = new ServiceCollection();

    /// <summary>
    /// Initializes a new instance of the <see cref="StringifyContextFactoryBuilder" /> class.
    /// </summary>
    public StringifyContextFactoryBuilder()
    {
        Services.AddStringify();
    }
}
