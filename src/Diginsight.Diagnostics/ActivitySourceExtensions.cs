using Diginsight.Runtime;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class ActivitySourceExtensions
{
    private static readonly ConcurrentDictionary<(Type, string, string?), string> ActivityNameCache = new();

    extension(ActivitySource activitySource)
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Activity? CreateRichActivity(
            string activityName,
            object inputs,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null
        )
        {
            if (inputs is null) { throw new ArgumentNullException(nameof(inputs)); }
            if (!activitySource.HasListeners()) return null;

            Type callerType = RuntimeUtils.GetCallerType(1);
            return activitySource.CoreCreateRichActivity(null, () => inputs, activityName, true, activityKind, logLevel, false, callerType, null);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Activity? CreateRichActivity(
            string activityName,
            Func<object>? makeInputs = null,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null
        )
        {
            if (!activitySource.HasListeners()) return null;

            Type callerType = RuntimeUtils.GetCallerType(1);
            return activitySource.CoreCreateRichActivity(null, makeInputs, activityName, true, activityKind, logLevel, false, callerType, null);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Activity? CreateRichActivity(
            ILogger logger,
            string activityName,
            object inputs,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null
        )
        {
            if (inputs is null) { throw new ArgumentNullException(nameof(inputs)); }
            if (!activitySource.HasListeners()) return null;

            Type callerType = RuntimeUtils.GetCallerType(1);
            return activitySource.CoreCreateRichActivity(logger, () => inputs, activityName, true, activityKind, logLevel, false, callerType, null);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Activity? CreateRichActivity(
            ILogger logger,
            string activityName,
            Func<object>? makeInputs = null,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null
        )
        {
            if (!activitySource.HasListeners()) return null;

            Type callerType = RuntimeUtils.GetCallerType(1);
            return activitySource.CoreCreateRichActivity(logger, makeInputs, activityName, true, activityKind, logLevel, false, callerType, null);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Activity? StartRichActivity(
            string activityName,
            object inputs,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null
        )
        {
            if (inputs is null) { throw new ArgumentNullException(nameof(inputs)); }
            if (!activitySource.HasListeners()) return null;

            Type callerType = RuntimeUtils.GetCallerType(1);
            return activitySource.CoreCreateRichActivity(null, () => inputs, activityName, true, activityKind, logLevel, true, callerType, null);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Activity? StartRichActivity(
            string activityName,
            Func<object>? makeInputs = null,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null
        )
        {
            if (!activitySource.HasListeners()) return null;

            Type callerType = RuntimeUtils.GetCallerType(1);
            return activitySource.CoreCreateRichActivity(null, makeInputs, activityName, true, activityKind, logLevel, true, callerType, null);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Activity? StartRichActivity(
            ILogger logger,
            string activityName,
            object inputs,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null
        )
        {
            if (inputs is null) { throw new ArgumentNullException(nameof(inputs)); }
            if (!activitySource.HasListeners()) return null;

            Type callerType = RuntimeUtils.GetCallerType(1);
            return activitySource.CoreCreateRichActivity(logger, () => inputs, activityName, true, activityKind, logLevel, true, callerType, null);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Activity? StartRichActivity(
            ILogger logger,
            string activityName,
            Func<object>? makeInputs = null,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null
        )
        {
            if (!activitySource.HasListeners()) return null;

            Type callerType = RuntimeUtils.GetCallerType(1);
            return activitySource.CoreCreateRichActivity(logger, makeInputs, activityName, true, activityKind, logLevel, true, callerType, null);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Activity? CreateMethodActivity(
            object inputs,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            if (inputs is null) { throw new ArgumentNullException(nameof(inputs)); }
            if (!activitySource.HasListeners()) return null;

            var (callerType, localFunctionName) = RuntimeUtils.GetCallerInfo(1);
            return activitySource.CoreCreateRichActivity(null, () => inputs, callerMemberName, false, activityKind, logLevel, false, callerType, localFunctionName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Activity? CreateMethodActivity(
            Func<object>? makeInputs = null,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            if (!activitySource.HasListeners()) return null;

            var (callerType, localFunctionName) = RuntimeUtils.GetCallerInfo(1);
            return activitySource.CoreCreateRichActivity(null, makeInputs, callerMemberName, false, activityKind, logLevel, false, callerType, localFunctionName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Activity? CreateMethodActivity(
            ILogger logger,
            object inputs,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            if (inputs is null) { throw new ArgumentNullException(nameof(inputs)); }

            if (!activitySource.HasListeners()) return null;

            var (callerType, localFunctionName) = RuntimeUtils.GetCallerInfo(1);
            return activitySource.CoreCreateRichActivity(logger, () => inputs, callerMemberName, false, activityKind, logLevel, false, callerType, localFunctionName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Activity? CreateMethodActivity(
            ILogger logger,
            Func<object>? makeInputs = null,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            if (!activitySource.HasListeners()) return null;

            var (callerType, localFunctionName) = RuntimeUtils.GetCallerInfo(1);
            return activitySource.CoreCreateRichActivity(logger, makeInputs, callerMemberName, false, activityKind, logLevel, false, callerType, localFunctionName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Activity? StartMethodActivity(
            object inputs,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            if (inputs is null) { throw new ArgumentNullException(nameof(inputs)); }
            if (!activitySource.HasListeners()) return null;

            var (callerType, localFunctionName) = RuntimeUtils.GetCallerInfo(1);
            return activitySource.CoreCreateRichActivity(null, () => inputs, callerMemberName, false, activityKind, logLevel, true, callerType, localFunctionName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Activity? StartMethodActivity(
            Func<object>? makeInputs = null,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            if (!activitySource.HasListeners()) return null;

            var (callerType, localFunctionName) = RuntimeUtils.GetCallerInfo(1);
            return activitySource.CoreCreateRichActivity(null, makeInputs, callerMemberName, false, activityKind, logLevel, true, callerType, localFunctionName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Activity? StartMethodActivity(
            ILogger logger,
            object inputs,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            if (inputs is null) { throw new ArgumentNullException(nameof(inputs)); }
            if (!activitySource.HasListeners()) return null;

            var (callerType, localFunctionName) = RuntimeUtils.GetCallerInfo(1);
            return activitySource.CoreCreateRichActivity(logger, () => inputs, callerMemberName, false, activityKind, logLevel, true, callerType, localFunctionName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Activity? StartMethodActivity(
            ILogger logger,
            Func<object>? makeInputs = null,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            if (!activitySource.HasListeners()) return null;

            var (callerType, localFunctionName) = RuntimeUtils.GetCallerInfo(1);
            return activitySource.CoreCreateRichActivity(logger, makeInputs, callerMemberName, false, activityKind, logLevel, true, callerType, localFunctionName);
        }

        // ── Generic overloads: caller provides type explicitly, no stack walk needed ──

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Activity? CreateRichActivity<TCallerType>(
            string activityName,
            object inputs,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null
        )
        {
            if (inputs is null) { throw new ArgumentNullException(nameof(inputs)); }
            if (!activitySource.HasListeners()) return null;
            return activitySource.CoreCreateRichActivity(null, () => inputs, activityName, true, activityKind, logLevel, false, typeof(TCallerType), null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Activity? CreateRichActivity<TCallerType>(
            string activityName,
            Func<object>? makeInputs = null,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null
        )
        {
            if (!activitySource.HasListeners()) return null;
            return activitySource.CoreCreateRichActivity(null, makeInputs, activityName, true, activityKind, logLevel, false, typeof(TCallerType), null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Activity? CreateRichActivity<TCallerType>(
            ILogger logger,
            string activityName,
            object inputs,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null
        )
        {
            if (inputs is null) { throw new ArgumentNullException(nameof(inputs)); }
            if (!activitySource.HasListeners()) return null;
            return activitySource.CoreCreateRichActivity(logger, () => inputs, activityName, true, activityKind, logLevel, false, typeof(TCallerType), null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Activity? CreateRichActivity<TCallerType>(
            ILogger logger,
            string activityName,
            Func<object>? makeInputs = null,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null
        )
        {
            if (!activitySource.HasListeners()) return null;
            return activitySource.CoreCreateRichActivity(logger, makeInputs, activityName, true, activityKind, logLevel, false, typeof(TCallerType), null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Activity? StartRichActivity<TCallerType>(
            string activityName,
            object inputs,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null
        )
        {
            if (inputs is null) { throw new ArgumentNullException(nameof(inputs)); }
            if (!activitySource.HasListeners()) return null;
            return activitySource.CoreCreateRichActivity(null, () => inputs, activityName, true, activityKind, logLevel, true, typeof(TCallerType), null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Activity? StartRichActivity<TCallerType>(
            string activityName,
            Func<object>? makeInputs = null,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null
        )
        {
            if (!activitySource.HasListeners()) return null;
            return activitySource.CoreCreateRichActivity(null, makeInputs, activityName, true, activityKind, logLevel, true, typeof(TCallerType), null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Activity? StartRichActivity<TCallerType>(
            ILogger logger,
            string activityName,
            object inputs,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null
        )
        {
            if (inputs is null) { throw new ArgumentNullException(nameof(inputs)); }
            if (!activitySource.HasListeners()) return null;
            return activitySource.CoreCreateRichActivity(logger, () => inputs, activityName, true, activityKind, logLevel, true, typeof(TCallerType), null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Activity? StartRichActivity<TCallerType>(
            ILogger logger,
            string activityName,
            Func<object>? makeInputs = null,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null
        )
        {
            if (!activitySource.HasListeners()) return null;
            return activitySource.CoreCreateRichActivity(logger, makeInputs, activityName, true, activityKind, logLevel, true, typeof(TCallerType), null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Activity? CreateMethodActivity<TCallerType>(
            object inputs,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            if (inputs is null) { throw new ArgumentNullException(nameof(inputs)); }
            if (!activitySource.HasListeners()) return null;
            return activitySource.CoreCreateRichActivity(null, () => inputs, callerMemberName, false, activityKind, logLevel, false, typeof(TCallerType), null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Activity? CreateMethodActivity<TCallerType>(
            Func<object>? makeInputs = null,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            if (!activitySource.HasListeners()) return null;
            return activitySource.CoreCreateRichActivity(null, makeInputs, callerMemberName, false, activityKind, logLevel, false, typeof(TCallerType), null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Activity? CreateMethodActivity<TCallerType>(
            ILogger logger,
            object inputs,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            if (inputs is null) { throw new ArgumentNullException(nameof(inputs)); }
            if (!activitySource.HasListeners()) return null;
            return activitySource.CoreCreateRichActivity(logger, () => inputs, callerMemberName, false, activityKind, logLevel, false, typeof(TCallerType), null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Activity? CreateMethodActivity<TCallerType>(
            ILogger logger,
            Func<object>? makeInputs = null,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            if (!activitySource.HasListeners()) return null;
            return activitySource.CoreCreateRichActivity(logger, makeInputs, callerMemberName, false, activityKind, logLevel, false, typeof(TCallerType), null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Activity? StartMethodActivity<TCallerType>(
            object inputs,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            if (inputs is null) { throw new ArgumentNullException(nameof(inputs)); }
            if (!activitySource.HasListeners()) return null;
            return activitySource.CoreCreateRichActivity(null, () => inputs, callerMemberName, false, activityKind, logLevel, true, typeof(TCallerType), null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Activity? StartMethodActivity<TCallerType>(
            Func<object>? makeInputs = null,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            if (!activitySource.HasListeners()) return null;
            return activitySource.CoreCreateRichActivity(null, makeInputs, callerMemberName, false, activityKind, logLevel, true, typeof(TCallerType), null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Activity? StartMethodActivity<TCallerType>(
            ILogger logger,
            object inputs,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            if (inputs is null) { throw new ArgumentNullException(nameof(inputs)); }
            if (!activitySource.HasListeners()) return null;
            return activitySource.CoreCreateRichActivity(logger, () => inputs, callerMemberName, false, activityKind, logLevel, true, typeof(TCallerType), null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Activity? StartMethodActivity<TCallerType>(
            ILogger logger,
            Func<object>? makeInputs = null,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            if (!activitySource.HasListeners()) return null;
            return activitySource.CoreCreateRichActivity(logger, makeInputs, callerMemberName, false, activityKind, logLevel, true, typeof(TCallerType), null);
        }

        // ── Type-based overloads: caller passes Type explicitly, no stack walk needed ──

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Activity? CreateRichActivity(
            Type callerType,
            string activityName,
            object inputs,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null
        )
        {
            if (inputs is null) { throw new ArgumentNullException(nameof(inputs)); }
            if (!activitySource.HasListeners()) return null;
            return activitySource.CoreCreateRichActivity(null, () => inputs, activityName, true, activityKind, logLevel, false, callerType, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Activity? CreateRichActivity(
            Type callerType,
            string activityName,
            Func<object>? makeInputs = null,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null
        )
        {
            if (!activitySource.HasListeners()) return null;
            return activitySource.CoreCreateRichActivity(null, makeInputs, activityName, true, activityKind, logLevel, false, callerType, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Activity? CreateRichActivity(
            Type callerType,
            ILogger logger,
            string activityName,
            object inputs,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null
        )
        {
            if (inputs is null) { throw new ArgumentNullException(nameof(inputs)); }
            if (!activitySource.HasListeners()) return null;
            return activitySource.CoreCreateRichActivity(logger, () => inputs, activityName, true, activityKind, logLevel, false, callerType, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Activity? CreateRichActivity(
            Type callerType,
            ILogger logger,
            string activityName,
            Func<object>? makeInputs = null,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null
        )
        {
            if (!activitySource.HasListeners()) return null;
            return activitySource.CoreCreateRichActivity(logger, makeInputs, activityName, true, activityKind, logLevel, false, callerType, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Activity? StartRichActivity(
            Type callerType,
            string activityName,
            object inputs,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null
        )
        {
            if (inputs is null) { throw new ArgumentNullException(nameof(inputs)); }
            if (!activitySource.HasListeners()) return null;
            return activitySource.CoreCreateRichActivity(null, () => inputs, activityName, true, activityKind, logLevel, true, callerType, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Activity? StartRichActivity(
            Type callerType,
            string activityName,
            Func<object>? makeInputs = null,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null
        )
        {
            if (!activitySource.HasListeners()) return null;
            return activitySource.CoreCreateRichActivity(null, makeInputs, activityName, true, activityKind, logLevel, true, callerType, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Activity? StartRichActivity(
            Type callerType,
            ILogger logger,
            string activityName,
            object inputs,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null
        )
        {
            if (inputs is null) { throw new ArgumentNullException(nameof(inputs)); }
            if (!activitySource.HasListeners()) return null;
            return activitySource.CoreCreateRichActivity(logger, () => inputs, activityName, true, activityKind, logLevel, true, callerType, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Activity? StartRichActivity(
            Type callerType,
            ILogger logger,
            string activityName,
            Func<object>? makeInputs = null,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null
        )
        {
            if (!activitySource.HasListeners()) return null;
            return activitySource.CoreCreateRichActivity(logger, makeInputs, activityName, true, activityKind, logLevel, true, callerType, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Activity? CreateMethodActivity(
            Type callerType,
            object inputs,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            if (inputs is null) { throw new ArgumentNullException(nameof(inputs)); }
            if (!activitySource.HasListeners()) return null;
            return activitySource.CoreCreateRichActivity(null, () => inputs, callerMemberName, false, activityKind, logLevel, false, callerType, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Activity? CreateMethodActivity(
            Type callerType,
            Func<object>? makeInputs = null,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            if (!activitySource.HasListeners()) return null;
            return activitySource.CoreCreateRichActivity(null, makeInputs, callerMemberName, false, activityKind, logLevel, false, callerType, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Activity? CreateMethodActivity(
            Type callerType,
            ILogger logger,
            object inputs,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            if (inputs is null) { throw new ArgumentNullException(nameof(inputs)); }
            if (!activitySource.HasListeners()) return null;
            return activitySource.CoreCreateRichActivity(logger, () => inputs, callerMemberName, false, activityKind, logLevel, false, callerType, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Activity? CreateMethodActivity(
            Type callerType,
            ILogger logger,
            Func<object>? makeInputs = null,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            if (!activitySource.HasListeners()) return null;
            return activitySource.CoreCreateRichActivity(logger, makeInputs, callerMemberName, false, activityKind, logLevel, false, callerType, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Activity? StartMethodActivity(
            Type callerType,
            object inputs,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            if (inputs is null) { throw new ArgumentNullException(nameof(inputs)); }
            if (!activitySource.HasListeners()) return null;
            return activitySource.CoreCreateRichActivity(null, () => inputs, callerMemberName, false, activityKind, logLevel, true, callerType, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Activity? StartMethodActivity(
            Type callerType,
            Func<object>? makeInputs = null,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            if (!activitySource.HasListeners()) return null;
            return activitySource.CoreCreateRichActivity(null, makeInputs, callerMemberName, false, activityKind, logLevel, true, callerType, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Activity? StartMethodActivity(
            Type callerType,
            ILogger logger,
            object inputs,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            if (inputs is null) { throw new ArgumentNullException(nameof(inputs)); }
            if (!activitySource.HasListeners()) return null;
            return activitySource.CoreCreateRichActivity(logger, () => inputs, callerMemberName, false, activityKind, logLevel, true, callerType, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Activity? StartMethodActivity(
            Type callerType,
            ILogger logger,
            Func<object>? makeInputs = null,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            if (!activitySource.HasListeners()) return null;
            return activitySource.CoreCreateRichActivity(logger, makeInputs, callerMemberName, false, activityKind, logLevel, true, callerType, null);
        }

        private Activity? CoreCreateRichActivity(
            ILogger? logger,
            Func<object>? makeInputs,
            string activityNameHint,
            bool isStandalone,
            ActivityKind activityKind,
            LogLevel? logLevel,
            bool start,
            Type callerType,
            string? localFunctionName
        )
        {
            string finalActivityName;
            if (isStandalone)
            {
                finalActivityName = activityNameHint;
            }
            else
            {
                finalActivityName = ActivityNameCache.GetOrAdd(
                    (callerType, activityNameHint, localFunctionName),
                    static key =>
                    {
                        var (ct, hint, lfn) = key;
                        string suffix = lfn switch
                        {
                            "" => $"{hint}+<anon>",
                            not null => $"{hint}+{lfn}",
                            null => hint,
                        };
                        return $"{ct.Name}.{suffix}";
                    });
            }

            Activity? activity = activitySource.CreateActivity(finalActivityName, activityKind);
            if (activity is null) { return null; }
            
            activity.SetCustomProperty(ActivityCustomPropertyNames.Logger, logger);
            activity.SetCustomProperty(ActivityCustomPropertyNames.LogLevel, logLevel);
            activity.SetCustomProperty(ActivityCustomPropertyNames.MakeInputs, makeInputs);
            activity.SetCustomProperty(ActivityCustomPropertyNames.CallerType, callerType);
            activity.SetCustomProperty(ActivityCustomPropertyNames.IsStandalone, isStandalone);
            if (start) { activity.Start(); }
            
            return activity;
        }
    }
}
