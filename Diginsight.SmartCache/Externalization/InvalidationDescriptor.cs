namespace Diginsight.SmartCache.Externalization;

public sealed record InvalidationDescriptor(string Emitter, IInvalidationRule Rule);
