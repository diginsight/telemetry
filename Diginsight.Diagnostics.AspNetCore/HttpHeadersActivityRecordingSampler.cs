using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace Diginsight.Diagnostics.AspNetCore;

public sealed class HttpHeadersActivityRecordingSampler : IActivityRecordingSampler
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public HttpHeadersActivityRecordingSampler(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public void ShouldRecord(Activity activity, Type? callerType, ref bool? result)
    {
        result = HttpHeadersHelper.ShouldInclude(activity.OperationName, "Activity-Recording", httpContextAccessor);
    }
}
