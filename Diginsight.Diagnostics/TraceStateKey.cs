namespace Diginsight.Diagnostics;

// TODO Validate
public readonly struct TraceStateKey(string? TenantId, string SystemId)
{
    public TraceStateKey(string systemId)
        : this(null, systemId) { }

    public override string ToString() => TenantId is null ? SystemId : $"{TenantId}@{SystemId}";

    public static implicit operator TraceStateKey(string str)
    {
        return str.Split('@') switch
        {
            [ var systemId ] => new TraceStateKey(systemId),
            [ var tenantId, var systemId ] => new TraceStateKey(tenantId, systemId),
            _ => throw new FormatException("Invalid tracestate key"),
        };
    }
}
