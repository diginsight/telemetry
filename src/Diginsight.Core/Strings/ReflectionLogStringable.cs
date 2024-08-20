using System.Linq.Expressions;
using System.Reflection;

namespace Diginsight.Strings;

public abstract class ReflectionLogStringable : ILogStringable
{
    private static readonly MethodInfo AppendDirectStringMethod =
        typeof(AppendingContext).GetMethod(nameof(AppendingContext.AppendDirect), [ typeof(string) ])!;
    private static readonly MethodInfo AppendDirectCharMethod =
        typeof(AppendingContext).GetMethod(nameof(AppendingContext.AppendDirect), [ typeof(char) ])!;
    private static readonly MethodInfo AppendErrorMethod =
        typeof(AppendingContextExtensions).GetMethod(nameof(AppendingContextExtensions.AppendError))!;
    private static readonly MethodInfo ComposeAndAppendMethod =
        typeof(AppendingContext).GetMethod(nameof(AppendingContext.ComposeAndAppend))!;
    private static readonly MethodInfo TryToLogStringableMethod =
        typeof(ILogStringProvider).GetMethod(nameof(ILogStringProvider.TryToLogStringable))!;

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

    protected LogStringAppender MakeAppender(MemberInfo member, string? outputName, (Type, object[])? providerInfo)
    {
        outputName ??= member.Name;

        ParameterExpression oParam = Expression.Parameter(typeof(object), "o");
        ParameterExpression aParam = Expression.Parameter(typeof(AppendingContext), "a");

        LabelTarget returnTarget = Expression.Label();

        MemberExpression memberExpr = member switch
        {
            FieldInfo field => Expression.Field(Expression.Convert(oParam, field.DeclaringType!), field),
            PropertyInfo property => Expression.Property(Expression.Convert(oParam, property.DeclaringType!), property),
            _ => throw new ArgumentException("Expected field or property", nameof(member)),
        };

        ParameterExpression finalValueVar = Expression.Variable(typeof(object), "finalValue");

        static Expression Box(Expression expr) => expr.Type.IsValueType ? Expression.Convert(expr, typeof(object)) : expr;

        static Expression Null<T>() => Expression.Constant(null, typeof(T));

        Expression tryBodyExpr;
        if (providerInfo is var (providerType, providerArgs))
        {
            ILogStringProvider customLogStringProvider = helper.GetLogStringProvider(providerType, providerArgs);

            Type memberType = memberExpr.Type;
            ParameterExpression valueVar = Expression.Variable(memberType, "value");

            Expression finalValueExpr;
            if (!memberType.IsValueType)
            {
                Expression nullExpr = Null<object>();

                finalValueExpr = Expression.Condition(
                    Expression.NotEqual(valueVar, nullExpr),
                    Expression.Coalesce(
                        Expression.Call(Expression.Constant(customLogStringProvider, typeof(ILogStringProvider)), TryToLogStringableMethod, valueVar),
                        Expression.Convert(valueVar, typeof(object))
                    ),
                    nullExpr
                );
            }
            else if (Nullable.GetUnderlyingType(memberType) is not null)
            {
                finalValueExpr = Expression.Condition(
                    Expression.Property(valueVar, nameof(Nullable<int>.HasValue)),
                    Expression.Coalesce(
                        Expression.Call(Expression.Constant(customLogStringProvider, typeof(ILogStringProvider)), TryToLogStringableMethod, Box(Expression.Property(valueVar, nameof(Nullable<int>.Value)))),
                        Box(valueVar)
                    ),
                    Null<object>()
                );
            }
            else
            {
                Expression valueExpr = Box(valueVar);

                finalValueExpr = Expression.Coalesce(
                    Expression.Call(Expression.Constant(customLogStringProvider, typeof(ILogStringProvider)), TryToLogStringableMethod, valueExpr),
                    valueExpr
                );
            }

            tryBodyExpr = Expression.Block(
                typeof(void),
                [ valueVar ],
                Expression.Assign(valueVar, memberExpr),
                Expression.Assign(finalValueVar, finalValueExpr)
            );
        }
        else
        {
            tryBodyExpr = Expression.Block(typeof(void), Expression.Assign(finalValueVar, Box(memberExpr)));
        }

        Expression<LogStringAppender> appenderExpr = Expression.Lambda<LogStringAppender>(
            Expression.Block(
                typeof(void),
                [ finalValueVar ],
                Expression.Call(aParam, AppendDirectStringMethod, Expression.Constant(outputName, typeof(string))),
                Expression.Call(aParam, AppendDirectCharMethod, Expression.Constant(LogStringTokens.Value, typeof(char))),
                Expression.Block(
                    typeof(void),
                    Expression.TryCatch(
                        tryBodyExpr,
                        Expression.Catch(
                            typeof(Exception),
                            Expression.Block(
                                Expression.Call(AppendErrorMethod, aParam),
                                Expression.Return(returnTarget)
                            )
                        )
                    )
                ),
                Expression.Call(
                    aParam,
                    ComposeAndAppendMethod,
                    finalValueVar,
                    Null<bool?>(),
                    Null<Action<LogStringVariableConfiguration>>(),
                    Null<Action<IDictionary<string, object?>>>()
                ),
                Expression.Label(returnTarget)
            ),
            oParam,
            aParam
        );

        helper.LogAppenderExpression(member, outputName, providerInfo, appenderExpr);

        return appenderExpr.Compile();
    }

    protected abstract AllottingCounter Count(AppendingContext appendingContext);
}
