using Diginsight.Options;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics.AspNetCore;

public class HttpHeadersSpanDurationMetricRecordingFilter : IMetricRecordingFilter
{
    public const string HeaderName = "Activity-Span-Recording";

    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly IClassAwareOptions<DiginsightActivitiesOptions> activitiesOptions;

    public HttpHeadersSpanDurationMetricRecordingFilter(
        IHttpContextAccessor httpContextAccessor,
        IClassAwareOptions<DiginsightActivitiesOptions> activitiesOptions
    )
    {
        this.httpContextAccessor = httpContextAccessor;
        this.activitiesOptions = activitiesOptions;
    }

    private bool ShouldHandle(Activity activity, Instrument instrument)
    {
        IDiginsightActivitiesSpanDurationOptions metricOptions = activitiesOptions.Get(activity.GetCallerType());

        return instrument is Histogram<double> { Unit: "ms" } histogram
            && histogram.Name == metricOptions.MetricName
            && histogram.Meter.Name == metricOptions.MeterName;
    }

    public virtual bool? ShouldRecord(Activity activity, Instrument instrument)
    {
        return ShouldHandle(activity, instrument)
            ? HttpHeadersHelper.ShouldInclude(activity.Source.Name, activity.OperationName, HeaderName, httpContextAccessor)
            : null;
    }
}
