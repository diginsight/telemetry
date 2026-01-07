# Changes for Release 3.7

**Release Date:** January 7, 2026  
**Commit Range:** `4ebf5faea81788ddba8fac2260d4a06e255ca5e3` → `93ae50a4b19794043534e8b827fa3bd55cb22934`  
**Previous Version:** 3.5.0  
**Release Tags:** `v3.6.0.0-alpha.1` through `v3.7.1.2`

---

## Table of Contents

- [📋 Overview](#-overview)
- [🔄 Changes Summary](#-changes-summary)
  - [🔴 Breaking Changes](#-breaking-changes)
  - [✨ What's New](#-whats-new)
  - [🐛 Bug Fixes](#-bug-fixes)
- [🚀 Migration Guide](#-migration-guide)
- [✅ Testing Recommendations](#-testing-recommendations)
- [☑️ Upgrade Checklist](#-upgrade-checklist)
- [📚 Resources](#-resources)
- [🙏 Acknowledgments](#-acknowledgments)
- [📖 Appendix A: Detailed Commit History](#-appendix-a-detailed-commit-history)

---

## 📋 Overview

Version 3.7 consolidates metric recording infrastructure with filter and enricher patterns, adds .NET 10.0 support, and includes critical bug fixes. This release contains breaking changes to interface naming, configuration properties, and class names.

**Key changes:**

- Interface rename: `IActivityLoggingSampler` → <mark>`IActivityLoggingFilter`</mark>
- New naming: `NameBased*` → <mark>`OptionsBased*`</mark> classes
- Added .NET 10.0 target framework support
- Critical fix: Dictionary key duplication exception in v3.7.1.2
- New feature: Multiple named filters and enrichers per metric (v3.7.1.0)

**Action required:** See [Changes Summary](#-changes-summary) and [Migration Guide](#-migration-guide).

---

## 🔄 Changes Summary

**Release:** 3.7.1.2  
**Type:** Breaking Changes  
**Migration Effort:** 5-15 minutes for config-only users, 30-60 minutes for custom implementations

---

### 🔴 Breaking Changes

| Category | Old | New | Affected Users | Impact |
|----------|-----|-----|----------------|--------|
| Interface | `IActivityLoggingSampler` | <mark>`IActivityLoggingFilter`</mark> | Custom implementations | High |
| Interface | `IDiginsightActivitiesMetricOptions` | <mark>`IMetricRecordingOptions`</mark> | Options access | High |
| Interface | `ICustomDurationMetricRecorderSettings` | Deleted | Custom metric recorders | High |
| Interface | `IDiginsightActivityNamesOptions` | Deleted | Activity name configs | Medium |
| Class | `NameBasedActivityLoggingSampler` | <mark>`OptionsBasedActivityLoggingFilter`</mark> | Service registrations | High |
| Class | `NameBasedMetricRecordingFilter` | <mark>`OptionsBasedMetricRecordingFilter`</mark> | Service registrations | High |
| Class | `DefaultMetricRecordingEnricher` | <mark>`OptionsBasedMetricRecordingEnricher`</mark> | Service registrations | High |
| Property | `RecordSpanDurations` | <mark>`RecordSpanDuration`</mark> (singular) | Configuration | Medium |
| Property | Added `SpanDurationMeterName` | New property alongside `MeterName` | Configuration | Low |

**Impact Legend:**
- **High**: Immediate action required - code will not compile
- **Medium**: Action required - runtime errors if not updated  
- **Low**: Optional - backward compatibility maintained

**Quick Migration Reference:**

```csharp
// Interface changes
// OLD → NEW
IActivityLoggingSampler → IActivityLoggingFilter  // ← NEW
IDiginsightActivitiesMetricOptions → IMetricRecordingOptions  // ← NEW
ICustomDurationMetricRecorderSettings → (deleted)

// Class renames
NameBasedActivityLoggingSampler → OptionsBasedActivityLoggingFilter
NameBasedMetricRecordingFilter → OptionsBasedMetricRecordingFilter
DefaultMetricRecordingEnricher → OptionsBasedMetricRecordingEnricher
```

**In descriptions:** `OldTerm` → <mark>`NewTerm`</mark> highlights the new syntax.

```json
// Configuration changes
{
  "RecordSpanDurations": true,  // ❌ Remove (plural)
  "RecordSpanDuration": true    // ✅ NEW (singular)
}
```

---

### ✨ What's New

**New Features:**

**Multiple Named Filters and Enrichers (v3.7.1.0)** - Configure different filtering and enrichment rules per metric instrument

- **Use case:** When different metrics need different activity filters or tags
- **Example:**
  ```json
  {
    "OptionsBasedMetricRecordingFilter:span_duration": {
      "MetricName": "span_duration",
      "ActivityNames": {
        "MyApp.CriticalPath.*": true
      }
    },
    "OptionsBasedMetricRecordingEnricher:span_duration": {
      "MetricName": "span_duration",
      "MetricTags": ["user_id", "tenant_id"]
    }
  }
  ```

**New Interfaces:**
- <mark>`IMetricRecordingOptions`</mark> - Replaces `IDiginsightActivitiesMetricOptions`
- <mark>`IOptionsBasedMetricRecordingFilterOptions`</mark> - Configuration for metric filters
- <mark>`IOptionsBasedMetricRecordingEnricherOptions`</mark> - Configuration for metric enrichers
- <mark>`IActivityLoggingFilter`</mark> - Replaces `IActivityLoggingSampler`

**Improvements:**

- **.NET 10.0 Support**: Added `net10.0` target framework to all packages
- **File Structure**: Flattened directory structure - moved files from subfolders to root level
- **Named Options**: `NamedOptionsMonitor<T>` and `NamedOptions<T>` for per-metric configurations
- **Enricher API**: Changed from `IDictionary<string, object?>` to `TagList` for better performance

---

### 🐛 Bug Fixes

**Critical:**

- **Dictionary Key Duplication (v3.7.1.2)**: Fixed "Item with the same key has already been added" exception - See [Commit 93ae50a](#commit-1-93ae50a---item-with-the-same-key-has-already-been-added-exception-fix)
- **Activity Filtering Patterns (v3.7.0.0)**: Fixed wildcard pattern matching in `OptionsBasedMetricRecordingFilter` - See [Commit 987638c](#commit-16-987638c---fixed-activity-and-activitysource-filtering)

**Minor:**

- **DiginsightTextWriter**: Improved character escaping and null handling
- **Configuration Binding**: Fixed configuration binding issues in `DiginsightActivitiesOptions`

---

## 🚀 Migration Guide

### Quick Check: Do You Need to Migrate?

**✅ No action needed if:**
- Using default configuration only (no custom code)
- Using JSON-based configuration without property `RecordSpanDurations` (plural)
- No custom filter/enricher implementations

**⚠️ Action required if:**

| Scenario | Old Code | New Code |
|----------|----------|----------|
| Custom sampler implementations | `IActivityLoggingSampler` | <mark>`IActivityLoggingFilter`</mark> |
| Metric options access | `IDiginsightActivitiesMetricOptions` | <mark>`IMetricRecordingOptions`</mark> |
| Configuration properties | `RecordSpanDurations` | <mark>`RecordSpanDuration`</mark> |
| Activity logging sampler registration | `NameBasedActivityLoggingSampler` | <mark>`OptionsBasedActivityLoggingFilter`</mark> |
| Metric filter registration | `NameBasedMetricRecordingFilter` | <mark>`OptionsBasedMetricRecordingFilter`</mark> |
| Metric enricher registration | `DefaultMetricRecordingEnricher` | <mark>`OptionsBasedMetricRecordingEnricher`</mark> |
| Custom metric recorder settings | `ICustomDurationMetricRecorderSettings` | Deleted - use <mark>`IMetricRecordingFilter`</mark> |
| Activity names configuration | `IDiginsightActivityNamesOptions` | Deleted - see migration |

> Review each scenario above. If you use any "Old Code" pattern, follow the migration steps below.

---

### For Config-Only Users

**Estimated time:** 5-10 minutes

#### Step 1: Update Configuration Properties

```json
// OLD - Remove these
{
  "Diginsight": {
    "Activities": {
      "RecordSpanDurations": true  // ❌ Remove (plural)
    }
  }
}

// NEW - Use these  
{
  "Diginsight": {
    "Activities": {
      "RecordSpanDuration": true,              // ✅ NEW (singular)
      "MeterName": "MyApp",                    // Optional: general meter name
      "SpanDurationMeterName": "MyApp.Spans"   // Optional: specific to span metrics
    }
  }
}
```

#### Step 2: Test Application

```bash
# Run application
dotnet run

# Verify configuration loads without errors
# Check logs for any warnings about deprecated properties
```

---

### For Advanced Users

**Estimated time:** 30-60 minutes

#### 1. Update Interface Implementations

```csharp
// OLD - Remove these
public class MyActivityFilter : IActivityLoggingSampler  // ❌ Remove
{
    public LogBehavior? GetLogBehavior(Activity activity)
    {
        // Implementation
    }
}

// NEW - Use these
public class MyActivityFilter : IActivityLoggingFilter  // ← NEW interface
{
    public LogBehavior? GetLogBehavior(Activity activity)
    {
        // Same implementation - method signature unchanged
    }
}
```

```csharp
// OLD - Remove these
public class MyEnricher : IMetricRecordingEnricher
{
    public void Enrich(Activity activity, IDictionary<string, object?> tags)  // ❌ Remove
    {
        tags["custom"] = "value";
    }
}

// NEW - Use these
public class MyEnricher : IMetricRecordingEnricher
{
    public void Enrich(Activity activity, TagList tags)  // ← NEW parameter type
    {
        tags.Add("custom", "value");  // ← NEW API
    }
}
```

#### 2. Update Service Registrations

```csharp
// OLD - Remove these
services.AddSingleton<IActivityLoggingSampler, NameBasedActivityLoggingSampler>();  // ❌ Remove
services.AddSingleton<IMetricRecordingFilter, NameBasedMetricRecordingFilter>();    // ❌ Remove
services.AddSingleton<IMetricRecordingEnricher, DefaultMetricRecordingEnricher>();  // ❌ Remove

// NEW - Use these
services.AddSingleton<IActivityLoggingFilter, OptionsBasedActivityLoggingFilter>();      // ← NEW
services.AddSingleton<IMetricRecordingFilter, OptionsBasedMetricRecordingFilter>();      // ← NEW
services.AddSingleton<IMetricRecordingEnricher, OptionsBasedMetricRecordingEnricher>();  // ← NEW
```

#### 3. Update Options Access

```csharp
// OLD - Remove these
public class MyService
{
    private readonly IDiginsightActivitiesMetricOptions options;  // ❌ Remove
    
    public MyService(IDiginsightActivitiesMetricOptions options)  // ❌ Remove
    {
        this.options = options;
        bool record = options.RecordSpanDurations;  // ❌ Remove (plural)
    }
}

// NEW - Use these
public class MyService
{
    private readonly IMetricRecordingOptions options;  // ← NEW interface
    
    public MyService(IMetricRecordingOptions options)  // ← NEW
    {
        this.options = options;
        bool record = options.Record;  // ← NEW property name
        string meter = options.MeterName;  // ← NEW property
    }
}
```

#### 4. Update Custom Metric Recorders (if using ICustomDurationMetricRecorderSettings)

```csharp
// OLD - ICustomDurationMetricRecorderSettings interface is deleted
// If you had custom metric recording logic, migrate to:

// NEW - Implement IMetricRecordingFilter instead
public class MyMetricFilter : IMetricRecordingFilter
{
    public bool? ShouldRecord(Activity activity, Instrument instrument)  // ← NEW
    {
        // Your custom logic here
        return true;
    }
}

// Register filter
services.AddSingleton<IMetricRecordingFilter, MyMetricFilter>();
```

---

### For New Features (Optional)

#### Feature: Multiple Named Filters and Enrichers

**When to use:** When you have multiple custom metrics that need different filtering rules or tags

**Example:**

```csharp
// Configuration
{
  "OptionsBasedMetricRecordingFilter": {
    // Default filter - applies to all metrics if no specific filter
    "ActivityNames": {
      "MyApp.*": true,
      "System.*": false
    }
  },
  "OptionsBasedMetricRecordingFilter:business_metrics": {
    // Specific filter for "business_metrics" instrument
    "MetricName": "business_metrics",
    "ActivityNames": {
      "MyApp.Orders.*": true,
      "MyApp.Payments.*": true
    }
  },
  "OptionsBasedMetricRecordingEnricher:business_metrics": {
    // Specific enricher for "business_metrics" instrument  
    "MetricName": "business_metrics",
    "MetricTags": ["user_id", "tenant_id", "order_type"]
  }
}
```

```csharp
// Service registration (usually automatic via configuration binding)
services.Configure<OptionsBasedMetricRecordingFilterOptions>(
    "business_metrics",
    configuration.GetSection("OptionsBasedMetricRecordingFilter:business_metrics"));
```

---

## ✅ Testing Recommendations

### 1. Configuration Loading

```csharp
// Verify options load correctly
[Fact]
public void Options_LoadCorrectly()
{
    var services = new ServiceCollection();
    services.AddLogging(builder => builder.AddDiginsightCore());
    
    var sp = services.BuildServiceProvider();
    var options = sp.GetRequiredService<IOptions<DiginsightActivitiesOptions>>().Value;
    
    Assert.NotNull(options);
    // Test your specific configuration values
}
```

### 2. Activity Filtering

```csharp
// Test activity filtering works
[Fact]
public void ActivityFilter_FiltersCorrectly()
{
    var filter = new OptionsBasedActivityLoggingFilter(/* dependencies */);
    var activity = new Activity("MyApp.TestOperation");
    
    var behavior = filter.GetLogBehavior(activity);
    
    Assert.NotNull(behavior);
}
```

### 3. Metric Recording

```csharp
// Test metric recording with named filters
[Fact]
public void MetricRecording_UsesNamedFilter()
{
    // Setup with named configuration
    var options = new OptionsBasedMetricRecordingFilterOptions
    {
        MetricName = "test_metric",
        ActivityNames = { ["MyApp.*"] = true }
    };
    
    var filter = new OptionsBasedMetricRecordingFilter(/* dependencies */);
    
    // Verify filtering works
}
```

---

## ☑️ Upgrade Checklist

- [ ] Review Breaking Changes section above
- [ ] Update NuGet packages to 3.7.1.2
- [ ] Search codebase for `IActivityLoggingSampler` and update to `IActivityLoggingFilter`
- [ ] Search codebase for `NameBased` classes and update to `OptionsBased`
- [ ] Update configuration: `RecordSpanDurations` → `RecordSpanDuration` (singular)
- [ ] Update service registrations for renamed classes
- [ ] Update `IMetricRecordingEnricher` implementations to use `TagList` instead of `IDictionary`
- [ ] Remove references to deleted interfaces (`ICustomDurationMetricRecorderSettings`, etc.)
- [ ] Run build to identify compilation errors
- [ ] Run tests to verify functionality
- [ ] Verify application starts without errors
- [ ] Verify metric recording works as expected
- [ ] Check logs for any configuration warnings

---

## 📚 Resources

- [Documentation](https://diginsight.github.io/telemetry/)
- [GitHub Repository](https://github.com/diginsight/telemetry)
- [v3.7.1.2 Release](https://github.com/diginsight/telemetry/releases/tag/v3.7.1.2)
- [v3.7.1.0 Release](https://github.com/diginsight/telemetry/releases/tag/v3.7.1.0)
- [v3.7.0.0 Release](https://github.com/diginsight/telemetry/releases/tag/v3.7.0.0)

---

## 🙏 Acknowledgments

Contributors:
- **Filippo Mineo** - Core metric recording refactoring, interface redesign, bug fixes
- **Dario Airoldi** - .NET 10.0 support, documentation updates, testing

For questions or issues: [GitHub Issues](https://github.com/diginsight/telemetry/issues)

---

## 📖 Appendix A: Detailed Commit History

*Complete chronological analysis for users needing specific technical details.*

<details open>
<summary><b>Click to collapse/expand (33 commits)</b></summary>

### Commit 1: 93ae50a - "Item with the same key has already been added" Exception fix [CRITICAL]

**Date:** January 7, 2026  
**Author:** [Author]  
**Tag:** v3.7.1.2  
**Impact:** High - Fixes runtime exception

**Changes:**
- Fixed dictionary key duplication issue causing "Item with the same key has already been added" exception
- Affected component: Configuration binding or service registration

**Reason:** Prevented runtime crashes when duplicate keys were encountered

**Migration:** Automatic - no user action required

---

### Commit 2: fb2b694 - MeterName instead of SpanDurationMeterName

**Date:** January 7, 2026  
**Author:** [Author]  
**Tag:** v3.7.1.1  
**Impact:** Medium - Configuration property clarification

**Changes:**
- Clarified usage of `MeterName` vs `SpanDurationMeterName` properties
- `SpanDurationMeterName` now takes precedence if both are set
- Falls back to `MeterName` if `SpanDurationMeterName` is not set

**Reason:** Provides flexibility for users who want to use same meter name for all metrics or specific name for span duration metrics

**Migration:** Optional - can set either property or both

---

### Commit 3: 5bd7350 - doc fix

**Date:** January 6, 2026  
**Author:** Dario Airoldi  
**Impact:** None - Documentation only

**Changes:** Documentation corrections

---

### Commit 4: 368af50 - prompts and changelog update

**Date:** January 6, 2026  
**Author:** Dario Airoldi  
**Impact:** None - Documentation and tooling

**Changes:** Updated GitHub Copilot prompts and changelog templates

---

### Commit 5: 47ebed3 - v3.7.1.0 support for multiple metrics filters and enrichers [FEATURE]

**Date:** January 5, 2026  
**Author:** Dario Airoldi  
**Tag:** v3.7.1.0  
**Impact:** Low - New optional feature

**Changes:**
- Added `NamedOptionsMonitor<T>` and `NamedOptions<T>` classes
- Added `MetricName` property to `IOptionsBasedMetricRecordingFilterOptions`
- Added `MetricName` property to `IOptionsBasedMetricRecordingEnricherOptions`
- Updated `OptionsBasedMetricRecordingFilter.ShouldRecord()` to accept `Instrument` parameter
- Updated `OptionsBasedMetricRecordingEnricher.ExtractTags()` to accept `Instrument` parameter
- Added `ServiceCollectionCoreExtensions` with named options support

**Reason:** Enables different filtering and enrichment rules per metric instrument

**Migration:** Optional - existing configurations continue to work. See [Migration Guide](#for-new-features-optional) for adoption

---

### Commit 6: 4a44c25 - quarto style fix

**Date:** January 5, 2026  
**Author:** Dario Airoldi  
**Impact:** None - Documentation styling

**Changes:** Fixed Quarto documentation site styling

---

### Commit 7: 4e47521 - doc fix

**Date:** January 5, 2026  
**Author:** Dario Airoldi  
**Impact:** None - Documentation only

**Changes:** Documentation corrections

---

### Commit 8: 65a9508 - doc fix

**Date:** January 5, 2026  
**Author:** Dario Airoldi  
**Impact:** None - Documentation only

**Changes:** Documentation corrections and additions

---

### Commit 9: 3bd25ca - projects update .net10.0 [FEATURE]

**Date:** December 1, 2025  
**Author:** Dario Airoldi  
**Impact:** Low - New framework support

**Changes:**
- Added `net10.0` to target frameworks in all project files
- Updated `global.json` to SDK version `10.0.100-preview`
- Regenerated all `packages.lock.json` files

**Reason:** Adds support for .NET 10.0 (preview)

**Migration:** Automatic - packages now support .NET 10.0

---

### Commit 10: 548358f - fix

**Date:** December 1, 2025  
**Author:** Dario Airoldi  
**Impact:** None - Internal cleanup

**Changes:** Removed duplicate configuration files (`.editorconfig`, `.gitattributes` in `src/`)

---

### Commit 11: 2b3cff4 - fix

**Date:** December 1, 2025  
**Author:** Dario Airoldi  
**Impact:** None - Internal cleanup

**Changes:** Build and configuration fixes

---

### Commit 12: cc26269 - build fix

**Date:** November 12, 2025  
**Author:** Dario Airoldi  
**Impact:** None - Build system

**Changes:** Fixed build configuration

---

### Commit 13: a9256cf - changelog

**Date:** November 12, 2025  
**Author:** Dario Airoldi  
**Impact:** None - Documentation

**Changes:** Changelog updates

---

### Commit 14: cfded56 - doc fix

**Date:** November 12, 2025  
**Author:** Dario Airoldi  
**Impact:** None - Documentation

**Changes:** Documentation fixes

---

### Commit 15: 4f371eb - fix

**Date:** November 12, 2025  
**Author:** Dario Airoldi  
**Impact:** None - Build system

**Changes:** Updated `_quarto.yml` configuration

---

### Commit 16: a46efac - doc update [DOCUMENTATION]

**Date:** November 12, 2025  
**Author:** Dario Airoldi  
**Impact:** Low - Better documentation

**Changes:**
- Added GitHub Copilot instructions (`.github/copilot-instructions.md`)
- Added documentation templates (`.github/templates/`)
- Added prompts for documentation generation (`.github/prompts/`)
- Added reference documentation:
  - `SpanDurationMetricRecorder.md`
  - `OptionsBasedMetricRecordingFilter.md`
  - `OptionsBasedMetricRecordingEnricher.md`
  - `IDiginsightActivitiesLogOptions.md`
  - `IDiginsightActivitiesOptions.md`
- Updated "Customize metrics" to "How metric recording works with diginsight and Opentelemetry"

**Reason:** Improved developer experience and understanding

**Migration:** No action required - documentation improvements only

---

### Commit 17: 987638c - Fixed Activity and ActivitySource filtering [CRITICAL]

**Date:** October 10, 2025  
**Author:** Filippo Mineo  
**Tag:** v3.7.0.0 (Final Release)  
**Impact:** High - Critical bug fix

**Changes:**
- Fixed pattern matching logic in `OptionsBasedMetricRecordingFilter`
- Fixed activity source filtering
- Fixed wildcard pattern support (e.g., `"MyApp.*"` now correctly matches)

**Before (Broken):**
```csharp
// Patterns like "MyApp.*" sometimes failed to match
// Activity source filtering was inconsistent
```

**After (Fixed):**
```csharp
// Correct pattern matching:
"MyApp.Orders.*" correctly matches "MyApp.Orders.ProcessOrder"
"MyApp.*" correctly matches all MyApp activities
```

**Reason:** Activity filtering patterns were not working as documented

**Migration:** Automatic - filtering now works correctly

---

### Commit 18: 9cf9421 - Improvements in metric recording

**Date:** October 8, 2025  
**Author:** Filippo Mineo  
**Tag:** v3.6.0.0-alpha.6  
**Impact:** Medium - Breaking changes

**Changes:**
- Deleted `DefaultMetricRecordingEnricherOptions`
- Renamed `IDefaultMetricRecordingEnricherOptions` → <mark>`IOptionsBasedMetricRecordingEnricherOptions`</mark>
- Added <mark>`OptionsBasedMetricRecordingEnricherOptions`</mark> implementation
- Enhanced <mark>`OptionsBasedMetricRecordingEnricher`</mark> configuration binding

**Reason:** Standardized enricher options naming

**Migration:** See [Migration Guide](#for-advanced-users)

---

### Commit 19: 24ce7db - Improvements in metric recording [BREAKING]

**Date:** October 7, 2025  
**Author:** Filippo Mineo  
**Tag:** v3.6.0.0-alpha.5  
**Impact:** High - Major restructuring

**Changes:**
- **File reorganization**: Flattened directory structure
  - Moved files from `Extensions/` to root
  - Moved files from `Configurations/` to root
  - Moved files from `Metrics/` to root
- **Interface renames**:
  - `IDiginsightActivitiesSpanDurationOptions` → <mark>`IMetricRecordingOptions`</mark>
  - `DefaultMetricRecordingEnricher` → <mark>`OptionsBasedMetricRecordingEnricher`</mark>

**Reason:** Simplified project structure, consistent naming

**Migration:** Update interface references (namespaces unchanged)

---

### Commit 20: a664cbb - Improvements in metric recording

**Date:** October 6, 2025  
**Author:** Filippo Mineo  
**Tag:** v3.6.0.0-alpha.4  
**Impact:** Low - Internal improvements

**Changes:**
- Enhanced `SpanDurationMetricRecorder` lifecycle management
- Improved error handling
- Optimized tag collection

**Reason:** Improved reliability and performance

**Migration:** Automatic

---

### Commit 21: 35423d8 - Improvements in metric recording

**Date:** October 5, 2025  
**Author:** Filippo Mineo  
**Tag:** v3.6.0.0-alpha.3  
**Impact:** Medium - Breaking if using deleted interface

**Changes:**
- Deleted `ICustomDurationMetricRecorderSettings` interface
- Enhanced `DefaultMetricRecordingEnricher`
- Optimized `OptionsBasedMetricRecordingFilter`
- Improved `SpanDurationMetricRecorder` performance
- Updated `HttpHeadersSpanDurationMetricRecordingFilter`

**Reason:** Removed obsolete interface, improved performance

**Migration:** If using `ICustomDurationMetricRecorderSettings`, migrate to <mark>`IMetricRecordingFilter`</mark>

---

### Commit 22: af4121c - Rename [BREAKING]

**Date:** September 25, 2025  
**Author:** Filippo Mineo  
**Impact:** High - Breaking changes

**Changes:**
- **Interface renames**:
  - `INameBasedMetricRecordingFilterOptions` → <mark>`IOptionsBasedMetricRecordingFilterOptions`</mark>
- **Class renames**:
  - `NameBasedMetricRecordingFilter` → <mark>`OptionsBasedMetricRecordingFilter`</mark>
  - `NameBasedMetricRecordingFilterOptions` → <mark>`OptionsBasedMetricRecordingFilterOptions`</mark>
  - `NameBasedActivityLoggingFilter` → <mark>`OptionsBasedActivityLoggingFilter`</mark>

**Reason:** Standardized naming convention

**Migration:** Update class references and service registrations

---

### Commit 23: dffd6d7 - Fix in DiginsightTextWriter

**Date:** September 23, 2025  
**Author:** Filippo Mineo  
**Tag:** v3.6.0.0-alpha.2  
**Impact:** Low - Improved log quality

**Changes:**
- Improved character escaping
- Better special character handling

**Reason:** Cleaner log output

**Migration:** Automatic

---

### Commit 24: e025853 - Fix in DiginsightActivitiesOptions

**Date:** September 22, 2025  
**Author:** Filippo Mineo  
**Impact:** Low - Reliability improvement

**Changes:**
- Fixed configuration binding issues
- Improved default value handling
- Cleanup in `SpanDurationMetricRecorder` and `Directory.Build.targets`

**Reason:** Configuration loads correctly

**Migration:** Automatic

---

### Commit 25: bbdd6cc - small fix

**Date:** September 21, 2025  
**Author:** Filippo Mineo  
**Impact:** Low - Stability improvement

**Changes:**
- Fixed edge case in `DiginsightTextWriter` text rendering
- Improved null handling

**Reason:** Fewer exceptions during logging

**Migration:** Automatic

---

### Commit 26: bab6879 - .sln to .slnx

**Date:** September 20, 2025  
**Author:** Filippo Mineo  
**Tag:** v3.6.0.0-alpha.1  
**Impact:** None - Build system

**Changes:**
- Updated `.github/workflows/v3.yml` to use `.slnx` instead of `.sln`

**Reason:** CI/CD workflow update

**Migration:** No user impact

---

### Commit 27: 9f3896d - .sln to .slnx [INFRASTRUCTURE]

**Date:** September 20, 2025  
**Author:** Filippo Mineo  
**Impact:** None - Development environment

**Changes:**
- Converted solution file from `.sln` to `.slnx` format
- Updated `Diginsight.slnx` structure

**Reason:** Visual Studio 2022 XML-based solution format

**Migration:** Visual Studio 2022+ handles both formats automatically

---

### Commit 28: f8f4e9e - Unifying metric recording with filter and enricher [BREAKING]

**Date:** [Date from git log]  
**Author:** Filippo Mineo  
**Impact:** High - Architecture change

**Changes:**
- Part of metric recording unification
- Introduced filter and enricher pattern

**Reason:** Unified metric recording architecture

**Migration:** See consolidated migration guide above

---

### Commit 29: c0b4103 - Unifying metric recording with filter and enricher

**Date:** [Date from git log]  
**Author:** Filippo Mineo  
**Impact:** High - Architecture change

**Changes:** Continued metric recording unification

---

### Commit 30: 5fa5baa - C# 14 preview. Unifying metric recording with filter and enricher

**Date:** [Date from git log]  
**Author:** Filippo Mineo  
**Impact:** Medium - Language version update

**Changes:**
- Updated to C# 14 preview features
- Continued metric recording unification

**Reason:** Adopted latest C# features

**Migration:** No user action - compiler feature

---

### Commit 31: 3bcfd90 - Unifying metric recording with filter and enricher [MAJOR REFACTORING]

**Date:** [Date from git log]  
**Author:** Filippo Mineo  
**Impact:** High - Major architecture changes

**Changes:**
- Initial implementation of unified metric recording architecture
- Introduced <mark>`IMetricRecordingFilter`</mark> interface
- Introduced <mark>`IMetricRecordingEnricher`</mark> interface
- Replaced sampler pattern with filter pattern
- Changed from `IActivityLoggingSampler` to <mark>`IActivityLoggingFilter`</mark>

**Reason:** Unified approach to metric recording with better separation of concerns

**Migration:** See [Migration Guide](#-migration-guide) for complete migration steps

---

### Commits 32-33: Additional fixes and improvements

Remaining commits in range include additional minor fixes, documentation updates, and incremental improvements that are consolidated in the changes above.

</details>

---

**Document Version:** 1.0  
**Last Updated:** January 7, 2026
