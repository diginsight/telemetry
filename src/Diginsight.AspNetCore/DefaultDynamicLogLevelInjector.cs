#if NET
using Microsoft.Extensions.Hosting;
#else
using Microsoft.AspNetCore.Hosting;
#endif
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Diginsight.AspNetCore;

/// <summary>
/// Customizes log levels by reading the <c>Log-Level</c> request header.
/// </summary>
/// <remarks>
///     <para>This class is designed for dependency injection and should not be instantiated manually.</para>
///     <para>
///     The <c>Log-Level</c> request header can be specified zero or more times.<br />
///     Each header entry must match one of the following formats:
///     <list type="bullet">
///         <item>
///             <term><i>category</i>=<i>level</i></term>
///         </item>
///         <item>
///             <term><i>category</i>=<i>level</i>;p=<i>provider</i></term>
///         </item>
///     </list>
///     where <i>category</i>, <i>level</i> and <i>provider</i> have the usual format and meaning.<br />
///     Invalid entries are ignored.<br />
///     If there are no valid entries, <see cref="TryCreateLoggerFactory" /> returns <c>null</c>.
///     </para>
/// </remarks>
public sealed class DefaultDynamicLogLevelInjector : IDynamicLogLevelInjector
{
    private const string HeaderName = "Log-Level";

#if NET
    private readonly IHostEnvironment hostEnvironment;
#else
    private readonly IHostingEnvironment hostEnvironment;
#endif
    private readonly IOptionsMonitor<LoggerFilterOptions> loggerFilterOptionsMonitor;
    private readonly IOptions<LoggerFactoryOptions> loggerFactoryOptions;

    /// <summary>
    /// DI constructor.
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

        bool any = DynamicHttpHeadersParser.UpdateLogLevel(context.Request.Headers[HeaderName].NormalizeHttpHeaderValue(), newLoggerFilterOptions, true);

        for (int i = 0; i < newRules.Count; i++)
        {
            if (newRules[i].LogLevel is not null)
                continue;

            newRules.RemoveAt(i);
            i--;
        }

        if (hostEnvironment.IsDevelopment())
        {
            context.Response.Headers[HeaderName] = newRules
                .Select(static x => $"{x.CategoryName ?? "Default"}={x.LogLevel ?? LogLevel.None}{(x.Filter is null ? "" : "!")}{(x.ProviderName is { } provider ? $";p={provider}" : "")}")
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

    public static void AddToServices(IServiceCollection services)
    {
        services.AddDynamicLogLevel<DefaultDynamicLogLevelInjector>();
        services.Configure<DiginsightDistributedContextOptions>(static x => { x.NonBaggageKeys.Add(HeaderName); });
    }
}
