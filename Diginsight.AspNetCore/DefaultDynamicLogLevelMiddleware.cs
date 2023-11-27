using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Text.RegularExpressions;

namespace Diginsight.AspNetCore;

public sealed class DefaultDynamicLogLevelMiddleware : DynamicLogLevelMiddleware
{
    private static readonly Regex SpecRegex = new ("^([^=]+?)=([a-z]+?)(?:;p=(.+?))?$", RegexOptions.IgnoreCase);

    private readonly IOptionsMonitor<LoggerFilterOptions> loggerFilterOptionsMonitor;
    private readonly IOptions<LoggerFactoryOptions> loggerFactoryOptions;

    public DefaultDynamicLogLevelMiddleware(
        ILoggerFactorySetter loggerFactorySetter,
        IOptionsMonitor<LoggerFilterOptions> loggerFilterOptionsMonitor,
        IOptions<LoggerFactoryOptions> loggerFactoryOptions
    )
        : base(loggerFactorySetter)
    {
        this.loggerFilterOptionsMonitor = loggerFilterOptionsMonitor;
        this.loggerFactoryOptions = loggerFactoryOptions;
    }

    protected override ILoggerFactory? TryCreateLoggerFactory(HttpContext context)
    {
        StringValues rawSpecs = context.Request.Headers["Log-Level"];
        if (rawSpecs.Count < 1)
        {
            return null;
        }

        LoggerFilterOptions oldLoggerFilterOptions = loggerFilterOptionsMonitor.CurrentValue;
        LoggerFilterOptions newLoggerFilterOptions = new LoggerFilterOptions()
        {
            CaptureScopes = oldLoggerFilterOptions.CaptureScopes,
        };
        IList<LoggerFilterRule> newRules = newLoggerFilterOptions.Rules;
        newRules.AddRange(oldLoggerFilterOptions.Rules);

        LogLevel newMinLevel = oldLoggerFilterOptions.MinLevel;
        bool any = false;
        foreach (string? rawSpec in rawSpecs)
        {
            if (SpecRegex.Match(rawSpec!) is not { Success: true } match)
            {
                continue;
            }

            if (!Enum.TryParse(match.Groups[2].Value, true, out LogLevel logLevel))
            {
                continue;
            }

            any = true;

            string category = match.Groups[1].Value;
            string? provider = match.Groups[3] is { Success: true } providerGroup ? providerGroup.Value : null;

            newMinLevel = logLevel < newMinLevel ? logLevel : newMinLevel;
            int index = newRules.FirstIndexWhere(x => string.Equals(x.CategoryName ?? "*", category, StringComparison.OrdinalIgnoreCase) && x.ProviderName == provider);
            if (index >= 0)
            {
                newRules[index] = new LoggerFilterRule(category, provider, logLevel, null);
            }
            else
            {
                newRules.Add(new LoggerFilterRule(category, provider, logLevel, null));
            }
        }

        if (!any)
        {
            return null;
        }

        newLoggerFilterOptions.MinLevel = newMinLevel;

        return new LoggerFactory(LoggerProviders, new LoggerFilterOptionsMonitor(newLoggerFilterOptions), loggerFactoryOptions);
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
