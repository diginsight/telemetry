using Microsoft.Extensions.DependencyInjection;

namespace Diginsight.SmartCache.Externalization;

public interface ICacheCompanionProviderInstaller
{
    void Install(IServiceCollection services, out Action uninstall);
}
