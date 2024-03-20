using Microsoft.Extensions.Options;

namespace Diginsight.CAOptions;

public interface IClassAwareOptions<out TOptions> : IOptions<TOptions>
    where TOptions : class
{
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    TOptions IOptions<TOptions>.Value => Get(null);
#endif

    TOptions Get(Type? @class);
}
