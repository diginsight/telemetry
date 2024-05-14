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
                ReadOnlySpan<char> span0 = (methodName == "MoveNext" ? innerDeclaringTypeName[1..^2] : methodName).AsSpan();
                ReadOnlySpan<char> span1 = span0[(span0.IndexOf('>') + 1)..];

                switch (span1[0])
                {
                    case 'b':
                        localFunctionName = "";
                        break;

                    case 'g':
                    {
                        ReadOnlySpan<char> span2 = span1[3..span1.IndexOf('|')];
#if NET || NETSTANDARD2_1_OR_GREATER
                        localFunctionName = new string(span2);
#else
                        localFunctionName = new string(span2.ToArray());
#endif
                        break;
                    }

                    case 'd':
                    {
                        ReadOnlySpan<char> span2 = span0[..span0.IndexOf('>')];
#if NET || NETSTANDARD2_1_OR_GREATER
                        localFunctionName = new string(span2);
#else
                        localFunctionName = new string(span2.ToArray());
#endif
                        break;
                    }

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
