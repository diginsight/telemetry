using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

public static class RuntimeUtils
{
    private static readonly IDictionary<MethodBase, (Type, string?)> CallerCache = new Dictionary<MethodBase, (Type, string?)>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static (Type DeclaringType, string? LocalFunctionName) GetCaller(int stackDepth = 0)
    {
        if (stackDepth < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stackDepth), "Negative stack depth");
        }

        MethodBase method = new StackFrame(stackDepth + 2, false).GetMethod()!;
        // ReSharper disable once InconsistentlySynchronizedField
        if (CallerCache.TryGetValue(method, out var caller))
        {
            return caller;
        }

        lock (((ICollection)CallerCache).SyncRoot)
        {
            if (CallerCache.TryGetValue(method, out caller))
            {
                return caller;
            }

            Type innerDeclaringType = method.DeclaringType!;
            string methodName = method.Name;
            bool isGenerated = innerDeclaringType.FullName!.Contains('<') || methodName.Contains('<');

            string? localFunctionName;
            if (!isGenerated)
            {
                localFunctionName = null;
            }
            else
            {
                string innerDeclaringTypeName = innerDeclaringType.Name;
                ReadOnlySpan<char> span = (methodName == "MoveNext" ? innerDeclaringTypeName[1..^2] : methodName).AsSpan();
                span = span[(span.IndexOf('>') + 1)..];

                switch (span[0])
                {
                    case 'b':
                        localFunctionName = "";
                        break;

                    case 'g':
                        span = span[3..span.IndexOf('|')];
#if NET || NETSTANDARD2_1_OR_GREATER
                        localFunctionName = new string(span);
#else
                        localFunctionName = new string(span.ToArray());
#endif
                        break;

                    default:
                        localFunctionName = null;
                        break;
                }
            }

            Type declaringType = innerDeclaringType;
            if (isGenerated && !method.IsDefined(typeof(CompilerGeneratedAttribute)))
            {
                while (!declaringType.IsDefined(typeof(CompilerGeneratedAttribute)))
                {
                    declaringType = declaringType.DeclaringType!;
                }
                declaringType = declaringType.DeclaringType!;
            }

            return CallerCache[method] = (declaringType, localFunctionName);
        }
    }
}
