using Diginsight.Analyzers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics.AspNetCore;

/// <summary>
/// Represents a metric recording filter that reads span duration recording decisions from HTTP headers.
/// </summary>
[NonSealed]
public class HttpHeadersSpanDurationMetricRecordingFilter : IMetricRecordingFilter
{
    /// <summary>
    /// The name of the HTTP header used to control span duration metric recording.
    /// </summary>
    public const string HeaderName = "Activity-Span-Recording";

    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly IOptions<DiginsightActivitiesOptions> activitiesOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpHeadersSpanDurationMetricRecordingFilter" /> class.
    /// </summary>
    /// <param name="httpContextAccessor">The accessor for the current HTTP context.</param>
    /// <param name="activitiesOptions">The options for <see cref="DiginsightActivitiesOptions" />.</param>
    /// <remarks>
    /// This class is designed to be either explicitly instantiated, instantiated through dependency injection, or derived.
    /// </remarks>
    public HttpHeadersSpanDurationMetricRecordingFilter(
        IHttpContextAccessor httpContextAccessor,
        IOptions<DiginsightActivitiesOptions> activitiesOptions
    )
    {
        this.httpContextAccessor = httpContextAccessor;
        this.activitiesOptions = activitiesOptions;
    }

    private bool ShouldHandle(Instrument instrument)
    {
        IMetricRecordingOptions metricOptions = activitiesOptions.Value;

        return instrument is Histogram<double> { Unit: "ms" } histogram
            && histogram.Name == metricOptions.MetricName
            && histogram.Meter.Name == metricOptions.MeterName;
    }

    /// <inheritdoc />
    public virtual bool? ShouldRecord(Activity activity, Instrument instrument)
    {
        return ShouldHandle(instrument)
            ? HttpHeadersHelper.ShouldInclude(activity.Source.Name, activity.OperationName, HeaderName, httpContextAccessor)
            : null;
    }
}
