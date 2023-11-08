using System.Text;

namespace Diginsight.Strings;

public interface IAppendingContextFactory
{
    AppendingContext MakeAppendingContext(StringBuilder stringBuilder);

    AppendingContextFactoryBuilder PrepareClone();
}
