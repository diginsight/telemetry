#if NET
using Microsoft.Extensions.Hosting;
#else
using Microsoft.AspNetCore.Hosting;
#endif
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Diginsight.AspNetCore;

/// <summary>
///     Customizes log levels by reading the <c>Log-Level</c> request header.
/// </summary>
/// <remarks>
///     <para>This class is designed for dependency injection and should not be instantiated manually.</para>
///     <para>
///         The <c>Log-Level</c> request header can be specified zero or more times.<br />
///         Each header entry must match one of the following formats:
///         <list type="bullet">
///             <item>
///                 <term><i>category</i>=<i>level</i></term>
///             </item>
///             <item>
///                 <term><i>category</i>=<i>level</i>;p=<i>provider</i></term>
///             </item>
///         </list>
///         where <i>category</i>, <i>level</i> and <i>provider</i> have the usual format and meaning.<br />
///         Invalid entries are ignored.<br />
///         If there are no valid entries, <see cref="TryCreateLoggerFactory" /> returns <see langword="null" />.
///     </para>
/// </remarks>
public sealed class DefaultDynamicLogLevelInjector : IDynamicLogLevelInjector
{
    private static readonly Regex SpecRegex = new ("^([^=]+?)=([a-z]+?)(?:;p=(.+?))?$", RegexOptions.IgnoreCase);

#if NET
    private readonly IHostEnvironment hostEnvironment;
#else
    private readonly IHostingEnvironment hostEnvironment;
#endif
    private readonly IOptionsMonitor<LoggerFilterOptions> loggerFilterOptionsMonitor;
    private readonly IOptions<LoggerFactoryOptions> loggerFactoryOptions;

    /// <summary>
    ///     DI constructor.
    /// </summary>
    /// <param name="hostEnvironment"></param>
    /// <param name="loggerFilterOptionsMonitor"></param>
    /// <param name="loggerFactoryOptions"></param>
    public DefaultDynamicLogLevelInjector(
#if NET
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

    /// <inheritdoc />
    public ILoggerFactory? TryCreateLoggerFactory(HttpContext context, IEnumerable<ILoggerProvider> loggerProviders)
    {
        LoggerFilterOptions oldLoggerFilterOptions = loggerFilterOptionsMonitor.CurrentValue;
        LoggerFilterOptions newLoggerFilterOptions = new ()
        {
            CaptureScopes = oldLoggerFilterOptions.CaptureScopes,
            MinLevel = oldLoggerFilterOptions.MinLevel,
        };

        IList<LoggerFilterRule> newRules = newLoggerFilterOptions.Rules;
        newRules.AddRange(oldLoggerFilterOptions.Rules);

        bool any = false;

        const string defaultCategory = "*";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static string? Collapse(string? c) => c is "" or defaultCategory ? null : c;

        foreach (string rawSpec in context.Request.Headers["Log-Level"].NormalizeHttpHeaderValue())
        {
            if (Enum.TryParse(rawSpec, true, out LogLevel minLogLevel))
            {
                SetRule(null, null, minLogLevel);
                continue;
            }

            if (SpecRegex.Match(rawSpec) is not { Success: true } match ||
                !Enum.TryParse(match.Groups[2].Value, true, out LogLevel logLevel))
            {
                continue;
            }

            string? category = Collapse(match.Groups[1].Value);
            string? provider = match.Groups[3] is { Success: true } providerGroup ? providerGroup.Value : null;
            SetRule(category, provider, logLevel);

            [SuppressMessage("ReSharper", "VariableHidesOuterVariable")]
            void SetRule(string? category, string? provider, LogLevel logLevel)
            {
                any = true;

                if (category is null && provider is null)
                {
                    newLoggerFilterOptions.MinLevel = logLevel;
                }

                int index = newRules.FirstIndexWhere(x => string.Equals(Collapse(x.CategoryName), category, StringComparison.OrdinalIgnoreCase) && x.ProviderName == provider);
                if (index >= 0)
                {
                    newRules[index] = new LoggerFilterRule(provider, category, logLevel, null);
                }
                else
                {
                    newRules.Add(new LoggerFilterRule(provider, category, logLevel, null));
                }
            }
        }

        if (hostEnvironment.IsDevelopment())
        {
            context.Response.Headers["Log-Level"] = newRules
                .Select(static x => $"{Collapse(x.CategoryName) ?? defaultCategory}={x.LogLevel ?? LogLevel.None}{(x.Filter is null ? "" : "!")}{(x.ProviderName is { } provider ? $";p={provider}" : "")}")
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
