using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics.AspNetCore;

public sealed class HttpHeadersActivityProcessingSampler : IActivityProcessingSampler
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public HttpHeadersActivityProcessingSampler(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public void ShouldLog(Activity activity, Type? callerType, ref bool? result)
    {
        ShouldProcess(activity.OperationName, "Activity-Logging", out result);
    }

    public void ShouldRecord(Activity activity, Type? callerType, ref bool? result)
    {
        ShouldProcess(activity.OperationName, "Activity-Recording", out result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ShouldProcess(string activityName, string headerName, out bool? result)
    {
        result = HttpHeadersHelper.ShouldInclude(activityName, headerName, httpContextAccessor)
            ?? HttpHeadersHelper.ShouldInclude(activityName, "Activity-Processing", httpContextAccessor);
    }
}
