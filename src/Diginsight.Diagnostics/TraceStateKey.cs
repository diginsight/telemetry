using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

/// <summary>
/// Represents a W3C trace state key.
/// </summary>
public readonly struct TraceStateKey : IEquatable<TraceStateKey>
{
    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public string? TenantId { get; }
    /// <summary>
    /// Gets the system identifier.
    /// </summary>
    public string SystemId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TraceStateKey" /> struct.
    /// </summary>
    /// <param name="systemId">The system identifier.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="systemId" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="systemId" /> is invalid.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TraceStateKey(string systemId)
        : this(null, systemId) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TraceStateKey" /> struct.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="systemId">The system identifier.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="systemId" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tenantId" /> or <paramref name="systemId" /> is invalid.</exception>
    public TraceStateKey(string? tenantId, string systemId)
        : this(tenantId, systemId, true) { }

    internal TraceStateKey(string? tenantId, string systemId, bool validate)
    {
        static bool IsValid(char ch, bool digit, bool punct)
        {
            return ch is >= 'a' and <= 'z'
                || (digit && ch is >= '0' and <= '9')
                || (punct && ch is '_' or '-' or '*' or '/');
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

    /// <inheritdoc />
    public override string ToString() => TenantId is null ? SystemId : $"{TenantId}@{SystemId}";

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is TraceStateKey other && Equals(other);
    }

    /// <inheritdoc />
    public bool Equals(TraceStateKey other)
    {
        return TenantId == other.TenantId && SystemId == other.SystemId;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(TenantId, SystemId);
    }

    /// <summary>
    /// Determines whether two <see cref="TraceStateKey" /> instances are equal.
    /// </summary>
    /// <param name="left">The left-hand side <see cref="TraceStateKey" />.</param>
    /// <param name="right">The right-hand side <see cref="TraceStateKey" />.</param>
    /// <returns><c>true</c> if the instances are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(TraceStateKey left, TraceStateKey right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="TraceStateKey" /> instances are not equal.
    /// </summary>
    /// <param name="left">The left-hand side <see cref="TraceStateKey" />.</param>
    /// <param name="right">The right-hand side <see cref="TraceStateKey" />.</param>
    /// <returns><c>true</c> if the instances are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(TraceStateKey left, TraceStateKey right) => !left.Equals(right);

    /// <summary>
    /// Implicitly converts a string to a <see cref="TraceStateKey" />.
    /// </summary>
    /// <param name="str">The string to convert.</param>
    /// <returns>The converted <see cref="TraceStateKey" /> instance.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="str" /> is invalid.</exception>
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
