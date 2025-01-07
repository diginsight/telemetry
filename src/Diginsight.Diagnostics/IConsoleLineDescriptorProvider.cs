using Diginsight.Diagnostics.TextWriting;

namespace Diginsight.Diagnostics;

public interface IConsoleLineDescriptorProvider
{
    LineDescriptor GetLineDescriptor(int? width);
}
