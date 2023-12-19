using StackExchange.Redis;

namespace Diginsight.SmartCache;

public interface IRedisDatabaseAccessor
{
    IDatabase? Database { get; }
}
