using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Diginsight.Stringify;

public sealed class StringifyContextFactoryBuilder
{
    private static IStringifyContextFactory? defaultFactory;

    public static StringifyContextFactoryBuilder DefaultBuilder { get; set; } = new ();

    [AllowNull]
    public static IStringifyContextFactory DefaultFactory
    {
        get => defaultFactory ??= DefaultBuilder.Build();
        set => defaultFactory = value;
    }

    public IServiceCollection Services { get; } = new ServiceCollection();

    public StringifyContextFactoryBuilder()
    {
        Services.AddStringify();
    }
}
