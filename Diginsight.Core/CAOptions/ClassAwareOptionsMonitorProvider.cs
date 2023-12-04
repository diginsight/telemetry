using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Diginsight.CAOptions;

internal sealed class ClassAwareOptionsMonitorProvider<TOptions> : IClassAwareOptionsMonitorProvider<TOptions>
    where TOptions : class
{
    private readonly IServiceProvider serviceProvider;

    public ClassAwareOptionsMonitorProvider(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public IOptionsMonitor<TOptions> For(Type? @class)
    {
        return @class is null
            ? serviceProvider.GetRequiredService<IOptionsMonitor<TOptions>>()
            : (IOptionsMonitor<TOptions>)serviceProvider.GetRequiredService(typeof(IClassAwareOptionsMonitor<,>).MakeGenericType(typeof(TOptions), @class));
    }
}
