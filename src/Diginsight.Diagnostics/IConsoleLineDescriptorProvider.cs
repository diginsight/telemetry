using Diginsight.Diagnostics.TextWriting;

namespace Diginsight.Diagnostics;

/// <summary>
/// Represents an interface for providing console line descriptors.
/// </summary>
public interface IConsoleLineDescriptorProvider
{
    /// <summary>
    /// Gets the line descriptor for the specified console width.
    /// </summary>
    /// <param name="width">The console width.</param>
    /// <returns>The line descriptor.</returns>
    LineDescriptor GetLineDescriptor(int? width);
}
