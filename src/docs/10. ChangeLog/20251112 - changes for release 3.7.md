# Changes for Release 3.7

**Release Date:** January 5, 2026  
**Commit Range:** `4ebf5faea81788ddba8fac2260d4a06e255ca5e3` → `47ebed3977a26e882fc232454f4c307b0a24dc51`  
**Release Tags:** `v3.7.0.0` (November 12, 2025), `v3.7.1.0` (January 5, 2026)

---

## Table of Contents

- [📋 Changes Overview](#changes-overview)
- [🔍 Changes Analysis](#changes-analysis)
  - [⚙️ Configuration Schema Updates](#1-configuration-schema-updates)
  - [🏗️ Architectural Improvements](#2-architectural-improvements)
  - [✨ Feature Enhancements](#3-feature-enhancements)
  - [🐛 Bug Fixes](#4-bug-fixes)
  - [🛠️ Development Updates](#5-development-updates)
- [🔄 Migration Guide](#migration-guide)
- [✅ Testing Recommendations](#testing-recommendations)
- [⚠️ Breaking Changes Summary](#breaking-changes-summary)
- [🚨 Deprecations](#deprecations)
- [☑️ Upgrade Checklist](#upgrade-checklist)
- [📚 Resources](#resources)
- [🙏 Acknowledgments](#acknowledgments)

---

## 📋 Changes Overview

This release includes **significant architectural improvements** focused on unifying and simplifying metric recording infrastructure. 
Release 3.7 introduces:
- new **filter** and **enricher**-based architecture for **metric recording**
- reorganizes project files for better maintainability
- fixes critical issues in **Activity and ActivitySource filtering**.

Release 3.7.0.0 (November 12, 2025) represents the culmination of work through alpha versions 3.6.0.0-alpha.1 through alpha.6, delivering a more consistent, flexible, and maintainable telemetry framework. Release 3.7.1.0 (January 5, 2026) adds support for multiple named metric filters and enrichers, enabling more granular control over metric recording behavior.

### Key Changes Summary

1. **Configuration Schema Updates** ⚠️
   - **Breaking**: Removed metric-related properties from `DiginsightActivitiesOptions`
   - **Breaking**: Removed `IDiginsightActivitiesMetricOptions` interface
   - **Breaking**: Property rename: `ActivityLogLevel` → <mark>`LogLevel`</mark> in <mark>`IDiginsightActivitiesLogOptions`</mark>
   - **Breaking**: Interface rename: `IActivityLoggingSampler` → <mark>`IActivityLoggingFilter`</mark>
   - **Breaking**: Class rename: `NameBasedActivityLoggingSampler` → <mark>`OptionsBasedActivityLoggingFilter`</mark>
   - **New**: Introduced <mark>`IDiginsightActivitiesSpanDurationOptions`</mark> for span duration metrics
   - **New**: Added <mark>`ActivityNames`</mark> dictionary to <mark>`IDiginsightActivitiesLogOptions`</mark>
   - Standardized class naming: `NameBased*` → <mark>`OptionsBased*`</mark> for <mark>filters and enrichers</mark>

2. **Architectural Improvements** ✅
   - Unified metric recording with <mark>filter</mark> and <mark>enricher</mark> pattern
   - Major folder reorganization: flattened structure by moving files from subfolders to root
   - Introduced <mark>`DefaultMetricRecordingEnricher`</mark> and <mark>`OptionsBasedMetricRecordingEnricher`</mark>
   - Renamed: <mark>`MetricRecordingNameBasedFilter`</mark> → `OptionsBasedMetricRecordingFilter`
   - Deleted obsolete `ObservabilityRegistry` class
   - Removed `ServiceCollectionExtensions` from Diginsight.Core
   - Consolidated metric recording classes into coherent structure

3. **Feature Enhancements** ✅
   - **NEW v3.7.1.0**: <mark>Support for multiple named metric filters and enrichers</mark> - different filtering and enrichment rules per metric
   - **NEW v3.7.1.0**: <mark>`NamedOptionsMonitor<T>` and `NamedOptions<T>`</mark> classes for named options support
   - **NEW v3.7.1.0**: <mark>`MetricName` property</mark> added to filter and enricher options interfaces
   - **NEW v3.7.1.0**: <mark>Updated filter and enricher signatures</mark> to accept `Instrument` parameter
   - **NEW v3.7.1.0**: <mark>ServiceCollectionCoreExtensions</mark> for easier named options registration
   - New <mark>`HttpHeadersSpanDurationMetricRecordingFilter`</mark> for HTTP-based metric filtering
   - Enhanced <mark>`SpanDurationMetricRecorder`</mark> with improved options handling and better lifecycle management
   - Added <mark>`IStringifyModifier`</mark> interface and `StringifyModifier` class for extensible stringify operations
   - New <mark>`LoggerFactoryStaticAccessor`</mark> for static logger factory access
   - Improved <mark>`DiginsightActivitiesOptions.Freeze()`</mark> with optimization to avoid redundant freezing
   - Enhanced <mark>`ActivityNames` property</mark> support in logging options

4. **Bug Fixes** ✅
   - **Critical**: <mark>Fixed Activity and ActivitySource filtering</mark> in <mark>`OptionsBasedMetricRecordingFilter`</mark>
   - Fixed <mark>`DiginsightTextWriter`</mark> null reference and rendering issues
   - Corrected `DiginsightActivitiesOptions.Freeze()` to avoid redundant operations
   - Fixed separator character in options parsing (`EqualsSeparator` changed from space to '=')
   - Resolved configuration edge cases in `DiginsightActivitiesOptions`

5. **Development Updates** ✅
   - **NEW v3.7.1.0**: Added .NET 10.0 support to all projects
   - **NEW v3.7.1.0**: Updated `global.json` to SDK 10.0.100-preview
   - **NEW v3.7.1.0**: Added comprehensive reference documentation for metric recording classes
   - **NEW v3.7.1.0**: Reorganized GitHub templates and prompts
   - Migrated solution format: `.sln` → `.slnx` (XML-based solution file)
   - Updated to **C# 14 preview** features  
   - Added `.csproj.DotSettings` files for ReSharper configuration
   - Updated CI/CD workflow (`.github/workflows/v3.yml`) to use `.slnx` format
   - Modernized polyfills: new `KeyValuePair` extensions, LINQ extensions
   - Updated `Directory.Build.props` and `Directory.Build.targets`
   - Removed duplicate `.editorconfig` and `.gitattributes` files

6. **Dependency Updates** ✅
   - **NEW v3.7.1.0**: All projects now target .NET 10.0 in addition to existing frameworks (10.0, 9.0, 8.0, 7.0, 6.0)
   - Updated `packages.lock.json` across all projects (major cleanup)
   - Updated Polyfills project configuration
   - Cleaned up project references and removed obsolete dependencies

---

## Detailed Changes Analysis (Chronological)

### Commit 1: 3bcfd90 - Unifying metric recording with filter and enricher

**Date:** September 17, 2025  
**Author:** Filippo Mineo  
**Files Changed:** 38 files, 357 insertions, 396 deletions

#### What Changed for Library Users:

**1. Folder Reorganization**
- **Configuration/` → `Configurations/`**: All configuration classes moved to plural folder name
  - Affects: `Diginsight.Core`, `Diginsight.Diagnostics`
  - User Impact: ✅ **No breaking changes** - namespaces remain the same
  
**2. File Movements**
- `Utils/ActivityUtils.cs` → <mark>`ActivityUtils.cs`</mark> (moved to root)
- `MetricRecording/*` → <mark>`Metrics/*`</mark> (folder renamed)
  - User Impact: ✅ **No breaking changes** - public API unchanged

**3. New Metric Recording Structure**
- Moved metric recording classes into `Metrics/` folder
- Introduced <mark>`MetricRecordingNameBasedFilterOptions.cs`</mark>
- User Impact: ✅ **Improved organization** - easier to find metric-related classes

**4. Configuration Property Updates**
```csharp
// BEFORE
public interface IDiginsightActivitiesOptions
{
    IReadOnlyDictionary<string, bool> ActivitySources { get; }
}

// AFTER  
public interface IDiginsightActivitiesOptions
{
    IReadOnlyDictionary<string, bool> ActivitySources { get; }
    // Additional configuration support added
}
```

**What Users Need to Do:**
- ✅ **Nothing** - changes are internal restructuring
- Configuration files remain unchanged

---

### Commit 2: 5fa5baa - C# 14 preview. Unifying metric recording with filter and enricher

**Date:** September 17, 2025 (after 3bcfd90)  
**Author:** Filippo Mineo  
**Files Changed:** 27 files, 140 insertions, 115 deletions

#### What Changed for Library Users:

**1. C# 14 Language Features**
- Upgraded project to use C# 14 preview
- Updated `Directory.Build.props` to set language version
- User Impact: ✅ **No runtime breaking changes** - compiled assemblies work on older runtimes

**2. New Stringify Infrastructure**
```csharp
// NEW: IStringifyModifier interface
public interface IStringifyModifier
{
    // Allows custom modification of stringify behavior
    object? Modify(object? value, StringifyContext context);
}

// NEW: StringifyModifier class
public sealed class StringifyModifier : IStringifyModifier
{
    // Default implementation
}
```
- User Impact: ✅ **New extensibility point** - users can implement custom stringify modifiers

**3. Added LoggerFactoryStaticAccessor**
```csharp
// NEW: Static accessor for ILoggerFactory
public static class LoggerFactoryStaticAccessor
{
    public static ILoggerFactory? LoggerFactory { get; set; }
}
```
- User Impact: ✅ **Convenience feature** - easier access to logger factory in static contexts

**4. Deleted ObservabilityRegistry**
- Removed obsolete `ObservabilityRegistry.cs`
- User Impact: ⚠️ **Breaking if used** - <mark>migrate to `LoggerFactoryStaticAccessor`</mark>

**5. New Polyfill Extensions**
```csharp
// NEW: KeyValuePair.cs polyfill
namespace System.Collections.Generic
{
    public static class KeyValuePair
    {
        public static KeyValuePair<TKey, TValue> Create<TKey, TValue>(TKey key, TValue value)
            => new KeyValuePair<TKey, TValue>(key, value);
    }
}
```
- User Impact: ✅ **Better compatibility** - modern syntax on older frameworks

**What Users Need to Do:**
- ⚠️ If using `ObservabilityRegistry`: Replace with `LoggerFactoryStaticAccessor`
- ✅ Otherwise: No changes needed

---

### Commit 3: c0b4103 - Unifying metric recording with filter and enricher

**Date:** September 18, 2025  
**Author:** Filippo Mineo  
**Files Changed:** 38 files, 462 insertions, 2276 deletions (major refactoring)

#### What Changed for Library Users:

**1. Removed Diginsight.Core ServiceCollectionExtensions**
- Deleted `Diginsight.Core/Extensions/ServiceCollectionExtensions.cs`
- User Impact: ⚠️ **Potential breaking** - if using Core-level extension methods, migrate to Diagnostics package

**2. New Metric Recording Architecture**

**Added:**
```csharp
// NEW: DefaultMetricRecordingEnricher
public class DefaultMetricRecordingEnricher : IMetricRecordingEnricher
{
    public void Enrich(Activity activity, TagList tags) { }
}

// NEW: DefaultMetricRecordingEnricherOptions
public class DefaultMetricRecordingEnricherOptions : IDefaultMetricRecordingEnricherOptions
{
    // Options for enricher configuration
}

// NEW: NameBasedMetricRecordingFilter
public class NameBasedMetricRecordingFilter : IMetricRecordingFilter
{
    public bool ShouldRecord(Activity activity) { }
}

// NEW: NameBasedMetricRecordingFilterOptions
public class NameBasedMetricRecordingFilterOptions : INameBasedMetricRecordingFilterOptions
{
    public IDictionary<string, bool> ActivityNames { get; set; }
}
```

**Deleted:**
```csharp
// REMOVED: Old implementations
- MetricRecordingEnricherOptions.cs
- MetricRecordingNameBasedFilter.cs (old version)
- MetricRecordingNameBasedFilterOptions.cs (old version)
- MetricRecordingTagsEnricher.cs
```

- User Impact: ⚠️ **Breaking** - if implementing custom enrichers or filters, update to new interfaces

**3. Updated IMetricRecordingEnricher and IMetricRecordingFilter**
```csharp
// BEFORE
public interface IMetricRecordingEnricher
{
    void Enrich(Activity activity, IDictionary<string, object?> tags);
}

// AFTER
public interface IMetricRecordingEnricher
{
    void Enrich(Activity activity, TagList tags);  // Changed to TagList
}
```

- User Impact: ⚠️ **Breaking** - custom enrichers must update method signature

**4. Enhanced SpanDurationMetricRecorder**
- Improved lifecycle management
- Better integration with filters and enrichers
- User Impact: ✅ **Performance improvement** - more efficient metric recording

**5. AspNetCore Updates**
```csharp
// REMOVED
- HttpHeadersMetricRecordingFilter.cs

// ADDED
- HttpHeadersSpanDurationMetricRecordingFilter.cs (new implementation)
```
- User Impact: ⚠️ **Breaking** - update registrations if using HTTP headers filtering

**What Users Need to Do:**
1. ⚠️ Update custom <mark>`IMetricRecordingEnricher` implementations to use `TagList` instead of `IDictionary`</mark>
2. ⚠️ Replace <mark>`MetricRecordingTagsEnricher` with `DefaultMetricRecordingEnricher`</mark>
3. ⚠️ Replace <mark>`HttpHeadersMetricRecordingFilter` with `HttpHeadersSpanDurationMetricRecordingFilter`</mark>

---

### Commit 4: f8f4e9e - Unifying metric recording with filter and enricher

**Date:** September 19, 2025  
**Author:** Filippo Mineo  
**Files Changed:** 11 files, 106 insertions, 50 deletions

#### What Changed for Library Users:

**1. Interface Rename: IActivityLoggingSampler → IActivityLoggingFilter**
```csharp
// BEFORE
public interface IActivityLoggingSampler
{
    LogBehavior? GetLogBehavior(Activity activity);
}

// AFTER
public interface IActivityLoggingFilter
{
    LogBehavior? GetLogBehavior(Activity activity);
}
```
- User Impact: ⚠️ **BREAKING** - update all references and registrations

**2. Class Rename: NameBasedActivityLoggingSampler → NameBasedActivityLoggingFilter**
```csharp
// BEFORE
public class NameBasedActivityLoggingSampler : IActivityLoggingSampler { }

// AFTER  
public class NameBasedActivityLoggingFilter : IActivityLoggingFilter { }
```
- User Impact: ⚠️ **BREAKING** - update service registrations

**3. AspNetCore Filter Rename**
```csharp
// BEFORE
public class HttpHeadersActivityLoggingSampler { }

// AFTER
public class HttpHeadersActivityLoggingFilter { }
```
- User Impact: ⚠️ **BREAKING** - update registrations if using HTTP headers

**4. Updated IDiginsightActivitiesLogOptions**
```csharp
// BEFORE
public interface IDiginsightActivitiesLogOptions
{
    LogBehavior LogBehavior { get; }
    LogLevel ActivityLogLevel { get; }  // Old name
    bool WriteActivityActionAsPrefix { get; }
    bool DisablePayloadRendering { get; }
}

// AFTER
public interface IDiginsightActivitiesLogOptions
{
    IReadOnlyDictionary<string, LogBehavior> ActivityNames { get; }  // NEW
    LogBehavior LogBehavior { get; }
    LogLevel LogLevel { get; }  // RENAMED
    bool WriteActivityActionAsPrefix { get; }
    bool DisablePayloadRendering { get; }
}
```
- User Impact: ⚠️ **Breaking** - update code accessing `ActivityLogLevel` to <mark>`LogLevel`</mark>

**5. Removed IDiginsightActivitiesMetricOptions, Added <mark>IDiginsightActivitiesSpanDurationOptions</mark>**
```csharp
// REMOVED
public interface IDiginsightActivitiesMetricOptions
{
    bool RecordSpanDurations { get; }
    string MeterName { get; }
    string MetricName { get; }
    string? MetricUnit { get; }
    string? MetricDescription { get; }
}

// ADDED
public interface IDiginsightActivitiesSpanDurationOptions
{
    bool Record { get; }  // Simplified
    string MeterName { get; }
    string MetricName { get; }
    string? MetricDescription { get; }
    // MetricUnit removed - not used
}
```
- User Impact: ⚠️ **BREAKING** - migrate to new interface

**6. Deleted <mark>IDiginsightActivityNamesOptions</mark>**
- Merged into `IDiginsightActivitiesLogOptions` via `ActivityNames` property
- User Impact: ⚠️ **Breaking** - <mark>use `IDiginsightActivitiesLogOptions.ActivityNames`</mark> instead

**What Users Need to Do:**
1. ⚠️ **Required**: Replace `IActivityLoggingSampler` with <mark>`IActivityLoggingFilter`</mark> in:
   - Service registrations
   - Interface implementations
   - Dependency injection

```csharp
// OLD
services.AddSingleton<IActivityLoggingSampler, NameBasedActivityLoggingSampler>();

// NEW
services.AddSingleton<IActivityLoggingFilter, NameBasedActivityLoggingFilter>();
```

2. ⚠️ **Required**: Update property access:
```csharp
// OLD
var level = options.ActivityLogLevel;

// NEW
var level = options.LogLevel;
```

3. ⚠️ **Required**: Replace `IDiginsightActivitiesMetricOptions` with <mark>`IDiginsightActivitiesSpanDurationOptions`</mark>

---

### Commit 5: 9f3896d - .sln to .slnx

**Date:** September 20, 2025  
**Author:** Filippo Mineo  
**Files Changed:** 2 files, 23 insertions, 109 deletions

#### What Changed for Library Users:

**1. Solution File Format Change**
- Deleted `Diginsight.sln` (traditional format)
- Added `Diginsight.slnx` (new XML-based format)
- User Impact: ✅ **No functional changes** - both formats supported by Visual Studio 2022+

**What Users Need to Do:**
- ✅ **Nothing** - Visual Studio 2022+ automatically handles both formats
- ℹ️ Visual Studio 2019 users may need to upgrade IDE

---

### Commit 6: bab6879 - .sln to .slnx

**Date:** September 20, 2025 (after 9f3896d)  
**Author:** Filippo Mineo  
**Files Changed:** 1 file, 1 insertion, 1 deletion

#### What Changed for Library Users:

**1. CI/CD Workflow Update**
- Updated `.github/workflows/v3.yml` to use `.slnx` instead of `.sln`
- User Impact: ✅ **No user impact** - internal build process change

**What Users Need to Do:**
- ✅ **Nothing** - build process change only

---

### Commit 7: bbdd6cc - small fix

**Date:** September 21, 2025  
**Author:** Filippo Mineo  
**Files Changed:** 1 file, 2 insertions, 1 deletion

#### What Changed for Library Users:

**1. DiginsightTextWriter Fix**
- Fixed edge case in text rendering
- Improved null handling
- User Impact: ✅ **Stability improvement** - fewer exceptions during logging

**What Users Need to Do:**
- ✅ **Nothing** - automatic improvement

---

### Commit 8: e025853 - Fix in DiginsightActivitiesOptions

**Date:** September 22, 2025  
**Author:** Filippo Mineo  
**Files Changed:** 3 files, 3 insertions, 37 deletions

#### What Changed for Library Users:

**1. DiginsightActivitiesOptions Configuration Fix**
- Fixed configuration binding issues
- Improved default value handling
- User Impact: ✅ **Reliability improvement** - configuration loads correctly

**2. SpanDurationMetricRecorder Cleanup**
- Removed redundant code
- User Impact: ✅ **No functional change**

**3. Directory.Build.targets Update**
- Removed obsolete build configurations
- User Impact: ✅ **No user impact**

**What Users Need to Do:**
- ✅ **Nothing** - automatic improvement

---

### Commit 9: dffd6d7 - Fix in DiginsightTextWriter

**Date:** September 23, 2025  
**Author:** Filippo Mineo  
**Files Changed:** 1 file, 3 insertions, 4 deletions

#### What Changed for Library Users:

**1. DiginsightTextWriter Rendering Fix**
- Improved character escaping
- Better handling of special characters
- User Impact: ✅ **Quality improvement** - cleaner log output

**What Users Need to Do:**
- ✅ **Nothing** - automatic improvement

---

### Commit 10: af4121c - Rename

**Date:** September 25, 2025  
**Author:** Filippo Mineo  
**Files Changed:** 5 files, 41 insertions, 41 deletions

#### What Changed for Library Users:

**1. Standardized Naming: NameBased → <mark>OptionsBased</mark>**

**Interfaces Renamed:**
```csharp
// BEFORE
INameBasedMetricRecordingFilterOptions

// AFTER
IOptionsBasedMetricRecordingFilterOptions
```

**Classes Renamed:**
```csharp
// BEFORE
NameBasedMetricRecordingFilter
NameBasedMetricRecordingFilterOptions
NameBasedActivityLoggingFilter

// AFTER
OptionsBasedMetricRecordingFilter
OptionsBasedMetricRecordingFilterOptions
OptionsBasedActivityLoggingFilter
```

- User Impact: ⚠️ **Breaking** - update class references and registrations

**What Users Need to Do:**
```csharp
// OLD
services.AddSingleton<INameBasedMetricRecordingFilterOptions, NameBasedMetricRecordingFilterOptions>();
services.AddSingleton<IMetricRecordingFilter, NameBasedMetricRecordingFilter>();

// NEW
services.AddSingleton<IOptionsBasedMetricRecordingFilterOptions, OptionsBasedMetricRecordingFilterOptions>();
services.AddSingleton<IMetricRecordingFilter, OptionsBasedMetricRecordingFilter>();
```

---

### Commit 11: 35423d8 - Improvements in metric recording

**Date:** October 5, 2025  
**Author:** Filippo Mineo  
**Files Changed:** 5 files, 27 insertions, 38 deletions

#### What Changed for Library Users:

**1. Deleted ICustomDurationMetricRecorderSettings**
- Removed obsolete interface
- User Impact: ⚠️ **Breaking if used** - migrate to standard options

**2. Enhanced Metric Recording Components**
- Improved <mark>`DefaultMetricRecordingEnricher`</mark>
- Optimized <mark>`OptionsBasedMetricRecordingFilter`</mark>
- Better <mark>`SpanDurationMetricRecorder`</mark> performance
- User Impact: ✅ **Performance improvement**

**3. HttpHeadersSpanDurationMetricRecordingFilter Update**
- Enhanced HTTP header parsing
- Better integration with filter pipeline
- User Impact: ✅ **Improved functionality**

**What Users Need to Do:**
- ⚠️ If using <mark>`ICustomDurationMetricRecorderSettings`</mark>: Migrate to <mark>standard options classes</mark>
- ✅ Otherwise: No changes needed

---

### Commit 12: a664cbb - Improvements in metric recording

**Date:** October 6, 2025  
**Author:** Filippo Mineo  
**Files Changed:** 1 file, 16 insertions, 14 deletions

#### What Changed for Library Users:

**1. SpanDurationMetricRecorder Enhancements**
- Improved metric recording lifecycle
- Better error handling
- Optimized tag collection
- User Impact: ✅ **Reliability and performance improvement**

**What Users Need to Do:**
- ✅ **Nothing** - automatic improvement

---

### Commit 13: 24ce7db - Improvements in metric recording

**Date:** October 7, 2025  
**Author:** Filippo Mineo (tag: v3.6.0.0-alpha.5)  
**Files Changed:** 26 files, 16 insertions, 25 deletions

#### What Changed for Library Users:

**1. Major File Reorganization - Flattened Structure**

**Moved from subfolders to root:**
```
Extensions/ActivityExtensions.cs              → ActivityExtensions.cs
Extensions/ActivityListenerExtensions.cs      → ActivityListenerExtensions.cs
Extensions/ActivitySourceExtensions.cs        → ActivitySourceExtensions.cs
Extensions/DependencyInjectionExtensions.cs   → DependencyInjectionExtensions.cs
Extensions/MeterExtensions.cs                 → MeterExtensions.cs
Configurations/DiginsightActivitiesOptions.cs → DiginsightActivitiesOptions.cs
Configurations/...                            → (all moved to root)
Metrics/...                                   → (all moved to root)
```

- User Impact: ✅ **No breaking changes** - namespaces unchanged

**2. Interface/Class Renames**
```csharp
// BEFORE
IDiginsightActivitiesSpanDurationOptions

// AFTER
IMetricRecordingOptions  // More generic name
```

```csharp
// BEFORE  
DefaultMetricRecordingEnricher

// AFTER
OptionsBasedMetricRecordingEnricher  // Consistent naming
```

- User Impact: ⚠️ **Breaking** - update references

**3. LoggerFactoryStaticAccessor Simplification**
- Removed unnecessary code
- User Impact: ✅ **No functional change**

**What Users Need to Do:**
```csharp
// OLD
services.Configure<IDiginsightActivitiesSpanDurationOptions>(options => { });

// NEW
services.Configure<IMetricRecordingOptions>(options => { });
```

---

### Commit 14: 9cf9421 - Improvements in metric recording

**Date:** October 8, 2025 (tag: v3.6.0.0-alpha.6)  
**Author:** Filippo Mineo  
**Files Changed:** 4 files, 37 insertions, 37 deletions

#### What Changed for Library Users:

**1. Enricher Options Reorganization**
```csharp
// DELETED
DefaultMetricRecordingEnricherOptions

// RENAMED  
IDefaultMetricRecordingEnricherOptions → IOptionsBasedMetricRecordingEnricherOptions

// ADDED
OptionsBasedMetricRecordingEnricherOptions  // New implementation
```

- User Impact: ⚠️ **Breaking** - update options configuration

**2. <mark>OptionsBasedMetricRecordingEnricher</mark> Updates**
- Enhanced configuration binding
- Better default values
- User Impact: ✅ **Improved functionality**

**What Users Need to Do:**
```csharp
// OLD
services.Configure<IDefaultMetricRecordingEnricherOptions>(options => { });

// NEW
services.Configure<IOptionsBasedMetricRecordingEnricherOptions>(options => { });
services.Configure<OptionsBasedMetricRecordingEnricherOptions>(options => { });
```

---

### Commit 15: 987638c - Fixed Activity and ActivitySource filtering

**Date:** October 10, 2025 (tag: v3.7.0.0 - Final Release)  
**Author:** Filippo Mineo  
**Files Changed:** 3 files, 19 insertions, 10 deletions

#### What Changed for Library Users:

**1. Critical Bug Fix in OptionsBasedMetricRecordingFilter**
- Fixed pattern matching logic for activity names
- Corrected activity source filtering
- Fixed wildcard pattern support
- User Impact: ✅ **CRITICAL FIX** - filtering now works as documented

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
Activity source name filtering works reliably
```

**2. Enhanced <mark>DependencyInjectionExtensions</mark>**
- Improved service registration
- Better validation
- User Impact: ✅ **Improved reliability**

**3. <mark>SpanDurationMetricRecorderRegistration</mark> Updates**
- Better lifecycle management
- Improved error handling
- User Impact: ✅ **More reliable metric recording**

**What Users Need to Do:**
- ✅ **Nothing** - automatic fix
- ℹ️ **Benefit**: Activity filtering now works correctly with wildcard patterns

---

### Commit 16: a46efac - doc update

**Date:** November 12, 2025  
**Author:** Dario Airoldi  
**Files Changed:** 10 files (documentation and templates added)

#### What Changed for Library Users:

**1. Documentation Enhancements**
- Added GitHub Copilot instructions and templates
- New reference documentation for metric recording classes
- Added prompts for automated documentation generation
- User Impact: ✅ **Better documentation** - easier to understand and use the library

**2. New Reference Documentation**
- **SpanDurationMetricRecorder.md** - Complete reference for metric recorder
- **OptionsBasedMetricRecordingFilter.md** - Filter configuration guide
- **OptionsBasedMetricRecordingEnricher.md** - Enricher configuration guide
- User Impact: ✅ **Improved developer experience**

**3. Updated Advanced Topics**
- Replaced "Customize metrics" with "How metric recording works with diginsight and Opentelemetry"
- More comprehensive explanation of metric recording architecture
- User Impact: ✅ **Better understanding** of how metrics work

**What Users Need to Do:**
- ✅ **Nothing** - documentation improvements only
- ℹ️ **Recommendation**: Review new documentation for better understanding

---

### Commit 17-21: Build and Documentation Fixes

**Commits:** 4f371eb, cfded56, a9256cf, cc26269, 2b3cff4  
**Date:** November 12, 2025 - December 1, 2025  
**Author:** Dario Airoldi

#### What Changed for Library Users:

**1. Build Configuration Updates**
- Updated `_quarto.yml` for documentation site
- Reorganized GitHub templates and prompts
- Added `.github/copilot-instructions.md`
- User Impact: ✅ **No functional changes** - infrastructure improvements

**2. Documentation Fixes**
- Fixed configuration documentation examples
- Updated HowTo guides
- User Impact: ✅ **More accurate documentation**

**3. Template Organization**
- Moved templates from `.github/copilot/templates/` to `.github/templates/`
- Moved prompts from `.github/copilot/prompts/` to `.github/prompts/`
- Added new article templates
- User Impact: ✅ **No functional changes** - better repository organization

**What Users Need to Do:**
- ✅ **Nothing** - internal changes only

---

### Commit 22-23: Editor Configuration Cleanup

**Commits:** 548358f, 2b3cff4  
**Date:** December 1, 2025  
**Author:** Dario Airoldi

#### What Changed for Library Users:

**1. Removed Duplicate Configuration Files**
- Deleted `src/.editorconfig` (using root `.editorconfig`)
- Deleted `src/.gitattributes` (using root `.gitattributes`)
- User Impact: ✅ **No functional changes** - configuration cleanup

**What Users Need to Do:**
- ✅ **Nothing** - internal cleanup only

---

### Commit 24: 3bd25ca - projects update .net10.0

**Date:** December 1, 2025  
**Author:** Dario Airoldi  
**Files Changed:** 21 files (all project files)

#### What Changed for Library Users:

**1. .NET 10.0 Support Added**
```xml
<!-- BEFORE (in .csproj files) -->
<TargetFrameworks>net9.0;net8.0;net7.0;net6.0</TargetFrameworks>

<!-- AFTER -->
<TargetFrameworks>net10.0;net9.0;net8.0;net7.0;net6.0</TargetFrameworks>
```

- User Impact: ✅ **Enhanced compatibility** - now supports .NET 10.0

**2. global.json Updated**
```json
{
  "sdk": {
    "version": "10.0.100-preview"
  }
}
```

- User Impact: ✅ **Future-ready** - prepared for .NET 10.0 release

**3. Package Lock Files Updated**
- All `packages.lock.json` files regenerated
- User Impact: ✅ **No breaking changes** - dependency updates

**What Users Need to Do:**
```csharp
// For .NET 10.0 projects
<PackageReference Include="Diginsight.Diagnostics" Version="3.7.1.0" />
// Works with .NET 10.0, 9.0, 8.0, 7.0, and 6.0
```

---

### Commit 25-26: Documentation Fixes

**Commits:** 65a9508, 4e47521, 4a44c25  
**Date:** January 5, 2026  
**Author:** Dario Airoldi

#### What Changed for Library Users:

**1. Reference Documentation Added**
- **IDiginsightActivitiesLogOptions.md** - Complete interface reference
- **IDiginsightActivitiesOptions.md** - Complete interface reference
- User Impact: ✅ **Better API documentation**

**2. VSCode Settings**
- Added `.vscode/settings.json` for better IDE support
- User Impact: ✅ **Improved developer experience**

**3. Style Improvements**
- Updated `styles.css` for documentation site
- User Impact: ✅ **Better documentation readability**

**What Users Need to Do:**
- ✅ **Nothing** - documentation improvements only
- ℹ️ **Recommendation**: Review new interface reference documentation

---

### Commit 27: 47ebed3 - v3.7.1.0 support for multiple metrics filters and enrichers

**Date:** January 5, 2026 (tag: v3.7.1.0 - Latest Release)  
**Author:** Dario Airoldi  
**Files Changed:** 7 files, 94 insertions, 38 deletions

#### What Changed for Library Users:

**1. NEW: Named Options Support** ⭐

Added `NamedOptionsMonitor<T>` and `NamedOptions<T>` classes to support multiple named metric filter and enricher instances:

```csharp
// NEW: Support for named options
public sealed class NamedOptionsMonitor<T> : IOptionsMonitor<T>
{
    private readonly IOptionsMonitor<T> innerOptionsMonitor;
    public T CurrentValue { get; set; }

    public NamedOptionsMonitor(IOptionsMonitor<T> innerOptionsMonitor, string? name = null)
    {
        this.innerOptionsMonitor = innerOptionsMonitor;
        this.CurrentValue = innerOptionsMonitor.Get(name);
    }
    
    public T Get(string? name) => innerOptionsMonitor.Get(name);
}

public sealed class NamedOptions<T> : IOptions<T> where T : class
{
    public T Value { get; }
    
    public NamedOptions(IOptionsMonitor<T> optionsMonitor, string name)
    {
        Value = optionsMonitor.Get(name);
    }
}
```

- User Impact: ✅ **Major enhancement** - now supports multiple independent filter and enricher configurations per metric

**2. Enhanced Filter Options Interface**

```csharp
// BEFORE
public interface IOptionsBasedMetricRecordingFilterOptions
{
    IDictionary<string, bool> ActivityNames { get; set; }
}

// AFTER - Added MetricName
public interface IOptionsBasedMetricRecordingFilterOptions
{
    string? MetricName { get; set; }  // NEW: Allows naming filters
    IDictionary<string, bool> ActivityNames { get; set; }
}
```

- User Impact: ✅ **Enhanced functionality** - filters can be bound to specific metrics by name

**3. Enhanced Enricher Options Interface**

```csharp
// BEFORE
public interface IOptionsBasedMetricRecordingEnricherOptions
{
    ICollection<string> MetricTags { get; set; }
}

// AFTER - Added MetricName
public interface IOptionsBasedMetricRecordingEnricherOptions
{
    string? MetricName { get; set; }  // NEW: Allows naming enrichers
    ICollection<string> MetricTags { get; set; }
}
```

- User Impact: ✅ **Enhanced functionality** - enrichers can be bound to specific metrics by name

**4. Updated Filter Implementation**

```csharp
// Enhanced OptionsBasedMetricRecordingFilter with Instrument parameter
public class OptionsBasedMetricRecordingFilter : IMetricRecordingFilter
{
    // NEW: ShouldRecord now accepts Instrument parameter
    public virtual bool? ShouldRecord(Activity activity, Instrument instrument)
    {
        string activitySourceName = activity.Source.Name;
        string activityName = activity.OperationName;
        
        // Get named options based on instrument name
        IEnumerable<bool> specificMatches = GetMatches(filterMonitor.Get(instrument.Name));
        if (specificMatches.Any())
        {
            return specificMatches.All(static x => x);
        }
        
        // Fall back to default options
        IEnumerable<bool> generalMatches = GetMatches(filterMonitor.CurrentValue);
        return generalMatches.Any() && generalMatches.All(static x => x);
    }
}
```

- User Impact: ✅ **Enhanced filtering** - different filters per metric instrument

**5. Updated Enricher Implementation**

```csharp
// Enhanced OptionsBasedMetricRecordingEnricher with Instrument parameter
public class OptionsBasedMetricRecordingEnricher : IMetricRecordingEnricher
{
    // NEW: ExtractTags now accepts Instrument parameter
    public virtual Tags ExtractTags(Activity activity, Instrument instrument)
    {
        // Get named enricher options based on instrument name
        return GetTagNames(enricherMonitor.Get(instrument.Name))
            .Concat(GetTagNames(enricherMonitor.CurrentValue))
            .Distinct()
            .Select(k => /* extract tag from activity */)
            .Where(static x => x.Value is not null)
            .Select(static x => new Tag(x.Key, x.Value));
    }
}
```

- User Impact: ✅ **Enhanced enrichment** - different enrichers per metric instrument

**6. Core Extensions Added**

Added `ServiceCollectionCoreExtensions.cs` to Diginsight.Core:

```csharp
// NEW: Core extension methods
public static class ServiceCollectionCoreExtensions
{
    // Support for named options registration
    public static IServiceCollection AddNamedOptions<T>(
        this IServiceCollection services, 
        string name) where T : class
    {
        // Implementation for named options support
    }
}
```

- User Impact: ✅ **New API** - easier registration of named options

**What Users Can Now Do:**

```json
// Configure MULTIPLE filters for different metrics
{
  "OptionsBasedMetricRecordingFilter": {
    // Default filter (applies to all metrics if no specific filter)
    "ActivityNames": {
      "MyApp.*": true,
      "System.*": false
    }
  },
  "OptionsBasedMetricRecordingFilter:span_duration": {
    // Specific filter for "span_duration" metric
    "MetricName": "span_duration",
    "ActivityNames": {
      "MyApp.CriticalPath.*": true,
      "MyApp.*": false
    }
  },
  "OptionsBasedMetricRecordingFilter:http_requests": {
    // Specific filter for "http_requests" metric
    "MetricName": "http_requests",
    "ActivityNames": {
      "Microsoft.AspNetCore.*": true
    }
  }
}
```

```json
// Configure MULTIPLE enrichers for different metrics
{
  "OptionsBasedMetricRecordingEnricher": {
    // Default enricher (applies to all metrics if no specific enricher)
    "MetricTags": ["environment", "version"]
  },
  "OptionsBasedMetricRecordingEnricher:span_duration": {
    // Specific enricher for "span_duration" metric
    "MetricName": "span_duration",
    "MetricTags": ["user_id", "tenant_id", "operation_type"]
  },
  "OptionsBasedMetricRecordingEnricher:http_requests": {
    // Specific enricher for "http_requests" metric
    "MetricName": "http_requests",
    "MetricTags": ["http_method", "http_route", "response_code"]
  }
}
```

**Service Registration:**

```csharp
// Register named filters
services.Configure<OptionsBasedMetricRecordingFilterOptions>(
    "span_duration", 
    configuration.GetSection("OptionsBasedMetricRecordingFilter:span_duration"));

services.Configure<OptionsBasedMetricRecordingFilterOptions>(
    "http_requests", 
    configuration.GetSection("OptionsBasedMetricRecordingFilter:http_requests"));

// Register named enrichers
services.Configure<OptionsBasedMetricRecordingEnricherOptions>(
    "span_duration", 
    configuration.GetSection("OptionsBasedMetricRecordingEnricher:span_duration"));

services.Configure<OptionsBasedMetricRecordingEnricherOptions>(
    "http_requests", 
    configuration.GetSection("OptionsBasedMetricRecordingEnricher:http_requests"));
```

**Benefits:**
- ✅ **Granular control**: Different filters per metric type
- ✅ **Different tags**: Different enrichers per metric type
- ✅ **Better organization**: Clear separation of concerns
- ✅ **Backward compatible**: Default options still work as before

**What Users Need to Do:**
- ✅ **Optional adoption**: Named options are optional - existing configurations continue to work
- ℹ️ **Recommendation**: Use named options for complex scenarios with multiple metrics requiring different filtering/enrichment rules

---

## Migration Guide

### For Most Users (JSON Configuration Only)

If you're using **JSON-based configuration only** and not implementing custom filters/enrichers, here's what you need to update:

#### Step 1: Update Configuration Properties

```json
{
  "Diginsight": {
    "Activities": {
      "ActivitySources": {
        "MyApp.*": true
      },
      "LogBehavior": "Hide",
      "LogLevel": "Debug",  // ⚠️ Was "ActivityLogLevel" (both work for backward compatibility)
      
      // ⚠️ IMPORTANT: Update these
      "RecordSpanDuration": false,  // ⚠️ Was "RecordSpanDurations" (plural)
      "SpanDurationMeterName": "MyApp",  // ⚠️ Was "MeterName"
      "SpanDurationMetricName": "span_duration",  // ⚠️ Was "MetricName"
      "SpanDurationMetricDescription": "Activity durations"  // ⚠️ Was "MetricDescription"
      // Note: "MetricUnit" has been removed (not used)
    }
  }
}
```

#### Step 2: Update Service Registrations (if any)

```csharp
// OLD - Update these if you have custom registrations
services.AddSingleton<IActivityLoggingSampler, NameBasedActivityLoggingSampler>();

// NEW
services.AddSingleton<IActivityLoggingFilter, OptionsBasedActivityLoggingFilter>();
```

#### Step 3: Test Your Application
- Test activity logging with your configured patterns
- Test metric recording if enabled
- Verify wildcard patterns work correctly

---

### For Advanced Users (Custom Implementations)

If you've implemented custom filters, enrichers, or access interfaces directly:

#### 1. Update Interface Implementations

**Activity Logging:**
```csharp
// OLD
public class MyActivityLoggingSampler : IActivityLoggingSampler
{
    public LogBehavior? GetLogBehavior(Activity activity)
    {
        // implementation
    }
}

// NEW
public class MyActivityLoggingFilter : IActivityLoggingFilter
{
    public LogBehavior? GetLogBehavior(Activity activity)
    {
        // implementation
    }
}
```

**Metric Enriching:**
```csharp
// OLD
public class MyEnricher : IMetricRecordingEnricher
{
    public void Enrich(Activity activity, IDictionary<string, object?> tags)
    {
        tags["custom"] = "value";
    }
}

// NEW - Changed to TagList
public class MyEnricher : IMetricRecordingEnricher
{
    public void Enrich(Activity activity, TagList tags)
    {
        tags.Add("custom", "value");
    }
}
```

#### 2. Update Options Access

```csharp
// OLD
public class MyService
{
    private readonly IDiginsightActivitiesMetricOptions metricOptions;
    
    public MyService(IDiginsightActivitiesMetricOptions metricOptions)
    {
        this.metricOptions = metricOptions;
        bool record = metricOptions.RecordSpanDurations;
        string meter = metricOptions.MeterName;
    }
}

// NEW
public class MyService
{
    private readonly IMetricRecordingOptions metricOptions;
    
    public MyService(IMetricRecordingOptions metricOptions)
    {
        this.metricOptions = metricOptions;
        bool record = metricOptions.Record;  // Renamed
        string meter = metricOptions.MeterName;
    }
}
```

#### 3. Update Class Names

Replace all occurrences:
- `NameBasedActivityLoggingSampler` → `OptionsBasedActivityLoggingFilter`
- `NameBasedMetricRecordingFilter` → `OptionsBasedMetricRecordingFilter`
- `NameBasedMetricRecordingFilterOptions` → `OptionsBasedMetricRecordingFilterOptions`
- `DefaultMetricRecordingEnricher` → `OptionsBasedMetricRecordingEnricher`

---

### Migrating to v3.7.1.0 - Multiple Named Filters and Enrichers (Optional)

**This is an OPTIONAL upgrade** - existing configurations continue to work unchanged.

#### When to Use Named Filters/Enrichers

Consider using named filters and enrichers if you need:
- **Different filtering rules per metric** (e.g., record all activities for one metric, only critical paths for another)
- **Different tags per metric** (e.g., include user_id for business metrics, but not for technical metrics)
- **Multiple custom metrics** with independent configurations

#### Step 1: Define Named Filter Configurations

```json
{
  "OptionsBasedMetricRecordingFilter": {
    // Default filter - applies when no specific filter is matched
    "ActivityNames": {
      "MyApp.*": true,
      "System.*": false
    }
  },
  "OptionsBasedMetricRecordingFilter:business_metrics": {
    "MetricName": "business_metrics",
    "ActivityNames": {
      "MyApp.Orders.*": true,
      "MyApp.Payments.*": true,
      "MyApp.*": false  // Only specific business activities
    }
  },
  "OptionsBasedMetricRecordingFilter:technical_metrics": {
    "MetricName": "technical_metrics",
    "ActivityNames": {
      "MyApp.*": true,  // All technical activities
      "System.*": true
    }
  }
}
```

#### Step 2: Define Named Enricher Configurations

```json
{
  "OptionsBasedMetricRecordingEnricher": {
    // Default enricher - applies when no specific enricher is matched
    "MetricTags": ["environment", "version"]
  },
  "OptionsBasedMetricRecordingEnricher:business_metrics": {
    "MetricName": "business_metrics",
    "MetricTags": ["user_id", "tenant_id", "order_type", "payment_method"]
  },
  "OptionsBasedMetricRecordingEnricher:technical_metrics": {
    "MetricName": "technical_metrics",
    "MetricTags": ["machine_name", "process_id", "thread_id"]
  }
}
```

#### Step 3: Register Named Options (if needed)

In most cases, configuration binding will handle this automatically. For advanced scenarios:

```csharp
// Register named filter options
services.Configure<OptionsBasedMetricRecordingFilterOptions>(
    "business_metrics",
    configuration.GetSection("OptionsBasedMetricRecordingFilter:business_metrics"));

services.Configure<OptionsBasedMetricRecordingFilterOptions>(
    "technical_metrics",
    configuration.GetSection("OptionsBasedMetricRecordingFilter:technical_metrics"));

// Register named enricher options
services.Configure<OptionsBasedMetricRecordingEnricherOptions>(
    "business_metrics",
    configuration.GetSection("OptionsBasedMetricRecordingEnricher:business_metrics"));

services.Configure<OptionsBasedMetricRecordingEnricherOptions>(
    "technical_metrics",
    configuration.GetSection("OptionsBasedMetricRecordingEnricher:technical_metrics"));
```

#### Step 4: Create Metrics with Matching Names

```csharp
// Create custom metric recorders with names matching your configuration
public class BusinessMetricsRecorder : IActivityListenerLogic
{
    private readonly IMeterFactory meterFactory;
    private readonly Histogram<double> histogram;
    
    public BusinessMetricsRecorder(IMeterFactory meterFactory)
    {
        this.meterFactory = meterFactory;
        
        // Metric name "business_metrics" matches configuration key
        var meter = meterFactory.Create("MyApp.Business");
        this.histogram = meter.CreateHistogram<double>("business_metrics", "ms", "Business operation durations");
    }
    
    public void ActivityStopped(Activity activity)
    {
        // Filter and enricher with name "business_metrics" will be used
        histogram.Record(activity.Duration.TotalMilliseconds);
    }
}

public class TechnicalMetricsRecorder : IActivityListenerLogic
{
    private readonly IMeterFactory meterFactory;
    private readonly Histogram<double> histogram;
    
    public TechnicalMetricsRecorder(IMeterFactory meterFactory)
    {
        this.meterFactory = meterFactory;
        
        // Metric name "technical_metrics" matches configuration key
        var meter = meterFactory.Create("MyApp.Technical");
        this.histogram = meter.CreateHistogram<double>("technical_metrics", "ms", "Technical operation durations");
    }
    
    public void ActivityStopped(Activity activity)
    {
        // Filter and enricher with name "technical_metrics" will be used
        histogram.Record(activity.Duration.TotalMilliseconds);
    }
}
```

#### Example: Different Rules for Different Metrics

```csharp
// Scenario: Record detailed user journeys for business analytics,
//           but only critical paths for technical monitoring

// Business metrics configuration
{
  "OptionsBasedMetricRecordingFilter:user_journey": {
    "MetricName": "user_journey",
    "ActivityNames": {
      "MyApp.UI.*": true,           // All UI interactions
      "MyApp.Orders.*": true,        // All order operations
      "MyApp.Checkout.*": true       // All checkout steps
    }
  },
  "OptionsBasedMetricRecordingEnricher:user_journey": {
    "MetricName": "user_journey",
    "MetricTags": [
      "user_id",
      "session_id",
      "page_name",
      "action_name",
      "conversion_funnel_step"
    ]
  }
}

// Technical metrics configuration
{
  "OptionsBasedMetricRecordingFilter:performance": {
    "MetricName": "performance",
    "ActivityNames": {
      "MyApp.Database.*": true,      // Only database operations
      "MyApp.Cache.*": false,        // Skip cache operations
      "MyApp.API.Critical.*": true   // Only critical API paths
    }
  },
  "OptionsBasedMetricRecordingEnricher:performance": {
    "MetricName": "performance",
    "MetricTags": [
      "operation_type",
      "resource_name",
      "is_cached"
    ]
  }
}
```

#### Benefits of Named Filters/Enrichers

✅ **Separation of concerns**: Business vs technical metrics have different requirements  
✅ **Reduced noise**: Only record what's relevant for each metric  
✅ **Better insights**: Different tags provide context-specific information  
✅ **Performance**: Less overhead by filtering earlier in the pipeline  
✅ **Flexibility**: Easy to add new metrics with independent configurations

---

## Testing Recommendations

After upgrading to 3.7, test the following scenarios:

### 1. Configuration Loading
```csharp
[Fact]
public void Configuration_Loads_Correctly()
{
    var options = serviceProvider
        .GetRequiredService<IOptions<DiginsightActivitiesOptions>>()
        .Value;
        
    Assert.Equal(LogBehavior.Hide, options.LogBehavior);
    Assert.False(options.RecordSpanDuration);
    Assert.Equal("MyApp", options.SpanDurationMeterName);
}
```

### 2. Activity Filtering with Wildcards
```csharp
[Fact]
public void Activity_Filtering_Works_With_Wildcards()
{
    // Configure
    options.ActivityNames["MyApp.Orders.*"] = true;
    
    var filter = serviceProvider.GetRequiredService<IActivityLoggingFilter>();
    
    using var activity = activitySource.StartActivity("MyApp.Orders.ProcessOrder");
    var behavior = filter.GetLogBehavior(activity);
    
    Assert.Equal(LogBehavior.Show, behavior);
}
```

### 3. Metric Recording
```csharp
[Fact]
public async Task Metrics_Are_Recorded_Correctly()
{
    // Configure metric recording
    options.RecordSpanDuration = true;
    options.SpanDurationMeterName = "TestMeter";
    
    using var activity = activitySource.StartActivity("TestOperation");
    await Task.Delay(100);
    activity?.Stop();
    
    // Verify metric was recorded
    var metrics = metricReader.GetMetrics();
    Assert.Contains(metrics, m => m.Name == "diginsight.span_duration");
}
```

### 4. HTTP Header Filtering (if using AspNetCore)
```csharp
[Fact]
public async Task HTTP_Header_Filtering_Works()
{
    var request = new HttpRequestMessage(HttpMethod.Get, "/api/test");
    request.Headers.Add("Activity-Recording", "true");
    
    var response = await client.SendAsync(request);
    
    // Verify activity was recorded based on header
    Assert.True(response.IsSuccessStatusCode);
    // Check metrics were recorded
}
```

---

## Breaking Changes Summary

| Change | Severity | Commit | Migration Required |
|--------|----------|--------|-------------------|
| `IActivityLoggingSampler` → `IActivityLoggingFilter` | ❗ High | f8f4e9e | Yes - update all registrations |
| `RecordSpanDurations` → `RecordSpanDuration` | ⚠️ Medium | f8f4e9e | Yes - update config files |
| `IDiginsightActivitiesMetricOptions` removed | ❗ High | f8f4e9e | Yes - use `IMetricRecordingOptions` |
| `ActivityLogLevel` → `LogLevel` property | ⚠️ Low | f8f4e9e | Only if accessing via interface |
| `MetricUnit` property removed | ⚠️ Low | f8f4e9e | Remove if used |
| `NameBased*` → `OptionsBased*` classes | ⚠️ Medium | af4121c | Yes - update class references |
| `IMetricRecordingEnricher` signature changed | ⚠️ Medium | c0b4103 | Only for custom implementations |
| `IDiginsightActivitiesSpanDurationOptions` → `IMetricRecordingOptions` | ⚠️ Medium | 24ce7db | Update interface references |
| Metric property prefixes (`MeterName` → `SpanDurationMeterName`) | ⚠️ Medium | Multiple | Update configuration |
| **v3.7.1.0**: `IMetricRecordingFilter.ShouldRecord()` now accepts `Instrument` parameter | ⚠️ Low | 47ebed3 | Only for custom filter implementations |
| **v3.7.1.0**: `IMetricRecordingEnricher.ExtractTags()` now accepts `Instrument` parameter | ⚠️ Low | 47ebed3 | Only for custom enricher implementations |

**Note**: v3.7.1.0 changes are **non-breaking** for standard usage - they only affect custom implementations of filters and enrichers.

---

## Deprecations

The following are deprecated and will be removed in v4.0:

- ❌ `IActivityLoggingSampler` - use `IActivityLoggingFilter`
- ❌ `NameBasedActivityLoggingSampler` - use `OptionsBasedActivityLoggingFilter`
- ❌ `NameBasedMetricRecordingFilter` - use `OptionsBasedMetricRecordingFilter`
- ❌ `IDiginsightActivitiesMetricOptions` - use `IMetricRecordingOptions`
- ❌ `ObservabilityRegistry` - use `LoggerFactoryStaticAccessor`
- ❌ `ICustomDurationMetricRecorderSettings` - use standard options

---

## Upgrade Checklist

Use this checklist to ensure a smooth upgrade:

### Core Upgrade Steps (v3.7.0.0)

- [ ] **Backup current configuration**
- [ ] **Update NuGet packages** to 3.7.x (3.7.0.0 or 3.7.1.0)
- [ ] **Update configuration files:**
  - [ ] Change `RecordSpanDurations` → `RecordSpanDuration`
  - [ ] Add `SpanDuration` prefix to metric properties
  - [ ] Remove `MetricUnit` if present
- [ ] **Update service registrations:**
  - [ ] Replace `IActivityLoggingSampler` with `IActivityLoggingFilter`
  - [ ] Replace `NameBased*` with `OptionsBased*` classes
- [ ] **Update custom implementations** (if any):
  - [ ] Migrate `IMetricRecordingEnricher` to use `TagList`
  - [ ] Update `IActivityLoggingFilter` implementations
  - [ ] Replace deprecated interfaces
- [ ] **Update code accessing interfaces directly:**
  - [ ] Replace `ActivityLogLevel` with `LogLevel`
  - [ ] Replace `IDiginsightActivitiesMetricOptions` with `IMetricRecordingOptions`
- [ ] **Run tests:**
  - [ ] Test configuration loading
  - [ ] Test activity filtering (especially wildcards)
  - [ ] Test metric recording
  - [ ] Test HTTP header filtering (if applicable)

### Optional: v3.7.1.0 Named Metrics (Advanced)

- [ ] **Evaluate named metrics need:**
  - [ ] Do you have multiple metrics requiring different filtering rules?
  - [ ] Do you need different tags for different metric types?
  - [ ] Would separate configurations improve clarity?
- [ ] **If adopting named metrics:**
  - [ ] Design metric naming strategy (e.g., `business_metrics`, `technical_metrics`)
  - [ ] Create named configuration sections for filters
  - [ ] Create named configuration sections for enrichers
  - [ ] Update or create custom metric recorders with matching instrument names
  - [ ] Test each named configuration independently
  - [ ] Verify fallback to default configuration works

### Finalization

- [ ] **Update documentation:**
  - [ ] Update code samples
  - [ ] Update configuration examples
  - [ ] Update Getting Started guides
  - [ ] Document any named metrics if used
- [ ] **Deploy to test environment**
- [ ] **Run regression tests**
- [ ] **Monitor metrics and logs** for correctness
- [ ] **Deploy to production**
  - [ ] Change `RecordSpanDurations` → `RecordSpanDuration`
  - [ ] Add `SpanDuration` prefix to metric properties
  - [ ] Remove `MetricUnit` if present
- [ ] **Update service registrations:**
  - [ ] Replace `IActivityLoggingSampler` with `IActivityLoggingFilter`
  - [ ] Replace `NameBased*` with `OptionsBased*` classes
- [ ] **Update custom implementations** (if any):
  - [ ] Migrate `IMetricRecordingEnricher` to use `TagList`
  - [ ] Update `IActivityLoggingFilter` implementations
  - [ ] Replace deprecated interfaces
- [ ] **Update code accessing interfaces directly:**
  - [ ] Replace `ActivityLogLevel` with `LogLevel`
  - [ ] Replace `IDiginsightActivitiesMetricOptions` with `IMetricRecordingOptions`
- [ ] **Run tests:**
  - [ ] Test configuration loading
  - [ ] Test activity filtering (especially wildcards)
  - [ ] Test metric recording
  - [ ] Test HTTP header filtering (if applicable)
- [ ] **Update documentation:**
  - [ ] Update code samples
  - [ ] Update configuration examples
  - [ ] Update Getting Started guides
- [ ] **Deploy to test environment**
- [ ] **Run regression tests**
- [ ] **Deploy to production**

---

## Benefits of This Release

### 1. Improved Architecture
- **Unified metric recording**: Consistent filter and enricher pattern
- **Better separation of concerns**: Clear responsibilities for each component
- **Enhanced extensibility**: Easier to implement custom behaviors

### 2. Better Naming Consistency
- **OptionsBased prefix**: Clear indication of options-driven components
- **Filter vs Sampler**: More accurate terminology
- **Property prefixes**: Clear indication of property purpose

### 3. Enhanced Functionality
- **Fixed critical bugs**: Activity and ActivitySource filtering now works correctly
- **Improved performance**: Optimized metric recording
- **Better reliability**: Fixed edge cases in configuration and text rendering

### 4. Modernized Codebase
- **C# 14 features**: Leverages latest language improvements
- **New solution format**: Modern .slnx format
- **Polyfills updated**: Better compatibility across frameworks

### 5. Improved Maintainability
- **Flattened structure**: Files easier to find
- **Consistent naming**: OptionsBased prefix throughout
- **Deleted obsolete code**: Removed unused classes and interfaces

---

## Resources

- [Diginsight Documentation](../../README.md)
- [Configuration Guide](../01.%20Concepts/01.00%20-%20Configure%20diginsight%20telemetry%20to%20the%20local%20text%20based%20streams.md)
- [OpenTelemetry Integration](../01.%20Concepts/02.00%20-%20HowTo%20-%20configure%20diginsight%20telemetry%20to%20the%20remote%20tools.md)
- [GitHub Repository](https://github.com/diginsight/telemetry)
- [Release Notes on GitHub](https://github.com/diginsight/telemetry/releases/tag/v3.7.0.0)
- [Discussions Forum](https://github.com/diginsight/telemetry/discussions)

---

## Getting Help

If you encounter issues during migration:

1. **Check this changelog** for breaking changes specific to your scenario
2. **Review the migration guide** for your use case
3. **Search existing issues** on GitHub
4. **Ask in discussions** forum
5. **Create an issue** with:
   - Version you're migrating from
   - Specific error messages
   - Configuration files
   - Code samples showing the problem

---

## Acknowledgments

Special thanks to all contributors who made this release possible:

- **Filippo Mineo** - Lead developer and architect for metric recording unification
- **Dario Airoldi** - Named options support, .NET 10.0 upgrade, and comprehensive documentation
- All community members who reported issues and provided feedback
- Contributors who tested alpha releases and provided valuable insights

This release represents months of careful refactoring and testing to deliver a more consistent, maintainable, and reliable telemetry framework. Version 3.7.1.0 extends this foundation with powerful new capabilities for managing multiple metrics with independent configurations.

---

**Release Tags:**  
- [v3.7.0.0](https://github.com/diginsight/telemetry/releases/tag/v3.7.0.0) - November 12, 2025  
- [v3.7.1.0](https://github.com/diginsight/telemetry/releases/tag/v3.7.1.0) - January 5, 2026 (Latest)

**Previous Release:** [v3.6.0.0-alpha.6](https://github.com/diginsight/telemetry/releases/tag/v3.6.0.0-alpha.6)  
**Next Release:** v3.8.0.0 (planned)
