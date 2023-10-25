using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;

namespace Diginsight.Diagnostics
{
    internal class Log4NetMessage
    {
        public string Message { get; set; }
        public bool IsActivity { get; set; }
        public TimeSpan? Duration { get; set; }

        public override string ToString()
        {
            return Message;
        }
    }
}
