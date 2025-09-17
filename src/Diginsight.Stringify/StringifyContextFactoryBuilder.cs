using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Diginsight.Stringify;

public sealed class StringifyContextFactoryBuilder
{
    public static StringifyContextFactoryBuilder DefaultBuilder { get; set; } = new ();

    [AllowNull]
    [field: MaybeNull]
    public static IStringifyContextFactory DefaultFactory
    {
        get => field ??= DefaultBuilder.Build();
        set;
    }

    public IServiceCollection Services { get; } = new ServiceCollection();

    public StringifyContextFactoryBuilder()
    {
        Services.AddStringify();
    }
}
