using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Diginsight.AspNetCore;

/// <summary>
/// Provides methods for parsing Diginsight dynamic HTTP headers.
/// </summary>
public static
#if NET
    partial
#endif
    class DynamicHttpHeadersParser
{
    private const string ConfigurationSpecRegexStr = "^([^= ]+?)(?: *= *([^ ]*))?$";
    private const string LogLevelSpecRegexStr = "^([^= ]+?) *=(?: *([a-z]+?))?(?: *; *p *= *([^ ]+?))?$";

#if NET
    [GeneratedRegex(ConfigurationSpecRegexStr)]
    private static partial Regex ConfigurationSpecRegexImpl();

    [GeneratedRegex(LogLevelSpecRegexStr, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LogLevelSpecRegexImpl();

    /// <inheritdoc cref="ConfigurationSpecRegexImpl" />
    private static Regex ConfigurationSpecRegex => ConfigurationSpecRegexImpl();

    /// <inheritdoc cref="LogLevelSpecRegexImpl" />
    private static Regex LogLevelSpecRegex => LogLevelSpecRegexImpl();
#else
    private static readonly Regex ConfigurationSpecRegex = new (ConfigurationSpecRegexStr);
    private static readonly Regex LogLevelSpecRegex = new (LogLevelSpecRegexStr, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
#endif

    /// <summary>
    /// Parses raw configuration specifications into configuration entries.
    /// </summary>
    /// <param name="rawSpecs">The raw configuration specifications.</param>
    /// <param name="allowUnset">Whether specifications without a value are allowed.</param>
    /// <returns>A lazy enumerable of configuration entries.</returns>
    public static IEnumerable<KeyValuePair<string, string?>> ParseConfiguration(IEnumerable<string> rawSpecs, bool allowUnset)
    {
        foreach (string rawSpec in rawSpecs)
        {
            if (ConfigurationSpecRegex.Match(rawSpec) is not { Success: true } match)
                continue;

            string? specValue = match.Groups[2] is { Success: true, Value: var matchValue } ? matchValue : null;
            if (specValue is null && !allowUnset)
                continue;

            string specKey = match.Groups[1].Value;

            yield return KeyValuePair.Create(specKey, specValue);
        }
    }

    /// <summary>
    /// Updates logger filter options according to raw log level specifications.
    /// </summary>
    /// <param name="rawSpecs">The raw log level specifications.</param>
    /// <param name="loggerFilterOptions">The logger filter options to update.</param>
    /// <param name="allowMinLevel">Whether a raw specification may set the minimum log level.</param>
    /// <returns><c>true</c> if at least one log level specification was applied; otherwise, <c>false</c>.</returns>
    public static bool UpdateLogLevel(IEnumerable<string> rawSpecs, LoggerFilterOptions loggerFilterOptions, bool allowMinLevel)
    {
        IList<LoggerFilterRule> rules = loggerFilterOptions.Rules;
        bool any = false;

        foreach (string rawSpec in rawSpecs)
        {
            if (allowMinLevel && Enum.TryParse(rawSpec, true, out LogLevel minLogLevel))
            {
                any = true;
                loggerFilterOptions.MinLevel = minLogLevel;
                continue;
            }

            if (LogLevelSpecRegex.Match(rawSpec) is not { Success: true } match)
            {
                continue;
            }

            LogLevel? finalLogLevel;
            Group logLevelGroup = match.Groups[2];
            if (!logLevelGroup.Success)
            {
                finalLogLevel = null;
            }
            else if (Enum.TryParse(logLevelGroup.Value, true, out LogLevel logLevel))
            {
                finalLogLevel = logLevel;
            }
            else
            {
                continue;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static string? Collapse(string? c) => c is null || c.Equals("Default", StringComparison.OrdinalIgnoreCase) ? null : c;

            string? category = Collapse(match.Groups[1].Value);
            string? provider = match.Groups[3] is { Success: true } providerGroup ? providerGroup.Value : null;

            SetRule(category, provider, finalLogLevel);

            [SuppressMessage("ReSharper", "VariableHidesOuterVariable")]
            void SetRule(string? category, string? provider, LogLevel? logLevel)
            {
                any = true;

                IEnumerable<int> indexes =
                [
                    ..rules.IndexesWhere(x => string.Equals(x.CategoryName, category, StringComparison.OrdinalIgnoreCase) && x.ProviderName == provider),
                ];
                if (indexes.Any())
                {
                    foreach (int index in indexes)
                    {
                        rules[index] = new LoggerFilterRule(provider, category, logLevel, null);
                    }
                }
                else
                {
                    rules.Add(new LoggerFilterRule(provider, category, logLevel, null));
                }
            }
        }

        return any;
    }
}
