using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace Diginsight.Diagnostics.AspNetCore;

public sealed class HttpHeadersActivityLoggingSampler : IActivityLoggingSampler
{
    private const string SourceHeaderName = "Activity-Source-Logging";
    private const string PlainHeaderName = "Activity-Logging";

    public static readonly IEnumerable<string> HeaderNames = [ SourceHeaderName, PlainHeaderName ];

    private readonly IHttpContextAccessor httpContextAccessor;

    public HttpHeadersActivityLoggingSampler(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public bool? ShouldLog(Activity activity)
    {
        return HttpHeadersHelper.ShouldInclude(activity.Source.Name, SourceHeaderName, httpContextAccessor) == false
            ? false
            : HttpHeadersHelper.ShouldInclude(activity.OperationName, PlainHeaderName, httpContextAccessor);
    }
}
