using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Appender = System.Action<object, System.Text.StringBuilder, Diginsight.Strings.LoggingContext>;

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

        logStringable = IsHandled(type) switch
        {
            Handling.Pass => null,
            Handling.Handle => new LogStringable(obj, this),
            Handling.Forbid => new NonLogStringable(type),
            _ => throw new UnreachableException($"Unrecognized {nameof(Handling)}"),
        };

        return logStringable is not null;
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

    protected abstract Handling IsHandled(Type type);

    private void AppendCore(object obj, StringBuilder stringBuilder, LoggingContext loggingContext)
    {
        IEnumerable<Appender> GetAppenders()
        {
            Type type = obj.GetType();
            lock (((ICollection)appendersCache).SyncRoot)
            {
                return appendersCache.TryGetValue(type, out var appenders)
                    ? appenders
                    : appendersCache[type] = MakeAppenders(type);
            }
        }

        using IEnumerator<Appender> appenderEnumerator = GetAppenders().GetEnumerator();

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

    protected abstract Appender[] MakeAppenders(Type type);

    protected Appender MakeAppender(string? outputName, Type? providerType, PropertyInfo property)
    {
        return MakeAppender(outputName ?? property.Name, providerType, property.GetValue);
    }

    protected Appender MakeAppender(string? outputName, Type? providerType, FieldInfo field)
    {
        return MakeAppender(outputName ?? field.Name, providerType, field.GetValue);
    }

    protected Appender MakeAppender(string outputName, Type? providerType, Func<object, object?> getValue)
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
                    customProvider = customProvidersCache[providerType] = (ILogStringProvider)ActivatorUtilities.CreateInstance(serviceProvider, providerType);
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
                .Append(outputName)
                .Append(LogStringTokens.Value)
                .AppendLogString(finalGetValue(obj), loggingContext);
        };
    }

    protected abstract AllottingCounter Count(LoggingContext loggingContext);

    protected enum Handling : byte
    {
        Pass,
        Handle,
        Forbid,
    }
}
