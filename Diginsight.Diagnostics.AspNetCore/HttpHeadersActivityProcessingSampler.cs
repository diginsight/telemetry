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

    public bool? ShouldLog(Activity activity, Type? callerType) => ShouldProcess(activity.OperationName, "Activity-Logging");

    public bool? ShouldRecord(Activity activity, Type? callerType) => ShouldProcess(activity.OperationName, "Activity-Recording");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool? ShouldProcess(string activityName, string headerName)
    {
        return HttpHeadersHelper.ShouldInclude(activityName, headerName, httpContextAccessor)
            ?? HttpHeadersHelper.ShouldInclude(activityName, "Activity-Processing", httpContextAccessor);
    }
}
