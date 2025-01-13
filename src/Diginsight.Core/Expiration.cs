using System.ComponentModel;

namespace Diginsight;

/// <summary>
/// Represents an expiration time span with special handling for "Never" expiration.
/// </summary>
[TypeConverter(typeof(ExpirationConverter))]
public readonly struct Expiration
    : IComparable<Expiration>
        , IEquatable<Expiration>
#if NET
        , ISpanFormattable
#else
        , IFormattable
#endif
#if NET8_0_OR_GREATER
        , IUtf8SpanFormattable
#endif
#if NET7_0_OR_GREATER
        , ISpanParsable<Expiration>
#endif
{
    /// <summary>
    /// The string representation of a "Never" expiration.
    /// </summary>
    public const string NeverString = "Never";

    /// <summary>
    /// Represents an expiration of zero time span.
    /// </summary>
    public static readonly Expiration Zero = default;

    /// <summary>
    /// Represents a "Never" expiration.
    /// </summary>
    public static readonly Expiration Never = new (TimeSpan.Zero, true);

    private readonly TimeSpan underlying;

    /// <summary>
    /// Gets the underlying time span value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the expiration is "Never".</exception>
    public TimeSpan Value => IsNever ? throw new InvalidOperationException("No expiration") : underlying;

    /// <summary>
    /// Gets a value indicating whether the expiration is "Never".
    /// </summary>
    public bool IsNever { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Expiration" /> struct with a specified time span.
    /// </summary>
    /// <param name="value">The time span value.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the time span is negative.</exception>
    public Expiration(TimeSpan value)
        : this(value, false) { }

    private Expiration(TimeSpan value, bool isNever)
    {
        if (value < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Negative time span");
        }

        underlying = value;
        IsNever = isNever;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Expiration other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => IsNever ? HashCode.Combine(true, TimeSpan.Zero) : HashCode.Combine(false, underlying);

    /// <inheritdoc />
    public bool Equals(Expiration other)
    {
        return IsNever && other.IsNever ||
            !IsNever && !other.IsNever && underlying.Equals(other.underlying);
    }

    /// <inheritdoc />
    public int CompareTo(Expiration other)
    {
        return (IsNever, other.IsNever) switch
        {
            (true, true) => 0,
            (true, false) => 1,
            (false, true) => -1,
            _ => underlying.CompareTo(other.underlying),
        };
    }

    /// <inheritdoc />
    public override string ToString() => ToString(null, null);

    /// <summary>
    /// Converts the expiration to its string representation using the specified format and format provider.
    /// </summary>
    /// <param name="format">The format string.</param>
    /// <param name="formatProvider">The format provider.</param>
    /// <returns>The string representation of the expiration.</returns>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return IsNever ? NeverString : underlying.ToString(format, formatProvider);
    }

#if NET
    /// <inheritdoc/>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (!IsNever)
        {
            return underlying.TryFormat(destination, out charsWritten, format, provider);
        }

        ReadOnlySpan<char> span = NeverString;
        if (span.TryCopyTo(destination))
        {
            charsWritten = span.Length;
            return true;
        }
        else
        {
            charsWritten = 0;
            return false;
        }
    }
#endif

#if NET8_0_OR_GREATER
    /// <inheritdoc/>
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (!IsNever)
        {
            return underlying.TryFormat(utf8Destination, out bytesWritten, format, provider);
        }

        ReadOnlySpan<byte> span = "Never"u8;
        if (span.TryCopyTo(utf8Destination))
        {
            bytesWritten = span.Length;
            return true;
        }
        else
        {
            bytesWritten = 0;
            return false;
        }
    }
#endif

    /// <summary>
    /// Parses a string to create an <see cref="Expiration" /> instance.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="provider">The format provider.</param>
    /// <returns>The parsed <see cref="Expiration" /> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the input string is null.</exception>
    public static Expiration Parse(string s, IFormatProvider? provider)
    {
        return (s ?? throw new ArgumentNullException(nameof(s))).Equals(NeverString, StringComparison.OrdinalIgnoreCase)
            ? Never
            : new Expiration(TimeSpan.Parse(s, provider));
    }

    /// <summary>
    /// Tries to parse a string to create an <see cref="Expiration" /> instance.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="provider">The format provider.</param>
    /// <param name="result">When this method returns, contains the parsed <see cref="Expiration" /> instance.</param>
    /// <returns><c>true</c> if the string was successfully parsed; otherwise, <c>false</c>.</returns>
    public static bool TryParse(string? s, IFormatProvider? provider, out Expiration result)
    {
        if (s?.Equals(NeverString, StringComparison.OrdinalIgnoreCase) ?? false)
        {
            result = Never;
            return true;
        }
        else if (TimeSpan.TryParse(s, provider, out TimeSpan underlying))
        {
            result = new Expiration(underlying);
            return true;
        }
        else
        {
            result = default;
            return false;
        }
    }

#if NET || NETSTANDARD2_1_OR_GREATER
    /// <summary>
    /// Parses a read-only span of characters to create an <see cref="Expiration"/> instance.
    /// </summary>
    /// <param name="s">The span of characters to parse.</param>
    /// <param name="provider">The format provider.</param>
    /// <returns>The parsed <see cref="Expiration"/> instance.</returns>
    public static Expiration Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        return s.Equals(NeverString, StringComparison.OrdinalIgnoreCase) ? Never : new Expiration(TimeSpan.Parse(s, provider));
    }

    /// <summary>
    /// Tries to parse a read-only span of characters to create an <see cref="Expiration"/> instance.
    /// </summary>
    /// <param name="s">The span of characters to parse.</param>
    /// <param name="provider">The format provider.</param>
    /// <param name="result">When this method returns, contains the parsed <see cref="Expiration"/> instance.</param>
    /// <returns><c>true</c> if the span was successfully parsed; otherwise, <c>false</c>.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Expiration result)
    {
        if (s.Equals(NeverString, StringComparison.OrdinalIgnoreCase))
        {
            result = Never;
            return true;
        }
        else if (TimeSpan.TryParse(s, provider, out TimeSpan underlying))
        {
            result = new Expiration(underlying);
            return true;
        }
        else
        {
            result = default;
            return false;
        }
    }
#endif

    /// <summary>
    /// Adds two <see cref="Expiration" /> instances.
    /// </summary>
    /// <param name="lhs">The left-hand side <see cref="Expiration" />.</param>
    /// <param name="rhs">The right-hand side <see cref="Expiration" />.</param>
    /// <returns>The sum of the two <see cref="Expiration" /> instances.</returns>
    public static Expiration operator +(Expiration lhs, Expiration rhs)
    {
        return lhs.IsNever || rhs.IsNever ? Never : new Expiration(lhs.Value + rhs.Value);
    }

    /// <summary>
    /// Determines whether two <see cref="Expiration" /> instances are equal.
    /// </summary>
    /// <param name="lhs">The left-hand side <see cref="Expiration" />.</param>
    /// <param name="rhs">The right-hand side <see cref="Expiration" />.</param>
    /// <returns><c>true</c> if the instances are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(Expiration lhs, Expiration rhs) => lhs.Equals(rhs);

    /// <summary>
    /// Determines whether two <see cref="Expiration" /> instances are not equal.
    /// </summary>
    /// <param name="lhs">The left-hand side <see cref="Expiration" />.</param>
    /// <param name="rhs">The right-hand side <see cref="Expiration" />.</param>
    /// <returns><c>true</c> if the instances are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(Expiration lhs, Expiration rhs) => !(lhs == rhs);

    /// <summary>
    /// Determines whether one <see cref="Expiration" /> instance is greater than another.
    /// </summary>
    /// <param name="lhs">The left-hand side <see cref="Expiration" />.</param>
    /// <param name="rhs">The right-hand side <see cref="Expiration" />.</param>
    /// <returns><c>true</c> if the left-hand side instance is greater; otherwise, <c>false</c>.</returns>
    public static bool operator >(Expiration lhs, Expiration rhs) => lhs.CompareTo(rhs) > 0;

    /// <summary>
    /// Determines whether one <see cref="Expiration" /> instance is less than another.
    /// </summary>
    /// <param name="lhs">The left-hand side <see cref="Expiration" />.</param>
    /// <param name="rhs">The right-hand side <see cref="Expiration" />.</param>
    /// <returns><c>true</c> if the left-hand side instance is less; otherwise, <c>false</c>.</returns>
    public static bool operator <(Expiration lhs, Expiration rhs) => lhs.CompareTo(rhs) < 0;

    /// <summary>
    /// Determines whether one <see cref="Expiration" /> instance is greater than or equal to another.
    /// </summary>
    /// <param name="lhs">The left-hand side <see cref="Expiration" />.</param>
    /// <param name="rhs">The right-hand side <see cref="Expiration" />.</param>
    /// <returns><c>true</c> if the left-hand side instance is greater than or equal; otherwise, <c>false</c>.</returns>
    public static bool operator >=(Expiration lhs, Expiration rhs) => lhs.CompareTo(rhs) >= 0;

    /// <summary>
    /// Determines whether one <see cref="Expiration" /> instance is less than or equal to another.
    /// </summary>
    /// <param name="lhs">The left-hand side <see cref="Expiration" />.</param>
    /// <param name="rhs">The right-hand side <see cref="Expiration" />.</param>
    /// <returns><c>true</c> if the left-hand side instance is less than or equal; otherwise, <c>false</c>.</returns>
    public static bool operator <=(Expiration lhs, Expiration rhs) => lhs.CompareTo(rhs) <= 0;

    /// <summary>
    /// Implicitly converts a <see cref="TimeSpan" /> to an <see cref="Expiration" />.
    /// </summary>
    /// <param name="obj">The time span to convert.</param>
    public static implicit operator Expiration(TimeSpan obj) => new (obj);
}
