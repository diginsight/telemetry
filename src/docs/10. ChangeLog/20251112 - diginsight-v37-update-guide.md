# Diginsight v3.7 Upgrade Guide

**Comprehensive Step-by-Step Migration Guide for All Samples**

**Version:** 3.7.0.0  
**Release Date:** November 12, 2025  
**Last Updated:** January 5, 2026

---

## Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Breaking Changes Quick Reference](#breaking-changes-quick-reference)
- [Step-by-Step Upgrade Process](#step-by-step-upgrade-process)
  - [Step 1: Backup Your Project](#step-1-backup-your-project)
  - [Step 2: Update NuGet Packages](#step-2-update-nuget-packages)
  - [Step 3: Update Configuration Files (appsettings.json)](#step-3-update-configuration-files-appsettingsjson)
  - [Step 4: Update C# Code Files](#step-4-update-c-code-files)
  - [Step 5: Build and Test](#step-5-build-and-test)
- [Sample-Specific Guidance](#sample-specific-guidance)
- [Troubleshooting](#troubleshooting)
- [Verification Checklist](#verification-checklist)
- [Resources](#resources)

---

## Overview

Diginsight v3.7 introduces significant architectural improvements focused on:
- **Unified metric recording** with filter and enricher pattern
- **Improved naming consistency** (OptionsBased prefix)
- **Critical bug fixes** for activity and ActivitySource filtering
- **Enhanced configuration schema**

This guide provides a systematic approach to upgrading all samples in this repository.

---

## Prerequisites

- ✅ .NET SDK 6.0 or higher
- ✅ Visual Studio 2022+ or VS Code
- ✅ Backup of existing code
- ✅ Access to [Diginsight v3.7 Changelog](https://diginsight.github.io/telemetry/src/docs/10.%20ChangeLog/20251112%20-%20changes%20for%20release%203.7.html)

---

## Breaking Changes Quick Reference

| Change | Impact | Required Action |
|--------|--------|-----------------|
| `RecordSpanDurations` → <mark>`RecordSpanDuration`</mark> | ⚠️ Medium | Update config files |
| `IActivityLoggingSampler` → <mark>`IActivityLoggingFilter`</mark> | ❗ High | Update service registrations |
| `NameBasedActivityLoggingSampler` → <mark>`OptionsBasedActivityLoggingFilter`</mark> | ❗ High | Update class references |
| `IDiginsightActivitiesMetricOptions` removed | ❗ High | Use <mark>`IMetricRecordingOptions`</mark> |
| `ActivityLogLevel` → <mark>`LogLevel`</mark> | ⚠️ Low | Update if accessing via interface |
| `MetricUnit` property removed | ⚠️ Low | Remove from configuration |
| Metric properties need `SpanDuration` prefix | ⚠️ Medium | Update configuration |
| <mark>`IMetricRecordingEnricher`</mark> signature changed | ⚠️ Medium | Update custom implementations |
| `NameBased*` → <mark>`OptionsBased*`</mark> classes | ⚠️ Medium | Update all class references |

---

## Step-by-Step Upgrade Process

### Step 1: Backup Your Project

Before making any changes, create a backup of your project:

```bash
# Create a backup branch or copy
git checkout -b backup-before-v37-upgrade
git commit -am "Backup before Diginsight v3.7 upgrade"
```

Or simply copy the project folder to a backup location.

---

### Step 2: Update NuGet Packages

Update all Diginsight NuGet packages to version 3.7.x:

#### Option A: Using Package Manager Console

```powershell
# Update all Diginsight packages
Update-Package Diginsight.Core -Version 3.7.0
Update-Package Diginsight.Diagnostics -Version 3.7.0
Update-Package Diginsight.Diagnostics.Log4Net -Version 3.7.0
Update-Package Diginsight.AspNetCore -Version 3.7.0
```

#### Option B: Using .NET CLI

```bash
dotnet add package Diginsight.Core --version 3.7.0
dotnet add package Diginsight.Diagnostics --version 3.7.0
dotnet add package Diginsight.Diagnostics.Log4Net --version 3.7.0
dotnet add package Diginsight.AspNetCore --version 3.7.0
```

#### Option C: Manual Edit of .csproj

Edit your `.csproj` file directly:

```xml
<PackageReference Include="Diginsight.Core" Version="3.7.0" />
<PackageReference Include="Diginsight.Diagnostics" Version="3.7.0" />
<PackageReference Include="Diginsight.Diagnostics.Log4Net" Version="3.7.0" />
<PackageReference Include="Diginsight.AspNetCore" Version="3.7.0" />
```

Then restore packages:

```bash
dotnet restore
```

---

### Step 3: Update Configuration Files (appsettings.json)

#### 3.1 Property Renames and Updates

**CRITICAL CHANGES:**

1. **`RecordSpanDurations` → `RecordSpanDuration` (singular)**
2. **Add `SpanDuration` prefix to metric properties**
3. **Remove `MetricUnit` property**

#### Before (v3.6 and earlier):

```json
{
  "Diginsight": {
    "Activities": {
      "LogBehavior": "Show",
      "ActivityLogLevel": "Debug",
      "RecordSpanDurations": true,
      "MeterName": "MySampleApp",
      "MetricName": "diginsight.span_duration",
      "MetricUnit": "ms",
      "MetricDescription": "Duration of application spans",
      "ActivitySources": {
        "MySampleApp": true
      }
    }
  }
}
```

#### After (v3.7):

```json
{
  "Diginsight": {
    "Activities": {
      "LogBehavior": "Show",
      "LogLevel": "Debug",
      "RecordSpanDuration": true,
      "SpanDurationMeterName": "MySampleApp",
      "SpanDurationMetricName": "diginsight.span_duration",
      "SpanDurationMetricDescription": "Duration of application spans",
      "ActivitySources": {
        "MySampleApp": true
      }
    }
  }
}
```

#### 3.2 Find and Replace in appsettings.json

Apply these replacements in ALL `appsettings.json` and `appsettings.Development.json` files:

| Old Property | New Property |
|-------------|--------------|
| `"RecordSpanDurations"` | `"RecordSpanDuration"` |
| `"MeterName"` | `"SpanDurationMeterName"` |
| `"MetricName"` | `"SpanDurationMetricName"` |
| `"MetricDescription"` | `"SpanDurationMetricDescription"` |
| `"ActivityLogLevel"` | `"LogLevel"` (optional, backward compatible) |

**Remove these lines if present:**
- `"MetricUnit": "ms"` or any other unit value

#### 3.3 Verify JSON Syntax

After making changes, ensure JSON is valid:
- No trailing commas
- Proper bracket matching
- Correct quotation marks

---

### Step 4: Update C# Code Files

#### 4.1 Update Service Registrations (ObservabilityExtensions.cs or Startup.cs)

**CRITICAL:** Replace deprecated interfaces and classes with new names.

#### Before:

```csharp
// OLD - Do NOT use
services.TryAddSingleton<IActivityLoggingSampler, NameBasedActivityLoggingSampler>();
```

#### After:

```csharp
// NEW - v3.7 compatible
services.TryAddSingleton<IActivityLoggingFilter, OptionsBasedActivityLoggingFilter>();
```

#### 4.2 Update Programmatic Configuration (services.Configure)

If you configure `DiginsightActivitiesOptions` programmatically, update property names:

**Before:**

```csharp
services.Configure<DiginsightActivitiesOptions>(
    dao =>
    {
        dao.LogBehavior = LogBehavior.Show;
        dao.RecordSpanDurations = true;           // Plural
        dao.MeterName = assemblyName;              // No prefix
        dao.MetricName = "diginsight.span_duration";
        dao.MetricDescription = "Duration of application spans";
        dao.MetricUnit = "ms";                     // Removed property
    }
);
```

**After:**

```csharp
services.Configure<DiginsightActivitiesOptions>(
    dao =>
    {
        dao.LogBehavior = LogBehavior.Show;
        dao.RecordSpanDuration = true;             // Singular
        dao.SpanDurationMeterName = assemblyName;  // SpanDuration prefix
        dao.SpanDurationMetricName = "diginsight.span_duration";
        dao.SpanDurationMetricDescription = "Duration of application spans";
        // dao.MetricUnit removed - no longer used
    }
);
```

**Property Mapping Table:**

| Old Property | New Property |
|--------------|--------------|
| `dao.RecordSpanDurations` | `dao.RecordSpanDuration` *(singular)* |
| `dao.MeterName` | `dao.SpanDurationMeterName` |
| `dao.MetricName` | `dao.SpanDurationMetricName` |
| `dao.MetricDescription` | `dao.SpanDurationMetricDescription` |
| `dao.MetricUnit` | *(removed - delete this line)* |

#### 4.3 Update Multi-Metric Configuration (Advanced)

If you configure filters and enrichers for **multiple metrics** using named options:

**Before:**

```csharp
var metricNames = new[] { "diginsight.span_duration", "diginsight.request_size", "diginsight.response_size" };
foreach (var metricName in metricNames)
{
    // Configure filter options per metric
    services.Configure<MetricRecordingNameBasedFilterOptions>(metricName, options =>
    {
        options.MetricName = metricName;

        var activitiesToUse = new Dictionary<string, bool>(defaultMetricActivities);
        var metricConfig = metricSpecificActivities?.FirstOrDefault(m => m.MetricName == options.MetricName);
        if (metricConfig != null) { activitiesToUse.AddRange(metricConfig.ActivityNames); }
        options.ActivityNames = activitiesToUse;
    });
    
    // Configure enricher options per metric
    services.Configure<MetricRecordingEnricherOptions>(metricName, options =>
    {
        options.MetricName = metricName;

        var tagsToUse = new List<string>(defaultMetricTags);
        var metricConfig = metricSpecificTags?.FirstOrDefault(m => m.MetricName == options.MetricName);
        if (metricConfig != null) { tagsToUse.AddRange(metricConfig.MetricTags); }
        options.MetricTags = tagsToUse;
    });
    
    // Register named filter per metric
    services.AddNamedSingleton<IMetricRecordingFilter, MetricRecordingNameBasedFilter>(
        metricName, (sp, key) =>
        {
            var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<MetricRecordingNameBasedFilterOptions>>();
            var namedOptionsMonitor = new NamedOptionsMonitor<MetricRecordingNameBasedFilterOptions>(optionsMonitor, (string)key!);

            var filter = new MetricRecordingNameBasedFilter(namedOptionsMonitor);
            return filter;
        }
    );
    
    // Register named enricher per metric
    services.AddNamedSingleton<IMetricRecordingEnricher, MetricRecordingTagsEnricher>(metricName, (sp, key) =>
    {
        var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<MetricRecordingEnricherOptions>>();
        var namedOptionsMonitor = new NamedOptionsMonitor<MetricRecordingEnricherOptions>(optionsMonitor, (string)key!);

        var enricher = new MetricRecordingTagsEnricher(namedOptionsMonitor);
        return enricher;
    });
}
```

**After:**

```csharp
var metricNames = new[] { "diginsight.span_duration", "diginsight.request_size", "diginsight.response_size" };
foreach (var metricName in metricNames)
{
    // Configure filter options per metric
    services.Configure<OptionsBasedMetricRecordingFilterOptions>(metricName, options =>
    {
        options.MetricName = metricName;

        var activitiesToUse = new Dictionary<string, bool>(defaultMetricActivities);
        var metricConfig = metricSpecificActivities?.FirstOrDefault(m => m.MetricName == options.MetricName);
        if (metricConfig != null) { activitiesToUse.AddRange(metricConfig.ActivityNames); }
        options.ActivityNames = activitiesToUse;
    });
    
    // Configure enricher options per metric
    services.Configure<OptionsBasedMetricRecordingEnricherOptions>(metricName, options =>
    {
        options.MetricName = metricName;

        var tagsToUse = new List<string>(defaultMetricTags);
        var metricConfig = metricSpecificTags?.FirstOrDefault(m => m.MetricName == options.MetricName);
        if (metricConfig != null) { tagsToUse.AddRange(metricConfig.MetricTags); }
        options.MetricTags = tagsToUse;
    });
    
    // Register named filter per metric
    services.AddNamedSingleton<IMetricRecordingFilter, OptionsBasedMetricRecordingFilter>(
        metricName, (sp, key) =>
        {
            var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<OptionsBasedMetricRecordingFilterOptions>>();
            var namedOptionsMonitor = new NamedOptionsMonitor<OptionsBasedMetricRecordingFilterOptions>(optionsMonitor, (string)key!);

            var filter = new OptionsBasedMetricRecordingFilter(namedOptionsMonitor);
            return filter;
        }
    );
    
    // Register named enricher per metric
    services.AddNamedSingleton<IMetricRecordingEnricher, OptionsBasedMetricRecordingEnricher>(metricName, (sp, key) =>
    {
        var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<OptionsBasedMetricRecordingEnricherOptions>>();
        var namedOptionsMonitor = new NamedOptionsMonitor<OptionsBasedMetricRecordingEnricherOptions>(optionsMonitor, (string)key!);

        var enricher = new OptionsBasedMetricRecordingEnricher(namedOptionsMonitor);
        return enricher;
    });
}
```

**Class Name Changes:**

| Old Class | New Class |
|-----------|-----------|
| `MetricRecordingNameBasedFilterOptions` | `OptionsBasedMetricRecordingFilterOptions` |
| `MetricRecordingNameBasedFilter` | `OptionsBasedMetricRecordingFilter` |
| `MetricRecordingEnricherOptions` | `OptionsBasedMetricRecordingEnricherOptions` |
| `MetricRecordingTagsEnricher` | `OptionsBasedMetricRecordingEnricher` |

**Note:** The internal logic remains unchanged - only the class names follow the new `OptionsBased` naming convention.

#### 4.4 Update All Interface References

Search for and replace throughout your codebase:

| Old Interface/Class | New Interface/Class |
|---------------------|---------------------|
| `IActivityLoggingSampler` | `IActivityLoggingFilter` |
| `NameBasedActivityLoggingSampler` | `OptionsBasedActivityLoggingFilter` |
| `NameBasedMetricRecordingFilter` | `OptionsBasedMetricRecordingFilter` |
| `NameBasedMetricRecordingFilterOptions` | `OptionsBasedMetricRecordingFilterOptions` |
| `DefaultMetricRecordingEnricher` | `OptionsBasedMetricRecordingEnricher` |
| `MetricRecordingTagsEnricher` | `OptionsBasedMetricRecordingEnricher` |
| `MetricRecordingEnricherOptions` | `OptionsBasedMetricRecordingEnricherOptions` |
| `IDiginsightActivitiesMetricOptions` | `IMetricRecordingOptions` |

#### 4.5 Update Custom Implementations (if any)

If you have custom activity logging filters:

**Before:**

```csharp
public class MyCustomSampler : IActivityLoggingSampler
{
    public LogBehavior? GetLogBehavior(Activity activity)
    {
        // Your logic
    }
}
```

**After:**

```csharp
public class MyCustomFilter : IActivityLoggingFilter
{
    public LogBehavior? GetLogBehavior(Activity activity)
    {
        // Your logic (unchanged)
    }
}
```

#### 4.5 Update Custom Metric Enrichers (if any)

If you have custom metric enrichers, update the signature:

**Before:**

```csharp
public class MyEnricher : IMetricRecordingEnricher
{
    public void Enrich(Activity activity, IDictionary<string, object?> tags)
    {
        tags["custom"] = "value";
    }
}
```

**After:**

```csharp
public class MyEnricher : IMetricRecordingEnricher
{
    public void Enrich(Activity activity, TagList tags)
    {
        tags.Add("custom", "value");
    }
}
```

#### 4.6 Update Options Access

If you're injecting options interfaces:

**Before:**

```csharp
public MyService(IDiginsightActivitiesMetricOptions metricOptions)
{
    bool record = metricOptions.RecordSpanDurations;
}
```

**After:**

```csharp
public MyService(IMetricRecordingOptions metricOptions)
{
    bool record = metricOptions.Record;
}
```

#### 4.7 Update Property Access

**Before:**

```csharp
var level = logOptions.ActivityLogLevel;
```

**After:**

```csharp
var level = logOptions.LogLevel;
```

#### 4.8 Remove ObservabilityRegistry References (if any)

**Before:**

```csharp
var loggerFactory = ObservabilityRegistry.LoggerFactory;
```

**After:**

```csharp
var loggerFactory = LoggerFactoryStaticAccessor.LoggerFactory;
```

---

### Step 5: Build and Test

#### 5.1 Clean and Rebuild

```bash
# Clean the solution
dotnet clean

# Rebuild
dotnet build
```

#### 5.2 Check for Compilation Errors

Address any compilation errors related to:
- Missing interface references
- Deprecated class names
- Missing properties

#### 5.3 Run Unit Tests

```bash
dotnet test
```

#### 5.4 Run the Application

```bash
dotnet run
```

#### 5.5 Verify Logging Output

Check that:
- ✅ Activities are being logged correctly
- ✅ Metrics are being recorded (if enabled)
- ✅ Wildcard patterns in `ActivitySources` work correctly
- ✅ No deprecation warnings appear

---

## Sample-Specific Guidance

### Console Applications (C01_00, C01_01, C01_02)

1. Update `appsettings.json` following Step 3
2. Update `Observability/ObservabilityExtensions.cs` following Step 4.1
3. Verify `Program.cs` uses correct service setup

**Key File Locations:**
- `Observability/ObservabilityExtensions.cs`
- `appsettings.json`
- `appsettings.Development.json`

### WPF Applications (C01_00, C01_01, C05_00, C10_00)

1. Update `appsettings.json` following Step 3
2. Update `Observability/ObservabilityExtensions.cs` following Step 4.1
3. Check `App.xaml.cs` for any custom service registrations

**Key File Locations:**
- `Observability/ObservabilityExtensions.cs`
- `appsettings.json`
- `App.xaml.cs`

### Web API Applications (S01_00, S01_01, S01_02, S02_00)

1. Update `appsettings.json` following Step 3
2. Update `Observability/ObservabilityExtensions.cs` OR `Startup.cs` following Step 4.1
3. Update `Program.cs` if services are registered there
4. **IMPORTANT:** If using HTTP header filters, update to `HttpHeadersSpanDurationMetricRecordingFilter`

**Key File Locations:**
- `Observability/ObservabilityExtensions.cs`
- `Program.cs` (for .NET 6+ minimal APIs)
- `Startup.cs` (for .NET 5 and earlier)
- `appsettings.json`

### ASP.NET Applications (S05_00, S05_01)

1. Update `web.config` or `appsettings.json` following Step 3
2. Update `Global.asax.cs` or equivalent startup code following Step 4.1
3. Verify older framework compatibility

---

## Troubleshooting

### Issue: Build fails with "IActivityLoggingSampler not found"

**Solution:** Replace with `IActivityLoggingFilter` - see [Step 4.2](#42-update-all-interface-references)

---

### Issue: Runtime error "RecordSpanDurations property not found"

**Solution:** Configuration property has been renamed to `RecordSpanDuration` (singular) - see [Step 3.2](#32-find-and-replace-in-appsettingsjson)

---

### Issue: Activity filtering not working with wildcards

**Solution:** This was a critical bug fixed in v3.7. Ensure you've updated to at least v3.7.0.0 - see [Step 2](#step-2-update-nuget-packages)

---

### Issue: Custom metric enricher throws exception

**Solution:** Update enricher signature to use `TagList` instead of `IDictionary<string, object?>` - see [Step 4.4](#44-update-custom-metric-enrichers-if-any)

---

### Issue: Deprecation warnings still appearing

**Solution:** Verify all replacements have been made:
```bash
# Search for deprecated terms
grep -r "IActivityLoggingSampler" .
grep -r "NameBasedActivityLoggingSampler" .
grep -r "RecordSpanDurations" .
```

---

### Issue: Configuration not loading correctly

**Solution:** 
1. Verify JSON syntax is valid
2. Check that property names match exactly (case-sensitive)
3. Ensure no trailing commas or syntax errors

---

## Verification Checklist

Use this checklist to verify your upgrade is complete:

- [ ] **NuGet Packages**
  - [ ] All Diginsight packages updated to 3.7.x
  - [ ] `dotnet restore` runs successfully
  
- [ ] **Configuration Files**
  - [ ] `RecordSpanDurations` → `RecordSpanDuration`
  - [ ] `MeterName` → `SpanDurationMeterName`
  - [ ] `MetricName` → `SpanDurationMetricName`
  - [ ] `MetricDescription` → `SpanDurationMetricDescription`
  - [ ] `MetricUnit` removed (if present)
  - [ ] All `appsettings.json` files updated
  - [ ] All `appsettings.Development.json` files updated
  
- [ ] **C# Code**
  - [ ] `IActivityLoggingSampler` → `IActivityLoggingFilter`
  - [ ] `NameBasedActivityLoggingSampler` → `OptionsBasedActivityLoggingFilter`
  - [ ] All service registrations updated
  - [ ] Custom implementations updated (if any)
  - [ ] No compilation errors
  
- [ ] **Testing**
  - [ ] Project builds successfully
  - [ ] Unit tests pass
  - [ ] Application runs without errors
  - [ ] Logging output is correct
  - [ ] Metrics are recorded (if enabled)
  - [ ] Wildcard patterns work correctly
  - [ ] No deprecation warnings
  
- [ ] **Documentation**
  - [ ] README updated (if needed)
  - [ ] Comments updated for changed code
  
- [ ] **Cleanup**
  - [ ] No obsolete interfaces/classes referenced
  - [ ] No unused using statements
  - [ ] Code formatted consistently

---

## Resources

### Official Documentation
- [Diginsight v3.7 Release Changelog](https://diginsight.github.io/telemetry/src/docs/10.%20ChangeLog/20251112%20-%20changes%20for%20release%203.7.html)
- [Diginsight Documentation](https://diginsight.github.io/telemetry/src/README.md)
- [Configuration Guide](https://diginsight.github.io/telemetry/src/docs/01.%20Concepts/01.00%20-%20Configure%20diginsight%20telemetry%20to%20the%20local%20text%20based%20streams.html)
- [OpenTelemetry Integration](https://diginsight.github.io/telemetry/src/docs/01.%20Concepts/02.00%20-%20HowTo%20-%20configure%20diginsight%20telemetry%20to%20the%20remote%20tools.html)

### GitHub
- [GitHub Repository](https://github.com/diginsight/telemetry)
- [Release Notes v3.7.0.0](https://github.com/diginsight/telemetry/releases/tag/v3.7.0.0)
- [Discussions Forum](https://github.com/diginsight/telemetry/discussions)
- [Report Issues](https://github.com/diginsight/telemetry/issues)

### Support
- Open an issue on [GitHub Issues](https://github.com/diginsight/telemetry/issues)
- Ask in [GitHub Discussions](https://github.com/diginsight/telemetry/discussions)
- Check existing issues for similar problems

---

## Quick Reference: Search & Replace Commands

For VS Code or Visual Studio, use these find/replace patterns:

### Configuration Files (appsettings*.json)

```regex
Find:    "RecordSpanDurations"
Replace: "RecordSpanDuration"

Find:    "MeterName"
Replace: "SpanDurationMeterName"

Find:    "MetricName"
Replace: "SpanDurationMetricName"

Find:    "MetricDescription"
Replace: "SpanDurationMetricDescription"

Find:    "MetricUnit".*,?\r?\n
Replace: (empty - remove the line)
```

### C# Code Files (*.cs)

```regex
Find:    IActivityLoggingSampler
Replace: IActivityLoggingFilter

Find:    NameBasedActivityLoggingSampler
Replace: OptionsBasedActivityLoggingFilter

Find:    NameBasedMetricRecordingFilter
Replace: OptionsBasedMetricRecordingFilter

Find:    MetricRecordingNameBasedFilterOptions
Replace: OptionsBasedMetricRecordingFilterOptions

Find:    MetricRecordingNameBasedFilter
Replace: OptionsBasedMetricRecordingFilter

Find:    MetricRecordingEnricherOptions
Replace: OptionsBasedMetricRecordingEnricherOptions

Find:    MetricRecordingTagsEnricher
Replace: OptionsBasedMetricRecordingEnricher

Find:    IDiginsightActivitiesMetricOptions
Replace: IMetricRecordingOptions

Find:    DefaultMetricRecordingEnricher
Replace: OptionsBasedMetricRecordingEnricher

Find:    ObservabilityRegistry
Replace: LoggerFactoryStaticAccessor

Find:    \.ActivityLogLevel
Replace: .LogLevel

Find:    \.RecordSpanDurations
Replace: .Record

Find:    \.MeterName
Replace: .SpanDurationMeterName

Find:    \.MetricName
Replace: .SpanDurationMetricName

Find:    \.MetricDescription
Replace: .SpanDurationMetricDescription

Find:    \.MetricUnit.*
Replace: (empty - remove the line)
```

---

## Automated Script (Optional)

For bulk updates across multiple samples, you can use this PowerShell script:

```powershell
# Save as Update-ToDiginsightV37.ps1

param(
    [Parameter(Mandatory=$true)]
    [string]$RootPath
)

Write-Host "Updating Diginsight configuration to v3.7..." -ForegroundColor Green

# Find all appsettings.json files
$configFiles = Get-ChildItem -Path $RootPath -Filter "appsettings*.json" -Recurse

foreach ($file in $configFiles) {
    Write-Host "Processing: $($file.FullName)" -ForegroundColor Yellow
    
    $content = Get-Content $file.FullName -Raw
    
    # Update configuration properties
    $content = $content -replace '"RecordSpanDurations"', '"RecordSpanDuration"'
    $content = $content -replace '"MeterName"(\s*:)', '"SpanDurationMeterName"$1'
    $content = $content -replace '"MetricName"(\s*:)', '"SpanDurationMetricName"$1'
    $content = $content -replace '"MetricDescription"(\s*:)', '"SpanDurationMetricDescription"$1'
    $content = $content -replace '(?m)^\s*"MetricUnit".*,?\r?\n', ''
    
    Set-Content -Path $file.FullName -Value $content
}

# Find all .cs files in Observability folders
$csFiles = Get-ChildItem -Path $RootPath -Filter "*.cs" -Recurse | 
           Where-Object { $_.FullName -like "*Observability*" }

foreach ($file in $csFiles) {
    Write-Host "Processing: $($file.FullName)" -ForegroundColor Yellow
    
    $content = Get-Content $file.FullName -Raw
    
    # Update C# code
    $content = $content -replace 'IActivityLoggingSampler', 'IActivityLoggingFilter'
    $content = $content -replace 'NameBasedActivityLoggingSampler', 'OptionsBasedActivityLoggingFilter'
    $content = $content -replace 'NameBasedMetricRecordingFilter', 'OptionsBasedMetricRecordingFilter'
    $content = $content -replace 'IDiginsightActivitiesMetricOptions', 'IMetricRecordingOptions'
    
    Set-Content -Path $file.FullName -Value $content
}

Write-Host "`nUpdate complete! Please review changes and test." -ForegroundColor Green
Write-Host "Remember to update NuGet packages to v3.7.0" -ForegroundColor Cyan
```

**Usage:**

```powershell
.\Update-ToDiginsightV37.ps1 -RootPath "E:\dev.darioa.live\Diginsight\telemetry.samples\src"
```

---

## Revision History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | January 5, 2026 | Initial comprehensive guide created |

---

## Credits

**Author:** Diginsight Team  
**Maintainer:** [Your Name]  
**Based on:** [Diginsight v3.7 Release Changelog](https://diginsight.github.io/telemetry/src/docs/10.%20ChangeLog/20251112%20-%20changes%20for%20release%203.7.html)

---

**Need Help?** If you encounter issues not covered in this guide, please:
1. Check the [Troubleshooting](#troubleshooting) section
2. Search [GitHub Issues](https://github.com/diginsight/telemetry/issues)
3. Ask in [GitHub Discussions](https://github.com/diginsight/telemetry/discussions)
4. Create a new issue with:
   - Your current version
   - Error messages
   - Configuration files
   - Code samples

---

**Happy Upgrading! 🚀**
