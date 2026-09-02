using System.Linq.Expressions;
using System.Reflection;

namespace Diginsight.Stringify;

/// <summary>
/// Represents an interface for helper services used by reflection-based stringification.
/// </summary>
public interface IReflectionStringifyHelper
{
    /// <summary>
    /// Gets cached reflection appenders for the specified type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="makeAppenders">The function used to create appenders.</param>
    /// <returns>The cached appenders for the type.</returns>
    IEnumerable<StringifyAppender> GetCachedAppenders(Type type, Func<Type, StringifyAppender[]> makeAppenders);

    /// <summary>
    /// Gets a stringifier instance for the specified type and constructor arguments.
    /// </summary>
    /// <param name="stringifierType">The stringifier type.</param>
    /// <param name="stringifierArgs">The stringifier constructor arguments.</param>
    /// <returns>The stringifier instance.</returns>
    IStringifier GetStringifier(Type stringifierType, object[] stringifierArgs);

    /// <summary>
    /// Logs the expression used to append a member.
    /// </summary>
    /// <param name="member">The member.</param>
    /// <param name="outputName">The output member name.</param>
    /// <param name="stringifierInfo">The custom stringifier information.</param>
    /// <param name="appenderExpr">The appender expression.</param>
    void LogAppenderExpression(MemberInfo member, string outputName, (Type, object[])? stringifierInfo, Expression<StringifyAppender> appenderExpr);
}
