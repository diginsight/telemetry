using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

public readonly struct TraceStateKey : IEquatable<TraceStateKey>
{
    public string? TenantId { get; }
    public string SystemId { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TraceStateKey(string systemId)
        : this(null, systemId) { }

    public TraceStateKey(string? tenantId, string systemId)
        : this(tenantId, systemId, true) { }

    internal TraceStateKey(string? tenantId, string systemId, bool validate)
    {
        static bool IsValid(char ch, bool digit, bool punct)
        {
            return ch is >= 'a' and <= 'z'
                || digit && ch is >= '0' and <= '9'
                || punct && ch is '_' or '-' or '*' or '/';
        }

        static void Validate(string str, int maxLength, bool firstDigit, string argName)
        {
            int length = str.Length;

            if (length < 1 || length > maxLength)
                throw new ArgumentException("Invalid tracestate key length", argName);

            if (!IsValid(str[0], firstDigit, false))
                throw new ArgumentException("Invalid tracestate key character", argName);

            for (int i = 1; i < length; i++)
            {
                if (!IsValid(str[i], true, true))
                    throw new ArgumentException("Invalid tracestate key character", argName);
            }
        }

        if (systemId is null)
            throw new ArgumentNullException(nameof(systemId));

        if (validate)
        {
            if (tenantId is null)
            {
                Validate(systemId, 256, false, nameof(systemId));
            }
            else
            {
                Validate(tenantId, 241, false, nameof(tenantId));
                Validate(systemId, 14, false, nameof(systemId));
            }
        }

        TenantId = tenantId;
        SystemId = systemId;
    }

    public override string ToString() => TenantId is null ? SystemId : $"{TenantId}@{SystemId}";

    public override bool Equals(object? obj)
    {
        return obj is TraceStateKey other && Equals(other);
    }

    public bool Equals(TraceStateKey other)
    {
        return TenantId == other.TenantId && SystemId == other.SystemId;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(TenantId, SystemId);
    }

    public static bool operator ==(TraceStateKey left, TraceStateKey right) => left.Equals(right);

    public static bool operator !=(TraceStateKey left, TraceStateKey right) => !left.Equals(right);

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
