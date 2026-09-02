using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace Diginsight.Diagnostics.AspNetCore;

/// <summary>
/// Represents an activity logging filter that reads log behavior from HTTP headers.
/// </summary>
public class HttpHeadersActivityLoggingFilter : IActivityLoggingFilter
{
    /// <summary>
    /// The name of the HTTP header used to control activity logging.
    /// </summary>
    public const string HeaderName = "Activity-Logging";

    private readonly IHttpContextAccessor httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpHeadersActivityLoggingFilter" /> class.
    /// </summary>
    /// <param name="httpContextAccessor">The accessor for the current HTTP context.</param>
    /// <remarks>
    /// This class is designed to be either explicitly instantiated, instantiated through dependency injection, or derived.
    /// </remarks>
    public HttpHeadersActivityLoggingFilter(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public virtual LogBehavior? GetLogBehavior(Activity activity)
    {
        return HttpHeadersHelper.GetMatches(activity.Source.Name, activity.OperationName, HeaderName, httpContextAccessor)
            .Select(static x => x is null ? (true, LogBehavior.Show) : (Enum.TryParse(x, true, out LogBehavior result), result))
            .Where(static x => x.Item1)
            .Select(static x => (LogBehavior?)x.Item2)
            .Max();
    }
}
