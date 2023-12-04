using Microsoft.Extensions.Options;

namespace Diginsight.CAOptions;

public interface IClassAwareOptionsMonitorProvider<out TOptions>
    where TOptions : class
{
    IOptionsMonitor<TOptions> For(Type? @class);
}
