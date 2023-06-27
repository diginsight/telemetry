
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
#if NET6_0_OR_GREATER
    [InterpolatedStringHandler]
    public ref struct TraceLoggerInterpolatedStringHandler
    {
        internal StringBuilder formatTemplate;
        internal object[] formatParameters;
        int i = 0;

        public TraceLoggerInterpolatedStringHandler(int literalLength, int formattedCount) // object logger, LogLevel logLevel
        {
            formatTemplate = new StringBuilder(literalLength + 3 * formattedCount);
            this.formatParameters = new object[formattedCount];
        }

        public void AppendLiteral(string s)
        {
            formatTemplate.Append(s);
        }

        public void AppendFormatted<T>(T t)
        {
            formatTemplate.Append($"{{{i}}}");
            formatParameters[i] = t;
            i++;
        }

        public string GetFormattedText() => string.Format(formatTemplate.ToString(), formatParameters);
        //override public string ToString() => builder.ToString();
    }
#endif

}
