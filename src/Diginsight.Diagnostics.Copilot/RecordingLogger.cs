using Diginsight.CAOptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public class RecordingLogger : ILogger
{
    //private readonly IClassAwareOptionsMonitor<FeatureFlagOptions> featureFlagOptionsMonitor;

    public RecordingLogger() // IClassAwareOptionsMonitor<FeatureFlagOptions> featureFlagOptionsMonitor
    {
        //this.featureFlagOptionsMonitor = featureFlagOptionsMonitor;
    }

    IDisposable ILogger.BeginScope<TState>(TState state)
    {
        Console.WriteLine(state);
        return null!;
    }

    bool ILogger.IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception, string> formatter)
    {
        if (formatter == null)
        {
            Console.WriteLine(state);
        }
        else
        {
            var message = formatter(state, exception!);
            Console.Write(message);
        }
    }
}

public class RecordingLoggerProvider : ILoggerProvider
{
    //private readonly IClassAwareOptionsMonitor<FeatureFlagOptions> featureFlagOptionsMonitor;

    public RecordingLoggerProvider() // IClassAwareOptionsMonitor<FeatureFlagOptions> featureFlagOptionsMonitor
    {
        //this.featureFlagOptionsMonitor = featureFlagOptionsMonitor;

    }

    public ILogger CreateLogger(string categoryName)
    {
        var logger = new RecordingLogger(); // this.featureFlagOptionsMonitor
        return logger;
    }
    public void Dispose()
    {
        ;
    }
}

public static class RecordingLoggerExtensions
{
    public static ILoggingBuilder AddDiginsightRuntimeAnalysis(this ILoggingBuilder builder, IConfiguration? config = null) // , IServiceProvider serviceProvider
    {
        var recordingLogProvider = new RecordingLoggerProvider();
        builder.AddProvider(recordingLogProvider);
        return builder;
    }
}


public class FeatureFlagOptions : IDynamicallyConfigurable
{
    public bool RecordApplicationFlowEnabled { get; set; }

    object IDynamicallyConfigurable.MakeFiller() => this;
}
