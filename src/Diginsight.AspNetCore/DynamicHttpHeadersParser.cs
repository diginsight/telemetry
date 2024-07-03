using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Diginsight.AspNetCore;

public static class DynamicHttpHeadersParser
{
    private static readonly Regex ConfigurationSpecRegex = new ("^([^= ]+?)(?: *= *([^ ]*))?$");
    private static readonly Regex LogLevelSpecRegex = new ("^([^= ]+?) *= *([a-z]+?)(?: *; *p *= *([^ ]+?))?$", RegexOptions.IgnoreCase);

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

            yield return new KeyValuePair<string, string?>(specKey, specValue);
        }
    }

    public static bool ParseLogLevel(IEnumerable<string> rawSpecs, LoggerFilterOptions loggerFilterOptions, bool allowMinLevel)
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

            if (LogLevelSpecRegex.Match(rawSpec) is not { Success: true } match ||
                !Enum.TryParse(match.Groups[2].Value, true, out LogLevel logLevel))
            {
                continue;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static string? Collapse(string? c) => c is null || c.Equals("Default", StringComparison.OrdinalIgnoreCase) ? null : c;

            string? category = Collapse(match.Groups[1].Value);
            string? provider = match.Groups[3] is { Success: true } providerGroup ? providerGroup.Value : null;
            SetRule(category, provider, logLevel);

            [SuppressMessage("ReSharper", "VariableHidesOuterVariable")]
            void SetRule(string? category, string? provider, LogLevel logLevel)
            {
                any = true;

                int index = rules.FirstIndexWhere(x => string.Equals(x.CategoryName, category, StringComparison.OrdinalIgnoreCase) && x.ProviderName == provider);
                if (index >= 0)
                {
                    rules[index] = new LoggerFilterRule(provider, category, logLevel, null);
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
