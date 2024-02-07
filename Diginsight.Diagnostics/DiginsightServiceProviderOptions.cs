using Microsoft.Extensions.DependencyInjection;

namespace Diginsight.Diagnostics;

public class DiginsightServiceProviderOptions : ServiceProviderOptions
{
    public IDeferredLoggerFactory? DeferredLoggerFactory { get; set; }
}
