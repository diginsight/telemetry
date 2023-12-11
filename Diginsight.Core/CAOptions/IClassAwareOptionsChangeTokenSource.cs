using Microsoft.Extensions.Options;

namespace Diginsight.CAOptions;

public interface IClassAwareOptionsChangeTokenSource<out TOptions> : IOptionsChangeTokenSource<TOptions>
{
    Type? Class { get; }
}
