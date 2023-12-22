using System.Runtime.Serialization;
using System;

namespace Common
{
    [Serializable]
    public class BreakLoopException : Exception
    {
        object Item { get; set; }

        public BreakLoopException() : base() { }
        public BreakLoopException(string message) : base(message) { }
        public BreakLoopException(string message, Exception inner) : base(message, inner) { }
        //public BreakLoopException(string message, object item) : base(message) { this.Item = item; }
        //public BreakLoopException(string message, object item, Exception inner) : base(message, inner) { this.Item = item; }
        protected BreakLoopException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    }

    public interface IParallelServiceOptions
    {
        int LowConcurrency { get; set; }
        int MediumConcurrency { get; set; }
        int HighConcurrency { get; set; }
    }
}

