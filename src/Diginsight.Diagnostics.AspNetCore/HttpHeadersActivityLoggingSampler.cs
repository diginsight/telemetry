using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace Diginsight.Diagnostics.AspNetCore;

public class HttpHeadersActivityLoggingSampler : IActivityLoggingSampler
{
    public const string HeaderName = "Activity-Logging";

    private readonly IHttpContextAccessor httpContextAccessor;

    public HttpHeadersActivityLoggingSampler(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public virtual bool? ShouldLog(Activity activity)
    {
        return HttpHeadersHelper.ShouldInclude(activity.Source.Name, activity.OperationName, HeaderName, httpContextAccessor);
    }
}
