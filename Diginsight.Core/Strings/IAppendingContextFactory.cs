using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Diginsight.Strings;

public interface IAppendingContextFactory
{
    AppendingContext MakeAppendingContext([NotNull] ref StringBuilder? stringBuilder);

    AppendingContextFactoryBuilder PrepareClone();
}
