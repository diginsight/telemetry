using Microsoft.Extensions.Options;

namespace Diginsight.CAOptions;

// TODO Replace in DI
public interface IClassAwareOptionsSnapshot<out TOptions> : IClassAwareOptions<TOptions>, IOptionsSnapshot<TOptions>
    where TOptions : class
{
    TOptions Get(string name, Type @class);

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    TOptions IClassAwareOptions<TOptions>.Get(Type @class) => Get(Options.DefaultName, @class);

    TOptions IOptionsSnapshot<TOptions>.Get(string? name) => Get(name ?? Options.DefaultName, ClassAwareOptions.NoType);
#endif
}
