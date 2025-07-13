# Diginsight.Diagnostics

**Diginsight.Diagnostics implements main observability and diagnostics functionality** that is based on .NET logging, activity tracing, and metrics collection capabilities. It seamlessly integrates with the .NET ecosystem including System.Diagnostics, Microsoft.Extensions.Logging, and OpenTelemetry.

## 🎯 Purpose

The library provides:
- **Activity Lifecycle Logging**: Automatic start/stop logging for .NET Activities with rich contextual information
- **Enhanced Console Formatting**: Advanced console output with customizable layouts, colors, and hierarchical display
- **Metrics Collection**: Automatic span duration metrics and custom timing measurements
- **Developer Experience**: Simplified APIs for creating instrumented activities and methods
- **Performance Monitoring**: Built-in observability for application performance with minimal overhead

## 🏗️ Core Architecture

### Key Components

#### 1. Activity Lifecycle Management
- **`ActivityLifecycleLogEmitter`**: Core component that automatically logs activity start/stop events with inputs/outputs
- **`DeferredActivityLifecycleLogEmitter`**: Provides early logging capabilities before DI container is ready
- **`ActivitySourceExtensions`**: Extension methods for creating rich, instrumented activities with automatic naming

#### 2. Enhanced Console Logging
- **`DiginsightConsoleFormatter`**: Custom console formatter with advanced layout capabilities and token-based formatting
- **`DiginsightTextWriter`**: Sophisticated text rendering engine with modular token system
- **Line Tokens**: Modular formatting components (timestamp, category, indentation, duration, etc.)

#### 3. Metrics and Timing
- **`TimerHistogram`**: High-level wrapper for creating timing measurements with tags
- **`SpanDurationMetricRecorder`**: Automatic collection of activity duration metrics
- **`CustomDurationMetricRecorder`**: Configurable metrics recording for custom scenarios

#### 4. Configuration and Options
- **`DiginsightActivitiesOptions`**: Configuration for activity behavior, sampling, and logging
- **`DiginsightConsoleFormatterOptions`**: Console output customization and layout options
- **Activity Sampling**: Configurable sampling strategies for performance optimization

## 🚀 Key Features

### Rich Activity Creation
// Create method activity with automatic naming
using var activity = activitySource.StartMethodActivity(new { userId = 123, name = "John" });

// Create custom activity with inputs
using var activity = activitySource.StartRichActivity("ProcessOrder", 
    new { orderId = 456, customerId = 789 });

// Set outputs
activity?.SetOutput(result);
activity?.SetNamedOutputs(new { processedItems = 10, errors = 0 });
### Automatic Logging

Activities automatically log:
- **Start events**: `ProcessOrder({ orderId: 456, customerId: 789 }) START`
- **End events**: `ProcessOrder({ orderId: 456, customerId: 789 }) => { success: true } END`
- **Duration tracking**: Automatic timing information in logs and metrics
- **Hierarchical structure**: Nested activities with proper indentation and tree visualization

### Advanced Console Formatting

The console formatter provides:
- **Colorized output**: Different colors for log levels and components (when supported)
- **Flexible layouts**: Customizable token-based formatting system
- **Activity awareness**: Special formatting for activity lifecycle events with duration display
- **Responsive design**: Automatic width adjustment and intelligent line breaking

### Metrics Collection
// Automatic span duration metrics
services.AddSpanDurationMetricRecorder();

// Custom timer histograms
var timer = meter.CreateTimer("operation_duration");
using var lap = timer.StartLap(new { operation = "database_query" });
## 📋 Core Classes Reference

### Activity Management

| Class | Purpose |
|-------|---------|
| `ActivityLifecycleLogEmitter` | Handles automatic logging of activity start/stop events with payload serialization |
| `ActivitySourceExtensions` | Extension methods for creating instrumented activities with rich metadata |
| `ActivityUtils` | Utility methods for activity manipulation and property management |
| `ActivityExtensions` | Extensions for setting outputs, metadata, and custom properties |

### Logging Infrastructure

| Class | Purpose |
|-------|---------|
| `DiginsightConsoleFormatter` | Custom console formatter with enhanced capabilities and token system |
| `DiginsightTextWriter` | Core text rendering engine with modular token-based formatting |
| `DeferredLoggerFactory` | Early logging support before DI initialization |
| `EarlyLoggingManager` | Manages logging during application startup phase |

### Metrics and Timing

| Class | Purpose |
|-------|---------|
| `TimerHistogram` | High-level timing measurement wrapper with tag support |
| `TimerLap` | Individual timing measurement instance with automatic recording |
| `SpanDurationMetricRecorder` | Automatic activity duration metrics collection |
| `MeterExtensions` | Extensions for creating timers and histograms from meters |

### Configuration

| Class | Purpose |
|-------|---------|
| `DiginsightActivitiesOptions` | Activity behavior configuration including sampling and logging |
| `DiginsightConsoleFormatterOptions` | Console formatting options and layout customization |
| `LogBehavior` | Enumeration for activity logging behavior (Show, Hide, Truncate) |

### Text Formatting Tokens

| Token | Purpose |
|-------|---------|
| `TimestampToken` | Renders timestamp information with timezone support |
| `CategoryToken` | Displays logger category with customizable formatting |
| `IndentationToken` | Provides hierarchical indentation for nested activities |
| `DepthToken` | Shows activity nesting depth as numeric or visual indicator |
| `MessageToken` | Renders the log message with line breaking support |
| `DurationToken` | Displays activity duration with intelligent formatting |

## 🔧 Usage Examples

### Basic Setup
// Configure logging with Diginsight
builder.Services.AddLogging(logging =>
{
    logging.AddDiginsightConsole(options =>
    {
        options.UseColor = true;
        options.TimeZone = TimeZoneInfo.Local;
    });
});

// Add activity lifecycle logging
builder.Services.AddDiginsightCore();

// Add metrics collection
builder.Services.AddSpanDurationMetricRecorder();
### Creating Instrumented Activities
public class OrderService
{
    private static readonly ActivitySource ActivitySource = new("OrderService");
    private readonly ILogger<OrderService> logger;

    public async Task<Order> ProcessOrderAsync(int orderId)
    {
        // Automatic method activity with inputs
        using var activity = ActivitySource.StartMethodActivity(
            logger, 
            new { orderId }
        );

        try
        {
            var order = await GetOrderAsync(orderId);
            var result = await ProcessAsync(order);
            
            // Set outputs for logging
            activity?.SetOutput(result);
            activity?.SetNamedOutputs(new { 
                processedItems = result.Items.Count,
                totalAmount = result.TotalAmount 
            });
            
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
### Custom Metrics
public class MetricsService
{
    private readonly TimerHistogram dbTimer;
    
    public MetricsService(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("MyApp");
        dbTimer = meter.CreateTimer("database_operation_duration");
    }
    
    public async Task<T> ExecuteWithTimingAsync<T>(Func<Task<T>> operation, Tags tags)
    {
        using var lap = dbTimer.StartLap(tags);
        return await operation();
    }
}
### Early Logging Setup
// For libraries that need logging before DI container is ready
public class EarlyLoggingExample
{
    private static readonly EarlyLoggingManager earlyLogging = 
        new EarlyLoggingManager(source => source.Name.StartsWith("MyApp"));
    
    static EarlyLoggingExample()
    {
        // Use early logging
        var logger = earlyLogging.LoggerFactory.CreateLogger<EarlyLoggingExample>();
        logger.LogInformation("Early initialization started");
    }
    
    public static void ConfigureServices(IServiceCollection services)
    {
        // Attach to DI container
        earlyLogging.AttachTo(services);
    }
}
## 🎨 Console Output Examples

The Diginsight console formatter produces rich, hierarchical output:
15:30:45.123 [DBG] OrderService.ProcessOrderAsync({ orderId: 123 }) START
15:30:45.125 [INF] ├─ Getting order from database
15:30:45.130 [DBG] │  DatabaseService.GetOrderAsync({ orderId: 123 }) START
15:30:45.145 [DBG] │  DatabaseService.GetOrderAsync({ orderId: 123 }) => { order: {...} } END ⏱ 15ms
15:30:45.146 [INF] ├─ Processing payment
15:30:45.160 [DBG] │  PaymentService.ProcessPayment({ amount: 99.99 }) START
15:30:45.180 [DBG] │  PaymentService.ProcessPayment({ amount: 99.99 }) => { success: true } END ⏱ 20ms
15:30:45.181 [DBG] OrderService.ProcessOrderAsync({ orderId: 123 }) => { orderId: 123, status: "Completed" } END ⏱ 58ms
## 🔗 Integration Points

### OpenTelemetry
- Seamless integration with OpenTelemetry tracing
- Automatic suppression of instrumentation during logging to prevent loops
- Compatible with OTEL activity sampling and context propagation

### ASP.NET Core
- Automatic HTTP request tracing with activity correlation
- Integration with ASP.NET Core logging pipeline
- Support for structured logging and correlation IDs

### Dependency Injection
- Full support for Microsoft.Extensions.DependencyInjection
- Scoped and singleton service registration patterns
- Configuration binding and options pattern integration

## 🎛️ Configuration

### Activity Sources Configuration
{
  "Diginsight": {
    "Activities": {
      "ActivitySources": {
        "MyApp.*": true,
        "System.*": false,
        "Microsoft.*": false
      },
      "LogBehavior": "Show",
      "ActivityLogLevel": "Debug",
      "RecordSpanDurations": true
    }
  }
}
### Console Formatter Configuration
{
  "Logging": {
    "Console": {
      "FormatterName": "diginsight",
      "FormatterOptions": {
        "UseColor": true,
        "TotalWidth": 120,
        "TimeZone": "Local"
      }
    }
  }
}
## 🏗️ Target Frameworks

Supports multiple .NET versions:
- .NET Standard 2.0, 2.1
- .NET 6, 7, 8, 9

The library adapts its feature set based on the target framework, ensuring optimal performance and compatibility across different .NET versions.

## 📦 Dependencies

- **Microsoft.Extensions.Logging**: Core logging abstractions and console provider
- **Microsoft.Extensions.Diagnostics**: Metrics and observability infrastructure
- **System.Diagnostics.DiagnosticSource**: Activity tracing foundation
- **Diginsight.Stringify**: Object serialization for logging (internal dependency)
- **Pastel**: Console color support (.NET 6+)

This diagnostics library provides a solid foundation for building observable, maintainable .NET applications with comprehensive logging and metrics capabilities.