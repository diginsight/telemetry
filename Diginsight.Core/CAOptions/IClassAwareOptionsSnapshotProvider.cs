using Microsoft.Extensions.Options;

namespace Diginsight.CAOptions;

public interface IClassAwareOptionsSnapshotProvider<out TOptions>
    where TOptions : class
{
    IOptionsSnapshot<TOptions> For(Type? @class);
}
