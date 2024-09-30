using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace Diginsight.Strings;

internal sealed class ReflectionLogStringHelper : IReflectionLogStringHelper
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger? logger;
    private readonly IAppendingContextFactory? appendingContextFactory;
    private readonly IDictionary<Type, IEnumerable<LogStringAppender>> appendersCache = new Dictionary<Type, IEnumerable<LogStringAppender>>();
    private readonly IDictionary<Type, ILogStringProvider> customProvidersCache = new Dictionary<Type, ILogStringProvider>();

    public ReflectionLogStringHelper(
        IServiceProvider serviceProvider,
        ILoggerFactory? loggerFactory = null,
        IAppendingContextFactory? appendingContextFactory = null
    )
    {
        this.serviceProvider = serviceProvider;

        if (loggerFactory is not null && appendingContextFactory is not null)
        {
            logger = loggerFactory.CreateLogger<ReflectionLogStringHelper>();
            this.appendingContextFactory = appendingContextFactory;
        }
        else
        {
            logger = null;
            this.appendingContextFactory = null;
        }
    }

    public IEnumerable<LogStringAppender> GetCachedAppenders(Type type, Func<Type, LogStringAppender[]> makeAppenders)
    {
        if (appendersCache.TryGetValue(type, out var appenders))
        {
            return appenders;
        }

        lock (((ICollection)appendersCache).SyncRoot)
        {
            return appendersCache.TryGetValue(type, out appenders)
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

    public void LogAppenderExpression(MemberInfo member, string outputName, (Type, object[])? providerInfo, Expression<LogStringAppender> appenderExpr)
    {
        if (logger is null)
        {
            return;
        }

        if (providerInfo is var (providerType, _))
        {
            logger.LogTrace(
                "Built appender expression for {Member} with name {Name} and provider {ProviderType}:\n{Expression}",
                member.ToLogString(appendingContextFactory),
                outputName,
                providerType.ToLogString(appendingContextFactory),
                appenderExpr.ToString()
            );
        }
        else
        {
            logger.LogTrace(
                "Built appender expression for {Member} with name {Name}:\n{Expression}",
                member.ToLogString(appendingContextFactory),
                outputName,
                appenderExpr.ToString()
            );
        }
    }
}
