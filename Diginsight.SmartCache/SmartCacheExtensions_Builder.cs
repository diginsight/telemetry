using Diginsight.SmartCache.Externalization;
using Diginsight.SmartCache.Externalization.Local;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Diginsight.SmartCache;

public static partial class SmartCacheExtensions
{
    public static SmartCacheBuilder SetCompanion(
        this SmartCacheBuilder builder, ICacheCompanionInstaller installer
    )
    {
        CacheCompanionUninstaller uninstaller;
        if (builder.Services.FirstOrDefault(static x => x.ServiceType == typeof(CacheCompanionUninstaller)) is { } uninstallerServiceDescriptor)
        {
            uninstaller = (CacheCompanionUninstaller)uninstallerServiceDescriptor.ImplementationInstance!;
        }
        else
        {
            uninstaller = new CacheCompanionUninstaller();
            builder.Services.AddSingleton(uninstaller);
        }

        uninstaller.Uninstall?.Invoke();
        installer.Install(builder.Services, out Action uninstall);
        uninstaller.Uninstall = uninstall;

        return builder;
    }

    private sealed class CacheCompanionUninstaller
    {
        public Action? Uninstall { get; set; }
    }

    public static SmartCacheBuilder SetLocalCompanion(this SmartCacheBuilder builder) =>
        builder.SetCompanion(LocalCacheCompanionInstaller.Instance);

    public static SmartCacheBuilder SetSizeLimit(this SmartCacheBuilder builder, long? sizeLimit)
    {
        builder.Services.Configure<MemoryCacheOptions>(nameof(SmartCache), x => { x.SizeLimit = sizeLimit; });
        return builder;
    }
}
