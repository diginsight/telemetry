using Microsoft.Extensions.DependencyInjection;

namespace Diginsight.SmartCache;

public interface ICacheCompanionProviderInstaller
{
    void Install(IServiceCollection services, out Action uninstall);
}
