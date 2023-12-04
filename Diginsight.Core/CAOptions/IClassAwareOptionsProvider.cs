using Microsoft.Extensions.Options;

namespace Diginsight.CAOptions;

public interface IClassAwareOptionsProvider<out TOptions>
    where TOptions : class
{
    IOptions<TOptions> For(Type? @class);
}
