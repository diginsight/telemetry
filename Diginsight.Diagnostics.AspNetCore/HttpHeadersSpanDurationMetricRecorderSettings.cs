using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace Diginsight.Diagnostics.AspNetCore;

public class HttpHeadersSpanDurationMetricRecorderSettings : DefaultSpanDurationMetricRecorderSettings
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public HttpHeadersSpanDurationMetricRecorderSettings(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public override bool? ShouldRecord(Activity activity)
    {
        return HttpHeadersHelper.ShouldInclude(activity.Source.Name, "Activity-Source-Span-Recording", httpContextAccessor) == false
            ? false
            : HttpHeadersHelper.ShouldInclude(activity.OperationName, "Activity-Span-Recording", httpContextAccessor);
    }
}
