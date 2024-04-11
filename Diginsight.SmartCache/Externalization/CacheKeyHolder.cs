using Diginsight.Strings;

namespace Diginsight.SmartCache.Externalization;

public sealed class CacheKeyHolder : CachePayloadHolder<ICacheKey>, ILogStringable
{
    public ICacheKey Key => Payload;

    bool ILogStringable.IsDeep => false;
    bool ILogStringable.CanCycle => false;

    public CacheKeyHolder(ICacheKey key)
        : base(key, SmartCacheObservability.Tags.Subject.Key) { }

    public void AppendTo(AppendingContext appendingContext) => appendingContext.ComposeAndAppend(Key, false);
}
