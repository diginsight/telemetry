#region using
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using EkipConnect.Helpers;
#endregion

namespace Common
{

    public interface ISupportLogString
    {
        string ToLogString();
    }
    public interface IProvideLogString
    {
        string ToLogString(object source, HandledEventArgs arg);
    }

    public static partial class LogStringExtensions
    {
        private static readonly ICollection<IProvideLogString> providers = new List<IProvideLogString>();

        public static void RegisterLogstringProvider(IProvideLogString logStringProvider)
        {
            //LogStringProvider += logStringProvider;
            providers.Add(logStringProvider);
        }
        public static void AddLogstringProvider(IProvideLogString logStringProvider)
        {
            //LogStringProvider += logStringProvider;
            providers.Add(logStringProvider);
        }

        private static int _maxLogStringLen = 512;
        private static int _maxThicks = 1024;
        public static string GetLogString<T>(this T pthis, Func<string, string> adjust)
        {
            var res = pthis.GetLogString();
            res = adjust(res);
            return res;
        }
        public static string GetLogString<T>(this T pthis)
        {
            try
            {
                if (pthis == null) { return "null"; }

                switch (pthis)
                {
                    case KeyValuePair<string, string> o: return o.ToLogStringInternal();
                    case Type o: return o.ToLogStringInternal();
                    case Exception o: return o.ToLogStringInternal();
                    case ISupportLogString o: return o.ToLogString();
                    case byte[] o: return o.ToLogStringInternal();
                    case short[] o: return o.ToLogStringInternal();
                    case int[] o: return o.ToLogStringInternal();
                    case IDictionary o: return o.ToLogStringInternal();
                    case ICollection o: return o.ToLogStringInternal();
                    case Guid o: return o.ToLogStringInternal();
                    case Uri o: return o.ToLogStringInternal();
                    case DateTimeOffset o: return o.ToLogStringInternal();
                    case TimeSpan o: return o.ToLogStringInternal();
                    default: break;
                }

                var type = pthis.GetType();
                if (type.IsPrimitive || pthis is string || type.IsEnum)
                {
                    if (pthis is byte) { return ToLogStringInternal(Convert.ToByte(pthis)); }
                    return pthis.ToString();
                }
                if (type.IsAnonymousType())
                {
                    var props = type.GetProperties();
                    var propsString = string.Join(",", props.ToList().Select(p => $"{p.Name}:{p.GetValue(pthis).GetLogString()}").ToArray());
                    var typeString = $"{{{propsString}}}";
                    return propsString;
                }

                switch (pthis)
                {
                    case CultureInfo o: return o.ToLogStringInternal();
                    case Version o: return o.ToLogStringInternal();
                    case IEnumerable o: return o.ToLogStringInternal();
                }

                //if (LogStringProvider != null)
                //{
                //    var arg = new HandledEventArgs();
                //    var s = LogStringProvider.ToLogString(pthis, arg);
                //    if (!string.IsNullOrEmpty(s) || arg.Handled) { return s; }
                //}
                foreach (IProvideLogString provider in providers)
                {
                    var arg = new HandledEventArgs();
                    string logString = provider.ToLogString(pthis, arg);
                    if (arg.Handled) { return logString; }
                }

                return pthis.GetType().ToLogStringInternal();
            }
            catch (Exception /*ex*/) { }
            return null;
        }
        public static string ToLogStringInternal(this Uri pthis)
        {
            if (pthis.Equals(default(KeyValuePair<string, string>))) { return "null"; }
            string logString = $"{{{nameof(Uri)}:{{AbsoluteUri:{pthis.AbsoluteUri},OriginalString:{pthis.OriginalString},LocalPath:{pthis.LocalPath},AbsolutePath:{pthis.AbsolutePath},IsAbsoluteUri:{pthis.IsAbsoluteUri},UserEscaped:{pthis.UserEscaped},Scheme:{pthis.Scheme},Host:{pthis.Host},Authority:{pthis.Authority},Port:{pthis.Port},UserEscaped:{pthis.UserEscaped},DnsSafeHost:{pthis.DnsSafeHost},Fragment:{pthis.Fragment},HostNameType:{pthis.HostNameType},IdnHost:{pthis.IdnHost},IsDefaultPort:{pthis.IsDefaultPort},IsFile:{pthis.IsFile},IsLoopback:{pthis.IsLoopback},IsUnc:{pthis.IsUnc},PathAndQuery:{pthis.PathAndQuery}}}}}";
            return logString;
        }
        public static string ToLogStringInternal(this Guid pthis)
        {
            if (pthis.Equals(default(KeyValuePair<string, string>))) { return "null"; }
            var logString = pthis.ToString();
            return logString;
        }
        public static string ToLogStringInternal(this KeyValuePair<string, string> pthis)
        {
            if (pthis.Equals(default(KeyValuePair<string, string>))) { return "null"; }
            var logString = $"{{'{pthis.Key}':'{pthis.Value}'}}";
            return logString;
        }
        public static string ToLogStringInternal(this Exception pthis)
        {
            var logString = $"{pthis}";
            return logString;
        }
        private static string ToLogStringInternal(this IDictionary pthis)
        {
            var stopwatch = TraceManager.Stopwatch;
            var dic = pthis as IDictionary;
            var dicContent = new StringBuilder();

            var isLenOut = false; var isTimeOut = false;
            var startThicks = 0L;
            startThicks = stopwatch.ElapsedTicks;

            var list = dic.Keys.OfType<object>().TakeWhile(k =>
            {
                dicContent.Append($"{k}:{GetLogString(dic[k])},");

                isLenOut = dicContent.Length > _maxLogStringLen;
                isTimeOut = (stopwatch.ElapsedTicks - startThicks) > _maxThicks;
                return !isLenOut && !isTimeOut;
            }).ToList();
            if (dicContent.Length > 0) { dicContent.Length--; }
            if (isLenOut || isTimeOut) { dicContent.Append($"..."); }

            var logString = $"{GetLogString(dic.GetType())}(count {dic.Count}){{{(dicContent.Length > 0 ? dicContent.ToString() : null)}}}";
            return logString;
        }
        private static string ToLogStringInternal(this ICollection pthis)
        {
            var stopwatch = TraceManager.Stopwatch;
            var coll = pthis as ICollection;
            var collContent = new StringBuilder();

            var isLenOut = false; var isTimeOut = false;
            var startThicks = 0L;
            startThicks = stopwatch.ElapsedTicks;

            var list = coll.OfType<object>()?.TakeWhile(i =>
            {
                collContent.Append($"{GetLogString(i)},");

                isLenOut = collContent.Length > _maxLogStringLen;
                isTimeOut = (stopwatch.ElapsedTicks - startThicks) > _maxThicks;
                return !isLenOut && !isTimeOut;
            })?.ToList();
            if (collContent.Length > 0) { collContent.Length--; }
            if (isLenOut || isTimeOut) { collContent.Append($"..."); }
            var logString = $"{GetLogString(coll.GetType())}(count {coll.Count}){{{(collContent.Length > 0 ? collContent.ToString() : null)}}}";
            return logString;
        }
        private static string ToLogStringInternal(this IEnumerable pthis)
        {
            var stopwatch = TraceManager.Stopwatch;
            var coll = pthis as IEnumerable;
            var collContent = new StringBuilder();

            var isLenOut = false; var isTimeOut = false;
            var count = 0; var startThicks = 0L;
            startThicks = stopwatch.ElapsedTicks;

            var list = coll.OfType<object>()?.TakeWhile(i =>
            {
                count++;

                if (!isLenOut)
                {
                    collContent.Append($"{GetLogString(i)},");
                    isLenOut = collContent.Length > _maxLogStringLen;
                }

                isTimeOut = (stopwatch.ElapsedTicks - startThicks) > _maxThicks;
                return !isLenOut && !isTimeOut;
            })?.ToList();
            if (collContent.Length > 0) { collContent.Length--; }
            if (isLenOut || isTimeOut) { collContent.Append($"..."); }

            var logString = $"{GetLogString(coll.GetType())}(count {count}{((isLenOut || isTimeOut) ? "..." : "")}){{{(collContent.Length > 0 ? collContent.ToString() : null)}}}";
            return logString;
        }
        private static string ToLogStringInternal(this Type pthis)
        {
            if (pthis == null) { return "null"; }

            string logString = null;
            if (pthis.IsGenericType)
            {
                var arguments = pthis.GetGenericArguments();
                var argumentsDesc = string.Join(",", arguments.Select(p => p.GetLogString()).ToArray());
                logString = $"{pthis.Name}<{argumentsDesc}>";
            }
            else { logString = $"{pthis.Name}"; }
            return logString;
        }
        public static string ToLogStringInternal(this byte[] pthis)
        {
            var logString = $"byte[{pthis.Length}]{{{ArrayHelper.ByteArrayToString(pthis)}}}";
            return logString;
        }
        public static string ToLogStringInternal(this short[] pthis)
        {
            var logString = $"short[{pthis.Length}]{{{ArrayHelper.ShortArrayToString(pthis)}}}";
            return logString;
        }
        public static string ToLogStringInternal(this int[] pthis)
        {
            var logString = $"int[{pthis.Length}]{{{ArrayHelper.IntArrayToString(pthis)}}}";
            return logString;
        }
        private static string ToLogStringInternal(this byte pbyte)
        {
            //string logString = $"0x{pbyte:{{0:X2}}}";
            string logString = $"0x{pbyte.ToString("X2")}";
            return logString;
        }
        private static string ToLogStringInternal(this CultureInfo pthis)
        {
            string logString = $"{{CultureInfo:{{Name:{pthis.Name},DisplayName:{pthis.DisplayName},NativeName:{pthis.NativeName},EnglishName:{pthis.EnglishName},ThreeLetterISOLanguageName:{pthis.ThreeLetterISOLanguageName},ThreeLetterWindowsLanguageName:{pthis.ThreeLetterWindowsLanguageName},TwoLetterISOLanguageName:{pthis.TwoLetterISOLanguageName},DateTimeFormat:{pthis.DateTimeFormat},NumberFormat:{pthis.NumberFormat},LCID:{pthis.LCID},Calendar:{pthis.Calendar},CompareInfo:{pthis.CompareInfo},CultureTypes:{pthis.CultureTypes},IetfLanguageTag:{pthis.IetfLanguageTag},IsNeutralCulture:{pthis.IsNeutralCulture},IsReadOnly:{pthis.IsReadOnly},KeyboardLayoutId:{pthis.KeyboardLayoutId},OptionalCalendars:{pthis.OptionalCalendars},UseUserOverride:{pthis.UseUserOverride}}}}}";
            return logString;
        }
        private static string ToLogStringInternal(this Version pthis)
        {
            string logString = $"{{Version:{pthis.ToVersionString()}}}";
            return logString;
        }
        public static string ToLogStringInternal(this DateTimeOffset pthis)
        {
            if (pthis.Equals(default(DateTimeOffset))) { return "null"; }
            string logString = pthis.ToString("g");
            return logString;
        }
        public static string ToLogStringInternal(this TimeSpan pthis)
        {
            if (pthis.Equals(default(TimeSpan))) { return "null"; }
            string logString = pthis.ToString("g");
            return logString;
        }
        //case TimeSpan o: return o.ToLogStringInternal();

    }
}
