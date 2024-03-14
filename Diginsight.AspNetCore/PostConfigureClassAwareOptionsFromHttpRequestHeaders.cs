using Diginsight.CAOptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Diginsight.AspNetCore;

internal sealed class PostConfigureClassAwareOptionsFromHttpRequestHeaders<TOptions>
    : IPostConfigureClassAwareOptions<TOptions>, IOptionsChangeTokenSource<TOptions>
    where TOptions : class
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private ConfigurationReloadToken changeToken = new ();

    public string Name { get; }

    public PostConfigureClassAwareOptionsFromHttpRequestHeaders(
        IHttpContextAccessor httpContextAccessor,
        string? name = null
    )
    {
        this.httpContextAccessor = httpContextAccessor;
        Name = name ?? Options.DefaultName;
    }

    public void PostConfigure(string name, Type @class, TOptions options)
    {
        if (!string.Equals(Name, name, StringComparison.OrdinalIgnoreCase) ||
            httpContextAccessor.HttpContext is not { } httpContext)
            return;

        if (!httpContext.Items.ContainsKey(default(ChangeTokenFiringItemKey)))
        {
            httpContext.Items[default(ChangeTokenFiringItemKey)] = null;
            httpContext.Response.OnCompleted(FireChangeTokenAsync);
        }

        IReadOnlyDictionary<string, string?> headers = httpContext.Request.Headers
            .Where(static x => x.Key.StartsWith("c-", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(static x => x.Key[2..].Replace("__", ConfigurationPath.KeyDelimiter), static x => x.Value.LastOrDefault());
        if (!(headers.Count > 0))
            return;

        FilteredConfiguration.For(new ConfigurationBuilder().AddInMemoryCollection(headers).Build(), @class).Bind(options);
    }

    private Task FireChangeTokenAsync()
    {
        Interlocked.Exchange(ref changeToken, new ConfigurationReloadToken()).OnReload();
        return Task.CompletedTask;
    }

    public IChangeToken GetChangeToken() => changeToken;

    private readonly struct ChangeTokenFiringItemKey;
}
