using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics.AspNetCore;

public class HttpHeadersSpanDurationMetricRecordingFilter : IMetricRecordingFilter
{
    public const string HeaderName = "Activity-Span-Recording";

    private readonly IHttpContextAccessor httpContextAccessor;

    public HttpHeadersSpanDurationMetricRecordingFilter(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    private bool IsTheInstrument(Instrument instrument) => throw new NotImplementedException();

    public virtual bool? ShouldRecord(Activity activity, Instrument instrument)
    {
        return IsTheInstrument(instrument)
            ? HttpHeadersHelper.ShouldInclude(activity.Source.Name, activity.OperationName, HeaderName, httpContextAccessor)
            : null;
    }
}
