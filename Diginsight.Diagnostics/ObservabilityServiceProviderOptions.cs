using Microsoft.Extensions.DependencyInjection;

namespace Diginsight.Diagnostics;

public class ObservabilityServiceProviderOptions : ServiceProviderOptions
{
    public IDeferredLoggerFactory? DeferredLoggerFactory { get; set; }
}
