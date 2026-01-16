using Microsoft.Extensions.Primitives;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Diginsight;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class StringExtensions
{
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

    extension(string? str)
    {
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: NotNullIfNotNull(nameof(str))]
        public string? Truncate(int length)
        {
            return str?.Length > length ? str[..length] : str;
        }
    }

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
