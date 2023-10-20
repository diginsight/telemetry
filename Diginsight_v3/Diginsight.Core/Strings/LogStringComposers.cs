using System.Diagnostics.CodeAnalysis;

namespace Diginsight.Strings;

public static class LogStringComposers
{
    private static ILogStringComposer? @default;

    [AllowNull]
    public static ILogStringComposer Default
    {
        get => @default ??= LogStringComposerBuilder.Default.Build();
        set => @default = value;
    }
}
