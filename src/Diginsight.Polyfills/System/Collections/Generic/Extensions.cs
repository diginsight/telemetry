#if !(NET || NETSTANDARD2_1_OR_GREATER)
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Generic
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class Extensions
    {
        extension<T>(Stack<T> stack)
        {
            public bool TryPeek([MaybeNullWhen(false)] out T result)
            {
                if (stack.Count > 0)
                {
                    result = stack.Peek();
                    return true;
                }
                else
                {
                    result = default;
                    return false;
                }
            }

            public bool TryPop([MaybeNullWhen(false)] out T result)
            {
                if (stack.Count > 0)
                {
                    result = stack.Pop();
                    return true;
                }
                else
                {
                    result = default;
                    return false;
                }
            }
        }
    }
}
#endif
