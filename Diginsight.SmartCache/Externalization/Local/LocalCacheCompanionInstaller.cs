using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Diginsight.SmartCache.Externalization.Local;

public sealed class LocalCacheCompanionInstaller : ICacheCompanionInstaller
{
    public static readonly ICacheCompanionInstaller Instance = new LocalCacheCompanionInstaller();

    private LocalCacheCompanionInstaller() { }

    public void Install(IServiceCollection services, out Action uninstall)
    {
        ServiceDescriptor sd0 = ServiceDescriptor.Singleton<ICacheCompanion, LocalCacheCompanion>();
        services.TryAdd(sd0);

        uninstall = Uninstall;

        void Uninstall()
        {
            services.Remove(sd0);
        }
    }
}
