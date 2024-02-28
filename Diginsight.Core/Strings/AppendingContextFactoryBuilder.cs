using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Diginsight.Strings;

public sealed class AppendingContextFactoryBuilder
{
    private static IAppendingContextFactory? defaultFactory;

    public static AppendingContextFactoryBuilder DefaultBuilder { get; set; } = new ();

    [AllowNull]
    public static IAppendingContextFactory DefaultFactory
    {
        get => defaultFactory ??= DefaultBuilder.Build();
        set => defaultFactory = value;
    }

    public IServiceCollection Services { get; } = new ServiceCollection();

    public AppendingContextFactoryBuilder()
    {
        Services.AddLogStrings();
    }
}
