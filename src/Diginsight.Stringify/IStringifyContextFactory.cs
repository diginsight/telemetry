using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Diginsight.Stringify;

public interface IStringifyContextFactory
{
    StringifyContext MakeStringifyContext([NotNull] ref StringBuilder? stringBuilder);

    StringifyContextFactoryBuilder PrepareClone();

    IStringifiable ToStringifiable(object? obj);
}
