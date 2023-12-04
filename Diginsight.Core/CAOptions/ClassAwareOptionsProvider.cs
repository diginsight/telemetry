using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Diginsight.CAOptions;

internal sealed class ClassAwareOptionsProvider<TOptions> : IClassAwareOptionsProvider<TOptions>
    where TOptions : class
{
    private readonly IServiceProvider serviceProvider;

    public ClassAwareOptionsProvider(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public IOptions<TOptions> For(Type? @class)
    {
        return @class is null
            ? serviceProvider.GetRequiredService<IOptions<TOptions>>()
            : (IOptions<TOptions>)serviceProvider.GetRequiredService(typeof(IClassAwareOptions<,>).MakeGenericType(typeof(TOptions), @class));
    }
}
