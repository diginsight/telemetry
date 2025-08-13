using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace Diginsight.Diagnostics.AspNetCore;

public class HttpHeadersMetricRecordingFilter : IMetricRecordingFilter
{
    public const string HeaderName = "Activity-Span-Measuring";

    private readonly IHttpContextAccessor httpContextAccessor;

    public HttpHeadersMetricRecordingFilter(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public virtual bool? ShouldRecord(Activity activity)
    {
        return HttpHeadersHelper.ShouldInclude(activity.Source.Name, activity.OperationName, HeaderName, httpContextAccessor);
    }

    // public virtual IEnumerable<KeyValuePair<string, object?>> ExtractTags(Activity activity) => [ ];
}
