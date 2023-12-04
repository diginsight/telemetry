using Microsoft.Extensions.Options;

namespace Diginsight.CAOptions;

// ReSharper disable once UnusedTypeParameter
public interface IClassAwareOptions<out TOptions, TClass> : IOptions<TOptions>
    where TOptions : class;
