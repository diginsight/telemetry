using Diginsight.Diagnostics.TextWriting;
using Microsoft.Extensions.Options;

namespace Diginsight.Diagnostics.Log4Net;

internal sealed class Log4NetLineDescriptorProvider : ILog4NetLineDescriptorProvider
{
    private readonly IEnumerable<ILineTokenParser> customLineTokenParsers;
    private readonly IDiginsightLayoutSkeletonOptions layoutSkeletonOptions;

    private LineDescriptor? lineDescriptor;

    public Log4NetLineDescriptorProvider(
        IEnumerable<ILineTokenParser> customLineTokenParsers,
        IOptions<DiginsightLayoutSkeletonOptions> layoutSkeletonOptions
    )
    {
        this.customLineTokenParsers = customLineTokenParsers;
        this.layoutSkeletonOptions = layoutSkeletonOptions.Value;
    }

    public LineDescriptor GetLineDescriptor()
    {
        return lineDescriptor ??= LineDescriptor.ParseFull(layoutSkeletonOptions.Pattern, customLineTokenParsers);
    }
}
