using Microsoft.Extensions.Options;

namespace Diginsight.CAOptions;

// ReSharper disable once UnusedTypeParameter
public interface IClassAwareOptionsMonitor<out TOptions, TClass> : IOptionsMonitor<TOptions>
    where TOptions : class;
