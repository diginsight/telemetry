using StackExchange.Redis;

namespace Diginsight.SmartCache.Externalization.Redis;

public interface IRedisDatabaseAccessor
{
    IDatabase? Database { get; }
}
