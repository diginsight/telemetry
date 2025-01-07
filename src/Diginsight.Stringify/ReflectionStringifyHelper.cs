using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace Diginsight.Stringify;

internal sealed class ReflectionStringifyHelper : IReflectionStringifyHelper
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger? logger;
    private readonly IStringifyContextFactory? stringifyContextFactory;
    private readonly IDictionary<Type, IEnumerable<StringifyAppender>> appendersCache = new Dictionary<Type, IEnumerable<StringifyAppender>>();
    private readonly IDictionary<Type, IStringifier> customStringifiersCache = new Dictionary<Type, IStringifier>();

    public ReflectionStringifyHelper(
        IServiceProvider serviceProvider,
        ILoggerFactory? loggerFactory = null,
        IStringifyContextFactory? stringifyContextFactory = null
    )
    {
        this.serviceProvider = serviceProvider;

        if (loggerFactory is not null && stringifyContextFactory is not null)
        {
            logger = loggerFactory.CreateLogger<ReflectionStringifyHelper>();
            this.stringifyContextFactory = stringifyContextFactory;
        }
        else
        {
            logger = null;
            this.stringifyContextFactory = null;
        }
    }

    public IEnumerable<StringifyAppender> GetCachedAppenders(Type type, Func<Type, StringifyAppender[]> makeAppenders)
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

    public IStringifier GetStringifier(Type stringifierType, object[] stringifierArgs)
    {
        if (stringifierArgs.Length != 0)
        {
            return (IStringifier)ActivatorUtilities.CreateInstance(serviceProvider, stringifierType, stringifierArgs);
        }

        lock (((ICollection)customStringifiersCache).SyncRoot)
        {
            return customStringifiersCache.TryGetValue(stringifierType, out IStringifier? customStringifier)
                ? customStringifier
                : customStringifiersCache[stringifierType] = (IStringifier)ActivatorUtilities.CreateInstance(serviceProvider, stringifierType);
        }
    }

    public void LogAppenderExpression(MemberInfo member, string outputName, (Type, object[])? stringifierInfo, Expression<StringifyAppender> appenderExpr)
    {
        if (logger is null)
        {
            return;
        }

        if (stringifierInfo is var (stringifierType, _))
        {
            logger.LogTrace(
                "Built appender expression for {Member} with name {Name} and stringifier {StringifierType}:\n{Expression}",
                member.Stringify(stringifyContextFactory),
                outputName,
                stringifierType.Stringify(stringifyContextFactory),
                appenderExpr.ToString()
            );
        }
        else
        {
            logger.LogTrace(
                "Built appender expression for {Member} with name {Name}:\n{Expression}",
                member.Stringify(stringifyContextFactory),
                outputName,
                appenderExpr.ToString()
            );
        }
    }
}
