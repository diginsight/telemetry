using Diginsight.Strings;

namespace Diginsight.SmartCache;

public sealed class CacheKeyHolder : CachePayloadHolder<ICacheKey>, ILogStringable
{
    public ICacheKey Key => Payload;

    public bool IsDeep => false;
    public bool CanCycle => false;

    public CacheKeyHolder(ICacheKey key)
        : base(key, SmartCacheMetrics.Tags.Subject.Key) { }

    public void AppendTo(AppendingContext appendingContext) => appendingContext.ComposeAndAppend(Key, false);
}
