using Diginsight.SmartCache.Externalization;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;

namespace Diginsight.SmartCache;

[CacheInterchangeName("MCCK")]
public sealed record MethodCallCacheKey : ICacheKey
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MethodCallCacheKey(ICacheKeyService cacheKeyService, Type type, string methodName, params object?[]? arguments)
        : this(type, methodName, cacheKeyService.Wrap(arguments ?? [ ])) { }

    [JsonConstructor]
    public MethodCallCacheKey(Type type, string methodName, ICacheKey arguments)
    {
        Type = type;
        MethodName = methodName;
        Arguments = arguments;
    }

    public Type Type { get; }
    public string MethodName { get; }
    public ICacheKey Arguments { get; }
}
