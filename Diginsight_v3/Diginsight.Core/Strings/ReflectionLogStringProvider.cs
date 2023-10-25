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
    private readonly IServiceProvider serviceProvider;

    private readonly IDictionary<Type, IEnumerable<Appender>> appendersCache
        = new Dictionary<Type, IEnumerable<Appender>>();

    private readonly IDictionary<Type, ILogStringProvider> customProvidersCache = new Dictionary<Type, ILogStringProvider>();

    protected ReflectionLogStringProvider(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public bool TryAsLogStringable(object obj, [NotNullWhen(true)] out ILogStringable? logStringable)
    {
        Type type = obj.GetType();

        logStringable = IsHandled(type) switch
        {
            Handling.Pass => null,
            Handling.Handle => MakeLogStringable(obj),
            Handling.Forbid => new NonLogStringable(type),
            _ => throw new UnreachableException($"Unrecognized {nameof(Handling)}"),
        };

        return logStringable is not null;
    }

    protected abstract Handling IsHandled(Type type);

    protected abstract ILogStringable MakeLogStringable(object obj);

    protected abstract class ReflectionLogStringable : ILogStringable
    {
        private readonly object obj;
        private readonly ReflectionLogStringProvider owner;

        public bool IsDeep => true;
        public bool CanCycle => true;

        protected ReflectionLogStringable(object obj, ReflectionLogStringProvider owner)
        {
            this.obj = obj;
            this.owner = owner;
        }

        public void AppendTo(StringBuilder stringBuilder, LoggingContext loggingContext)
        {
            loggingContext.Append(obj.GetType(), stringBuilder);

            stringBuilder.Append(LogStringTokens.MapBegin);
            AppendCore(stringBuilder, loggingContext);
            stringBuilder.Append(LogStringTokens.MapEnd);
        }

        private void AppendCore(StringBuilder stringBuilder, LoggingContext loggingContext)
        {
            IEnumerable<Appender> GetAppenders()
            {
                Type type = obj.GetType();
                IDictionary<Type, IEnumerable<Appender>> appendersCache = owner.appendersCache;

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
                IDictionary<Type, ILogStringProvider> customProvidersCache = owner.customProvidersCache;

                ILogStringProvider? customProvider;
                lock (((ICollection)customProvidersCache).SyncRoot)
                {
                    if (!customProvidersCache.TryGetValue(providerType, out customProvider))
                    {
                        customProvider = customProvidersCache[providerType] = (ILogStringProvider)ActivatorUtilities.CreateInstance(owner.serviceProvider, providerType);
                    }
                }

                finalGetValue = o => getValue(o) is { } value
                    ? customProvider.TryAsLogStringable(value, out ILogStringable? logStringable)
                        ? logStringable
                        : value
                    : null;
            }

            return (o, stringBuilder, loggingContext) =>
            {
                stringBuilder
                    .Append(outputName)
                    .Append(LogStringTokens.Value)
                    .AppendLogString(finalGetValue(o), loggingContext);
            };
        }

        protected abstract AllottingCounter Count(LoggingContext loggingContext);
    }

    protected enum Handling : byte
    {
        Pass,
        Handle,
        Forbid,
    }
}
