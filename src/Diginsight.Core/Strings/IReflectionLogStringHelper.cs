using System.Linq.Expressions;
using System.Reflection;

namespace Diginsight.Strings;

public interface IReflectionLogStringHelper
{
    IEnumerable<LogStringAppender> GetCachedAppenders(Type type, Func<Type, LogStringAppender[]> makeAppenders);

    ILogStringProvider GetLogStringProvider(Type providerType, object[] providerArgs);

    void LogAppenderExpression(MemberInfo member, string outputName, (Type, object[])? providerInfo, Expression<LogStringAppender> appenderExpr);
}
