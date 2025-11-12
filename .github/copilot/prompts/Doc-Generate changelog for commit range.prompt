# Generate Changelog for Commit Range

## Goal
Generate a comprehensive changelog document for a specified commit range, analyzing all code changes and their impact on applications and configurations. The changelog should follow the structure defined in `YYYYMMDD ChangeLog Template.md`.

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

### 2. Analyze Git History

Execute git commands to gather comprehensive change information:

```bash
# Get commit list
git log --oneline --no-merges <start>..<end>

# Get detailed commit information
git log --no-merges --stat --pretty=format:"%h - %s%n" <start>..<end>

# Get file changes with names only
git log --no-merges --name-status --pretty=format:"%h - %s%n" <start>..<end>

# Get diff statistics
git diff --stat <start>..<end>
```

### 3. Categorize Changes

Group changes into the following categories based on commit messages, file patterns, and diff analysis:

#### Configuration Schema Updates
- Interface changes (property additions, renames, removals)
- Configuration class modifications
- Default value changes
- Breaking changes to configuration structure

**Identify by:**
- Files matching `*Options.cs`, `*Configuration.cs`, `*Config.cs`
- Interface files (`I*.cs`)
- Changes to property names, types, or accessibility

#### Architectural Improvements
- Folder/namespace reorganizations
- Class movements and refactorings
- Design pattern implementations
- Dependency injection changes

**Identify by:**
- File rename/move operations
- Namespace changes
- New folder structures
- Refactored class hierarchies

#### Feature Enhancements
- New functionality additions
- API expansions
- Performance improvements
- Enhanced capabilities

**Identify by:**
- New methods/properties added
- New classes/interfaces added
- Performance optimization commits
- Feature flag additions

#### Bug Fixes
- Error corrections
- Exception handling improvements
- Edge case fixes
- Stability improvements

**Identify by:**
- Commit messages containing: "fix", "bug", "issue", "resolve", "correct"
- Exception handling code changes
- Null check additions
- Validation improvements

#### Development Updates
- Build system changes
- Tool/SDK updates
- Code modernization
- Test improvements

**Identify by:**
- `.csproj`, `.sln`, `.slnx` file changes
- C# language version updates
- NuGet package version changes (filter out for separate section)
- CI/CD configuration changes

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

### 5. Analyze Each Change Set

For each category of changes identified, perform deep analysis:

#### For Configuration Changes:

**What Changed:**
```csharp
// BEFORE
public interface IMyOptions
{
    string OldPropertyName { get; }  // ? Old name
    bool OldFlag { get; }
}

// AFTER
public interface IMyOptions
{
    string NewPropertyName { get; }  // ? Renamed
    bool NewFlag { get; }           // ? Renamed
    string AddedProperty { get; }   // ? NEW
}
```

**Why Changes Were Applied:**
- Technical reasons for the change
- Problems solved by the change
- Architectural benefits
- Standards compliance improvements

**Impact on Applications:**
- ? **No breaking changes**: Backward compatible changes
- ?? **Code updates required**: Breaking changes needing code modifications
- ?? **Critical breaking changes**: Major API changes requiring immediate attention

**Configuration Migration:**
```json
// OLD
{
  "MySection": {
    "OldPropertyName": "value"
  }
}

// NEW
{
  "MySection": {
    "NewPropertyName": "value",  // Renamed
    "AddedProperty": "default"   // NEW - with default
  }
}
```

#### For Architectural Changes:

**What Changed:**
- File/folder moves
- Namespace reorganizations
- Class refactorings
- Pattern implementations

**Why Changes Were Applied:**
- Code organization improvements
- Better separation of concerns
- Improved maintainability
- Enhanced testability

**Impact on Applications:**
- Namespace import changes required
- Using directive updates needed
- No functional changes expected

#### For Feature Enhancements:

**What Changed:**
- New methods/properties added
- New classes/interfaces introduced
- Enhanced functionality

**Why Changes Were Applied:**
- New use cases supported
- Enhanced capabilities
- Performance improvements
- Better developer experience

**Impact on Applications:**
- ? **Optional adoption**: New features available but not required
- ?? **Documentation updates**: New APIs documented
- ?? **Best practices**: Recommended usage patterns

#### For Bug Fixes:

**What Was Fixed:**
- Specific error conditions
- Edge cases handled
- Exception scenarios resolved

**Why Fix Was Needed:**
- Problems encountered in production
- Reported issues from users
- Discovered during testing

**Impact:**
- ? **Reliability improvement**: More stable operation
- ? **Correctness**: Proper behavior restored
- ?? **Behavior changes**: Might affect code relying on old behavior

### 6. Generate Changelog Document

Create the changelog document in:
`docs/10. ChangeLog/`

Name the document as:
`<YYYYMMDD> - changes for release <Version>.md`

Example: `20251112 - changes for release 3.7.md`

### 7. Populate Changelog Sections

Following the template structure exactly:

#### Title and Metadata
```markdown
# Changes for Release <Version>

**Release Date:** <Date>  
**Commit Range:** `<start>` ? `<end>`
```

#### Changes Overview
List all changes grouped by category:
1. **Configuration Schema Updates**
2. **Architectural Improvements**
3. **Feature Enhancements**
4. **Bug Fixes**
5. **Development Updates**
6. **Dependency Updates** (if any non-version updates)

#### Changes Analysis
For each category, provide detailed subsections:

**Section Structure:**
```markdown
### 1. [Category Name]

#### 1.1 [Specific Change Set]

**What Changed:**
[Code comparison showing before/after]

**Why Changes Were Applied:**
[Technical reasoning and benefits]

**Impact on Applications:**
[Detailed impact analysis with severity indicators]

**Migration Guide:**
[Step-by-step migration instructions]
```

#### Migration Guide
Provide practical migration instructions:
- **For Most Users**: Minimal impact changes
- **For Advanced Users**: Code-level changes required
- **Code Examples**: Before/after migration examples

#### Breaking Changes Summary
Create a quick-reference table:

| Change | Severity | Migration Required |
|--------|----------|-------------------|
| [Change 1] | ?? High | Yes - details |
| [Change 2] | ?? Medium | Yes - details |
| [Change 3] | ?? Low | Optional |

#### Testing Recommendations
Provide specific test scenarios for verifying the upgrade.

#### Upgrade Checklist
Create a practical checklist for developers performing the upgrade.

### 8. Verification Checklist

Ensure the generated changelog:
- ? Follows template structure exactly
- ? Uses consistent markdown formatting
- ? Includes all commit changes from the range
- ? Provides "before/after" code examples for major changes
- ? Contains clear migration instructions
- ? Has accurate severity indicators (??????)
- ? Includes working configuration examples
- ? Documents all breaking changes
- ? Provides testing recommendations
- ? Contains practical upgrade checklist
- ? Has proper cross-references to related documentation
- ? Uses emojis consistently (? ?? ?? ?? ?? ?)

### 9. Analyze Code Changes for Impact

For files changed, perform detailed analysis:

#### Property Changes:
- **Added properties**: New configuration options available
- **Removed properties**: Breaking changes requiring migration
- **Renamed properties**: Breaking changes with clear migration path
- **Type changes**: Breaking changes requiring code updates

#### Method Changes:
- **Added methods**: New functionality available
- **Removed methods**: Breaking changes requiring alternatives
- **Signature changes**: Breaking changes with migration guide
- **Return type changes**: Breaking changes requiring updates

#### Interface Changes:
- **New interfaces**: Optional adoption, enhanced capabilities
- **Modified interfaces**: Breaking changes for implementers
- **Removed interfaces**: Critical breaking changes

#### Class Changes:
- **New classes**: New features and capabilities
- **Moved/renamed classes**: Namespace changes required
- **Refactored classes**: Internal improvements, minimal impact
- **Removed classes**: Breaking changes requiring alternatives

### 10. Generate Migration Paths

For each breaking change, provide:

**Migration Template:**
```markdown
#### [Change Description]

**Old Code:**
```csharp
// Code before change
```

**New Code:**
```csharp
// Code after change
```

**Migration Steps:**
1. [Step 1]
2. [Step 2]
3. [Step 3]

**Validation:**
```csharp
// Code to verify migration worked
```
```

### 11. Quality Assurance

#### Accuracy Verification:
- All commit changes are represented
- No commits omitted or overlooked
- Code examples compile correctly
- Configuration examples are valid JSON/YAML

#### Completeness Check:
- All breaking changes documented
- All new features explained
- All bug fixes listed
- Migration paths provided for breaking changes

#### Clarity Assessment:
- Technical descriptions are clear
- Examples are practical and relevant
- Impact assessments are accurate
- Migration instructions are step-by-step

### 12. Final Output

After generating the changelog:

**Report:**
- **Location**: Full path to the changelog file
- **Commit Range Analyzed**: Start and end commits
- **Total Commits**: Number of commits analyzed
- **Categories**: Number of changes per category
- **Breaking Changes**: Count of breaking changes identified
- **File Changes**: Number of files modified
- **Documentation Status**: Confirmation of template compliance

**Summary:**
```
? Changelog generated: docs/10. ChangeLog/20251112 - changes for release 3.7.md

?? Analysis Results:
- Commits analyzed: 17
- Configuration changes: 4
- Architectural improvements: 3
- Feature enhancements: 5
- Bug fixes: 3
- Development updates: 2

?? Breaking Changes: 3
?? High severity: 2
?? Medium severity: 1

?? Documentation:
- Migration guides: Complete
- Code examples: 12
- Testing scenarios: 5
- Upgrade checklist: ?
```

**Validation Status:**
- ? Template compliance verified
- ? All commits accounted for
- ? Breaking changes documented
- ? Migration paths provided
- ? Examples validated

**Next Steps:**
1. Review changelog for accuracy
2. Update related documentation if needed
3. Prepare release notes summary
4. Update README or CHANGELOG.md in repository root
5. Tag release in git
6. Create GitHub release with changelog

## Additional Guidelines

### Commit Message Analysis

Parse commit messages for keywords:
- **Breaking changes**: "BREAKING", "breaking change", "breaks"
- **Features**: "feat:", "feature:", "add", "new"
- **Fixes**: "fix:", "bug:", "resolve", "correct"
- **Refactor**: "refactor:", "restructure", "reorganize"
- **Docs**: "docs:", "documentation"
- **Test**: "test:", "tests"
- **Chore**: "chore:", "build:", "ci:"

### Severity Assessment

**?? High (Critical Breaking Changes):**
- Public API removals
- Interface signature changes
- Configuration structure changes
- Namespace changes affecting public APIs

**?? Medium (Breaking Changes with Easy Migration):**
- Property renames with clear migration path
- Method signature changes with overloads
- Configuration property renames

**?? Low (Non-Breaking Changes):**
- New optional features
- Bug fixes maintaining behavior
- Internal refactorings
- Performance improvements
- Documentation updates

### Configuration Impact Analysis

For each configuration change:
1. **Identify affected configuration files**: `appsettings.json`, `web.config`, etc.
2. **Determine backward compatibility**: Will old configs still work?
3. **Provide migration examples**: Show before/after configuration
4. **List default values**: What happens if config is missing?
5. **Document validation**: What config values are valid?

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