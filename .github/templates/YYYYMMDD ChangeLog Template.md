# Changes for Release [Version]

**Release Date:** [YYYYMMDD]  
**Commit Range:** `[start-commit]` → `[end-commit]`  
**Previous Version:** [Version]

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

[3-4 sentences maximum - neutral, factual tone]

Version [X.X.X] [consolidates|updates|changes] [area of focus]. This release includes [breaking|non-breaking] changes to [specific components].

**Key changes:**
- [Change 1 - brief, factual]
- [Change 2 - brief, factual]
- [Change 3 - brief, factual]
- [Change 4 - brief, factual]
- [Change 5 - brief, factual - max 5 bullets]

**Action required:** See [Changes Summary](#-changes-summary) and [Migration Guide](#-migration-guide).

---

## 🔄 Changes Summary

**Release:** [X.X.X]  
**Type:** [Breaking|Non-Breaking] Changes  
**Migration Effort:** [Estimated - e.g., "5-15 minutes for config-only users, 30 minutes for custom implementations"]

---

### 🔴 Breaking Changes

| Category | Old | New | Affected Users | Impact |
|----------|-----|-----|----------------|--------|
| Interface | `IOldName` | <mark>`INewName`</mark> | Custom implementations | High |
| Property | `OldProperty` | <mark>`NewProperty`</mark> | Config files | Medium |
| Class | `OldClass` | <mark>`NewClass`</mark> | Service registrations | Medium |

**Impact Legend:**
- **High**: Immediate action required - code will not compile
- **Medium**: Action required - runtime errors if not updated  
- **Low**: Optional - backward compatibility maintained

**Quick Migration Reference:**

> **Note:** In tables, new syntax is highlighted with <mark>highlighting</mark>. In code blocks, look for `← NEW` comment markers.

```json
// Configuration changes
{
  "OldProperty": "value",      // ❌ Remove
  "NewProperty": "value"       // ✅ NEW
}
```

```csharp
// Code changes
// OLD → NEW
IOldInterface → INewInterface  // ← NEW
OldClass → NewClass            // ← NEW
```

---

### ✨ What's New

**New Features:**

**[Feature name]** - [Brief, neutral description]
- **Use case:** [When to use - factual]
- **Example:**
  ```csharp
  // Usage example
  [Code]
  ```

**Improvements:**

- **[Area]**: [Description - no marketing language]
- **[Performance]**: [Metric if available - e.g., "30% faster"]
- **[Compatibility]**: [Framework/version support - factual]

---

### 🐛 Bug Fixes

**Critical:**

- **[Bug]**: [What was fixed - factual] - See [commit hash or appendix reference]

**Minor:**

- **[Bug]**: [What was fixed - brief]
- **[Bug]**: [What was fixed - brief]

---

## 🚀 Migration Guide

### Quick Check: Do You Need to Migrate?

**✅ No action needed if:**
- Using default configuration only
- No custom filter/enricher implementations
- No direct interface usage

**⚠️ Action required if:**

| Scenario | Old Code | New Code |
|----------|----------|----------|
| Custom implementations | `IOldInterface` | <mark>`INewInterface`</mark> |
| Configuration properties | `OldProperty` | <mark>`NewProperty`</mark> |
| Service registrations | `OldClass` | <mark>`NewClass`</mark> |
| Deleted interfaces | `IDeletedInterface` | Removed - see migration |

> Review each scenario above. If you use any "Old Code" pattern, migration is required.

---

### For Config-Only Users

**Estimated time:** 5-10 minutes

#### Step 1: Update Configuration Properties

```json
// OLD - Remove these
{
  "OldProperty": "value",          // ❌ Remove
  "AnotherOldProperty": "value"    // ❌ Remove
}

// NEW - Use these  
{
  "NewProperty": "value",          // ✅ NEW
  "AnotherNewProperty": "value"    // ✅ NEW
}
```

#### Step 2: Test Application

```bash
# Run application
dotnet run

# Verify configuration loads
# Check for warnings in logs
```

---

### For Advanced Users

**Estimated time:** 15-30 minutes

#### 1. Update Interface Implementations

```csharp
// OLD - Remove these
public class MyClass : IOldInterface  // ❌ Remove
{
    public void OldMethod() { }       // ❌ Remove
}

// NEW - Use these
public class MyClass : INewInterface  // ← NEW interface
{
    public void NewMethod() { }       // ← NEW method
}
```

#### 2. Update Service Registrations

```csharp
// OLD - Remove these
services.AddSingleton<IOldInterface, MyClass>();  // ❌ Remove

// NEW - Use these
services.AddSingleton<INewInterface, MyClass>();  // ← NEW interface
```

#### 3. Update Property Access

```csharp
// OLD - Remove these
var value = options.OldProperty;  // ❌ Remove

// NEW - Use these
var value = options.NewProperty;  // ← NEW property
```

---

### For New Features (Optional)

[Only if release includes optional new features]

#### Feature: [Name]

**When to use:** [Scenario description]

```csharp
// Example usage
[Code]
```

---

## ✅ Testing Recommendations

### 1. Configuration Loading

```csharp
// Verify config loads
[Test code]
```

### 2. Feature Testing

```csharp
// Test specific feature
[Test code]
```

---

## ☑️ Upgrade Checklist

- [ ] Review Breaking Changes
- [ ] Update NuGet packages to [Version]
- [ ] Update configuration files (see Migration Guide)
- [ ] Update interface implementations (if applicable)
- [ ] Update service registrations (if applicable)
- [ ] Run tests
- [ ] Verify application starts
- [ ] Verify functionality works as expected

---

## 📚 Resources

- [Documentation](link)
- [GitHub Release](link)
- [Discussions](link)

---

## 🙏 Acknowledgments

Contributors:
- [Name] - [Specific contributions]
- [Name] - [Specific contributions]

For questions: [GitHub repository link]

---

## 📖 Appendix A: Detailed Commit History

*Complete chronological analysis for users needing specific technical details.*

<details open>
<summary><b>Click to collapse/expand</b></summary>

### Commit 1: [hash] - [title] [BREAKING]

**Date:** [Date]  
**Author:** [Author]  
**Impact:** High/Medium/Low - [Who is affected]

**Changes:**
```csharp
// What changed (start vs end comparison)
```

**Reason:** [Why - technical justification only, no marketing language]

**Migration:** See [Migration Guide](#-migration-guide) section [specific subsection]

---

### Commit 2: [hash] - [title]

**Date:** [Date]  
**Author:** [Author]  
**Impact:** None - Internal change

**Changes:** [Brief description]

**Reason:** [Technical justification]

---

### Commit 3: [hash] - [title]

**Date:** [Date]  
**Author:** [Author]  
**Impact:** Low - [Specific improvement]

**Changes:** [Description]

---

[Continue for all commits - use condensed format for non-breaking changes]

</details>

---

**Document Version:** 1.0  
**Last Updated:** [Date]

