using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace Diginsight.Diagnostics.AspNetCore;

public class HttpHeadersSpanDurationMetricRecorderSettings : ISpanDurationMetricRecorderSettings
{
    public const string HeaderName = "Activity-Span-Measuring";

    private readonly IHttpContextAccessor httpContextAccessor;

    public HttpHeadersSpanDurationMetricRecorderSettings(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public virtual bool? ShouldRecord(Activity activity)
    {
        return HttpHeadersHelper.ShouldInclude(activity.Source.Name, activity.OperationName, HeaderName, httpContextAccessor);
    }

    public virtual IEnumerable<KeyValuePair<string, object?>> ExtractTags(Activity activity) => [ ];
}
