#region using
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace Common
{
    internal static class StringExtensions
    {
        public static ushort[] HexStringToWordArray(this string text, char[] separator)
        {
            string[] splits = text.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            List<ushort> words = new List<ushort>();
            foreach (string split in splits)
            {
                ushort word;

                if (ushort.TryParse(split.Replace("0x", "").Replace("0X", "").Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out word))
                    words.Add(word);
            }

            return words.ToArray();
        }
        public static ushort HexStringToWord(this string text)
        {
            ushort word;
            ushort.TryParse(text.Replace("0x", "").Replace("0X", "").Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out word);
            return word;
        }

        public static T ConvertFromInvariantString<T>(this string input, T def = default(T))
        {
            try
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                if (converter == null) { return def; }
                if (input == null) { return def; }

                // Cast ConvertFromString(string text) : object to (T)
                return (T)converter.ConvertFromInvariantString(input);
            }
            catch (Exception) { return def; }
        }
        public static T ConvertFrom<T>(this string input, T def = default(T))
        {
            try
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                if (converter == null) { return def; }
                if (input == null) { return def; }

                // Cast ConvertFromString(string text) : object to (T)
                return (T)converter.ConvertFrom(input);
            }
            catch (Exception) { return def; }
        }
        public static string FirstWord(this string x)
        {
            x = x.Trim();
            return (x.IndexOf(" ") != -1) ? x.Substring(0, x.IndexOf(" ")) : x;
        }
        public static bool IsNullOrEmptyOrWhiteSpace(this string x)
        {
            return string.IsNullOrEmpty(x) || string.IsNullOrWhiteSpace(x);
        }
        public static string UppercaseFirst(this string x)
        {
            return x != null ? char.ToUpper(x[0]) + x.Substring(1) : null;
        }

        public static string PadLeftExact(this string pthis, int size, char? padchar = null)
        {
            var s = pthis;
            if (s == null) { return padchar == null ? s : new string(padchar.Value, size); }

            if (s.Length > size) { s = s.Substring(s.Length - size); ; }
            if (s.Length < size) s = s.PadLeft(size);
            return s;
        }
        public static string PadRightExact(this string pthis, int size, char? padchar = null, bool truncateWithEllipses = true)
        {
            var s = pthis;
            if (s == null) { return padchar == null ? s : new string(padchar.Value, size); }

            if (s.Length > size) { s = truncateWithEllipses && size > 3 ? $"{s.Substring(0, size - 3)}..." : s.Substring(0, size); }
            if (s.Length < size) s = s.PadRight(size);
            return s;
        }
        public static string TrimSafe(this string pthis)
        {
            if (pthis == null) { return null; }
            return pthis.Trim();
        }
        public static string TrimEndSafe(this string pthis)
        {
            if (pthis == null) { return null; }
            return pthis.TrimEnd();
        }
        public static string TrimStartSafe(this string pthis)
        {
            if (pthis == null) { return null; }
            return pthis.TrimStart();
        }
        public static string ToUpperSafe(this string pthis)
        {
            if (pthis == null) { return null; }
            return pthis.ToUpper();
        }
        public static string ToLowerSafe(this string pthis)
        {
            if (pthis == null) { return null; }
            return pthis.ToLower();
        }

        public static bool EqualsSafe(this string pthis, string otherString)
        {
            if (pthis == null) { return (otherString == null); }
            return pthis.Equals(otherString);
        }

        #region ReplacePlaceholders
        public static string ReplacePlaceholders(this string pthis, Dictionary<string, string> dicPlaceholders)
        {
            if (dicPlaceholders == null) { return pthis; }
            if (string.IsNullOrEmpty(pthis)) { return pthis; }
            dicPlaceholders.ForEach(p => pthis = pthis.Replace(p.Key, p.Value));
            return pthis;
        }
        #endregion

        public static int IndexOfNth(this string str, string value, int nth = 1)
        {
            if (nth <= 0) throw new ArgumentException("Can not find the zeroth index of substring in string. Must start with 1");
            int offset = str.IndexOf(value);
            for (int i = 1; i < nth; i++)
            {
                if (offset == -1) return -1;
                offset = str.IndexOf(value, offset + 1);
            }
            return offset;
        }
    }
}
