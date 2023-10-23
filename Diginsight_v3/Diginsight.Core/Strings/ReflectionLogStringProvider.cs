using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace Diginsight.Strings;

internal abstract class ReflectionLogStringProvider : ILogStringProvider
{
    private readonly IMemberLogStringProvider memberLogStringProvider;
    private readonly IServiceProvider serviceProvider;

    private readonly IDictionary<Type, IEnumerable<Action<object, StringBuilder, LoggingContext>>> appendersCache
        = new Dictionary<Type, IEnumerable<Action<object, StringBuilder, LoggingContext>>>();

    private readonly IDictionary<Type, ILogStringProvider> customProvidersCache = new Dictionary<Type, ILogStringProvider>();

    protected ReflectionLogStringProvider(
        IMemberLogStringProvider memberLogStringProvider,
        IServiceProvider serviceProvider
    )
    {
        this.memberLogStringProvider = memberLogStringProvider;
        this.serviceProvider = serviceProvider;
    }

    public bool TryAsLogStringable(object obj, [NotNullWhen(true)] out ILogStringable? logStringable)
    {
        Type type = obj.GetType();

        if (IsHandled(type))
        {
            logStringable = new LogStringable(obj, this);
            return true;
        }
        else
        {
            logStringable = null;
            return false;
        }
    }

    private sealed class LogStringable : ILogStringable
    {
        private readonly object obj;
        private readonly ReflectionLogStringProvider owner;

        public bool IsDeep => true;
        public bool CanCycle => true;

        public LogStringable(object obj, ReflectionLogStringProvider owner)
        {
            this.obj = obj;
            this.owner = owner;
        }

        public void AppendTo(StringBuilder stringBuilder, LoggingContext loggingContext)
        {
            owner.memberLogStringProvider.Append(obj.GetType(), stringBuilder, loggingContext);

            stringBuilder.Append(LogStringTokens.MapBegin);
            owner.AppendCore(obj, stringBuilder, loggingContext);
            stringBuilder.Append(LogStringTokens.MapEnd);
        }
    }

    protected abstract bool IsHandled(Type type);

    private void AppendCore(object obj, StringBuilder stringBuilder, LoggingContext loggingContext)
    {
        IEnumerable<Action<object, StringBuilder, LoggingContext>> GetAppenders()
        {
            Type type = obj.GetType();
            lock (((ICollection)appendersCache).SyncRoot)
            {
                if (!appendersCache.TryGetValue(type, out var appenders))
                {
                    appendersCache[type] = appenders = MakeAppenders(type);
                }

                return appenders;
            }
        }

        using IEnumerator<Action<object, StringBuilder, LoggingContext>> appenderEnumerator = GetAppenders().GetEnumerator();

        if (!appenderEnumerator.MoveNext())
            return;

        AllottingCounter counter = Count(loggingContext);

        try
        {
            void AppendEntry()
            {
                counter.Decrement();
                appenderEnumerator.Current!(obj, stringBuilder, loggingContext);
            }

            AppendEntry();
            while (appenderEnumerator.MoveNext())
            {
                stringBuilder.Append(LogStringTokens.Separator2);
                AppendEntry();
            }
        }
        catch (MaxAllottedShortCircuit)
        {
            stringBuilder.Append(LogStringTokens.Ellipsis);
        }
    }

    protected abstract Action<object, StringBuilder, LoggingContext>[] MakeAppenders(Type type);

    protected Action<object, StringBuilder, LoggingContext> MakeAppender(string? outputName, Type? providerType, PropertyInfo property)
    {
        return MakeAppender(outputName, providerType, property, property.GetValue);
    }

    protected Action<object, StringBuilder, LoggingContext> MakeAppender(string? outputName, Type? providerType, FieldInfo field)
    {
        return MakeAppender(outputName, providerType, field, field.GetValue);
    }

    private Action<object, StringBuilder, LoggingContext> MakeAppender(string? outputName, Type? providerType, MemberInfo member, Func<object, object?> getValue)
    {
        Func<object, object?> finalGetValue;
        if (providerType is null)
        {
            finalGetValue = getValue;
        }
        else
        {
            ILogStringProvider? customProvider;
            lock (((ICollection)customProvidersCache).SyncRoot)
            {
                if (!customProvidersCache.TryGetValue(providerType, out customProvider))
                {
                    customProvidersCache[providerType] = customProvider = (ILogStringProvider)ActivatorUtilities.CreateInstance(serviceProvider, providerType);
                }
            }

            finalGetValue = obj => getValue(obj) is { } value
                ? customProvider.TryAsLogStringable(value, out ILogStringable? logStringable)
                    ? logStringable
                    : value
                : null;
        }

        return (obj, stringBuilder, loggingContext) =>
        {
            stringBuilder
                .Append(outputName ?? member.Name)
                .Append(LogStringTokens.Value)
                .AppendLogString(finalGetValue(obj), loggingContext);
        };
    }

    protected abstract AllottingCounter Count(LoggingContext loggingContext);
}
