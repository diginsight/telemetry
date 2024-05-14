using System.Reflection;

namespace Diginsight.Strings;

public abstract class ReflectionLogStringable : ILogStringable
{
    private readonly object obj;
    private readonly IReflectionLogStringHelper helper;
    private readonly bool dontCacheAppenders;

#if !(NET || NETSTANDARD2_1_OR_GREATER)
    bool ILogStringable.IsDeep => true;
#endif
    object ILogStringable.Subject => obj;

    protected ReflectionLogStringable(object obj, IReflectionLogStringHelper helper, bool dontCacheAppenders = false)
    {
        this.obj = obj;
        this.helper = helper;
        this.dontCacheAppenders = dontCacheAppenders;
    }

    public void AppendTo(AppendingContext appendingContext)
    {
        appendingContext
            .ComposeAndAppendType(obj.GetType())
            .AppendDelimited(
                LogStringTokens.MapBegin,
                LogStringTokens.MapEnd,
                AppendCore
            );
    }

    private void AppendCore(AppendingContext appendingContext)
    {
        Type type = obj.GetType();
        IEnumerable<LogStringAppender> appenders = dontCacheAppenders ? MakeAppenders(type) : helper.GetCachedAppenders(type, MakeAppenders);
        using IEnumerator<LogStringAppender> appenderEnumerator = appenders.GetEnumerator();

        appendingContext.AppendEnumerator(
            appenderEnumerator,
            (ac, e) => { e.Current(obj, ac); },
            Count(appendingContext)
        );
    }

    protected abstract LogStringAppender[] MakeAppenders(Type type);

    protected LogStringAppender MakeAppender(string? outputName, (Type, object[])? providerInfo, PropertyInfo property)
    {
        return MakeAppender(outputName ?? property.Name, providerInfo, property.GetValue);
    }

    protected LogStringAppender MakeAppender(string? outputName, (Type, object[])? providerInfo, FieldInfo field)
    {
        return MakeAppender(outputName ?? field.Name, providerInfo, field.GetValue);
    }

    protected LogStringAppender MakeAppender(string outputName, (Type, object[])? providerInfo, Func<object, object?> getValue)
    {
        Func<object, object?> finalGetValue;
        if (providerInfo is var (providerType, providerArgs))
        {
            ILogStringProvider customLogStringProvider = helper.GetLogStringProvider(providerType, providerArgs);
            finalGetValue = o => getValue(o) is { } value
                ? customLogStringProvider.TryToLogStringable(value) ?? value
                : null;
        }
        else
        {
            finalGetValue = getValue;
        }

        return (o, appendingContext) =>
        {
            appendingContext.AppendDirect(outputName).AppendDirect(LogStringTokens.Value);

            object? finalValue;
            try
            {
                finalValue = finalGetValue(o);
            }
            catch (Exception)
            {
                appendingContext.AppendError();
                return;
            }

            appendingContext.ComposeAndAppend(finalValue);
        };
    }

    protected abstract AllottingCounter Count(AppendingContext appendingContext);
}
