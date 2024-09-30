using System.Linq.Expressions;
using System.Reflection;

namespace Diginsight.Stringify;

public interface IReflectionStringifyHelper
{
    IEnumerable<StringifyAppender> GetCachedAppenders(Type type, Func<Type, StringifyAppender[]> makeAppenders);

    IStringifier GetStringifier(Type stringifierType, object[] stringifierArgs);

    void LogAppenderExpression(MemberInfo member, string outputName, (Type, object[])? stringifierInfo, Expression<StringifyAppender> appenderExpr);
}
