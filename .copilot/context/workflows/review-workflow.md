# Article Review Workflow

This workflow guides you through reviewing and updating existing articles to ensure they remain accurate, current, and high-quality.

## When to Review Articles

### Scheduled Reviews
- **Quarterly** (every 90 days): Technical articles with version-specific information
- **Annually**: General concept articles and tutorials
- **As-needed**: When technology updates or reader feedback indicates issues

### Triggered Reviews
Review immediately when:
- Technology mentioned has a major version update
- Reader reports an issue or inaccuracy
- Related articles are updated (may need cross-reference updates)
- Article appears in stale validation report
- More than 180 days since last review

## Review Process Overview

```
Check Status → Run Stale Validations → Update Content → Re-validate → Update Metadata → Publish
```

## Detailed Steps

### Step 1: Check Current Status

**Review the metadata file:**
```yaml
# Check article-name.metadata.yml
article:
  status: "published"
  last_updated: "2025-08-19"  # How old?
  
validations:
  facts:
    last_run: "2025-08-19"  # Stale if > 30 days
  grammar:
    last_run: "2025-08-19"  # Stale if > 7 days
```

**Use automation:**
```powershell
# Find all articles with stale validations
.\.copilot\scripts\check-stale-validations.ps1
```

**Assessment questions:**
- How long since last update?
- Which validations are stale?
- What's the article status?
- Have related technologies changed?

### Step 2: Prioritize What to Check

**Critical validations** (must be current):
- ✅ Facts (< 30 days for technical content)
- ✅ Structure (if template changed)
- ✅ Links (no broken links)

**Important validations** (should be recent):
- ✅ Grammar (< 7 days if content changed)
- ✅ Readability (< 7 days if content changed)
- ✅ Logic (< 30 days)

**Optional validations** (run if major changes):
- Gap analysis (to find missing information)
- Understandability (if audience feedback received)
- Series validation (if part of series)

### Step 3: Review Content for Currency

**Technical accuracy check:**
- [ ] Version numbers current?
- [ ] APIs or features changed?
- [ ] Screenshots/images outdated?
- [ ] Links still working?
- [ ] Examples still valid?
- [ ] Best practices still current?

**Manual review:**
1. Open the article
2. Check the introduction - is it still relevant?
3. Scan each section - spot obvious outdated info
4. Click all external links - verify they work
5. Note any needed updates

### Step 4: Run Stale Validations

**Run only the validations that are stale or failed:**

**If facts are stale (> 30 days):**
```
/fact-checking
```
This will:
- Verify all technical claims
- Check version information
- Test code examples
- Validate sources

**If grammar/readability stale:**
```
/grammar-review
/readability-review
```

**If structure changed:**
```
/structure-validation
```

**Update metadata after each validation:**
```
/metadata-update
```

### Step 5: Update Content (If Needed)

**For minor updates:**
- Fix broken links
- Update version numbers
- Correct factual errors
- Update code examples

**For major updates:**
- Rewrite outdated sections
- Add new information
- Update examples
- Consider incrementing version number

**Version guidelines:**
```yaml
# Minor updates (patch): 1.0 → 1.1
- Fixed typos
- Updated links
- Minor clarifications

# Major updates: 1.x → 2.0
- Significant content changes
- Major restructuring
- New sections added
```

### Step 6: Re-validate After Changes

**If you made content changes, re-run:**
- `/grammar-review` (if text changed)
- `/readability-review` (if substantial changes)
- `/logic-analysis` (if structure changed)
- `/fact-checking` (for new claims)

**Update metadata:**
```
/metadata-update
```

**Update article metadata:**
```yaml
article:
  last_updated: "2025-11-19"
  version: "1.1"  # or "2.0" for major changes
```

### Step 7: Verify Publication Readiness

**Run comprehensive check:**
```
/publish-ready
```

**Expected result:**
- ✅ All critical validations passed
- ✅ No broken links
- ✅ Facts verified within 30 days
- ✅ Metadata current

### Step 8: Publish Updates

**If article is published:**
```yaml
article:
  status: "published"
  last_updated: "2025-11-19"
  version: "1.1"

publication:
  last_review_date: "2025-11-19"
  next_review_date: "2026-02-19"  # 90 days for technical
```

**Commit changes:**
```bash
git add [article-file] [metadata-file]
git commit -m "Review and update: [article title]"
git push
```

## Review Checklist

### Content Currency
- [ ] Technology versions current
- [ ] Links all working
- [ ] Screenshots/images current
- [ ] Code examples tested
- [ ] Best practices still valid
- [ ] No deprecated features

### Quality Standards
- [ ] Grammar validated (passed)
- [ ] Readability appropriate
- [ ] Structure compliant
- [ ] Facts verified (< 30 days)
- [ ] Logic flows correctly

### Metadata
- [ ] Last updated date current
- [ ] Version incremented (if changed)
- [ ] All validations recorded
- [ ] Next review date set
- [ ] Cross-references current

## Frequency Guidelines

### By Article Type

**Technical Documentation:**
- Review: Every 90 days
- Fact-check: Every 30 days
- Link check: Monthly

**Tutorials/HowTos:**
- Review: Every 90 days
- Test examples: Every 30 days
- Tool version updates: As released

**Concept Articles:**
- Review: Annually
- Fact-check: Every 90 days
- Link check: Quarterly

**Reference Material:**
- Review: With technology updates
- Fact-check: Every 30 days
- Completeness: Quarterly

### By Content Stability

**Highly Volatile** (frameworks, tools):
- Review: Monthly or with major releases
- Example: React, VS Code features

**Moderately Stable** (patterns, practices):
- Review: Quarterly
- Example: Design patterns, git workflows

**Stable** (fundamentals, concepts):
- Review: Annually
- Example: HTTP basics, algorithm theory

## Automation Helpers

### Find Articles Needing Review

**All stale validations:**
```powershell
.\.copilot\scripts\check-stale-validations.ps1 -ExportCsv
```

**Articles older than 90 days:**
```powershell
Get-ChildItem -Path . -Filter "*.metadata.yml" -Recurse | ForEach-Object {
    $metadata = Get-Content $_.FullName | ConvertFrom-Yaml
    $lastUpdated = [datetime]::Parse($metadata.article.last_updated)
    $daysOld = ((Get-Date) - $lastUpdated).TotalDays
    
    if ($daysOld -gt 90) {
        [PSCustomObject]@{
            Article = $metadata.article.title
            LastUpdated = $lastUpdated
            DaysOld = [math]::Round($daysOld)
            File = $_.FullName
        }
    }
}
```

### Batch Review Process

**For multiple articles needing review:**
1. Generate list of stale articles
2. Prioritize by importance and age
3. Review in batches (5-10 at a time)
4. Update metadata for all
5. Commit batch changes

## Common Review Scenarios

### Scenario 1: Technology Version Update

**Example**: Node.js 18 → Node.js 20

1. Search for version references: `grep -r "Node.js 18"`
2. Review each mention for currency
3. Update version numbers
4. Test code examples with new version
5. Update prerequisites in related articles
6. Run fact-checking
7. Update metadata

### Scenario 2: Broken Links

1. Run link checker or fact-checking prompt
2. For each broken link:
   - Find replacement URL (use archive.org if needed)
   - Update or remove link
   - Update References section
3. Re-run fact-checking
4. Update metadata

### Scenario 3: Reader Reports Issue

1. Verify the reported issue
2. Assess severity (critical vs. minor)
3. Fix the issue
4. Re-run relevant validations
5. Thank reader (if appropriate)
6. Update metadata
7. Publish update

### Scenario 4: Routine Quarterly Review

1. Run stale validation check
2. Review article for obvious issues
3. Run fact-checking (most critical)
4. Update any outdated info
5. Re-run validations if content changed
6. Update metadata with review date
7. Set next review date

## Tips for Efficient Reviews

### Do:
✅ Use automation to find stale articles
✅ Batch similar reviews together
✅ Focus on facts and links (most critical)
✅ Update metadata immediately
✅ Set next review date
✅ Document major changes in metadata history

### Don't:
❌ Re-run all validations if nothing changed
❌ Skip fact-checking on technical articles
❌ Forget to test code examples
❌ Leave broken links
❌ Skip metadata updates

## Tracking Review History

**In metadata, record major reviews:**
```yaml
history:
  major_revisions:
    - date: "2025-11-19"
      version: "2.0"
      description: "Updated for Node.js 20, revised examples"
      author: "Your Name"
    - date: "2025-08-15"
      version: "1.1"
      description: "Updated links, verified facts"
      author: "Your Name"
```

## Review Outcomes

### Up-to-Date ✅
- All validations current
- No issues found
- Metadata updated with review date
- Next review date set

### Minor Updates ⚠️
- Some validations stale but passed
- Minor corrections made
- Metadata updated
- Monitoring for future changes

### Major Revision Needed 🔄
- Significant outdated information
- Multiple validations failed
- Requires substantial rewrite
- Consider archiving if can't update

### Archive Candidate 📦
- Technology obsolete
- No longer maintained
- Replaced by newer content
- Mark as archived, don't delete

---

*Use this workflow regularly to maintain high-quality, current documentation.*
