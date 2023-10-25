using Microsoft.Extensions.DependencyInjection;
using System.Collections;

namespace Diginsight.Strings;

internal sealed class ReflectionLogStringHelper : IReflectionLogStringHelper
{
    private readonly IServiceProvider serviceProvider;
    private readonly IDictionary<Type, IEnumerable<LogStringAppender>> appendersCache = new Dictionary<Type, IEnumerable<LogStringAppender>>();
    private readonly IDictionary<Type, ILogStringProvider> customProvidersCache = new Dictionary<Type, ILogStringProvider>();

    public ReflectionLogStringHelper(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public IEnumerable<LogStringAppender> GetCachedAppenders(Type type, Func<Type, LogStringAppender[]> makeAppenders)
    {
        lock (((ICollection)appendersCache).SyncRoot)
        {
            return appendersCache.TryGetValue(type, out var appenders)
                ? appenders
                : appendersCache[type] = makeAppenders(type);
        }
    }

    public ILogStringProvider GetLogStringProvider(Type providerType, object[] providerArgs)
    {
        if (providerArgs.Length != 0)
        {
            return (ILogStringProvider)ActivatorUtilities.CreateInstance(serviceProvider, providerType, providerArgs);
        }

        lock (((ICollection)customProvidersCache).SyncRoot)
        {
            return customProvidersCache.TryGetValue(providerType, out ILogStringProvider? customProvider)
                ? customProvider
                : customProvidersCache[providerType] = (ILogStringProvider)ActivatorUtilities.CreateInstance(serviceProvider, providerType);
        }
    }
}
