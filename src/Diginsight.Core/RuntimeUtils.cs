using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Diginsight;

public static class RuntimeUtils
{
    private static readonly Type AsyncMethodBuilderCoreType = Type.GetType("System.Runtime.CompilerServices.AsyncMethodBuilderCore")!;

    private static readonly IDictionary<MethodBase, Type> CallerTypeCache = new Dictionary<MethodBase, Type>();
    private static readonly IDictionary<MethodBase, (string, string?)> CallerNameCache = new Dictionary<MethodBase, (string, string?)>();

    private static MethodBase FindMethod(int skipMethods)
    {
        int maxFrames = new StackTrace().FrameCount;
        int skipFrames = 1;

        while (skipMethods >= 0)
        {
            bool isAsync = skipFrames + 1 < maxFrames && new StackFrame(skipFrames + 1).GetMethod()!.DeclaringType == AsyncMethodBuilderCoreType;
            skipFrames += isAsync ? 3 : 1;

            skipMethods--;
        }

        return new StackFrame(skipFrames).GetMethod()!;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Type GetCallerType(int stackDepth = 1)
    {
        if (stackDepth < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stackDepth), "Negative stack depth");
        }

        MethodBase method = FindMethod(stackDepth);
        if (CallerTypeCache.TryGetValue(method, out Type? cached))
        {
            return cached;
        }

        lock (((ICollection)CallerTypeCache).SyncRoot)
        {
            return CallerTypeCache.TryGetValue(method, out cached)
                ? cached
                : CallerTypeCache[method] = CoreGet(method);

            static Type CoreGet(MethodBase method)
            {
                Type innerDeclaringType = method.DeclaringType!;
                bool isGenerated = Enumerable.Contains(innerDeclaringType.FullName!, '<') || Enumerable.Contains(method.Name, '<');

                Type declaringType = innerDeclaringType;
                if (isGenerated && !method.IsDefined(typeof(CompilerGeneratedAttribute)))
                {
                    while (!declaringType.IsDefined(typeof(CompilerGeneratedAttribute)))
                    {
                        declaringType = declaringType.DeclaringType!;
                    }
                    declaringType = declaringType.DeclaringType!;
                }

                return declaringType;
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static (string Member, string? LocalFunction) GetCallerName(int stackDepth = 1)
    {
        if (stackDepth < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stackDepth), "Negative stack depth");
        }

        MethodBase method = FindMethod(stackDepth);
        if (CallerNameCache.TryGetValue(method, out (string, string?) cached))
        {
            return cached;
        }

        lock (((ICollection)CallerNameCache).SyncRoot)
        {
            return CallerNameCache.TryGetValue(method, out cached)
                ? cached
                : CallerNameCache[method] = CoreGet(method);

            static (string, string?) CoreGet(MethodBase method)
            {
                Type innerDeclaringType = method.DeclaringType!;
                string methodName = method.Name;
                bool isGenerated = Enumerable.Contains(innerDeclaringType.FullName!, '<') || Enumerable.Contains(methodName, '<');

                string memberName;
                string? localFunctionName;
                if (!isGenerated)
                {
                    memberName = methodName;
                    localFunctionName = null;
                }
                else
                {
                    string innerDeclaringTypeName = innerDeclaringType.Name;
                    ReadOnlySpan<char> span = (methodName == "MoveNext" ? innerDeclaringTypeName[..^2] : methodName).AsSpan();

                    int closeAngleIndex = span.IndexOf('>');
#if NET || NETSTANDARD2_1_OR_GREATER
                    memberName = new string(span[(span.LastIndexOf('<') + 1) ..closeAngleIndex]);
#else
                    memberName = new string(span[(span.LastIndexOf('<') + 1) ..closeAngleIndex].ToArray());
#endif

                    span = span[(closeAngleIndex + 1)..];
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

                return (memberName, localFunctionName);
            }
        }
    }
}
