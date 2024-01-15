namespace Diginsight.Diagnostics.TextWriting;

public interface IConsoleLineDescriptorProvider
{
    LineDescriptor GetLineDescriptor(int? width);
}
