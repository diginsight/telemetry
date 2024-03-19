using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Diginsight.AspNetCore;

public class PostConfigureOptionsFromHttpRequestHeaders<TOptions>
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
        PostConfigureCore(name ?? Options.DefaultName, options);
    }

    protected void PostConfigureCore(string name, TOptions options, Func<IConfiguration, IConfiguration>? enrichConfiguration = null)
    {
        if (Name is not null && !string.Equals(Name, name, StringComparison.OrdinalIgnoreCase))
            return;

        if (httpContextAccessor.HttpContext is not { } httpContext)
            return;

        object chanegTokenFiringItemKey = GetChangeTokenFiringItemKey(name);
        if (!httpContext.Items.ContainsKey(chanegTokenFiringItemKey))
        {
            httpContext.Items[chanegTokenFiringItemKey] = null;
            httpContext.Response.OnCompleted(FireChangeTokenAsync);
        }

        IDictionary<string, string?> dict = new Dictionary<string, string?>();
        foreach (string? rawSpec in httpContext.Request.Headers["Dynamic-Configuration"])
        {
            if (Statics.SpecRegex.Match(rawSpec!) is not { Success: true } match)
                continue;

            dict[match.Groups[1].Value] = match.Groups[2].Value;
        }

        if (!(dict.Count > 0))
            return;

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
        if (enrichConfiguration is not null)
        {
            configuration = enrichConfiguration(configuration);
        }
        object filler = makeFiller?.Invoke(options) ?? options;
        configuration.Bind(filler);
    }

    protected virtual object GetChangeTokenFiringItemKey(string name) => new ChangeTokenFiringItemKey(name);

    private Task FireChangeTokenAsync()
    {
        Interlocked.Exchange(ref changeToken, new ConfigurationReloadToken()).OnReload();
        return Task.CompletedTask;
    }

    public IChangeToken GetChangeToken() => changeToken;

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local")]
    private readonly record struct ChangeTokenFiringItemKey(string Name);
}

file static class Statics
{
    internal static readonly Regex SpecRegex = new ("^([^=]+?)=(.*)$");
}
