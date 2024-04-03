using Diginsight.CAOptions;
using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;

namespace Diginsight.AspNetCore;

public class PostConfigureClassAwareOptionsFromHttpRequestHeaders<TOptions>
    : PostConfigureOptionsFromHttpRequestHeaders<TOptions>,
        IPostConfigureClassAwareOptions<TOptions>,
        IClassAwareOptionsChangeTokenSource<TOptions>
    where TOptions : class, IDynamicallyPostConfigurable
{
    public PostConfigureClassAwareOptionsFromHttpRequestHeaders(
        string name,
        IHttpContextAccessor httpContextAccessor
    )
        : base(name, httpContextAccessor) { }

    public void PostConfigure(string name, Type @class, TOptions options)
    {
        PostConfigureCore(name, options, configuration => FilteredConfiguration.For(configuration, @class));
    }

    protected override object GetChangeTokenFiringItemKey(string name) => new ChangeTokenFiringItemKey(name);

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local")]
    private readonly record struct ChangeTokenFiringItemKey(string Name);
}
