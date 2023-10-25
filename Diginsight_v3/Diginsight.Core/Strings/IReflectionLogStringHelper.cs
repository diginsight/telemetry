namespace Diginsight.Strings;

public interface IReflectionLogStringHelper
{
    IEnumerable<LogStringAppender> GetCachedAppenders(Type type, Func<Type, LogStringAppender[]> makeAppenders);

    ILogStringProvider GetLogStringProvider(Type providerType);
}
