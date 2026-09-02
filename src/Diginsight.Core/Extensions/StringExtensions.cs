using Microsoft.Extensions.Primitives;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Diginsight;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class StringExtensions
{
    /// <summary>
    /// Converts the specified object to its string representation using the invariant culture.
    /// </summary>
    /// <remarks>
    /// Booleans are rendered in lowercase. <see cref="IFormattable" /> instances are formatted with <see cref="CultureInfo.InvariantCulture" />.
    /// </remarks>
    /// <param name="obj">The object to convert.</param>
    /// <returns>The invariant string representation of <paramref name="obj" />, or <c>null</c> when <paramref name="obj" /> is <c>null</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNullIfNotNull(nameof(obj))]
    public static string? ToStringInvariant(this object? obj)
    {
        return obj switch
        {
            bool b => b.ToString().ToLowerInvariant(),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => obj?.ToString(),
        };
    }

    /// <param name="str">The string (possibly <c>null</c>).</param>
    extension(string? str)
    {
        /// <summary>
        /// Trims the string and converts an empty result to <c>null</c>.
        /// </summary>
        /// <returns>The trimmed string, or <c>null</c> when the string is <c>null</c>, empty, or whitespace only.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string? HardTrim()
        {
            str = (str ?? "").Trim();
            return str switch
            {
                "" => null,
                _ => str,
            };
        }

        /// <summary>
        /// Truncates the string to the specified maximum length.
        /// </summary>
        /// <param name="length">The maximum number of characters to keep.</param>
        /// <returns>The truncated string, or the original string when it is <c>null</c> or not longer than <paramref name="length" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: NotNullIfNotNull(nameof(str))]
        public string? Truncate(int length)
        {
            return str?.Length > length ? str[..length] : str;
        }
    }

    /// <summary>
    /// Normalizes an HTTP header value into its constituent tokens, honoring quoting and escaping.
    /// </summary>
    /// <remarks>
    /// Values may be separated by commas and optionally enclosed in double quotes; escaped characters within quoted segments are unescaped.
    /// </remarks>
    /// <param name="stringValues">The raw header values to normalize.</param>
    /// <returns>The sequence of normalized header tokens.</returns>
    /// <exception cref="FormatException">Thrown when the header value is malformed, such as an unexpected escape, an unterminated quoted string, or a missing comma.</exception>
    public static IEnumerable<string> NormalizeHttpHeaderValue(this StringValues stringValues)
    {
        const int outerMode = 0,
            innerMode = 1,
            escapeMode = 2,
            commaMode = 3;

        ICollection<string> coll = new List<string>();
        foreach (string str in stringValues.OfType<string>())
        {
            int len = str.Length;

            int mode = outerMode;
            char[] dst = new char[len];
            int written = 0;

            void Flush()
            {
                coll.Add(new string(dst, 0, written));
                dst = new char[len];
                written = 0;
            }

            for (ReadOnlySpan<char> src = str.AsSpan(); !src.IsEmpty; src = src[1..])
            {
                char c = src[0];
                switch (mode, c)
                {
                    case (outerMode, ' '):
                        if (written > 0)
                        {
                            Flush();
                            mode = commaMode;
                        }
                        break;

                    case (outerMode, ','):
                        if (written > 0)
                        {
                            Flush();
                        }
                        break;

                    case (outerMode, '"'):
                        mode = innerMode;
                        break;

                    case (outerMode, '\\'):
                        throw new FormatException("Unexpected escape");

                    case (innerMode, '\\'):
                        mode = escapeMode;
                        break;

                    case (innerMode, '"'):
                        Flush();
                        mode = commaMode;
                        break;

                    case (outerMode or innerMode, _):
                        dst[written++] = c;
                        break;

                    case (escapeMode, _):
                        dst[written++] = c;
                        mode = innerMode;
                        break;

                    case (commaMode, ' '):
                        break;

                    case (commaMode, ','):
                        mode = outerMode;
                        break;

                    case (commaMode, _):
                        throw new FormatException("Expected comma or end of string");
                }
            }

            switch (mode)
            {
                case innerMode:
                    throw new FormatException("Unterminated quoted string");

                case escapeMode:
                    throw new FormatException("Dangling escape");

                default:
                    if (written > 0)
                    {
                        Flush();
                    }
                    break;
            }
        }

        return coll;
    }
}
