using Microsoft.Extensions.Options;

namespace Diginsight.CAOptions;

// ReSharper disable once UnusedTypeParameter
public interface IClassAwareOptionsSnapshot<out TOptions, TClass> : IClassAwareOptions<TOptions, TClass>, IOptionsSnapshot<TOptions>
    where TOptions : class;
