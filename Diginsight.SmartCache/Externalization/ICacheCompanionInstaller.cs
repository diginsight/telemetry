using Microsoft.Extensions.DependencyInjection;

namespace Diginsight.SmartCache.Externalization;

public interface ICacheCompanionInstaller
{
    void Install(IServiceCollection services, out Action uninstall);
}
