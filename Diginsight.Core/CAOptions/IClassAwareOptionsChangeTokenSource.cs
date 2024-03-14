using Microsoft.Extensions.Primitives;

namespace Diginsight.CAOptions;

// ReSharper disable once UnusedTypeParameter
public interface IClassAwareOptionsChangeTokenSource<out TOptions>
{
    IChangeToken GetChangeToken();

    string Name { get; }
}
