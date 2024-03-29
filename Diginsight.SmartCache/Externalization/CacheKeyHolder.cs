﻿using Diginsight.Strings;
using Microsoft.Extensions.Logging;

namespace Diginsight.SmartCache.Externalization;

public sealed class CacheKeyHolder : CachePayloadHolder<ICacheKey>, ILogStringable
{
    public ICacheKey Key => Payload;

    public bool IsDeep => false;
    public bool CanCycle => false;

    public CacheKeyHolder(ICacheKey key, ILogger logger)
        : base(key, logger, SmartCacheObservability.Tags.Subject.Key) { }

    public void AppendTo(AppendingContext appendingContext) => appendingContext.ComposeAndAppend(Key, false);
}
