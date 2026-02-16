using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics.AspNetCore;

public class HttpHeadersSpanDurationMetricRecordingFilter : IMetricRecordingFilter
{
    public const string HeaderName = "Activity-Span-Recording";

    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly IOptions<DiginsightActivitiesOptions> activitiesOptions;

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

    public virtual bool? ShouldRecord(Activity activity, Instrument instrument)
    {
        return ShouldHandle(instrument)
            ? HttpHeadersHelper.ShouldInclude(activity.Source.Name, activity.OperationName, HeaderName, httpContextAccessor)
            : null;
    }
}
