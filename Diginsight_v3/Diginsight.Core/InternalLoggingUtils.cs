using Diginsight.Strings;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Diginsight;

internal static class InternalLoggingUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringBuilder AppendLogString(
        this StringBuilder stringBuilder,
        object? obj,
        LoggingContext loggingContext,
        bool incrementDepth = true,
        Action<LogStringThresholdConfiguration>? configureThresholds = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        loggingContext.Append(obj, stringBuilder, incrementDepth, configureThresholds, configureMetaProperties);
        return stringBuilder;
    }

    // TODO Use
    public static (Type DeclaringType, string? LocalFunctionName) GetCaller(int stackDepth = 0)
    {
        MethodBase method = new StackFrame(stackDepth + 2).GetMethod()!;
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
            ReadOnlySpan<char> span =
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                methodName == "MoveNext" ? innerDeclaringTypeName[1..^2] : methodName;
#else
                (methodName == "MoveNext" ? innerDeclaringTypeName.Substring(1, innerDeclaringTypeName.Length - 2) : methodName).AsSpan();
#endif
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            span = span[(span.IndexOf('>') + 1)..];
#else
            span = span.Slice(span.IndexOf('>') + 1);
#endif
            localFunctionName = span[0] switch
            {
                'b' => "",
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                'g' => new string(span[3..span.IndexOf('|')]),
#else
                'g' => new string(span.Slice(3, span.IndexOf('|') - 3).ToArray()),
#endif
                _ => null,
            };
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

        return (declaringType, localFunctionName);
    }
}
