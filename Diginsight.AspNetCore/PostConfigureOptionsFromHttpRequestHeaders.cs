using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Diginsight.AspNetCore;

public sealed class PostConfigureOptionsFromHttpRequestHeaders<TOptions>
    : IPostConfigureOptions<TOptions>, IOptionsChangeTokenSource<TOptions>
    where TOptions : class
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly Func<TOptions, object>? makeFiller;

    private ConfigurationReloadToken changeToken = new ();

    public string? Name { get; }

    public PostConfigureOptionsFromHttpRequestHeaders(
        string? name,
        IHttpContextAccessor httpContextAccessor,
        Func<TOptions, object>? makeFiller = null
    )
    {
        this.httpContextAccessor = httpContextAccessor;
        this.makeFiller = makeFiller;
        Name = name;
    }

    public void PostConfigure(string? name, TOptions options)
    {
        name ??= Options.DefaultName;

        if (Name is not null && !string.Equals(Name, name, StringComparison.OrdinalIgnoreCase))
            return;

        if (httpContextAccessor.HttpContext is not { } httpContext)
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

        object filler = makeFiller?.Invoke(options) ?? options;
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(headers).Build();
        configuration.Bind(filler);
    }

    private Task FireChangeTokenAsync()
    {
        Interlocked.Exchange(ref changeToken, new ConfigurationReloadToken()).OnReload();
        return Task.CompletedTask;
    }

    public IChangeToken GetChangeToken() => changeToken;

    private readonly struct ChangeTokenFiringItemKey;
}
