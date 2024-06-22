using Diginsight.CAOptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace Diginsight.AspNetCore;

public class PostConfigureOptionsFromHttpRequestHeaders<TOptions> : IPostConfigureOptions<TOptions>
    where TOptions : class, IDynamicallyPostConfigurable
{
    internal const string HeaderName = "Dynamic-Configuration";

    private readonly IHttpContextAccessor httpContextAccessor;

    public string? Name { get; }

    public PostConfigureOptionsFromHttpRequestHeaders(
        string? name,
        IHttpContextAccessor httpContextAccessor
    )
    {
        this.httpContextAccessor = httpContextAccessor;
        Name = name;
    }

    public void PostConfigure(string? name, TOptions options)
    {
        PostConfigureCore(name ?? Options.DefaultName, options);
    }

    protected void PostConfigureCore(string name, TOptions options, Func<IConfiguration, IConfiguration>? enrichConfiguration = null)
    {
        if (Name is not null && !string.Equals(Name, name, StringComparison.Ordinal))
            return;

        if (httpContextAccessor.HttpContext is not { } httpContext)
        {
            return;
        }

        IDictionary<string, string?> dict = new Dictionary<string, string?>();
        foreach (string rawSpec in httpContext.Request.Headers[HeaderName].NormalizeHttpHeaderValue())
        {
            if (Statics.SpecRegex.Match(rawSpec) is not { Success: true } match)
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
        configuration.Bind(options.MakeFiller());
    }
}

file static class Statics
{
    internal static readonly Regex SpecRegex = new ("^([^=]+?)=(.*)$");
}
