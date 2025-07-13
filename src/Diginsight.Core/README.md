# Diginsight.Core

## Overview

**Diginsight.Core is a foundational library for diginsight providing configuration management, dependency injection enhancements, logging and runtime helpers.**

## Key Features

### 🔧 Class-Aware Configuration System
Configuration management that goes behond traditional .NET Options pattern:

- **Filtered Configuration**: Scoped configuration sections with inheritance
- **Class-Aware Options**: Configuration binding that considers the calling class context
- **Dynamic Configuration**: Real-time configuration changes with validation
- **Volatile Configuration**: Dynamic configuration updates without application restart

### 🏗️ Enhanced Dependency Injection
Powerful extensions to Microsoft's built-in DI container:

- **Diginsight Service Provider**: Enhanced service provider with advanced features
- **Service Validation**: Comprehensive validation in development environments
- **Decorators and Interceptors**: Advanced service decoration patterns
- **Lifecycle Management**: Custom service lifetime management

### 📝 Advanced Logging Infrastructure
Structured logging with rich metadata support:

- **Metadata Logging**: Attach contextual metadata to log entries
- **Interpolated String Handlers**: High-performance structured logging (.NET 7+)
- **Logger Factory Management**: Centralized logger configuration
- **Log Level Extensions**: Convenient logging methods for all levels

### ⚡ Performance & Runtime Utilities
High-performance utilities for common operations:

- **Task Utilities**: Advanced async patterns including `WhenAnyValid`
- **Runtime Reflection**: Efficient caller type and method name detection
- **Heuristic Size Calculation**: Memory usage estimation for objects
- **Collection Extensions**: Performance-optimized collection operations

### ⏱️ Time & Expiration Management
Sophisticated time handling with special "Never" semantics:

- **Expiration Type**: TimeSpan with "Never" support and proper formatting
- **Type Conversion**: Built-in converters for configuration binding
- **Arithmetic Operations**: Safe time arithmetic with overflow handling

## Installation

```bash
dotnet add package Diginsight.Core
```

## Quick Start

### Basic Setup with Enhanced DI

```csharp
using Diginsight;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Use Diginsight service provider with validation
builder.UseDiginsightServiceProvider(validateInDevelopment: true);

// Add class-aware options
builder.Services.AddClassAwareOptions();

var host = builder.Build();
await host.RunAsync();
```

### Class-Aware Configuration

```csharp
// appsettings.json
{
  "MyApp": {
    "Services": {
      "EmailService": {
        "RetryCount": 3,
        "Timeout": "00:00:30"
      },
      "PaymentService": {
        "RetryCount": 5,
        "Timeout": "00:01:00"
      }
    }
  }
}

// Service registration
services.ConfigureClassAware<ServiceOptions>(configuration.GetSection("MyApp:Services"));

// Usage - automatically gets configuration for the calling class
public class EmailService
{
    private readonly ServiceOptions _options;
    
    public EmailService(IClassAwareOptions<ServiceOptions> options)
    {
        _options = options.Value; // Gets EmailService-specific config
    }
}
```

### Advanced Logging with Metadata

```csharp
using Diginsight.Logging;

public class OrderProcessor
{
    private readonly ILogger<OrderProcessor> _logger;
    
    public OrderProcessor(ILogger<OrderProcessor> logger)
    {
        _logger = logger;
    }
    
    public async Task ProcessOrderAsync(Order order)
    {
        // Add metadata to all log entries in this scope
        var enrichedLogger = _logger.WithMetadata(new LogMetadataCarrier
        {
            ["OrderId"] = order.Id,
            ["CustomerId"] = order.CustomerId,
            ["Amount"] = order.Total
        });
        
        // .NET 7+ interpolated string handler for high performance
        enrichedLogger.LogInformation($"Processing order {order.Id} for ${order.Total}");
        
        try
        {
            await ProcessOrderInternalAsync(order);
            enrichedLogger.LogInformation($"Order {order.Id} processed successfully");
        }
        catch (Exception ex)
        {
            enrichedLogger.LogError(ex, $"Failed to process order {order.Id}");
            throw;
        }
    }
}
```

### Volatile Configuration for Runtime Updates

```csharp
// Configuration class implementing volatile interface
public class FeatureFlags : IVolatilelyConfigurable
{
    public bool EnableNewCheckout { get; set; }
    public int MaxRetries { get; set; }
    public Expiration CacheTimeout { get; set; } = TimeSpan.FromMinutes(5);
}

// Service registration
services.VolatilelyConfigureClassAware<FeatureFlags>();

// Usage
public class CheckoutService
{
    private readonly IClassAwareOptionsMonitor<FeatureFlags> _featureFlags;
    
    public CheckoutService(IClassAwareOptionsMonitor<FeatureFlags> featureFlags)
    {
        _featureFlags = featureFlags;
        
        // React to configuration changes
        _featureFlags.OnChange((flags, name) => 
        {
            // Handle configuration change
            ReconfigureService(flags);
        });
    }
    
    public async Task ProcessCheckoutAsync()
    {
        var flags = _featureFlags.CurrentValue;
        
        if (flags.EnableNewCheckout)
        {
            await UseNewCheckoutFlowAsync();
        }
        else
        {
            await UseLegacyCheckoutFlowAsync();
        }
    }
}
```

### Advanced Task Utilities

```csharp
using Diginsight;

// Execute multiple tasks and return first valid result
var taskFactories = new[]
{
    (CancellationToken ct) => CallPrimaryServiceAsync(ct),
    (CancellationToken ct) => CallSecondaryServiceAsync(ct),
    (CancellationToken ct) => CallFallbackServiceAsync(ct)
};

try
{
    var result = await TaskUtils.WhenAnyValid(
        taskFactories,
        prefetchCount: 3,           // Start all 3 tasks
        maxParallelism: 2,          // But limit to 2 concurrent
        maxDelay: TimeSpan.FromSeconds(5), // 5 second timeout per task
        isValid: task => ValueTask.FromResult(task.Status == TaskStatus.RanToCompletion),
        cancellationToken: cancellationToken
    );
    
    // Use the first successful result
    ProcessResult(result);
}
catch (InvalidOperationException)
{
    // No task completed successfully
    HandleAllTasksFailed();
}
```

### Runtime Utilities

```csharp
using Diginsight.Runtime;

public class DiagnosticService
{
    public void AnalyzeMemoryUsage(object data)
    {
        // Get calling class information
        var callerType = RuntimeUtils.GetCallerType();
        var (memberName, localFunction) = RuntimeUtils.GetCallerName();
        
        // Calculate object size heuristically
        var size = data.GetSizeHeuristically(depthLimit: 10);
        
        _logger.LogInformation(
            "Memory analysis from {CallerType}.{Member}: {Size} bytes",
            callerType.Name, memberName, size);
    }
}
```

### Collection Extensions

```csharp
using Diginsight;

// Find indexes of matching elements
var numbers = new[] { 1, 3, 5, 3, 7, 3, 9 };
var threeIndexes = numbers.IndexesWhere(x => x == 3); // [1, 3, 5]

// Check sequence equivalence (same elements, any order)
var list1 = new[] { 1, 2, 3 };
var list2 = new[] { 3, 1, 2 };
bool equivalent = list1.SequenceEquivalent(list2); // true

// Get sequence hash code for use in dictionaries
var hash = list1.SequenceGetHashCode(); // Order-independent hash
```

## API Reference

### Core Interfaces

- **`IClassAwareOptions<T>`**: Class-aware options access
- **`IVolatilelyConfigurable`**: Marker for volatile configuration
- **`IDynamicallyConfigurable`**: Marker for dynamic configuration
- **`ILogMetadata`**: Structured logging metadata
- **`IHeuristicSizeProvider`**: Custom size calculation

### Key Classes

- **`DiginsightServiceProviderFactory`**: Enhanced DI container factory
- **`Expiration`**: TimeSpan with "Never" semantics
- **`MetadataLogger`**: Logger with attached metadata
- **`RuntimeUtils`**: Reflection and memory utilities
- **`TaskUtils`**: Advanced async patterns

### Extension Methods

- **`UseDiginsightServiceProvider()`**: Enable enhanced DI
- **`AddClassAwareOptions()`**: Register class-aware configuration
- **`ConfigureClassAware<T>()`**: Bind class-aware configuration
- **`VolatilelyConfigureClassAware<T>()`**: Enable volatile updates
- **`WithMetadata()`**: Add metadata to logger

## Configuration

The library supports extensive configuration through standard .NET configuration providers:

```json
{
  "Diginsight": {
    "Options": {
      "ReplaceClassAgnosticOptions": true,
      "EnableDynamicConfiguration": true
    },
    "Logging": {
      "EnableMetadata": true,
      "DefaultMetadata": {
        "Application": "MyApp",
        "Version": "1.0.0"
      }
    }
  }
}
```

## Performance Considerations

- **Zero-allocation logging**: Interpolated string handlers minimize allocations
- **Cached reflection**: Caller type/name detection is cached for performance
- **Lazy evaluation**: Configuration and services are created on-demand
- **Memory-efficient**: Optimized data structures and algorithms throughout

## Thread Safety

All components are designed to be thread-safe:
- Configuration updates are atomic
- Logger metadata is immutable
- Caches use concurrent collections
- Service resolution is safe across threads

## License

MIT License - see LICENSE file for details.

## Related Packages

- **Diginsight.Diagnostics**: Advanced diagnostics and tracing
- **Diginsight.AspNetCore**: ASP.NET Core integration
- **Diginsight.Json**: JSON serialization extensions
- **Diginsight.Stringify**: Object stringification utilities