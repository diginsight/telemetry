using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Diginsight.CAOptions;

internal sealed class ClassAwareOptionsSnapshotProvider<TOptions> : IClassAwareOptionsSnapshotProvider<TOptions>
    where TOptions : class
{
    private readonly IServiceProvider serviceProvider;

    public ClassAwareOptionsSnapshotProvider(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public IOptionsSnapshot<TOptions> For(Type? @class)
    {
        return @class is null
            ? serviceProvider.GetRequiredService<IOptionsSnapshot<TOptions>>()
            : (IOptionsSnapshot<TOptions>)serviceProvider.GetRequiredService(typeof(IClassAwareOptionsSnapshot<,>).MakeGenericType(typeof(TOptions), @class));
    }
}
