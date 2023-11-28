using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Text.RegularExpressions;
#if NET6_0_OR_GREATER
using Microsoft.Extensions.Hosting;
#else
using Microsoft.AspNetCore.Hosting;
#endif

namespace Diginsight.AspNetCore;

public sealed class DefaultDynamicLogLevelInjector : IDynamicLogLevelInjector
{
    private static readonly Regex SpecRegex = new ("^([^=]+?)=([a-z]+?)(?:;p=(.+?))?$", RegexOptions.IgnoreCase);

#if NET6_0_OR_GREATER
    private readonly IHostEnvironment hostEnvironment;
#else
    private readonly IHostingEnvironment hostEnvironment;
#endif
    private readonly IOptionsMonitor<LoggerFilterOptions> loggerFilterOptionsMonitor;
    private readonly IOptions<LoggerFactoryOptions> loggerFactoryOptions;

    public DefaultDynamicLogLevelInjector(
#if NET6_0_OR_GREATER
        IHostEnvironment hostEnvironment,
#else
        IHostingEnvironment hostEnvironment,
#endif
        IOptionsMonitor<LoggerFilterOptions> loggerFilterOptionsMonitor,
        IOptions<LoggerFactoryOptions> loggerFactoryOptions
    )
    {
        this.hostEnvironment = hostEnvironment;
        this.loggerFilterOptionsMonitor = loggerFilterOptionsMonitor;
        this.loggerFactoryOptions = loggerFactoryOptions;
    }

    public ILoggerFactory? TryCreateLoggerFactory(HttpContext context, IEnumerable<ILoggerProvider> loggerProviders)
    {
        LoggerFilterOptions oldLoggerFilterOptions = loggerFilterOptionsMonitor.CurrentValue;
        LoggerFilterOptions newLoggerFilterOptions = new LoggerFilterOptions()
        {
            CaptureScopes = oldLoggerFilterOptions.CaptureScopes,
            MinLevel = oldLoggerFilterOptions.MinLevel,
        };

        IList<LoggerFilterRule> newRules = newLoggerFilterOptions.Rules;
        newRules.AddRange(oldLoggerFilterOptions.Rules);

        bool any = false;

        foreach (string? rawSpec in context.Request.Headers["Log-Level"])
        {
            if (Enum.TryParse(rawSpec!, true, out LogLevel minLogLevel))
            {
                newLoggerFilterOptions.MinLevel = minLogLevel;
                any = true;
                continue;
            }

            if (SpecRegex.Match(rawSpec!) is not { Success: true } match ||
                !Enum.TryParse(match.Groups[2].Value, true, out LogLevel logLevel))
            {
                continue;
            }

            any = true;

            string category = match.Groups[1].Value;
            string? provider = match.Groups[3] is { Success: true } providerGroup ? providerGroup.Value : null;

            int index = newRules.FirstIndexWhere(x => string.Equals(x.CategoryName ?? "*", category, StringComparison.OrdinalIgnoreCase) && x.ProviderName == provider);
            if (index >= 0)
            {
                newRules[index] = new LoggerFilterRule(provider, category, logLevel, null);
            }
            else
            {
                newRules.Add(new LoggerFilterRule(provider, category, logLevel, null));
            }
        }

        if (hostEnvironment.IsDevelopment())
        {
            context.Response.Headers["Log-Level"] = newRules
                .Select(static x => $"{(x.CategoryName is { } category ? $"{category}=" : "")}{x.LogLevel ?? LogLevel.None}{(x.Filter is null ? "" : "!")}{(x.ProviderName is { } provider ? $";p={provider}" : "")}")
                .Prepend(newLoggerFilterOptions.MinLevel.ToString())
                .ToArray();
        }

        return any
            ? new LoggerFactory(loggerProviders, new LoggerFilterOptionsMonitor(newLoggerFilterOptions), loggerFactoryOptions)
            : null;
    }

    private sealed class LoggerFilterOptionsMonitor : IOptionsMonitor<LoggerFilterOptions>
    {
        public LoggerFilterOptions CurrentValue { get; }

        public LoggerFilterOptionsMonitor(LoggerFilterOptions loggerFilterOptions)
        {
            CurrentValue = loggerFilterOptions;
        }

        public LoggerFilterOptions Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<LoggerFilterOptions, string?> listener) => null;
    }
}
