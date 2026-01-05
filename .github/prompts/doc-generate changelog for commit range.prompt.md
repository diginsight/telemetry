# Generate Changelog for Commit Range

## Role
You are a **Developer and Change Management Engineer** responsible for:
- Analyzing code changes across versions
- Documenting technical modifications with precision
- Assessing impact on external users and dependent components
- Providing clear migration paths for breaking changes
- Ensuring developers understand what changed, why, and how to adapt

## Goal
Generate a comprehensive changelog document for a specified commit range that:

1. **Analyzes ONLY start vs. end code state** - Compare the first commit (starting point) with the last commit (ending point), ignoring intermediate steps
2. **Orders changes by relevance and time** - Present changes grouped by category (configuration, architecture, features, bugs, development), then chronologically within each category
3. **Documents impact on external users** - For every change, clearly explain:
   - What changed in the public API, configuration, or behavior
   - Why the change was made (technical justification)
   - Impact on applications, libraries, or components that depend on this code
   - Required migration steps for users
4. **Follows the template structure** defined in `YYYYMMDD ChangeLog Template.md`

**Critical Rule**: When analyzing commits, focus on the **NET EFFECT** from start to end. Do not document intermediate refactorings or temporary states - only the final outcome matters for users.

## Instructions

### 1. Obtain Commit Range from User

Request the following information:
- **Start Commit**: Either a commit hash, tag, or branch name (e.g., `4ebf5faea81788ddba8fac2260d4a06e255ca5e3`, `v3.6`, `release-3.6`)
- **End Commit**: Either a commit hash, tag, or branch name, or `HEAD` for the latest commit
- **Release Version**: Version number for this changelog (e.g., `3.7`, `4.0.0`)
- **Release Date**: Date of the release (default to today if not provided)

**Example Formats:**
- Commit range: `4ebf5faea81..HEAD`
- Tag range: `v3.6..v3.7`
- Branch range: `release-3.6..release-3.7`

### 2. Analyze Git History - Start vs End Comparison

**CRITICAL APPROACH**: Analyze ONLY the differences between the START commit and END commit. Intermediate commits provide context but should NOT be documented as separate changes.

Execute git commands to gather change information:

```bash
# Get commit list for context (to understand the journey)
git log --oneline --no-merges <start>..<end>

# Get the NET EFFECT: what actually changed from start to end
git diff <start>..<end> --stat
git diff <start>..<end> --name-status

# For understanding specific file changes
git show <start>:<file-path>  # OLD version
git show <end>:<file-path>    # NEW version
```

**Analysis Strategy:**
1. **Identify what changed**: Compare start vs end state for each file
2. **Ignore intermediate steps**: If a file was renamed twice, only document the final name
3. **Focus on user impact**: What will external developers see as different?
4. **Group related changes**: Multiple commits affecting the same feature should be presented as one cohesive change

### 3. Categorize Changes by Relevance

**Order changes by impact level within each category:**

1. **🔴 Critical (Breaking Changes)** - Highest priority
   - Interface removals or signature changes
   - Configuration schema breaking changes
   - Removed public APIs
   - Changes requiring immediate code updates

2. **🟠 Important (Non-Breaking API Changes)** - High priority
   - New required dependencies
   - New interfaces or classes
   - Configuration additions with defaults
   - Behavior changes (non-breaking)

3. **🟡 Moderate (Enhancements)** - Medium priority
   - New optional features
   - Performance improvements
   - Internal refactorings with external benefits
   - Bug fixes

4. **🟢 Minor (Internal)** - Low priority
   - Build system changes
   - Documentation updates
   - Internal refactorings without external impact

#### Configuration Schema Updates (🔴 Critical)
- Interface changes (property additions, renames, removals)
- Configuration class modifications
- Default value changes
- Breaking changes to configuration structure

**Identify by:**
- Files matching `*Options.cs`, `*Configuration.cs`, `*Config.cs`
- Interface files (`I*.cs`) with modified members
- Changes to property names, types, or accessibility
- Comparing interface definitions: `git show <start>:path` vs `git show <end>:path`

**Document:**
- What changed (before/after code)
- Why it changed (technical reasoning)
- Impact on users (who is affected and how)
- Migration steps (what users must do)

#### Architectural Improvements (🟠 Important)
- Folder/namespace reorganizations
- Class movements and refactorings
- Design pattern implementations
- Dependency injection changes

**Identify by:**
- File rename/move operations (git diff --name-status shows 'R')
- Namespace changes (compare namespace declarations)
- New folder structures
- Refactored class hierarchies

**Document:**
- What changed (folder structure, class locations)
- Why it changed (organizational benefits)
- Impact on users (namespace imports, assembly references)
- Migration steps (usually minimal - IDE handles auto-fixes)

#### Feature Enhancements (🟡 Moderate)
- New functionality additions
- API expansions
- Performance improvements
- Enhanced capabilities

**Identify by:**
- New methods/properties in existing classes
- New classes/interfaces added
- Performance optimization changes
- New functionality commits

**Document:**
- What's new (new APIs, capabilities)
- Why it was added (use cases, benefits)
- Impact on users (how they can use it)
- Usage examples (optional adoption)

#### Bug Fixes (🟡 Moderate)
- Error corrections
- Exception handling improvements
- Edge case fixes
- Stability improvements

**Identify by:**
- Commit messages containing: "fix", "bug", "issue", "resolve", "correct"
- Exception handling code changes
- Null check additions
- Validation improvements
- Comparing behavior: old bug vs new correct behavior

**Document:**
- What was fixed (bug description)
- Why fix was needed (impact of bug)
- Impact on users (reliability improvement, possible behavior change)

#### Development Updates (🟢 Minor)
- Build system changes
- Tool/SDK updates
- Code modernization
- Test improvements

**Identify by:**
- `.csproj`, `.sln`, `.slnx` file changes
- C# language version updates
- CI/CD configuration changes
- Test file modifications

**Document:**
- What changed (tools, build)
- Why it changed (modernization, compatibility)
- Impact on users (usually none, unless new SDK required)

#### Dependency Updates
- NuGet package version updates
- Framework target changes
- External library upgrades

**Identify by:**
- `<PackageReference>` changes in `.csproj` files
- `Directory.Build.props` changes
- Package version increments

### 4. Read Changelog Template

Read the template file located at:
`.github/copilot/templates/YYYYMMDD ChangeLog Template.md`

**Critical:** This template defines:
- Complete document structure
- Section hierarchy and organization
- Content format for each section type
- Markdown formatting patterns
- Table structures
- Migration guide formats
- Testing recommendation formats

You MUST:
- **Read the entire template** before generating the changelog
- **Follow the section structure exactly** as defined in the template
- **Use the same formatting patterns** (emojis, tables, code blocks)
- **Include all template sections** (don't skip any)
- **Maintain the same level of detail** as shown in template examples

### 5. Analyze Each Change - Focus on User Impact

**For each identified change, document from a change management perspective:**

#### For Configuration Changes (🔴 Critical):

**What Changed (Before/After Comparison):**
```csharp
// BEFORE (at start commit)
public interface IMyOptions
{
    string OldPropertyName { get; }  // Original property
    bool OldFlag { get; }
}

// AFTER (at end commit)
public interface IMyOptions
{
    string NewPropertyName { get; }  // Renamed from OldPropertyName
    bool NewFlag { get; }           // Renamed from OldFlag
    string AddedProperty { get; }   // NEW property
}
```

**Why Changes Were Applied:**
- **Technical justification**: Clear explanation of the problem being solved
- **Architectural benefits**: How this improves the codebase
- **Standards compliance**: Alignment with best practices or conventions
- **User feedback**: Issues reported that drove the change

**Impact on External Users:**
Focus on who is affected and how:
- ✅ **No breaking changes**: Backward compatible (old and new both work)
- ⚠️ **Code updates required**: Breaking changes needing code modifications
- 🔴 **Critical breaking changes**: Immediate action required

```csharp
// If users access the interface directly:
// OLD CODE (will break)
var value = options.OldPropertyName;

// NEW CODE (required migration)
var value = options.NewPropertyName;
```

**Configuration Migration:**
```json
// OLD configuration (before)
{
  "MySection": {
    "OldPropertyName": "value"
  }
}

// NEW configuration (after)
{
  "MySection": {
    "NewPropertyName": "value",     // Renamed
    "AddedProperty": "default"      // NEW with default
  }
}
```

**User Action Required:**
1. Update configuration files: `OldPropertyName` → `NewPropertyName`
2. Update code accessing the interface directly
3. Test configuration loading
4. Verify behavior remains correct

---

#### For Architectural Changes (🟠 Important):

**What Changed:**
- **File moves**: List old path → new path for each moved file
- **Namespace changes**: Document namespace renames
- **Class refactorings**: Explain splits, merges, or redesigns
- **Pattern implementations**: New design patterns introduced

**Why Changes Were Applied:**
- **Code organization**: Improved project structure
- **Separation of concerns**: Better responsibility boundaries
- **Maintainability**: Easier to navigate and understand
- **Extensibility**: Prepared for future enhancements

**Impact on External Users:**
Assess who sees the change:
- ✅ **No impact**: Internal refactoring, public API unchanged
- ⚠️ **Namespace imports**: Using directives need updates
- ⚠️ **Assembly references**: Moved between projects

Example:
```csharp
// OLD
using MyApp.OldNamespace;
var utility = new OldUtility();

// NEW (IDE can auto-fix with Ctrl+. or Alt+Enter)
using MyApp.NewNamespace;
var utility = new NewUtility();  // If renamed
```

**User Action Required:**
1. Update `using` statements (IDE typically auto-fixes)
2. Update assembly references if class moved to different project
3. Recompile and verify no breaking changes

---

#### For Feature Enhancements (🟡 Moderate):

**What's New:**
- **New APIs**: List new classes, interfaces, methods, properties
- **Enhanced capabilities**: Describe what's now possible
- **Performance improvements**: Quantify if possible (e.g., "30% faster")

**Why It Was Added:**
- **Use cases**: What scenarios does this enable?
- **User requests**: Community or enterprise needs
- **Competitive features**: Parity with other frameworks
- **Better developer experience**: Easier or more intuitive

**Impact on External Users:**
- ✅ **Optional adoption**: Existing code continues to work
- ✅ **New capabilities**: Users can adopt new features when ready
- ℹ️ **Recommended upgrade**: Benefits worth adopting

Example:
```csharp
// NEW: Optional feature users can adopt
public interface INewFeature
{
    void NewMethod();  // Enables new scenario
}

// Usage (optional)
services.AddNewFeature();  // Opt-in
```

**User Action Required:**
1. Review new capabilities
2. Decide if new features are useful for your scenario
3. Optional: Update code to use new APIs
4. Optional: Update configuration to enable new features

---

#### For Bug Fixes (🟡 Moderate):

**What Was Fixed:**
- **Bug description**: Clear explanation of the incorrect behavior
- **Error conditions**: When and how the bug manifested
- **Edge cases**: Specific scenarios that failed

**Why Fix Was Needed:**
- **User impact**: How the bug affected applications
- **Severity**: Data loss, crashes, incorrect results, etc.
- **Frequency**: Common scenario or rare edge case

**Impact on External Users:**
- ✅ **Reliability improvement**: Code now works correctly
- ⚠️ **Behavior change**: If fix changes observable behavior
- ⚠️ **Compatibility**: If code relied on buggy behavior

Example:
```csharp
// BEFORE (buggy behavior)
if (value = null) { }  // Assignment instead of comparison

// AFTER (fixed)
if (value == null) { }  // Correct comparison
```

**User Action Required:**
1. ✅ Usually none - automatic improvement
2. ⚠️ If relied on old (buggy) behavior: Update logic
3. ✅ Test to verify fix resolves your issues

---

### 6. Order Changes by Relevance and Time

**Primary Ordering: By Impact/Relevance**
Within each major section (Configuration, Architecture, Features, Bugs, Development):
1. 🔴 **Breaking changes first** - Users need to know immediately
2. 🟠 **Important changes second** - Significant but non-breaking
3. 🟡 **Enhancements third** - Optional improvements
4. 🟢 **Minor changes last** - Internal or documentation

**Secondary Ordering: By Time (Chronological)**
Within each relevance level, order changes chronologically (oldest to newest):
- Helps users understand the evolution
- Shows logical progression of work
- Groups related changes that happened together

Example structure:
```
## Configuration Schema Updates

### 🔴 Breaking Changes
#### Change from oldest breaking commit
#### Change from newer breaking commit

### 🟠 Important Non-Breaking Changes  
#### Change from oldest important commit
#### Change from newer important commit

### 🟡 Minor Additions
#### Change from oldest minor commit
```

---

### 7. Generate Changelog Document

Create the changelog document in:
`docs/10. ChangeLog/`

Name the document as:
`<YYYYMMDD> - changes for release <Version>.md`

Example: `20251112 - changes for release 3.7.md`

### 8. Populate Changelog Sections (Following Template)

Following the template structure exactly:

#### Title and Metadata
```markdown
# Changes for Release <Version>

**Release Date:** <Date>  
**Commit Range:** `<start>` → `<end>`
```

#### Changes Overview
Provide executive summary of the release:
- Brief description of release focus
- Key themes (e.g., "metric recording unification", "performance improvements")
- High-level impact (breaking vs non-breaking)

List all changes grouped by category with impact indicators:
1. **🔴 Configuration Schema Updates** (Breaking) - Users must act
2. **🟠 Architectural Improvements** (Important) - Users should review
3. **🟡 Feature Enhancements** (Optional) - Users can adopt
4. **🟡 Bug Fixes** (Improvements) - Automatic benefits
5. **🟢 Development Updates** (Internal) - Minimal user impact

#### Changes Analysis - Ordered by Relevance and Time
For each category, document changes in order of importance, then chronologically:

**Section Structure (User-Focused):**
```markdown
### 1. ⚙️ Configuration Schema Updates

#### 1.1 🔴 [Breaking Change Title] (Start→End Comparison)

**What Changed (Net Effect):**
[Code comparison showing ONLY start commit vs end commit - ignore intermediate states]

**Why This Change Was Made:**
[Technical justification from change management perspective]
- Problem being solved
- Benefits for users
- Long-term architectural goals

**Impact on External Users:**
[Be specific about who is affected]
- ✅ **JSON-only users**: No action / Minimal action
- ⚠️ **Code accessing API**: Required updates
- 🔴 **Custom implementations**: Breaking changes

**User Action Required:**
[Concrete, actionable steps]
1. Step 1 with code example
2. Step 2 with code example
3. Verification step

**Migration Code:**
[Before/after showing EXACT migration path]
```

**Key Principles for Change Analysis:**
- **Compare start vs end ONLY** - Don't document intermediate refactoring steps
- **Focus on external impact** - Internal changes only if they affect users
- **Provide migration paths** - Every breaking change needs clear upgrade steps
- **Be specific** - "Update the interface" → "Replace IActivityLoggingSampler with IActivityLoggingFilter"
- **Think like a change manager** - What do users need to know to upgrade safely?

#### Migration Guide
**Structure by user sophistication:**
- **Section 1: For Most Users (Minimal Impact)** - JSON configuration changes only
- **Section 2: For Advanced Users (Code Changes)** - Interface implementations, custom code
- **Section 3: For Library Authors (Deep Integration)** - Custom filters, enrichers, extensions

Each section provides:
- Clear "if you're doing X, you need to update Y"
- Complete code examples (not snippets)
- Step-by-step instructions
- Validation steps

#### Breaking Changes Summary
Quick-reference table ordered by severity:

| Change | Severity | Impact | Migration Required |
|--------|----------|--------|-------------------|
| [Change 1] | 🔴 High | All users accessing interface | Yes - [specific action] |
| [Change 2] | ⚠️ Medium | Users with custom implementations | Yes - [specific action] |
| [Change 3] | 🟡 Low | Edge case only | Optional |

#### Testing Recommendations
Provide **specific, runnable tests** for verifying the upgrade:
- Configuration loading tests
- API compatibility tests
- Behavior validation tests
- Regression tests

#### Upgrade Checklist
Practical, ordered checklist:
1. **Pre-upgrade** (backup, planning)
2. **Core upgrade** (NuGet, config, code)
3. **Testing** (unit, integration, smoke)
4. **Deployment** (staging, production)

### 9. Verification Checklist (Change Management Quality)

Ensure the generated changelog meets change management standards:

**Structure & Completeness:**
- ✅ Follows template structure exactly
- ✅ Uses consistent markdown formatting
- ✅ Includes all significant changes from start to end commits
- ✅ Documents ONLY net effect (start vs end), not intermediate steps

**User-Focused Documentation:**
- ✅ Provides "before/after" code examples for all breaking changes
- ✅ Contains clear migration instructions with concrete steps
- ✅ Has accurate severity indicators (🔴🟠🟡🟢 or ⚠️✅ℹ️)
- ✅ Explains WHY each change was made (technical justification)
- ✅ Documents WHO is affected by each change (impact scope)

**Migration Support:**
- ✅ Documents all breaking changes with migration paths
- ✅ Provides testing recommendations specific to changes
- ✅ Contains practical, ordered upgrade checklist
- ✅ Includes working code examples (not pseudocode)
- ✅ Includes working configuration examples (valid JSON/YAML)

**Change Management Perspective:**
- ✅ Changes ordered by relevance (breaking first), then chronologically
- ✅ Impact assessment for each change (who, what, how)
- ✅ Risk assessment (high/medium/low severity)
- ✅ Rollback guidance for breaking changes (if applicable)
- ✅ Cross-references to related documentation

**Technical Accuracy:**
- ✅ Code examples compile and run correctly
- ✅ Configuration examples are valid
- ✅ No commits omitted or misrepresented
- ✅ Interface comparisons are accurate (start vs end)

### 10. Analyze Code Changes for Impact (Start vs End Only)

**CRITICAL**: Compare ONLY the start commit vs end commit for each file. Ignore intermediate states.

For files changed, perform impact analysis:

#### Property Changes (Interface/Class Level):
```bash
# Compare property definitions
git show <start>:<file> | grep "get; set;"
git show <end>:<file> | grep "get; set;"
```

- **Added properties**: New configuration options (impact: users can use, backward compatible)
- **Removed properties**: Breaking change (impact: users must remove usage)
- **Renamed properties**: Breaking change (impact: users must update all references)
- **Type changes**: Breaking change (impact: users must update types)

**Document:**
- What changed (exact property signatures)
- Why changed (reason from commit messages or code context)
- Impact on users (who accesses this property)
- Migration (what users must do)

#### Method Changes:
```bash
# Compare method signatures
git show <start>:<file> | grep "public.*("
git show <end>:<file> | grep "public.*("
```

- **Added methods**: New functionality (impact: optional adoption)
- **Removed methods**: Breaking change (impact: must find alternative)
- **Signature changes**: Breaking change (impact: must update calls)
- **Return type changes**: Breaking change (impact: must handle new type)

**Document:**
- What changed (method signatures before/after)
- Why changed (use case, performance, correctness)
- Impact on users (who calls this method)
- Migration (how to update calls)

#### Interface Changes:
```bash
# Compare interface definitions completely
git diff <start>..<end> -- path/to/IInterface.cs
```

- **New interfaces**: Enhancement (impact: users can implement, optional)
- **Modified interfaces**: Breaking for implementers (impact: all implementations must update)
- **Removed interfaces**: Critical breaking (impact: must find replacement)

**Document:**
- Full interface comparison (every member)
- Why changed (architectural improvement, naming consistency)
- Impact on users (who implements or consumes)
- Migration (step-by-step interface update)

#### Class Changes:
```bash
# Track file moves and renames
git diff --name-status <start>..<end> | grep "^R"
```

- **New classes**: Enhancement (impact: new capabilities available)
- **Moved/renamed classes**: Possibly breaking (impact: namespace imports)
- **Refactored classes**: Usually safe (impact: verify public API unchanged)
- **Removed classes**: Breaking (impact: must find alternative)

**Document:**
- What changed (class location, name, structure)
- Why changed (organization, clarity, consolidation)
- Impact on users (namespace changes, assembly references)
- Migration (update using statements)

### 11. Generate Migration Paths for All Breaking Changes

**For EVERY breaking change, provide complete migration guidance:**

**Migration Template:**
```markdown
#### 🔴 [Breaking Change Description]

**What Changed:**
```csharp
// START COMMIT (old code)
public interface IOldInterface
{
    void OldMethod();
}

// END COMMIT (new code)  
public interface INewInterface
{
    void NewMethod();  // Renamed from OldMethod
}
```

**Why This Changed:**
- [Technical reason]
- [User benefit]
- [Architectural improvement]

**Who Is Affected:**
- Applications implementing `IOldInterface`
- Code calling `OldMethod()`
- Configuration referencing old interface

**Migration Steps:**

1. **Update interface implementation:**
```csharp
// OLD
public class MyClass : IOldInterface
{
    public void OldMethod() { }
}

// NEW
public class MyClass : INewInterface
{
    public void NewMethod() { }  // Renamed
}
```

2. **Update service registration:**
```csharp
// OLD
services.AddSingleton<IOldInterface, MyClass>();

// NEW
services.AddSingleton<INewInterface, MyClass>();
```

3. **Update all call sites:**
```csharp
// OLD
myInstance.OldMethod();

// NEW
myInstance.NewMethod();
```

4. **Verify with test:**
```csharp
[Test]
public void Migration_Works()
{
    var instance = serviceProvider.GetService<INewInterface>();
    instance.NewMethod();  // Should work
}
```

**Rollback Plan (if upgrade fails):**
- Revert to previous NuGet version
- Restore old configuration
- Previous code continues to work
### 12. Quality Assurance (Change Management Standards)

#### Accuracy Verification:
- ✅ All significant changes from start→end are documented
- ✅ No important commits omitted or misrepresented
- ✅ Code examples compile correctly and are not pseudocode
- ✅ Configuration examples are valid JSON/YAML that actually works
- ✅ Interface comparisons show actual start vs end code (verified with `git show`)

#### Completeness Check (Change Management):
- ✅ **Every breaking change** has:
  - What changed (code comparison)
  - Why it changed (justification)
  - Who is affected (scope)
  - How to migrate (steps)
  - Verification (test)
- ✅ **Every feature** has:
  - What's new
  - Why it was added
  - How to use it
  - Optional adoption guidance
- ✅ **Every bug fix** has:
  - What was broken
  - What's fixed
  - Who benefits

#### Clarity Assessment (User Perspective):
- ✅ Technical descriptions are jargon-free when possible
- ✅ Examples are practical and representative of real usage
- ✅ Impact assessments specifically identify affected user groups
- ✅ Migration instructions are numbered, concrete steps (not vague guidance)
- ✅ Each change answers: "What does this mean for me?"

#### Change Management Best Practices:
- ✅ Changes ordered by impact/relevance, then chronologically
- ✅ Breaking changes clearly marked with 🔴 or ⚠️
- ✅ Risk assessment provided (high/medium/low)
- ✅ Rollback guidance for critical changes
- ✅ Testing strategy specific to this release
- ✅ Upgrade checklist is practical and ordered

### 13. Final Output and Reporting

After generating the changelog:

**Document Location:**
- Full path to the generated changelog file
- Confirmation file was created/updated successfully

**Analysis Summary:**
```
📄 Changelog Generated
══════════════════════════════════════════
File: docs/10. ChangeLog/YYYYMMDD - changes for release X.X.md
Commit Range: <start-hash> → <end-hash>
Date Range: <start-date> to <end-date>

📊 Change Statistics
══════════════════════════════════════════
Total Commits Analyzed: XX
Net File Changes: XX files modified

By Category:
- 🔴 Configuration Changes: X (Y breaking)
- 🟠 Architectural Changes: X
- 🟡 Feature Enhancements: X
- 🟡 Bug Fixes: X  
- 🟢 Development Updates: X

⚠️ Breaking Changes: X total
   🔴 High severity: X
   ⚠️ Medium severity: X
   🟡 Low severity: X

📝 Documentation Quality
══════════════════════════════════════════
✅ Migration guides: Complete for all breaking changes
✅ Code examples: XX examples provided
✅ Configuration examples: XX examples provided
✅ Testing scenarios: XX test recommendations
✅ Upgrade checklist: Complete
✅ Template compliance: Verified
```

**Validation Status:**
- ✅ Template structure followed exactly
- ✅ All significant start→end changes documented
- ✅ Intermediate steps not documented (as required)
- ✅ Breaking changes have migration paths
- ✅ Code examples validated
- ✅ Change management perspective applied

**User Impact Summary:**
- **🔴 Immediate action required**: [X changes require migration]
- **🟠 Review recommended**: [X changes may affect code]
- **🟡 Optional adoption**: [X new features available]
- **🟢 Automatic benefits**: [X bug fixes, X improvements]

**Next Steps for Release Manager:**
1. ✅ Review changelog for technical accuracy
2. ⏸️ Get stakeholder approval for release
3. ⏸️ Update root CHANGELOG.md with summary
4. ⏸️ Prepare release notes for GitHub release
5. ⏸️ Update documentation with new version references
6. ⏸️ Tag release in git: `git tag vX.X.X`
7. ⏸️ Create GitHub release with full changelog
8. ⏸️ Announce release to users with migration guidance

---

## Change Management Principles Summary

As a **Developer and Change Management Engineer**, remember:

1. **Users first**: Every change documented from user perspective - "What does this mean for MY code?"

2. **Start→End only**: Document net effect, not the journey. Users don't care that you refactored three times.

3. **Complete migration paths**: Breaking changes without migration guidance are unacceptable.

4. **Risk assessment**: Be honest about impact severity. Don't downplay breaking changes.

5. **Verification**: Provide tests users can run to verify migration success.

6. **Clarity over brevity**: Better to be verbose and clear than concise and confusing.

7. **Empathy**: Put yourself in the user's shoes - they're upgrading their production system and need confidence.

## Example Usage

```
User: Generate changelog from commit 4ebf5faea81 to HEAD for release 3.7
Agent: 
1. Analyzing commit range...
2. Comparing start vs end state for all changed files...
3. Identified 5 breaking changes, 8 enhancements, 3 bug fixes
4. Generating changelog with migration guides...
5. ✅ Complete: docs/10. ChangeLog/20251112 - changes for release 3.7.md

Summary:
- 🔴 Breaking: Interface renames, configuration schema changes
- 🟡 Features: Named filters/enrichers, .NET 10.0 support
- Migration guides: Complete for all breaking changes
- Ready for review
```

### Code Pattern Detection

Identify common refactoring patterns:
- **Rename**: Property/method/class name changes
- **Move**: Namespace or folder changes
- **Extract**: New classes extracted from existing ones
- **Merge**: Multiple classes combined into one
- **Replace**: Old implementation replaced with new one
- **Deprecate**: Marked for future removal

### Testing Strategy Recommendations

Based on changes, recommend:
- **Unit tests**: For bug fixes and new features
- **Integration tests**: For API changes
- **Configuration tests**: For configuration schema changes
- **Migration tests**: For breaking changes
- **Performance tests**: For performance-related changes
- **Regression tests**: For bug fixes

## Example Usage

```
User: Generate changelog from commit 4ebf5faea81 to HEAD for release 3.7