using Diginsight.Strings;
using System.ComponentModel;

namespace Diginsight;

[TypeConverter(typeof(ExpirationConverter))]
public readonly struct Expiration
    : ILogStringable
        , IComparable<Expiration>
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
    private const string NeverString = "Never";
    public static readonly Expiration Zero = default;
    public static readonly Expiration Never = new (default, true);

    private readonly TimeSpan underlying;

    public TimeSpan Value => IsNever ? throw new InvalidOperationException("No expiration") : underlying;

    public bool IsNever { get; }

    bool ILogStringable.IsDeep => false;
    object? ILogStringable.Subject => null;

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

    void ILogStringable.AppendTo(AppendingContext appendingContext)
    {
        if (IsNever)
        {
            appendingContext.AppendDirect($"{LogStringTokens.LiteralBegin}Never{LogStringTokens.LiteralEnd}");
        }
        else
        {
            appendingContext.ComposeAndAppend(underlying);
        }
    }

    public override bool Equals(object? obj) => obj is Expiration other && Equals(other);

    public override int GetHashCode() => IsNever ? HashCode.Combine(true, default(TimeSpan)) : HashCode.Combine(false, underlying);

    public bool Equals(Expiration other)
    {
        return IsNever && other.IsNever ||
            !IsNever && !other.IsNever && underlying.Equals(other.underlying);
    }

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

    public override string ToString() => ToString(null, null);

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return IsNever ? NeverString : underlying.ToString(format, formatProvider);
    }

#if NET
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

    public static Expiration Parse(string s, IFormatProvider? provider)
    {
        return (s ?? throw new ArgumentNullException(nameof(s))).Equals(NeverString, StringComparison.OrdinalIgnoreCase)
            ? Never
            : new Expiration(TimeSpan.Parse(s, provider));
    }

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
    public static Expiration Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        return s.Equals(NeverString, StringComparison.OrdinalIgnoreCase) ? Never : new Expiration(TimeSpan.Parse(s, provider));
    }

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

    public static Expiration operator +(Expiration lhs, Expiration rhs)
    {
        return lhs.IsNever || rhs.IsNever ? Never : new Expiration(lhs.Value + rhs.Value);
    }

    public static bool operator ==(Expiration lhs, Expiration rhs) => lhs.Equals(rhs);

    public static bool operator !=(Expiration lhs, Expiration rhs) => !(lhs == rhs);

    public static bool operator >(Expiration lhs, Expiration rhs) => lhs.CompareTo(rhs) > 0;

    public static bool operator <(Expiration lhs, Expiration rhs) => lhs.CompareTo(rhs) < 0;

    public static bool operator >=(Expiration lhs, Expiration rhs) => lhs.CompareTo(rhs) >= 0;

    public static bool operator <=(Expiration lhs, Expiration rhs) => lhs.CompareTo(rhs) <= 0;

    public static implicit operator Expiration(TimeSpan obj) => new (obj);
}
