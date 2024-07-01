using Diginsight.CAOptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Diginsight.AspNetCore;

public class DynamicallyConfigureOptionsFromHttpRequestHeaders<TOptions> : IConfigureNamedOptions<TOptions>, IPostConfigureOptions<TOptions>
    where TOptions : class, IDynamicallyConfigurable
{
    internal const string HeaderName = "Dynamic-Configuration";

    private readonly string? name;
    private readonly IHttpContextAccessor httpContextAccessor;

    public DynamicallyConfigureOptionsFromHttpRequestHeaders(
        string? name,
        IHttpContextAccessor httpContextAccessor
    )
    {
        this.httpContextAccessor = httpContextAccessor;
        this.name = name;
    }

    public void Configure(TOptions options)
    {
        ConfigureCore(Options.DefaultName, options);
    }

    public void Configure([SuppressMessage("ReSharper", "ParameterHidesMember")] string? name, TOptions options)
    {
        ConfigureCore(name ?? Options.DefaultName, options);
    }

    public void PostConfigure([SuppressMessage("ReSharper", "ParameterHidesMember")] string? name, TOptions options)
    {
        ConfigureCore(name ?? Options.DefaultName, options);
    }

    protected void ConfigureCore(
        [SuppressMessage("ReSharper", "ParameterHidesMember")] string name,
        TOptions options,
        Func<IConfiguration, IConfiguration>? enrichConfiguration = null
    )
    {
        if (this.name is not null && !string.Equals(this.name, name, StringComparison.Ordinal))
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
