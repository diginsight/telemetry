using Diginsight.Diagnostics.TextWriting;

namespace Diginsight.Diagnostics.Log4Net;

public interface ILog4NetLineDescriptorProvider
{
    LineDescriptor GetLineDescriptor();
}
