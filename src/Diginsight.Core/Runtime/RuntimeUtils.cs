using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Diginsight.Runtime;

public static class RuntimeUtils
{
    private static readonly Type AsyncMethodBuilderCoreType = Type.GetType("System.Runtime.CompilerServices.AsyncMethodBuilderCore")!;

    private static readonly IDictionary<MethodBase, Type> CallerTypeCache = new Dictionary<MethodBase, Type>();
    private static readonly IDictionary<MethodBase, (string, string?)> CallerNameCache = new Dictionary<MethodBase, (string, string?)>();

    /// <summary>
    /// Gets the list of heuristic size providers.
    /// </summary>
    /// <remarks>
    /// Developers can add custom heuristic size providers to this list to provide custom size calculation logic for specific types.
    /// </remarks>
    public static IList<IHeuristicSizeProvider> HeuristicSizeProviders { get; } = new List<IHeuristicSizeProvider>();

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

    /// <summary>
    /// Gets the <see cref="Type" /> which declares the <paramref name="stackDepth" />-th caller method.
    /// </summary>
    /// <param name="stackDepth">The depth in the call stack to look for the caller method.</param>
    /// <returns>The <see cref="Type" /> that declares the caller method.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="stackDepth" /> is negative.</exception>
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
                bool isGenerated = innerDeclaringType.FullName!.Contains('<') || method.Name.Contains('<');

                Type declaringType = innerDeclaringType;
                if (isGenerated &&
                    !method.IsDefined(typeof(CompilerGeneratedAttribute)) &&
                    method != innerDeclaringType.Assembly.EntryPoint)
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

    /// <summary>
    /// Gets the name of the <paramref name="stackDepth" />-th caller method.
    /// </summary>
    /// <param name="stackDepth">The depth in the call stack to look for the caller method.</param>
    /// <returns>A tuple containing the member name and local function name (if any) of the caller method.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="stackDepth" /> is negative.</exception>
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
                bool isGenerated = innerDeclaringType.FullName!.Contains('<') || methodName.Contains('<');

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
                    memberName = new string(span[(span.LastIndexOf('<') + 1)..closeAngleIndex]);
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

    /// <summary>
    /// Gets the size of an object heuristically.
    /// </summary>
    /// <param name="obj">The object to calculate the size of.</param>
    /// <param name="depthLimit">The recursion depth limit for size calculation.</param>
    /// <returns>The size of the object in bytes.</returns>
    /// <exception cref="Exception">Thrown when an error occurs during size calculation.</exception>
    public static long GetSizeHeuristically(this object? obj, int depthLimit = 25)
    {
        (long size, Exception? exception) = SizeCalculator.Get(obj, depthLimit);
        return exception is null ? size : throw exception;
    }

    private static class SizeCalculator
    {
#if NET || NETSTANDARD2_1_OR_GREATER
        private static readonly MethodInfo IsReferenceOrContainsReferencesMethod = typeof(RuntimeHelpers)
            .GetMethod(nameof(RuntimeHelpers.IsReferenceOrContainsReferences), BindingFlags.Public | BindingFlags.Static)!;
#endif
        private static readonly MethodInfo GetUnmanagedSizeMethod = typeof(SizeCalculator)
            .GetMethod(nameof(GetUnmanagedSize), BindingFlags.NonPublic | BindingFlags.Static)!;

        private static readonly ConcurrentDictionary<Type, long> FixedSizes = new ();
        private static readonly ConcurrentDictionary<Type, ValueTuple> ManagedTypes = new ();
#if NET || NETSTANDARD2_1_OR_GREATER
        private static readonly ConcurrentDictionary<Type, (long, IEnumerable<int>)> VariableIndexCache = new ();
#endif
        private static readonly ConcurrentDictionary<Type, (long, IEnumerable<FieldInfo>)> VariableFieldCache = new ();

        public static (long, Exception?) Get(object? obj, int depthLimit)
        {
            ISet<object> seen = new HashSet<object>();
            StrongBox<int> depthBox = new (0);

            HeuristicSizeResult CoreGet(object? current)
            {
                if (depthBox.Value++ > depthLimit)
                {
                    return new HeuristicSizeResult(long.MaxValue, new InvalidOperationException("Size calculation recursion limit reached"));
                }

                try
                {
                    if (current is null)
                    {
                        return default;
                    }

                    if (current is Pointer or Delegate)
                    {
                        return new HeuristicSizeResult(IntPtr.Size, true);
                    }

                    Type type = current.GetType();
                    if (FixedSizes.TryGetValue(type, out long fsz))
                    {
                        return new HeuristicSizeResult(fsz, true);
                    }

                    if (!ManagedTypes.ContainsKey(type))
                    {
                        if (TryGetUnmanagedSize(type) is { } usz)
                        {
                            return new HeuristicSizeResult(FixedSizes[type] = usz, true);
                        }
                        else
                        {
                            ManagedTypes[type] = default;
                        }
                    }

                    if (current is string str)
                    {
                        return new HeuristicSizeResult(str.Length * sizeof(char));
                    }

#if NET || NETSTANDARD2_1_OR_GREATER
                    if (current is ITuple tuple)
                    {
                        if (VariableIndexCache.TryGetValue(type, out (long, IEnumerable<int>) pair))
                        {
                            (long baseSz, IEnumerable<int> indexes) = pair;
                            HeuristicSizeResult result = new (baseSz);

                            foreach (int index in indexes)
                            {
                                result += CoreGet(tuple[index]);
                            }

                            return result;
                        }

                        HeuristicSizeResult fixedResult = new (0, true);
                        HeuristicSizeResult variableResult = new (0, false);

                        ICollection<int> variableIndexes = new List<int>();

                        for (int index = 0; index < tuple.Length; index++)
                        {
                            HeuristicSizeResult currentResult = CoreGet(tuple[index]);
                            if (currentResult.Fxd)
                            {
                                fixedResult += currentResult;
                            }
                            else
                            {
                                variableResult += currentResult;
                                variableIndexes.Add(index);
                            }
                        }

                        if (!variableIndexes.Any())
                        {
                            return fixedResult;
                        }

                        VariableIndexCache[type] = (fixedResult.Sz, variableIndexes);
                        return fixedResult + variableResult;
                    }
#endif

                    if (current is Type)
                    {
                        return new HeuristicSizeResult(IntPtr.Size, true);
                    }

                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Lazy<>))
                    {
                        return CoreGet(type.GetProperty(nameof(Lazy<object>.Value))!.GetValue(current));
                    }

                    if (!seen.Add(current))
                    {
                        return new HeuristicSizeResult(IntPtr.Size);
                    }

                    try
                    {
                        if (current is ISizeableHeuristically sizeableHeuristically)
                        {
                            return sizeableHeuristically.GetSizeHeuristically(CoreGet);
                        }

                        foreach (IHeuristicSizeProvider heuristicSizeProvider in HeuristicSizeProviders)
                        {
                            if (heuristicSizeProvider.TryGetSizeHeuristically(current, CoreGet, out HeuristicSizeResult result))
                            {
                                return result;
                            }
                        }

                        if (current is IEnumerable enumerable)
                        {
                            HeuristicSizeResult result = default;

                            IEnumerator enumerator = enumerable.GetEnumerator();
                            try
                            {
                                bool SafeMoveNext(ref HeuristicSizeResult result)
                                {
                                    try
                                    {
                                        return enumerator.MoveNext();
                                    }
                                    catch (Exception e)
                                    {
                                        result += e;
                                        return false;
                                    }
                                }

                                while (SafeMoveNext(ref result))
                                {
                                    result += CoreGet(enumerator.Current);
                                }
                            }
                            finally
                            {
                                (enumerator as IDisposable)?.Dispose();
                            }

                            return result;
                        }

                        if (VariableFieldCache.TryGetValue(type, out (long, IEnumerable<FieldInfo>) pair))
                        {
                            (long baseSz, IEnumerable<FieldInfo> fields) = pair;
                            HeuristicSizeResult result = new (baseSz);

                            foreach (FieldInfo field in fields)
                            {
                                object? fieldValue;
                                try
                                {
                                    fieldValue = field.GetValue(current);
                                }
                                catch (Exception e)
                                {
                                    result += e;
                                    continue;
                                }

                                result += CoreGet(fieldValue);
                            }

                            return result;
                        }

                        HeuristicSizeResult fixedResult = new (0, true);
                        HeuristicSizeResult variableResult = new (0, false);

                        FieldInfo[] allFields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        ICollection<FieldInfo> variableFields = new List<FieldInfo>();

                        foreach (FieldInfo field in allFields)
                        {
                            object? fieldValue;
                            try
                            {
                                fieldValue = field.GetValue(current);
                            }
                            catch (Exception e)
                            {
                                variableResult += e;
                                continue;
                            }

                            HeuristicSizeResult currentResult = CoreGet(fieldValue);
                            if (currentResult.Fxd)
                            {
                                fixedResult += currentResult;
                            }
                            else
                            {
                                variableResult += currentResult;
                                variableFields.Add(field);
                            }
                        }

                        if (!variableFields.Any())
                        {
                            return fixedResult;
                        }

                        VariableFieldCache[type] = (fixedResult.Sz, variableFields);
                        return fixedResult + variableResult;
                    }
                    finally
                    {
                        seen.Remove(current);
                    }
                }
                finally
                {
                    depthBox.Value--;
                }
            }

            HeuristicSizeResult result = CoreGet(obj);
            return (result.Sz, result.Exc);
        }

        private static long? TryGetUnmanagedSize(Type type)
        {
            try
            {
                return IsUnmanaged(type)
                    ? (long)GetUnmanagedSizeMethod.MakeGenericMethod(type).Invoke(null, [ ])!
                    : null;
            }
            catch (Exception)
            {
                return null;
            }

            static bool IsUnmanaged(Type type)
            {
#if NET || NETSTANDARD2_1_OR_GREATER
                return !(bool)IsReferenceOrContainsReferencesMethod.MakeGenericMethod(type).Invoke(null, [ ])!;
#else
                if (type.IsPrimitive || type.IsEnum || type.IsPointer)
                    return true;
                if (!type.IsValueType)
                    return false;
                return type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).All(static f => IsUnmanaged(f.FieldType));
#endif
            }
        }

        private static unsafe long GetUnmanagedSize<T>() where T : unmanaged => sizeof(T);
    }
}
