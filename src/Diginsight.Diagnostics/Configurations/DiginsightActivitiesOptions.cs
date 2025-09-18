using Diginsight.Options;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Diginsight.Diagnostics;

public sealed class DiginsightActivitiesOptions
    : IDiginsightActivitiesOptions,
        IDiginsightActivityNamesOptions,
        IDiginsightActivitiesLogOptions,
        IDynamicallyConfigurable,
        IVolatilelyConfigurable
{
    private readonly bool frozen;

    private LogBehavior logBehavior = LogBehavior.Hide;
    private LogLevel activityLogLevel = LogLevel.Debug;
    private bool writeActivityActionAsPrefix;
    private bool disablePayloadRendering;

    public IDictionary<string, bool> ActivitySources { get; }

    IReadOnlyDictionary<string, bool> IDiginsightActivitiesOptions.ActivitySources => (IReadOnlyDictionary<string, bool>)ActivitySources;

    public LogBehavior LogBehavior
    {
        get => logBehavior;
        set => logBehavior = frozen ? throw new InvalidOperationException($"{nameof(DiginsightActivitiesOptions)} instance is frozen") : value;
    }

    public LogLevel ActivityLogLevel
    {
        get => activityLogLevel;
        set => activityLogLevel = frozen ? throw new InvalidOperationException($"{nameof(DiginsightActivitiesOptions)} instance is frozen") : value;
    }

    public bool WriteActivityActionAsPrefix
    {
        get => writeActivityActionAsPrefix;
        set => writeActivityActionAsPrefix = frozen ? throw new InvalidOperationException($"{nameof(DiginsightActivitiesOptions)} instance is frozen") : value;
    }

    public bool DisablePayloadRendering
    {
        get => disablePayloadRendering;
        set => disablePayloadRendering = frozen ? throw new InvalidOperationException($"{nameof(DiginsightActivitiesOptions)} instance is frozen") : value;
    }

    public IDictionary<string, LogBehavior> LoggedActivityNames { get; }

    IReadOnlyDictionary<string, LogBehavior> IDiginsightActivityNamesOptions.LoggedActivityNames => (IReadOnlyDictionary<string, LogBehavior>)LoggedActivityNames;

    public DiginsightActivitiesOptions()
        : this(
            false,
            new Dictionary<string, bool>(),
            new Dictionary<string, LogBehavior>()
        ) { }

    private DiginsightActivitiesOptions(
        bool frozen,
        IDictionary<string, bool> activitySources,
        IDictionary<string, LogBehavior> loggedActivityNames
    )
    {
        this.frozen = frozen;
        ActivitySources = activitySources;
        LoggedActivityNames = loggedActivityNames;
    }

    public DiginsightActivitiesOptions Freeze()
    {
        if (frozen)
            return this;

        return new DiginsightActivitiesOptions(
            true,
            ActivitySources.ToImmutableDictionary(),
            LoggedActivityNames.ToImmutableDictionary()
        )
        {
            logBehavior = logBehavior,
            activityLogLevel = activityLogLevel,
            writeActivityActionAsPrefix = writeActivityActionAsPrefix,
            disablePayloadRendering = disablePayloadRendering,
        };
    }

    object IDynamicallyConfigurable.MakeFiller() => new Filler(this);

    object IVolatilelyConfigurable.MakeFiller() => new Filler(this);

    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private class Filler
    {
#if NET || NETSTANDARD2_1_OR_GREATER
        private const char SpaceSeparator = ' ';
        private const char EqualsSeparator = '=';
#else
        private static readonly char[] SpaceSeparator = [ ' ' ];
        private static readonly char[] EqualsSeparator = [ '=' ];
#endif

        private readonly DiginsightActivitiesOptions filled;

        public LogBehavior LogBehavior
        {
            get => filled.LogBehavior;
            set => filled.LogBehavior = value;
        }

        public LogLevel ActivityLogLevel
        {
            get => filled.ActivityLogLevel;
            set => filled.ActivityLogLevel = value;
        }

        public bool DisablePayloadRendering
        {
            get => filled.DisablePayloadRendering;
            set => filled.DisablePayloadRendering = value;
        }

        public string LoggedActivityNames
        {
            get => string.Join(" ", filled.LoggedActivityNames);
            set
            {
                filled.LoggedActivityNames.Clear();
                filled.LoggedActivityNames.AddRange(
                    value.Split(SpaceSeparator, StringSplitOptions.RemoveEmptyEntries)
                        .Select(
                            static x => x.Split(EqualsSeparator, 2) switch
                            {
                                [ var x0 ] => KeyValuePair.Create(x0, LogBehavior.Show),
                                [ var x0, var x1 ] when Enum.TryParse(x1, true, out LogBehavior b) => KeyValuePair.Create(x0, b),
                                _ => (KeyValuePair<string, LogBehavior>?)null,
                            }
                        )
                        .OfType<KeyValuePair<string, LogBehavior>>()
                );
            }
        }

        public Filler(DiginsightActivitiesOptions filled)
        {
            this.filled = filled;
        }
    }
}
