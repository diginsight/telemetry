using System.Reflection;
using System.Text;

namespace Diginsight.Strings;

public abstract class ReflectionLogStringable : ILogStringable
{
    private readonly object obj;
    private readonly IReflectionLogStringHelper helper;
    private readonly bool dontCacheAppenders;

    public bool IsDeep => true;
    public bool CanCycle => true;

    protected ReflectionLogStringable(object obj, IReflectionLogStringHelper helper, bool dontCacheAppenders = false)
    {
        this.obj = obj;
        this.helper = helper;
        this.dontCacheAppenders = dontCacheAppenders;
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
        Type type = obj.GetType();
        IEnumerable<LogStringAppender> appenders = dontCacheAppenders ? MakeAppenders(type) : helper.GetCachedAppenders(type, MakeAppenders);
        using IEnumerator<LogStringAppender> appenderEnumerator = appenders.GetEnumerator();

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

    protected abstract LogStringAppender[] MakeAppenders(Type type);

    protected LogStringAppender MakeAppender(string? outputName, Type? providerType, PropertyInfo property)
    {
        return MakeAppender(outputName ?? property.Name, providerType, property.GetValue);
    }

    protected LogStringAppender MakeAppender(string? outputName, Type? providerType, FieldInfo field)
    {
        return MakeAppender(outputName ?? field.Name, providerType, field.GetValue);
    }

    protected LogStringAppender MakeAppender(string outputName, Type? providerType, Func<object, object?> getValue)
    {
        Func<object, object?> finalGetValue;
        if (providerType is null)
        {
            finalGetValue = getValue;
        }
        else
        {
            ILogStringProvider customLogStringProvider = helper.GetLogStringProvider(providerType);
            finalGetValue = o => getValue(o) is { } value
                ? customLogStringProvider.TryAsLogStringable(value, out ILogStringable? logStringable)
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
