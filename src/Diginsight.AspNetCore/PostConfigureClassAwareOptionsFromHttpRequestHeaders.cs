using Diginsight.CAOptions;
using Microsoft.AspNetCore.Http;

namespace Diginsight.AspNetCore;

public class PostConfigureClassAwareOptionsFromHttpRequestHeaders<TOptions>
    : PostConfigureOptionsFromHttpRequestHeaders<TOptions>, IPostConfigureClassAwareOptions<TOptions>
    where TOptions : class, IDynamicallyPostConfigurable
{
    public PostConfigureClassAwareOptionsFromHttpRequestHeaders(
        string? name,
        IHttpContextAccessor httpContextAccessor
    )
        : base(name, httpContextAccessor) { }

    public void PostConfigure(string name, Type @class, TOptions options)
    {
        PostConfigureCore(name, options, configuration => FilteredConfiguration.For(configuration, @class));
    }
}
