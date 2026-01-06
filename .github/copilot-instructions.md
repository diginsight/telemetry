# Diginsight Telemetry - Copilot Instructions

## Repository Overview

**Diginsight Telemetry** is a .NET observability library providing automatic, comprehensive application flow tracing through standard `ILogger<>` and `System.Diagnostics.Activity` APIs. It integrates with OpenTelemetry, supports dynamic logging/configuration, and targets netstandard2.0+ through .NET 8+.

## Architecture

### Multi-Package Structure
The solution is organized into distinct NuGet packages under `src/`:

**Core Packages:**
- **Diginsight.Core**: Foundation - class-aware options, enhanced DI, logging infrastructure, volatile configuration
- **Diginsight.Diagnostics**: Activity lifecycle logging, console formatters, metrics collection, timing infrastructure
- **Diginsight.Stringify**: Object rendering for logs - customizable stringification with depth control
- **Diginsight.Json**: JSON utilities for structured data handling

**Platform Integration:**
- **Diginsight.AspNetCore**: ASP.NET Core integration - dynamic logging via HTTP headers, context propagation
- **Diginsight.Diagnostics.AspNetCore**: ASP.NET-specific diagnostic features
- **Diginsight.Diagnostics.OpenTelemetry**: OpenTelemetry bridge - tracers, meters, exporters
- **Diginsight.Diagnostics.AspNetCore.OpenTelemetry**: ASP.NET + OpenTelemetry integration
- **Diginsight.Diagnostics.Log4Net**: Log4Net adapter with Diginsight layout

**Specialized:**
- **Diginsight.Atomify**: JSON composition helpers (Newtonsoft.Json & System.Text.Json)
- **Diginsight.Polyfills**: Compatibility shims for older .NET versions

### Key Architectural Patterns

**1. Activity-Based Tracing:**
All telemetry flows through `System.Diagnostics.Activity`. Extensions in `ActivitySourceExtensions` provide fluent APIs:
```csharp
using Activity activity = activitySource.StartMethodActivity(logger, payload: new { userId, orderId });
```

**2. Class-Aware Configuration:**
Options can vary by calling class context via `IClassAwareOptions<T>`. Enable with `services.AddClassAwareOptions()`. This powers component-level feature flags and per-class log levels.

**3. Deferred/Early Logging:**
Before DI container is built, use `DeferredLoggerFactory` and `DeferredActivityLifecycleLogEmitter`. They buffer events and flush to real implementations once available via `FlushOnCreateServiceProvider()`.

**4. Dynamic Configuration:**
Options implementing `IDynamicallyConfigurable` can change at runtime without app restart. Paired with `VolatileConfiguration` system for HTTP header-driven config overrides.

## Essential Setup Patterns

### Basic Diginsight Integration
```csharp
// Program.cs or Startup.cs
var builder = WebApplication.CreateBuilder(args);

// Enable enhanced DI with validation
builder.UseDiginsightServiceProvider(validateInDevelopment: true);

// Add core diagnostics (activity logging + class-aware options)
builder.Logging.AddDiginsightCore();

// Console output with Diginsight formatting
builder.Logging.AddDiginsightConsole();

// Span duration metrics
builder.Services.AddSpanDurationMetricRecorder();
```

### Dynamic Logging via HTTP Headers
```csharp
// In ASP.NET Core projects, enable log level overrides:
services.Configure<DiginsightDistributedContextOptions>(options =>
{
    // Exclude these from baggage propagation:
    options.NonBaggageKeys.Add("Log-Level");
    options.NonBaggageKeys.Add("Metric-Recording-Enabled");
});
```
Clients can then send `Log-Level: Debug` headers to get detailed traces for specific requests on live environments.

### OpenTelemetry Integration
```csharp
// Add Diginsight to OpenTelemetry pipeline:
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddDiginsight() // Registers Diginsight activity sources
        .AddAzureMonitorTraceExporter())
    .WithMetrics(metrics => metrics
        .AddDiginsight() // Registers span_duration meters
        .AddAzureMonitorMetricExporter());

builder.Logging.AddDiginsightOpenTelemetry();
```

## Development Workflows

### Building
Standard .NET SDK commands from `src/`:
```bash
dotnet build Diginsight.slnx
dotnet test
```

Build configuration in `Directory.Build.props`:
- LangVersion: `preview`
- Nullable: `enable`
- Package lock files enabled
- Suppressed warnings: CA2255, IDE0051, IDE0290

### Testing
Test projects follow convention: `<PackageName>.Tests` (not shown in structure but standard pattern).

### Debugging
Enable full observability during development:
```json
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Trace",
      "Diginsight": "Debug"
    }
  },
  "Diginsight": {
    "Activities": {
      "LogActivities": true,
      "ActivitySources": { "*": true }
    }
  }
}
```

### Documentation Site
Root-level Quarto site (`docs/` output from `*.md` sources). Build with:
```bash
quarto render
```

Markdown articles in `src/docs/` with dual metadata structure (top YAML for Quarto, bottom HTML comment for validation tracking).

## Coding Conventions

### Dependency Injection Extensions
- Place in `<ProjectName>Extensions` classes marked `[EditorBrowsable(EditorBrowsableState.Never)]`
- Chain setup: `AddDiginsightCore()` → `AddDiginsightConsole()` / `AddDiginsightOpenTelemetry()`
- Use `TryAdd*` for non-destructive registration
- Core always calls `.AddClassAwareOptions()` and `.AddActivityListenersAdder()`

### Activity Logging Pattern
```csharp
// Standard method activity wrapper:
using Activity? activity = activitySource.StartMethodActivity(logger);
try
{
    // Work here
    activity?.SetOutput(result); // Optional
    return result;
}
catch (Exception ex)
{
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    throw;
}
```

### Options Classes
- Implement interfaces from `Diginsight.Options` namespace
- Volatile options: implement `IVolatileConfiguration`
- Class-aware: no special interface, just configure with `services.Configure<TOptions>()`
- Dynamic: implement `IDynamicallyConfigurable` + call `services.FlagAsDynamic<TOptions>(name)`

### Logging
Use interpolated string handlers for structured logging (net7.0+):
```csharp
logger.LogDebug($"Processing order {orderId} for user {userId}");
// Auto-captures as structured properties, not concatenated string
```

## Key Files and References

- [`src/Diginsight.Core/Extensions/DependencyInjectionExtensions.cs`](src/Diginsight.Core/Extensions/DependencyInjectionExtensions.cs) - Core DI setup
- [`src/Diginsight.Diagnostics/DependencyInjectionExtensions.cs`](src/Diginsight.Diagnostics/DependencyInjectionExtensions.cs) - Diagnostics registration
- [`src/Diginsight.Diagnostics/ActivitySourceExtensions.cs`](src/Diginsight.Diagnostics/ActivitySourceExtensions.cs) - Activity creation fluent API
- [`src/Diginsight.Core/Options/`](src/Diginsight.Core/Options/) - Class-aware options implementation
- [`src/docs/00. Getting Started/Getting Started.md`](src/docs/00. Getting Started/Getting Started.md) - Integration walkthrough
- [`src/docs/01. Concepts/`](src/docs/01. Concepts/) - Architecture and feature explanations

## Cross-Cutting Concerns

- **W3C Trace Context**: Full support via OpenTelemetry integration; TraceIds preserved across services
- **Performance**: Dynamic compilation, intelligent sampling, automatic truncation to minimize overhead
- **Multi-Framework**: Single codebase targets netstandard2.0 through net8.0+ using conditional compilation
- **Backward Compat**: Polyfills package provides missing APIs for older frameworks

## Documentation Articles (Dual Metadata)

Articles in `src/docs/` use dual YAML metadata blocks:
1. **Top YAML** (lines 1-X): Quarto frontmatter (`title`, `author`, `date`, `categories`) - edit manually only
2. **Bottom HTML Comment** (after References): Validation tracking (`validations`, `article_metadata`) - updated by validation tools

When working with documentation:
- Never modify top YAML block via automated tools
- Update bottom metadata block for validation results only
- Maintain consistent article structure (TOC, body, references)

