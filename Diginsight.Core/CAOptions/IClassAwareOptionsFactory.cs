using Microsoft.Extensions.Options;

namespace Diginsight.CAOptions;

public interface IClassAwareOptionsFactory<TOptions> : IOptionsFactory<TOptions>
    where TOptions : class
{
    TOptions Create(string name, Type @class);

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    TOptions IOptionsFactory<TOptions>.Create(string name) => Create(name, ClassAwareOptions.NoClass);
#endif
}
