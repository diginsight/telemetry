
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Common
{
    public interface IProvideFormatStringParameters
    {
        StringBuilder FormatTemplate { get; }
        object[] FormatParameters { get; }
    }

#if NET6_0_OR_GREATER
    [InterpolatedStringHandler]
    public ref struct TraceLoggerInterpolatedStringHandler 
    {
        public StringBuilder FormatTemplate { get; }
        public object[] FormatParameters { get; }

        int i = 0;

        public TraceLoggerInterpolatedStringHandler(int literalLength, int formattedCount) // isEnalbed
        {
            this.FormatTemplate = new StringBuilder(literalLength + 3 * formattedCount);
            this.FormatParameters = new object[formattedCount];
        }

        public void AppendLiteral(string s)
        {
            this.FormatTemplate.Append(s.Replace("{", "{{").Replace("}", "}}"));
        }

        public void AppendFormatted<T>(T t)
        {
            this.FormatTemplate.Append($"{{{i}}}");
            this.FormatParameters[i] = t;
            i++;
        }
        public void AppendFormatted<T>(T t, string format) 
        {
            this.FormatTemplate.Append(t is IFormattable?  $"{{{i}:{format}}}": $"{{{i}}}");
            this.FormatParameters[i] = t;
            i++;
        }

        public string GetFormattedText() => string.Format(this.FormatTemplate.ToString(), this.FormatParameters);
        override public string ToString() => string.Format(this.FormatTemplate.ToString(), this.FormatParameters);
    }

#endif

}
