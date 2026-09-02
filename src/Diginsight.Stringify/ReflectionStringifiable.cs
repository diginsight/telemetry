using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Diginsight.Stringify;

/// <summary>
/// Represents a base class for reflection-based stringifiable values.
/// </summary>
public abstract class ReflectionStringifiable : IStringifiable
{
    private static readonly MethodInfo AppendDirectStringMethod =
        typeof(StringifyContext).GetMethod(nameof(StringifyContext.AppendDirect), [ typeof(string) ])!;

    private static readonly MethodInfo AppendDirectCharMethod =
        typeof(StringifyContext).GetMethod(nameof(StringifyContext.AppendDirect), [ typeof(char) ])!;

    private static readonly MethodInfo AppendErrorMethod =
        typeof(StringifyContextExtensions).GetMethod(nameof(StringifyContextExtensions.AppendError))!;

    private static readonly MethodInfo ComposeAndAppendMethod =
        typeof(StringifyContext).GetMethod(nameof(StringifyContext.ComposeAndAppend))!;

    private static readonly MethodInfo TryStringifyMethod =
        typeof(IStringifier).GetMethod(nameof(IStringifier.TryStringify))!;

    private readonly object obj;
    private readonly IReflectionStringifyHelper helper;
    private readonly bool dontCacheAppenders;

#if !(NET || NETSTANDARD2_1_OR_GREATER)
    bool IStringifiable.IsDeep => true;
#endif
    object IStringifiable.Subject => obj;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReflectionStringifiable" /> class.
    /// </summary>
    /// <param name="obj">The object to stringify.</param>
    /// <param name="helper">The reflection stringify helper.</param>
    /// <param name="dontCacheAppenders"><c>true</c> to rebuild the member appenders on each render; <c>false</c> to reuse cached appenders for the object type.</param>
    protected ReflectionStringifiable(object obj, IReflectionStringifyHelper helper, bool dontCacheAppenders = false)
    {
        this.obj = obj;
        this.helper = helper;
        this.dontCacheAppenders = dontCacheAppenders;
    }

    /// <inheritdoc />
    public void AppendTo(StringifyContext stringifyContext)
    {
        stringifyContext
            .ComposeAndAppendType(obj.GetType())
            .AppendDelimited(
                StringifyTokens.MapBegin,
                StringifyTokens.MapEnd,
                AppendCore
            );
    }

    private void AppendCore(StringifyContext stringifyContext)
    {
        Type type = obj.GetType();
        IEnumerable<StringifyAppender> appenders = dontCacheAppenders ? MakeAppenders(type) : helper.GetCachedAppenders(type, MakeAppenders);
        using IEnumerator<StringifyAppender> appenderEnumerator = appenders.GetEnumerator();

        stringifyContext.AppendEnumerator(
            appenderEnumerator,
            (sc, e) =>
            {
#if NET
                e.Current
#else
                e.Current!
#endif
                    (obj, sc);
            },
            Count(stringifyContext)
        );
    }

    /// <summary>
    /// Creates appenders for the specified type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The stringify appenders.</returns>
    protected abstract StringifyAppender[] MakeAppenders(Type type);

    /// <summary>
    /// Creates an appender for a reflected member.
    /// </summary>
    /// <param name="member">The member.</param>
    /// <param name="outputName">The output member name.</param>
    /// <param name="stringifierInfo">The custom stringifier information.</param>
    /// <returns>The compiled stringify appender.</returns>
    protected StringifyAppender MakeAppender(MemberInfo member, string? outputName, (Type, object[])? stringifierInfo)
    {
        outputName ??= member.Name;

        ParameterExpression oParam = Expression.Parameter(typeof(object), "o");
        ParameterExpression aParam = Expression.Parameter(typeof(StringifyContext), "a");

        LabelTarget returnTarget = Expression.Label();

        MemberExpression memberExpr = member switch
        {
            FieldInfo field => Expression.Field(Expression.Convert(oParam, field.DeclaringType!), field),
            PropertyInfo property => Expression.Property(Expression.Convert(oParam, property.DeclaringType!), property),
            _ => throw new ArgumentException("Expected field or property", nameof(member)),
        };

        ParameterExpression finalValueVar = Expression.Variable(typeof(object), "finalValue");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Expression Box(Expression expr) => expr.Type.IsValueType ? Expression.Convert(expr, typeof(object)) : expr;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Expression Null<T>() => Expression.Constant(null, typeof(T));

        Expression tryBodyExpr;
        if (stringifierInfo is var (stringifierType, stringifierArgs))
        {
            IStringifier customStringifier = helper.GetStringifier(stringifierType, stringifierArgs);

            Type memberType = memberExpr.Type;
            ParameterExpression valueVar = Expression.Variable(memberType, "value");

            Expression finalValueExpr;
            if (!memberType.IsValueType)
            {
                Expression nullExpr = Null<object>();

                finalValueExpr = Expression.Condition(
                    Expression.NotEqual(valueVar, nullExpr),
                    Expression.Coalesce(
                        Expression.Call(Expression.Constant(customStringifier, typeof(IStringifier)), TryStringifyMethod, valueVar),
                        Expression.Convert(valueVar, typeof(object))
                    ),
                    nullExpr
                );
            }
            else if (Nullable.GetUnderlyingType(memberType) is not null)
            {
                finalValueExpr = Expression.Condition(
                    Expression.Property(valueVar, nameof(Nullable<>.HasValue)),
                    Expression.Coalesce(
                        Expression.Call(Expression.Constant(customStringifier, typeof(IStringifier)), TryStringifyMethod, Box(Expression.Property(valueVar, nameof(Nullable<>.Value)))),
                        Box(valueVar)
                    ),
                    Null<object>()
                );
            }
            else
            {
                Expression valueExpr = Box(valueVar);

                finalValueExpr = Expression.Coalesce(
                    Expression.Call(Expression.Constant(customStringifier, typeof(IStringifier)), TryStringifyMethod, valueExpr),
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

        Expression<StringifyAppender> appenderExpr = Expression.Lambda<StringifyAppender>(
            Expression.Block(
                typeof(void),
                [ finalValueVar ],
                Expression.Call(aParam, AppendDirectStringMethod, Expression.Constant(outputName, typeof(string))),
                Expression.Call(aParam, AppendDirectCharMethod, Expression.Constant(StringifyTokens.Value, typeof(char))),
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
                    Null<Action<StringifyVariableConfiguration>>(),
                    Null<Action<IDictionary<string, object?>>>()
                ),
                Expression.Label(returnTarget)
            ),
            oParam,
            aParam
        );

        helper.LogAppenderExpression(member, outputName, stringifierInfo, appenderExpr);

        return appenderExpr.Compile();
    }

    /// <summary>
    /// Creates an allotted counter for the specified maximum count.
    /// </summary>
    /// <param name="stringifyContext">The stringify context.</param>
    /// <returns>The allotted counter.</returns>
    protected abstract AllottedCounter Count(StringifyContext stringifyContext);
}
