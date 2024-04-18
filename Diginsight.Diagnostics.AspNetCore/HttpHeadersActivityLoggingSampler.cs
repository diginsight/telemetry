using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace Diginsight.Diagnostics.AspNetCore;

public sealed class HttpHeadersActivityLoggingSampler : IActivityLoggingSampler
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public HttpHeadersActivityLoggingSampler(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public bool? ShouldLog(Activity activity)
    {
        return HttpHeadersHelper.ShouldInclude(activity.Source.Name, "Activity-Source-Logging", httpContextAccessor) == false
            ? false
            : HttpHeadersHelper.ShouldInclude(activity.OperationName, "Activity-Logging", httpContextAccessor);
    }
}
