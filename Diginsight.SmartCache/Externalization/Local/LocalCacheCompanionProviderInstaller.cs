using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Diginsight.SmartCache.Externalization.Local;

public sealed class LocalCacheCompanionProviderInstaller : ICacheCompanionProviderInstaller
{
    public static readonly ICacheCompanionProviderInstaller Instance = new LocalCacheCompanionProviderInstaller();

    private LocalCacheCompanionProviderInstaller() { }

    public void Install(IServiceCollection services, out Action uninstall)
    {
        ServiceDescriptor sd0 = ServiceDescriptor.Singleton<ICacheCompanionProvider, LocalCacheCompanionProvider>();
        services.TryAdd(sd0);

        uninstall = Uninstall;

        void Uninstall()
        {
            services.Remove(sd0);
        }
    }
}
