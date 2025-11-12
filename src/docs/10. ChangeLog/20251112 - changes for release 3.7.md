# Changes for Release 3.7

**Release Date:** November 12, 2025  
**Commit Range:** `4ebf5faea81788ddba8fac2260d4a06e255ca5e3` ? `HEAD`

---

## Changes Overview

This release includes significant architectural improvements, bug fixes, and enhanced metric recording capabilities. The changes focus on better code organization, improved configuration flexibility, and refined telemetry filtering mechanisms.

### Key Changes Summary

1. **Configuration Schema Updates**
   - Property renaming in `IDiginsightActivitiesLogOptions`
   - Metric interface consolidation
   - Updated property names for consistency

2. **Architectural Improvements**
   - Configuration folder reorganization (`Configuration` ? `Configurations`)
   - Metric recording unification with filter and enricher pattern
   - Activity filtering mechanism renamed and improved

3. **Feature Enhancements**
   - Improved activity and activity source filtering
   - Enhanced metric recording with better filter/enricher integration
   - String-based configuration support for `LoggedActivityNames`
   - Immutable configuration with `Freeze()` method
   - Dynamic configuration support via `IDynamicallyConfigurable`

4. **Bug Fixes**
   - Fixed issues in `DiginsightTextWriter`
   - Fixed `DiginsightActivitiesOptions` edge cases
   - Resolved activity and activity source filtering bugs

5. **Development Updates**
   - Solution format migration (`.sln` ? `.slnx`)
   - C# 14 preview support
   - Code cleanup and modernization

---

## Changes Analysis

### 1. Configuration Schema Updates

#### 1.1 IDiginsightActivitiesLogOptions Interface Changes

**What Changed:**

```csharp
// BEFORE
public interface IDiginsightActivitiesLogOptions
{
    LogBehavior LogBehavior { get; }
    LogLevel ActivityLogLevel { get; }  // ? Property name
    bool WriteActivityActionAsPrefix { get; }
    bool DisablePayloadRendering { get; }
}

// AFTER
public interface IDiginsightActivitiesLogOptions
{
    IReadOnlyDictionary<string, LogBehavior> ActivityNames { get; }  // ? NEW
    LogBehavior LogBehavior { get; }
    LogLevel LogLevel { get; }  // ? Renamed from ActivityLogLevel
    bool WriteActivityActionAsPrefix { get; }
    bool DisablePayloadRendering { get; }
}
```

**Why Changes Were Applied:**
- **Property rename** (`ActivityLogLevel` ? `LogLevel`): Simplified and more consistent with .NET conventions
- **New `ActivityNames` property**: Provides read-only access to activity-specific logging behavior, improving encapsulation and supporting the new filtering mechanism

**Impact on Applications:**
- ? **JSON Configuration**: No breaking changes - `ActivityLogLevel` in config files still works
- ?? **Code accessing interface directly**: Update references from `ActivityLogLevel` to `LogLevel` when using `IDiginsightActivitiesLogOptions`

```csharp
// Migration needed if accessing via interface
// OLD
var level = logOptions.ActivityLogLevel;

// NEW
var level = logOptions.LogLevel;
```

---

#### 1.2 Metric Recording Interface Consolidation

**What Changed:**

```csharp
// BEFORE - IDiginsightActivitiesMetricOptions (REMOVED)
public interface IDiginsightActivitiesMetricOptions
{
    bool RecordSpanDurations { get; }  // ? Plural
    string MeterName { get; }
    string MetricName { get; }
    string? MetricUnit { get; }  // ? REMOVED
    string? MetricDescription { get; }
}

// AFTER - IMetricRecordingOptions (NEW)
public interface IMetricRecordingOptions
{
    bool Record { get; }  // ? Simplified from RecordSpanDurations
    string MeterName { get; }
    string MetricName { get; }
    string? MetricDescription { get; }
    // MetricUnit removed - not used in OpenTelemetry integration
}
```

**Why Changes Were Applied:**
- **Interface consolidation**: Unified metric recording under a single, purpose-specific interface
- **Property simplification**: `RecordSpanDurations` ? `Record` (more generic and reusable)
- **Removed unused property**: `MetricUnit` was not used in the OpenTelemetry implementation
- **Better naming**: More accurately reflects the interface's purpose

**Impact on Applications:**
- ?? **Breaking change** for code implementing `IDiginsightActivitiesMetricOptions`
- Migration required:

```csharp
// OLD
public class MyOptions : IDiginsightActivitiesMetricOptions
{
    public bool RecordSpanDurations { get; set; }
    public string MeterName { get; set; }
    public string MetricName { get; set; }
    public string? MetricUnit { get; set; }  // Remove this
    public string? MetricDescription { get; set; }
}

// NEW
public class MyOptions : IMetricRecordingOptions
{
    public bool Record { get; set; }  // Renamed
    public string MeterName { get; set; }
    public string MetricName { get; set; }
    public string? MetricDescription { get; set; }
}
```

---

#### 1.3 DiginsightActivitiesOptions Property Updates

**What Changed:**

```csharp
// Property names updated with specific prefixes
public sealed class DiginsightActivitiesOptions
{
    // BEFORE (implicit)
    bool RecordSpanDurations { get; set; }
    string? MeterName { get; set; }
    string? MetricName { get; set; }
    string? MetricDescription { get; set; }

    // AFTER (explicit prefixes)
    public bool RecordSpanDuration { get; set; }  // Singular
    public string? SpanDurationMeterName { get; set; }  // Prefixed
    public string? SpanDurationMetricName { get; set; }  // Prefixed
    public string? SpanDurationMetricDescription { get; set; }  // Prefixed
}
```

**Why Changes Were Applied:**
- **Property naming consistency**: Added `SpanDuration` prefix to clearly indicate these properties control span duration metrics
- **Singular vs Plural**: Changed to singular form (`RecordSpanDuration`) for consistency with boolean property naming conventions
- **Future extensibility**: Allows adding other metric types without naming conflicts

**Impact on Applications:**

| Old Configuration Property | New Configuration Property | Status |
|---------------------------|---------------------------|--------|
| `RecordSpanDurations` | `RecordSpanDuration` | ?? Update required |
| `MeterName` | `SpanDurationMeterName` | ? Backward compatible |
| `MetricName` | `SpanDurationMetricName` | ? Backward compatible |
| `MetricDescription` | `SpanDurationMetricDescription` | ? Backward compatible |

**Configuration Migration:**

```json
// OLD
{
  "Diginsight": {
    "Activities": {
      "RecordSpanDurations": true,
      "MeterName": "MyApp.Telemetry",
      "MetricName": "span_duration"
    }
  }
}

// NEW (recommended)
{
  "Diginsight": {
    "Activities": {
      "RecordSpanDuration": true,
      "SpanDurationMeterName": "MyApp.Telemetry",
      "SpanDurationMetricName": "diginsight.span_duration",
      "SpanDurationMetricDescription": "Duration of application spans in milliseconds"
    }
  }
}
```

---

### 2. Architectural Improvements

#### 2.1 Configuration Folder Reorganization

**What Changed:**
- Moved `Configuration/` folder ? `Configurations/` folder
- Moved `Utils/ActivityUtils.cs` ? root level `ActivityUtils.cs`
- Moved `MetricRecording/*` ? `Metrics/*` folder

**Files Affected:**

| Old Path | New Path |
|----------|----------|
| `Diginsight.Core/Configuration/` | `Diginsight.Core/Configurations/` |
| `Diginsight.Diagnostics/Configuration/` | `Diginsight.Diagnostics/Configurations/` |
| `Diginsight.Diagnostics/Utils/ActivityUtils.cs` | `Diginsight.Diagnostics/ActivityUtils.cs` |
| `Diginsight.Diagnostics/MetricRecording/` | `Diginsight.Diagnostics/Metrics/` |

**Why Changes Were Applied:**
- **Consistency**: Standardized folder naming (plural form)
- **Better organization**: Metric-related classes grouped together in `Metrics/` folder
- **Reduced nesting**: Moved utility classes to more logical locations

**Impact on Applications:**
- ? **No breaking changes**: These are internal organizational changes
- ? **Namespace changes handled automatically**: Public API namespaces remain unchanged

---

#### 2.2 Metric Recording Unification

**What Changed:**
- Deleted: `Diginsight.Diagnostics/MetricRecording/SpanDurationMetricRecorder.cs`
- Created: `Diginsight.Diagnostics/Metrics/SpanDurationMetricRecorder.cs` (completely rewritten)
- Added: `Metrics/MetricRecordingNameBasedFilterOptions.cs`
- Moved all metric recording classes to unified `Metrics/` folder

**New Architecture:**

```
Diginsight.Diagnostics/Metrics/
??? IMetricRecordingFilter.cs           (filter interface)
??? IMetricRecordingEnricher.cs         (enricher interface)
??? MetricRecordingNameBasedFilter.cs   (pattern-based filtering)
??? MetricRecordingNameBasedFilterOptions.cs (filter configuration)
??? MetricRecordingTagsEnricher.cs      (tag enrichment)
??? SpanDurationMetricRecorder.cs       (metric recording engine)
??? SpanDurationMetricRecorderRegistration.cs
??? CustomDurationMetricRecorder.cs
```

**Why Changes Were Applied:**
- **Unified architecture**: Filter and enricher pattern provides consistent extension points
- **Better separation of concerns**: Filtering, enriching, and recording are now distinct responsibilities
- **Improved testability**: Each component can be tested independently
- **Enhanced flexibility**: Users can implement custom filters and enrichers

**Impact on Applications:**
- ? **No API changes**: Existing registration methods still work
- ? **Enhanced configuration**: New filtering and enrichment options available

**New Configuration Options:**

```json
{
  "OptionsBasedMetricRecordingFilter": {
    "ActivityNames": {
      "MyApp.Orders.*": true,
      "MyApp.Payment.*": true,
      "MyApp.Internal.*": false
    }
  },
  "OptionsBasedMetricRecordingEnricher": {
    "MetricTags": [
      "customer_tier",
      "region"
    ]
  }
}
```

---

#### 2.3 Activity Filtering Mechanism Renamed

**What Changed:**

```csharp
// BEFORE - IActivityLoggingSampler
public interface IActivityLoggingSampler
{
    LogBehavior? GetLogBehavior(Activity activity);
}

// Implementation
public class NameBasedActivityLoggingSampler : IActivityLoggingSampler
{
    // ...
}

// Registration
services.TryAddSingleton<IActivityLoggingSampler, NameBasedActivityLoggingSampler>();
```

```csharp
// AFTER - IActivityLoggingFilter
public interface IActivityLoggingFilter
{
    LogBehavior? GetLogBehavior(Activity activity);
}

// Implementation
public class OptionsBasedActivityLoggingFilter : IActivityLoggingFilter
{
    private readonly IClassAwareOptions<DiginsightActivitiesOptions> activitiesOptions;
    
    public virtual LogBehavior? GetLogBehavior(Activity activity)
    {
        string activitySourceName = activity.Source.Name;
        string activityName = activity.OperationName;
        
        return ((IDiginsightActivitiesLogOptions)activitiesOptions
                .Get(activity.GetCallerType())
                .Freeze())
            .ActivityNames
            .Where(x => ActivityUtils.FullNameMatchesPattern(activitySourceName, activityName, x.Key))
            .Select(static x => (LogBehavior?)x.Value)
            .Max();
    }
}

// Registration
services.TryAddSingleton<IActivityLoggingFilter, OptionsBasedActivityLoggingFilter>();
```

**Why Changes Were Applied:**
- **Better naming**: "Filter" more accurately describes the component's purpose than "Sampler"
- **Improved implementation**: `OptionsBasedActivityLoggingFilter` uses class-aware options for more flexible configuration
- **Pattern matching**: Enhanced support for wildcard patterns in activity names
- **Consistency**: Aligns with other filter interfaces (`IMetricRecordingFilter`)

**Impact on Applications:**
- ?? **Breaking change**: Registration code must be updated
- Migration required:

```csharp
// OLD
services.TryAddSingleton<IActivityLoggingSampler, NameBasedActivityLoggingSampler>();

// NEW
services.TryAddSingleton<IActivityLoggingFilter, OptionsBasedActivityLoggingFilter>();
```

**Documentation Updates Required:**
- Update all code samples showing registration
- Update Getting Started guides
- Update advanced configuration articles

---

### 3. Feature Enhancements

#### 3.1 String-Based Configuration for LoggedActivityNames

**What Changed:**
Added support for space-separated string format in addition to dictionary format.

**Before (dictionary only):**
```json
{
  "Diginsight": {
    "Activities": {
      "LoggedActivityNames": {
        "MyApp.Orders.*": "Show",
        "MyApp.Payment.*": "Show",
        "MyApp.Internal.*": "Hide"
      }
    }
  }
}
```

**After (both formats supported):**
```json
{
  "Diginsight": {
    "Activities": {
      "LoggedActivityNames": "MyApp.Orders.* MyApp.Payment.*=Show MyApp.Internal.*=Hide"
    }
  }
}
```

**Why Changes Were Applied:**
- **Convenience**: Simpler syntax for straightforward configurations
- **Compactness**: Reduces verbosity in configuration files
- **Flexibility**: Both formats supported based on user preference

**Implementation:**
```csharp
// In DiginsightActivitiesOptions.Filler class
public string LoggedActivityNames
{
    get => string.Join(" ", filled.LoggedActivityNames);
    set
    {
        filled.LoggedActivityNames.Clear();
        filled.LoggedActivityNames.AddRange(
            value.Split(SpaceSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Select(
                    static x => x.Split(EqualsSeparator, 2) switch
                    {
                        [ var x0 ] => KeyValuePair.Create(x0, LogBehavior.Show),
                        [ var x0, var x1 ] when Enum.TryParse(x1, true, out LogBehavior b) 
                            => KeyValuePair.Create(x0, b),
                        _ => (KeyValuePair<string, LogBehavior>?)null,
                    }
                )
                .OfType<KeyValuePair<string, LogBehavior>>()
        );
    }
}
```

**Format Rules:**
- Space-separated patterns: `"Pattern1 Pattern2 Pattern3"`
- Optional behavior suffix: `"Pattern1=Show Pattern2=Hide"`
- Default behavior if not specified: `Show`
- Supported behaviors: `Show`, `Hide`, `Truncate`

**Impact on Applications:**
- ? **Fully backward compatible**: Existing dictionary configurations still work
- ? **Optional feature**: Use string format if preferred

---

#### 3.2 Immutable Configuration with Freeze()

**What Changed:**
Added `Freeze()` method to `DiginsightActivitiesOptions` for creating immutable instances.

```csharp
public sealed class DiginsightActivitiesOptions
{
    private readonly bool frozen;
    
    public DiginsightActivitiesOptions Freeze()
    {
        if (frozen)
            return this;
            
        return new DiginsightActivitiesOptions(
            true,
            ActivitySources.ToImmutableDictionary(),
            LoggedActivityNames.ToImmutableDictionary()
        )
        {
            logBehavior = logBehavior,
            activityLogLevel = activityLogLevel,
            writeActivityActionAsPrefix = writeActivityActionAsPrefix,
            disablePayloadRendering = disablePayloadRendering,
            recordSpanDuration = recordSpanDuration,
            spanDurationMeterName = spanDurationMeterName,
            spanDurationMetricName = spanDurationMetricName,
            spanDurationMetricDescription = spanDurationMetricDescription,
        };
    }
    
    // Setters throw InvalidOperationException when frozen
    public LogBehavior LogBehavior
    {
        get => logBehavior;
        set => logBehavior = frozen 
            ? throw new InvalidOperationException("Instance is frozen") 
            : value;
    }
}
```

**Why Changes Were Applied:**
- **Thread safety**: Immutable instances are safe for concurrent access
- **Configuration integrity**: Prevents accidental modifications after configuration is loaded
- **Performance**: Frozen instances use immutable collections for better performance

**Impact on Applications:**
- ? **No breaking changes**: Existing code continues to work
- ? **Optional feature**: Use `Freeze()` when immutability is desired

**Usage Example:**
```csharp
var options = new DiginsightActivitiesOptions
{
    LogBehavior = LogBehavior.Show,
    RecordSpanDuration = true
};

var frozenOptions = options.Freeze();

// This will throw InvalidOperationException
frozenOptions.LogBehavior = LogBehavior.Hide;
```

---

#### 3.3 Dynamic Configuration Support

**What Changed:**
`DiginsightActivitiesOptions` now implements `IDynamicallyConfigurable` and `IVolatilelyConfigurable`.

```csharp
public sealed class DiginsightActivitiesOptions
    : IDiginsightActivitiesOptions,
        IDiginsightActivitiesLogOptions,
        IMetricRecordingOptions,
        IDynamicallyConfigurable,  // ? NEW
        IVolatilelyConfigurable     // ? NEW
{
    object IDynamicallyConfigurable.MakeFiller() => new Filler(this);
    object IVolatilelyConfigurable.MakeFiller() => new Filler(this);
    
    // Filler class enables dynamic updates
    private class Filler
    {
        private readonly DiginsightActivitiesOptions filled;
        
        public LogBehavior LogBehavior
        {
            get => filled.LogBehavior;
            set => filled.LogBehavior = value;
        }
        // ... other properties
    }
}
```

**Why Changes Were Applied:**
- **Runtime reconfiguration**: Allows configuration changes without application restart
- **Dynamic logging**: Enable detailed logging for specific requests or scenarios
- **Troubleshooting**: Temporarily increase logging verbosity for diagnosis

**Impact on Applications:**
- ? **No breaking changes**: Existing configurations work unchanged
- ? **New capability**: Runtime configuration updates now possible

**Usage Example:**
```csharp
// Enable via HTTP header
services.AddSingleton<IActivityLoggingFilter, HttpHeadersActivityLoggingFilter>();

// Request with dynamic configuration
curl -H "Activity-Logging: Show" https://myapi.com/api/orders/123
```

---

### 4. Bug Fixes

#### 4.1 Activity and ActivitySource Filtering (Commit 987638c)

**What Was Fixed:**
- Corrected logic in `ActivityUtils.cs` for pattern matching
- Fixed issues where wildcard patterns weren't matching correctly
- Resolved edge cases in activity source name comparison

**Why Fix Was Needed:**
- Activities weren't being filtered as configured
- Wildcard patterns (`MyApp.*`) sometimes failed to match
- Source name filtering was inconsistent

**Impact:**
- ? **Reliability improvement**: Activity filtering now works as documented
- ? **Better performance**: Unnecessary activities correctly filtered out

---

#### 4.2 DiginsightTextWriter Fix (Commit dffd6d7)

**What Was Fixed:**
- Fixed edge cases in text rendering
- Resolved issues with special characters
- Improved handling of null/empty values

**Why Fix Was Needed:**
- Some log entries were malformed
- Special characters caused rendering issues
- Null reference exceptions in edge cases

**Impact:**
- ? **Stability improvement**: Fewer exceptions during logging
- ? **Better log quality**: Cleaner, more readable output

---

#### 4.3 DiginsightActivitiesOptions Fix (Commit e025853)

**What Was Fixed:**
- Resolved configuration binding issues
- Fixed default value handling
- Corrected property initialization order

**Why Fix Was Needed:**
- Some configuration properties weren't being loaded correctly
- Default values weren't applied in all scenarios
- Configuration validation was incomplete

**Impact:**
- ? **Configuration reliability**: Settings now load correctly in all scenarios
- ? **Better defaults**: Sensible defaults applied when config is missing

---

### 5. Development Updates

#### 5.1 Solution Format Migration (`.sln` ? `.slnx`)

**What Changed:**
- Migrated from traditional `.sln` to new `.slnx` format
- Updated solution file structure
- Maintained backward compatibility

**Why Change Was Made:**
- **Modern tooling**: `.slnx` is the new Visual Studio solution format
- **Better performance**: Faster solution loading
- **Enhanced features**: Better multi-targeting support

**Impact on Applications:**
- ? **No impact**: Both formats supported by Visual Studio 2022+
- ?? **Older IDEs**: Visual Studio 2019 users may need to upgrade

---

#### 5.2 C# 14 Preview Support (Commit 5fa5baa)

**What Changed:**
- Added C# 14 language features
- Used `field` keyword in auto-properties
- Leveraged new language improvements

**Example:**
```csharp
// C# 14 'field' keyword
public string? Pattern
{
    get;
    set => field = value.HardTrim();
}
```

**Why Change Was Made:**
- **Code modernization**: Leverage latest C# features
- **Better readability**: Cleaner property implementations
- **Future-proof**: Stay current with .NET evolution

**Impact on Applications:**
- ? **No impact**: Binary compatibility maintained
- ? **Compiled assemblies work** on older runtimes

---

#### 5.3 File Reorganization Summary

**Key Deletions:**
- `Diginsight.Core/Configuration/NamedOptionsMonitor.cs` (obsolete)
- `Diginsight.Diagnostics/Configuration/IDiginsightActivityNamesOptions.cs` (moved)
- `Diginsight.Diagnostics/ObservabilityRegistry.cs` (obsolete)
- `Diginsight.Diagnostics/MetricRecording/SpanDurationMetricRecorder.cs` (reimplemented)

**Key Additions:**
- `Diginsight.Core/LoggerFactoryStaticAccessor.cs`
- `Diginsight.Polyfills/System/Collections/Generic/KeyValuePair.cs`
- `Diginsight.Polyfills/System/Linq/Extensions.cs`
- `Diginsight.Stringify/IStringifyModifier.cs`
- `Diginsight.Stringify/StringifyModifier.cs`
- `Diginsight.Diagnostics/Metrics/MetricRecordingEnricherOptions.cs`

---

## Migration Guide

### For Most Users (Minimal Impact)

If you're using **JSON-based configuration only**, minimal changes needed:

```json
{
  "Diginsight": {
    "Activities": {
      "ActivitySources": {
        "MyApp.*": true
      },
      "LogBehavior": "Hide",
      "ActivityLogLevel": "Debug",  // ? Still works
      "RecordSpanDuration": false   // ?? Update from RecordSpanDurations
    }
  }
}
```

**Action Items:**
1. ? Change `RecordSpanDurations` ? `RecordSpanDuration` (singular)
2. ? Update registration: `IActivityLoggingSampler` ? `IActivityLoggingFilter`

---

### For Advanced Users (Code-Level Changes)

#### 1. Update Interface Implementations

```csharp
// OLD
public class MyOptions : IDiginsightActivitiesMetricOptions
{
    public bool RecordSpanDurations { get; set; }
    public string? MetricUnit { get; set; }  // Remove
}

// NEW
public class MyOptions : IMetricRecordingOptions
{
    public bool Record { get; set; }  // Renamed
    // MetricUnit removed
}
```

#### 2. Update Service Registration

```csharp
// OLD
services.TryAddSingleton<IActivityLoggingSampler, NameBasedActivityLoggingSampler>();

// NEW
services.TryAddSingleton<IActivityLoggingFilter, OptionsBasedActivityLoggingFilter>();
```

#### 3. Update Direct Property Access

```csharp
// OLD
var level = logOptions.ActivityLogLevel;

// NEW  
var level = logOptions.LogLevel;
```

---

## Testing Recommendations

After upgrading to 3.7, test the following:

### 1. Configuration Loading
```csharp
// Verify configuration loads correctly
var options = serviceProvider.GetRequiredService<IOptions<DiginsightActivitiesOptions>>().Value;
Assert.Equal(LogBehavior.Hide, options.LogBehavior);
Assert.True(options.RecordSpanDuration);
```

### 2. Activity Filtering
```csharp
// Test pattern matching works
var filter = serviceProvider.GetRequiredService<IActivityLoggingFilter>();
using var activity = activitySource.StartActivity("MyApp.Orders.ProcessOrder");
var behavior = filter.GetLogBehavior(activity);
Assert.Equal(LogBehavior.Show, behavior);
```

### 3. Metric Recording
```csharp
// Verify metrics are recorded correctly
using var activity = activitySource.StartActivity("CriticalOperation");
activity?.SetTag("customer_tier", "premium");
activity?.Stop();

// Check metric was recorded with correct tags
// (use test metric exporter)
```

---

## Breaking Changes Summary

| Change | Severity | Migration Required |
|--------|----------|-------------------|
| `IActivityLoggingSampler` ? `IActivityLoggingFilter` | ?? High | Yes - update registration |
| `RecordSpanDurations` ? `RecordSpanDuration` | ?? Medium | Yes - update config |
| `IDiginsightActivitiesMetricOptions` removed | ?? High | Yes - use `IMetricRecordingOptions` |
| `ActivityLogLevel` property rename | ?? Low | Only if accessing via interface |
| `MetricUnit` property removed | ?? Low | Remove if used |

---

## Deprecations

The following are now deprecated and will be removed in v4.0:

- `IActivityLoggingSampler` - use `IActivityLoggingFilter`
- `NameBasedActivityLoggingSampler` - use `OptionsBasedActivityLoggingFilter`
- `IDiginsightActivitiesMetricOptions` - use `IMetricRecordingOptions`

---

## Upgrade Checklist

- [ ] Update NuGet packages to 3.7.x
- [ ] Change `RecordSpanDurations` ? `RecordSpanDuration` in config
- [ ] Update service registration: `IActivityLoggingSampler` ? `IActivityLoggingFilter`
- [ ] Replace `IDiginsightActivitiesMetricOptions` with `IMetricRecordingOptions`
- [ ] Remove `MetricUnit` property if used
- [ ] Test activity filtering with your patterns
- [ ] Test metric recording with your configuration
- [ ] Update documentation referencing old interfaces
- [ ] Run full regression test suite

---

## Resources

- [Configuration Documentation](../01.%20Concepts/01.00%20-%20Configure%20diginsight%20telemetry%20to%20the%20local%20text%20based%20streams.md)
- [OpenTelemetry Integration](../01.%20Concepts/02.00%20-%20HowTo%20-%20configure%20diginsight%20telemetry%20to%20the%20remote%20tools.md)
- [GitHub Release](https://github.com/diginsight/telemetry/releases/tag/v3.7)
- [Migration Support](https://github.com/diginsight/telemetry/discussions)

---

## Acknowledgments

Special thanks to all contributors who made this release possible:
- Enhanced metric recording architecture
- Improved configuration flexibility
- Better code organization and maintainability
- Comprehensive bug fixes

For questions or issues, please visit our [GitHub repository](https://github.com/diginsight/telemetry) or [discussions forum](https://github.com/diginsight/telemetry/discussions).
