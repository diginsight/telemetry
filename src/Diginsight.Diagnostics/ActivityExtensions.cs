using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

/// <summary>
/// Provides extension methods for working with <see cref="Activity" /> instances.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ActivityExtensions
{
    private static class CustomPropertyNames
    {
        public const string CustomDurationMetric = nameof(CustomDurationMetric);
        public const string CustomDurationMetricTags = nameof(CustomDurationMetricTags);
        public const string Depth = nameof(Depth);
        public const string Label = nameof(Label);
        public const string LogBehavior = nameof(LogBehavior);
    }

    /// <param name="activity">The activity to work with.</param>
    extension(Activity? activity)
    {
        /// <summary>
        /// Sets the output payload associated with the activity.
        /// </summary>
        /// <param name="output">The output payload.</param>
        /// <exception cref="ArgumentException">Thrown when the activity does not contain a valid logger.</exception>
        public void SetOutput(object? output)
        {
            if (activity is null)
            {
                return;
            }
            if (activity.GetCustomProperty(ActivityCustomPropertyNames.Logger) is null)
            {
                throw new ArgumentException("Invalid logger in activity");
            }

            activity.SetCustomProperty(ActivityCustomPropertyNames.Output, new StrongBox<object?>(output));
        }

        /// <summary>
        /// Sets the named output payloads associated with the activity.
        /// </summary>
        /// <param name="namedOutputs">The named output payloads.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="namedOutputs" /> is <c>null</c>.</exception>
        public void SetNamedOutputs(object namedOutputs)
        {
            if (namedOutputs is null)
            {
                throw new ArgumentNullException(nameof(namedOutputs));
            }

            activity?.SetCustomProperty(ActivityCustomPropertyNames.NamedOutputs, namedOutputs);
        }

        /// <summary>
        /// Gets the activity depth associated with the activity.
        /// </summary>
        /// <returns>The activity depth.</returns>
        public ActivityDepth GetDepth()
        {
            if (activity is null)
            {
                return default;
            }

            if (activity.GetCustomProperty(CustomPropertyNames.Depth) is not ActivityDepth depth)
            {
                depth = ActivityDepth.FromTraceStateValue(TraceState.Parse(activity.TraceStateString).GetValueOrDefault(ActivityDepth.TraceStateKey))
                    ?? activity.Parent.GetDepth().MakeLocalChild();

                activity.SetCustomProperty(CustomPropertyNames.Depth, depth);
            }

            return depth;
        }
    }

    /// <param name="activity">The activity to work with.</param>
    extension(Activity activity)
    {
        /// <summary>
        /// Gets the caller type associated with the activity.
        /// </summary>
        /// <returns>The caller type, or <c>null</c> if no caller type is associated.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the activity contains an invalid caller type.</exception>
        public Type? GetCallerType()
        {
            return activity.GetCustomProperty(ActivityCustomPropertyNames.CallerType) switch
            {
                Type t => t,
                null => null,
                _ => throw new InvalidOperationException("Invalid caller type in activity"),
            };
        }

        /// <summary>
        /// Gets the label associated with the activity.
        /// </summary>
        /// <returns>The activity label, or <c>null</c> if no label is associated.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="activity" /> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the activity contains an invalid label.</exception>
        public string? GetLabel()
        {
            if (activity is null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            return activity.GetCustomProperty(CustomPropertyNames.Label) switch
            {
                string s => s,
                null => null,
                _ => throw new InvalidOperationException("Invalid label in activity"),
            };
        }

        /// <summary>
        /// Sets the label associated with the activity.
        /// </summary>
        /// <param name="label">The activity label.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="activity" /> is <c>null</c>.</exception>
        public void SetLabel(string? label)
        {
            if (activity is null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            activity.SetCustomProperty(CustomPropertyNames.Label, label);
        }

        /// <summary>
        /// Finds the nearest ancestor with the specified label.
        /// </summary>
        /// <param name="label">The label to search for.</param>
        /// <returns>The matching ancestor activity, or <c>null</c> if none is found.</returns>
        public Activity? FindLabeledParent(string label)
        {
            return activity.GetAncestors(true).SkipWhile(a => a.GetLabel() != label).FirstOrDefault();
        }

        /// <summary>
        /// Gets the ancestors of the activity.
        /// </summary>
        /// <param name="includeSelf">A value indicating whether to include the activity itself.</param>
        /// <returns>A lazy enumerable of ancestor activities.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="activity" /> is <c>null</c>.</exception>
        public IEnumerable<Activity> GetAncestors(bool includeSelf = false)
        {
            if (activity is null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            if (includeSelf)
            {
                yield return activity;
            }
            for (Activity? current = activity.Parent; current is not null; current = current.Parent)
            {
                yield return current;
            }
        }

        /// <summary>
        /// Gets the custom duration metric associated with the activity.
        /// </summary>
        /// <returns>The custom duration metric, or <c>null</c> if none is associated.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the activity contains an invalid duration metric.</exception>
        public Instrument? GetCustomDurationMetric()
        {
            return activity.GetCustomProperty(CustomPropertyNames.CustomDurationMetric) switch
            {
                null => null,
                Instrument instrument and (Histogram<double> or Histogram<long>) => instrument,
                _ => throw new InvalidOperationException("Invalid duration metric in activity"),
            };
        }

        /// <summary>
        /// Sets a custom long duration metric associated with the activity.
        /// </summary>
        /// <param name="metric">The custom duration metric.</param>
        /// <param name="tags">The tags to record with the metric.</param>
        public void SetCustomDurationMetric(Histogram<long> metric, params Tag[] tags)
        {
            activity.SetCustomDurationMetric((Instrument)metric, tags);
        }

        /// <summary>
        /// Sets a custom double duration metric associated with the activity.
        /// </summary>
        /// <param name="metric">The custom duration metric.</param>
        /// <param name="tags">The tags to record with the metric.</param>
        public void SetCustomDurationMetric(Histogram<double> metric, params Tag[] tags)
        {
            activity.SetCustomDurationMetric((Instrument)metric, tags);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetCustomDurationMetric(Instrument instrument, params Tag[] tags)
        {
            if (activity is null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            activity.SetCustomProperty(CustomPropertyNames.CustomDurationMetric, instrument);
            activity.SetCustomProperty(CustomPropertyNames.CustomDurationMetricTags, tags);
        }

        /// <summary>
        /// Adds tags to the custom duration metric associated with the activity.
        /// </summary>
        /// <param name="tags">The tags to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="activity" /> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the activity has no associated custom duration metric.</exception>
        public void AddTagsToCustomDurationMetric(params Tag[] tags)
        {
            if (activity is null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            if (activity.GetCustomDurationMetric() is null)
            {
                throw new ArgumentException("Activity has no associated custom duration metric");
            }

            Tag[] allTags =
            [
                ..tags
                    .Concat(activity.GetCustomDurationMetricTags())
#if NET
                    .DistinctBy(static x => x.Key),
#else
                    .GroupBy(static x => x.Key, static (_, xs) => xs.First()),
#endif
            ];
            activity.SetCustomProperty(CustomPropertyNames.CustomDurationMetricTags, allTags);
        }

        internal Tag[] GetCustomDurationMetricTags()
        {
            return activity.GetCustomProperty(CustomPropertyNames.CustomDurationMetricTags) switch
            {
                Tag[] tags => tags,
                null => [ ],
                _ => throw new InvalidOperationException("Invalid custom duration metric tags in activity"),
            };
        }

        internal LogBehavior? GetLogBehavior()
        {
            return activity.GetCustomProperty(CustomPropertyNames.LogBehavior) switch
            {
                LogBehavior lb => lb,
                null => null,
                _ => throw new InvalidOperationException("Invalid log behavior in activity"),
            };
        }

        internal void SetLogBehavior(LogBehavior logBehavior)
        {
            activity.SetCustomProperty(CustomPropertyNames.LogBehavior, logBehavior);
            if (logBehavior != LogBehavior.Show)
            {
                activity.SetCustomProperty(CustomPropertyNames.Depth, activity.GetDepth().MakeHidden());
            }
        }
    }
}
