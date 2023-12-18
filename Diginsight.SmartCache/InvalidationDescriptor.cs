namespace Diginsight.SmartCache;

public sealed record InvalidationDescriptor(string Emitter, IInvalidationRule Rule);
