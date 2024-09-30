using Microsoft.Extensions.Options;

namespace Diginsight.Options;

public interface IClassAwareOptionsSnapshot<out TOptions> : IClassAwareOptions<TOptions>, IOptionsSnapshot<TOptions>
    where TOptions : class
{
    TOptions Get(string? name, Type? @class);

#if NET || NETSTANDARD2_1_OR_GREATER
    TOptions IClassAwareOptions<TOptions>.Get(Type? @class) => Get(null, @class);

    TOptions IOptionsSnapshot<TOptions>.Get(string? name) => Get(name, null);
#endif
}
