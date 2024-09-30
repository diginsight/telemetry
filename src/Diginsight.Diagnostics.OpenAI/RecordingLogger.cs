using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public class RecordingLogger : ILogger
{
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
            if (exception != null)
            {
                var message = formatter(state, exception);
                Console.Write(message);
            }
        }
    }
}

public class RecordingLoggerProvider : ILoggerProvider
{
    public RecordingLoggerProvider() { }

    public ILogger CreateLogger(string categoryName)
    {
        var logger = new RecordingLogger();
        return logger;
    }
    public void Dispose()
    {
        ;
    }
}

public static class RecordingLoggerExtensions {
    public static ILoggingBuilder AddDiginsightRuntimeAnalysis(this ILoggingBuilder builder, ILoggerProvider logProvider, IConfiguration config = null, string configurationPrefix = null) // , IServiceProvider serviceProvider
    {

        var recordingLogProvider = new RecordingLoggerProvider();

        builder.AddProvider(recordingLogProvider);
        return builder;
    }
}

