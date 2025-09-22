using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace Diginsight.Diagnostics;

public static class LoggerFactoryStaticAccessor
{
    [field: MaybeNull]
    public static ILoggerFactory LoggerFactory
    {
        get => field ?? throw new InvalidOperationException("LoggerFactory is unset");
        set;
    }
}
