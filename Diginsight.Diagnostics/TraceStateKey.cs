namespace Diginsight.Diagnostics;

public readonly struct TraceStateKey(string? TenantId, string SystemId)
{
    public TraceStateKey(string systemId)
        : this(null, systemId) { }

    public override string ToString() => TenantId is null ? SystemId : $"{TenantId}@{SystemId}";
}
