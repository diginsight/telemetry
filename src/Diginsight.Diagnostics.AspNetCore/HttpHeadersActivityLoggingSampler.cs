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

    public virtual LogBehavior? GetLogBehavior(Activity activity)
    {
        return HttpHeadersHelper.GetMatches(activity.Source.Name, activity.OperationName, HeaderName, httpContextAccessor)
            .Select(static x => x is null ? (true, LogBehavior.Show) : (Enum.TryParse(x, true, out LogBehavior result), result))
            .Where(static x => x.Item1)
            .Select(static x => (LogBehavior?)x.Item2)
            .Max();
    }
}
