using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Diginsight.Strings;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class LogStringExtensions
{
    private static readonly Histogram<double> LogStringDuration = SelfObservabilityUtils.Meter.CreateHistogram<double>("diginsight.log_string_duration", "ms");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ILogStringable ToLogStringable(
        this object? obj,
        IAppendingContextFactory? appendingContextFactory = null
    )
    {
        return (appendingContextFactory ?? AppendingContextFactoryBuilder.DefaultFactory)
            .ToLogStringable(obj);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToLogString(
        this object? obj,
        IAppendingContextFactory? appendingContextFactory = null,
        Action<LogStringVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        return (appendingContextFactory ?? AppendingContextFactoryBuilder.DefaultFactory)
            .MakeLogString(obj, configureVariables, configureMetaProperties);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string MakeLogString(
        this IAppendingContextFactory appendingContextFactory,
        object? obj,
        Action<LogStringVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        bool success = true;
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            appendingContextFactory.MakeAppendingContext(out StringBuilder stringBuilder)
                .ComposeAndAppend(
                    obj,
                    configureVariables: configureVariables,
                    configureMetaProperties: configureMetaProperties
                );
            return stringBuilder.ToString();
        }
        catch (Exception)
        {
            success = false;
            throw;
        }
        finally
        {
            LogStringDuration.Record(stopwatch.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("success", success));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool CannotCustomizeLogString(this Type type) => type.IsBanned();
}
