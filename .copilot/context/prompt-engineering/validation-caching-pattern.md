# Validation Caching Pattern (7-Day Rule)

**Purpose**: Standardized approach for managing validation metadata and caching validation results to reduce redundant processing.

**Referenced by**: All validation prompts (grammar-review, readability-review, structure-validation, fact-checking, logic-analysis, publish-ready), metadata-manager agent

---

## Overview

The Validation Caching Pattern implements a **7-day caching policy** where validation results are stored in metadata and reused if:
1. Less than 7 days have passed since last validation
2. File content has not changed since last validation

This pattern reduces token usage, improves execution speed, and provides validation history tracking.

---

## Dual YAML Metadata Architecture

**Critical Distinction**: Articles use TWO separate YAML blocks that serve completely different purposes and MUST NEVER be confused.

### Top YAML Block (Quarto Metadata) - DO NOT TOUCH

```yaml
---
title: "Article Title"
author: "Author Name"  
date: "2025-12-06"
categories: [tech, azure]
description: "Brief description"
---
```

**Purpose**: 
- Quarto rendering configuration
- Site generation metadata
- Display properties

**Managed by**: 
- Authors manually only
- **NEVER** modified by validation prompts or automation

**Location**: Beginning of file

**Visibility**: Rendered in site output

---

### Bottom Metadata Block (Validation Tracking) - VALIDATION UPDATES HERE

```html
<!-- 
---
validations:
  grammar:
    status: "passed"
    last_run: "2025-12-06T10:30:00Z"
    model: "claude-sonnet-4.5"
    issues_found: 0
  readability:
    status: "passed"
    last_run: "2025-12-05T14:20:00Z"
    model: "claude-sonnet-4.5"
    score: "8.5"
  structure:
    status: "passed"
    last_run: "2025-12-06T09:15:00Z"
    model: "claude-sonnet-4.5"
article_metadata:
  filename: "article.md"
  last_updated: "2025-12-06T10:00:00Z"
  content_hash: "a3f5b8c2d..."
---
-->
```

**Purpose**:
- Validation history tracking
- Caching validation results
- Quality metric storage
- Automation metadata

**Managed by**:
- Validation prompts (grammar-review, readability-review, etc.)
- Content management tools
- Automated quality checks

**Location**: End of file (after References section)

**Visibility**: Hidden in rendered output (HTML comment)

---

## 7-Day Caching Logic

### When to Skip Validation

```python
def should_skip_validation(validation_type: str, file_content: str) -> bool:
    """
    Determines if validation should be skipped based on cache rules
    """
    # Parse bottom metadata block
    metadata = parse_bottom_metadata(file_content)
    
    # Check if validation type exists
    if validation_type not in metadata.get('validations', {}):
        return False  # No previous validation, must run
    
    validation = metadata['validations'][validation_type]
    last_run = parse_datetime(validation['last_run'])
    
    # Check 7-day threshold
    days_since_last_run = (datetime.now() - last_run).days
    if days_since_last_run >= 7:
        return False  # Cache expired, must re-validate
    
    # Check content changes
    last_content_hash = metadata['article_metadata'].get('content_hash')
    current_content_hash = compute_hash(extract_article_content(file_content))
    
    if last_content_hash != current_content_hash:
        return False  # Content changed, must re-validate
    
    # Both conditions met: cache is valid
    return True
```

### Validation Prompt Workflow

```markdown
## Process (for any validation prompt)

### Phase 1: Cache Check

1. Read file and parse bottom metadata block
2. Check `validations.{validation_type}.last_run` timestamp
3. Check `article_metadata.content_hash` vs current content

**Decision Tree**:
```
IF last_run exists AND last_run < 7 days ago AND content unchanged:
    → Skip validation
    → Report cached result to user
    → EXIT
ELSE:
    → Proceed to Phase 2
```

### Phase 2: Validation Execution

[Validation-specific logic here]

### Phase 3: Metadata Update

1. Update `validations.{validation_type}` section ONLY
2. Set `last_run` to current ISO 8601 timestamp
3. Set `status` to validation result
4. Add validation-specific metrics
5. Update `article_metadata.content_hash` if content changed
6. Preserve all other validation sections unchanged
```

---

## Metadata Structure Specification

### Complete Bottom Metadata Template

```yaml
---
validations:
  grammar:
    status: "passed" | "failed" | "warning"
    last_run: "YYYY-MM-DDTHH:MM:SSZ"  # ISO 8601 UTC
    model: "claude-sonnet-4.5"
    issues_found: 0
    issues_detail:  # Optional
      - type: "spelling"
        line: 42
        suggestion: "..."
  
  readability:
    status: "passed" | "failed" | "warning"
    last_run: "YYYY-MM-DDTHH:MM:SSZ"
    model: "claude-sonnet-4.5"
    score: "8.5"  # Flesch Reading Ease or similar
    grade_level: "12"
  
  structure:
    status: "passed" | "failed" | "warning"
    last_run: "YYYY-MM-DDTHH:MM:SSZ"
    model: "claude-sonnet-4.5"
    missing_sections: []  # Empty if passed
    extra_sections: []
  
  fact_checking:
    status: "passed" | "failed" | "warning"
    last_run: "YYYY-MM-DDTHH:MM:SSZ"
    model: "claude-sonnet-4.5"
    facts_checked: 15
    facts_verified: 15
    facts_unverified: 0
  
  logic_analysis:
    status: "passed" | "failed" | "warning"
    last_run: "YYYY-MM-DDTHH:MM:SSZ"
    model: "claude-sonnet-4.5"
    logic_score: "9.2"
    issues_found: 0
  
  publish_ready:
    status: "passed" | "failed" | "warning"
    last_run: "YYYY-MM-DDTHH:MM:SSZ"
    model: "claude-sonnet-4.5"
    all_validations_passed: true
    blocking_issues: []

article_metadata:
  filename: "example-article.md"
  last_updated: "YYYY-MM-DDTHH:MM:SSZ"
  content_hash: "sha256:a3f5b8c2d..."  # SHA-256 of article content (excluding metadata blocks)
  word_count: 1500
  created_date: "2025-12-01T00:00:00Z"
---
```

### Field Specifications

#### Status Values

| Status | Meaning | Action Required |
|--------|---------|-----------------|
| `passed` | No issues found | None, can proceed |
| `warning` | Minor issues, not blocking | Review recommended |
| `failed` | Critical issues found | Must fix before publishing |

#### Timestamp Format

**Required**: ISO 8601 UTC format: `YYYY-MM-DDTHH:MM:SSZ`

**Examples**:
- ✅ Good: `2025-12-06T10:30:00Z`
- ❌ Bad: `2025-12-06` (missing time)
- ❌ Bad: `12/06/2025 10:30 AM` (not ISO 8601)
- ❌ Bad: `2025-12-06T10:30:00+01:00` (use UTC, not local timezone)

#### Content Hash Algorithm

```python
import hashlib

def compute_content_hash(file_content: str) -> str:
    """
    Compute SHA-256 hash of article content, excluding metadata blocks
    """
    # Extract article content (between top YAML and bottom metadata)
    article_content = extract_article_content(file_content)
    
    # Normalize whitespace to prevent spurious changes
    normalized = article_content.strip()
    
    # Compute SHA-256
    hash_object = hashlib.sha256(normalized.encode('utf-8'))
    return f"sha256:{hash_object.hexdigest()}"
```

**Important**: 
- Hash ONLY article content (exclude both YAML blocks)
- Normalize whitespace before hashing
- Use SHA-256 for consistency
- Prefix with `sha256:` for clarity

---

## Implementation Examples

### Example 1: Grammar Review Prompt (with caching)

```yaml
---
description: "Grammar and spelling validation with 7-day caching"
tools: ['read_file', 'replace_string_in_file']
agent: plan
---

# Grammar Review Prompt

## Process

### Phase 1: Cache Check

1. Read file: `read_file(target_file)`
2. Parse bottom metadata block (HTML comment at end of file)
3. Check cache validity:
   ```
   IF validations.grammar exists:
     last_run = parse_datetime(validations.grammar.last_run)
     IF (now - last_run) < 7 days AND content_hash matches:
       REPORT cached result: validations.grammar.status
       EXIT
   ```

### Phase 2: Grammar Validation

4. Extract article content (between top YAML and bottom metadata)
5. Check grammar, spelling, punctuation
6. Identify issues with line numbers

### Phase 3: Metadata Update

7. Update ONLY `validations.grammar` section in bottom metadata:
   ```yaml
   validations:
     grammar:
       status: "passed"
       last_run: "2025-12-06T10:30:00Z"
       model: "claude-sonnet-4.5"
       issues_found: 0
   ```
8. Update `article_metadata.content_hash` if content was modified
9. Preserve all other validation sections unchanged

## Boundaries

### 🚫 Never Do
- NEVER modify the top YAML block
- NEVER overwrite other validation sections (readability, structure, etc.)
- NEVER skip cache check
```

### Example 2: Metadata Manager Agent

```yaml
---
description: "Specialized agent for validation metadata management"
tools: ['read_file', 'replace_string_in_file']
agent: agent
---

# Metadata Manager Agent

You are a metadata management specialist responsible for creating and updating validation tracking metadata in article files.

## Your Responsibilities

1. **Create Bottom Metadata Blocks**
   - Add HTML comment block at end of file (after References)
   - Initialize all validation sections with "not_run" status
   - Set `article_metadata` with filename, created_date, content_hash

2. **Update Validation Results**
   - Update ONLY the specific validation section provided
   - Preserve all other validation sections exactly as-is
   - Update `article_metadata.last_updated` timestamp
   - Recalculate `content_hash` if content changed

3. **Validate Metadata Integrity**
   - Ensure ISO 8601 timestamps
   - Verify content_hash format (sha256:...)
   - Check all required fields present

## Process

When called by a validation prompt with results:

1. Read current file
2. Locate bottom metadata block (HTML comment)
3. Parse existing metadata
4. Update ONLY the specified validation section:
   ```python
   metadata['validations'][validation_type] = {
       'status': result_status,
       'last_run': current_timestamp_iso8601(),
       'model': 'claude-sonnet-4.5',
       **validation_specific_fields
   }
   ```
5. Update `article_metadata.last_updated`
6. Recalculate `content_hash` if needed
7. Write updated metadata back to file

## Boundaries

### ✅ Always Do
- Preserve top YAML block exactly as-is
- Preserve all other validation sections
- Use ISO 8601 UTC timestamps
- Validate YAML syntax before saving

### 🚫 Never Do
- NEVER modify top YAML block
- NEVER overwrite unrelated validation sections
- NEVER use non-UTC timestamps
```

---

## Migration Guide

### Adding Validation Caching to Existing Prompts

**Before** (no caching):
```yaml
---
description: "Grammar review"
tools: ['read_file', 'replace_string_in_file']
---

## Process
1. Read file
2. Check grammar
3. Report issues
```

**After** (with caching):
```yaml
---
description: "Grammar review with 7-day caching"
tools: ['read_file', 'replace_string_in_file']
agent: plan
---

## Process

### Phase 1: Cache Check
1. Read file
2. Parse bottom metadata: `validations.grammar`
3. Check cache validity (7-day rule + content_hash)
4. If valid cache exists → Report cached result → EXIT

### Phase 2: Grammar Validation
5. Extract article content
6. Check grammar
7. Identify issues

### Phase 3: Metadata Update
8. Update `validations.grammar` in bottom metadata
9. Update `article_metadata.content_hash`
10. Report results
```

### Creating Bottom Metadata for Legacy Articles

```markdown
## Migration Script Workflow

FOR EACH article without bottom metadata:
  1. Read file
  2. Check if bottom metadata exists
  3. If missing:
     - Create HTML comment block at end
     - Initialize all validation sections with status "not_run"
     - Set article_metadata:
       - filename: from file path
       - created_date: from git history or file stats
       - content_hash: compute from current content
       - word_count: count words in article content
  4. Save updated file
```

---

## Troubleshooting

### Issue 1: Cache Always Skipped

**Symptom**: Validation runs every time despite recent last_run

**Diagnosis**:
```markdown
1. Check `last_run` timestamp format
   - Must be ISO 8601 UTC: "2025-12-06T10:30:00Z"
   - ❌ Wrong: "2025-12-06" or "12/06/2025 10:30"

2. Check content_hash comparison
   - Print both hashes to debug
   - Ensure whitespace normalization
   - Verify hash algorithm (SHA-256)
```

**Fix**:
- Use `datetime.now().isoformat() + 'Z'` for timestamps
- Normalize content before hashing (strip, consistent line endings)

### Issue 2: Metadata Overwrites

**Symptom**: Other validation sections disappear after update

**Diagnosis**:
```markdown
Problem: Validation prompt is overwriting entire metadata block instead of updating one section

❌ Wrong approach:
metadata = {
  'validations': {
    'grammar': new_results  # Only grammar, lost others
  }
}

✅ Correct approach:
metadata = parse_existing_metadata()
metadata['validations']['grammar'] = new_results  # Update only grammar
```

**Fix**:
- Always parse existing metadata first
- Update only the specific section
- Preserve all other sections

### Issue 3: Top YAML Accidentally Modified

**Symptom**: Quarto rendering breaks, article metadata corrupted

**Diagnosis**:
```markdown
Problem: Validation prompt modified top YAML block

Common mistakes:
- Updating `date` field in top YAML
- Adding validation results to top YAML
- Confusing top and bottom blocks
```

**Fix**:
- **CRITICAL RULE**: Validation prompts NEVER touch top YAML
- Only update bottom metadata (HTML comment)
- Add explicit boundary: "NEVER modify YAML between lines 1-10"

---

## Performance Benefits

### Token Usage Reduction

**Without Caching**:
```
Average validation: 2,000-5,000 tokens per run
6 validations × 1 run/day × 30 days = 360,000-900,000 tokens/month
```

**With 7-Day Caching**:
```
Average validation: 2,000-5,000 tokens per run
Cache hit rate: ~70% (articles don't change daily)
Effective validations: 108,000-270,000 tokens/month
Savings: 70% reduction in validation token usage
```

### Execution Speed

| Operation | Without Cache | With Cache | Speedup |
|-----------|---------------|------------|---------|
| Cache check | N/A | ~200ms | N/A |
| Grammar validation | ~5-10s | 0s (skipped) | Instant |
| Readability validation | ~3-7s | 0s (skipped) | Instant |
| Structure validation | ~2-5s | 0s (skipped) | Instant |
| **Total (all 6 validations)** | **~20-40s** | **~1s** | **20-40x faster** |

---

## Best Practices

### 1. Always Check Cache First

```markdown
✅ **CORRECT WORKFLOW**:
Phase 1: Cache Check (ALWAYS)
Phase 2: Validation (IF cache invalid)
Phase 3: Metadata Update (IF validation ran)

❌ **WRONG WORKFLOW**:
Phase 1: Validation (runs every time)
Phase 2: Update metadata (wasted if cached result would have been valid)
```

### 2. Atomic Metadata Updates

```markdown
✅ **CORRECT APPROACH**:
1. Read entire metadata block
2. Parse to dict
3. Update specific section
4. Serialize entire block
5. Write once

❌ **WRONG APPROACH**:
1. Read file
2. Use regex to replace specific section (fragile)
3. Write (might corrupt YAML structure)
```

### 3. Defensive Parsing

```python
# ✅ Good: Handle missing fields gracefully
def get_validation_status(metadata, validation_type):
    try:
        return metadata['validations'][validation_type]['status']
    except (KeyError, TypeError):
        return 'not_run'  # Default if missing

# ❌ Bad: Assume structure exists
def get_validation_status(metadata, validation_type):
    return metadata['validations'][validation_type]['status']  # Crashes if missing
```

### 4. Content Hash Stability

```python
# ✅ Good: Normalize before hashing
def compute_hash(content):
    normalized = content.strip()  # Remove leading/trailing whitespace
    normalized = normalized.replace('\r\n', '\n')  # Consistent line endings
    return hashlib.sha256(normalized.encode('utf-8')).hexdigest()

# ❌ Bad: Hash raw content
def compute_hash(content):
    return hashlib.sha256(content.encode('utf-8')).hexdigest()
    # Different results for whitespace-only changes
```

### 5. Clear Cache Invalidation

```markdown
## When to Force Re-Validation (ignore cache)

User explicitly requests:
  - "Re-run grammar check" → Ignore cache, validate fresh
  - "Validate all" → Ignore all caches

Content changed:
  - content_hash mismatch → Auto-invalidate, validate fresh

Cache expired:
  - last_run > 7 days ago → Auto-invalidate, validate fresh

Validation logic updated:
  - Increment validation version in metadata
  - Compare versions, invalidate if mismatch
```

---

## Version History

| Version | Date | Changes | Author |
|---------|------|---------|--------|
| 1.0.0 | 2025-12-10 | Initial version with 7-day caching pattern | System |

---

## References

- **Context Engineering Principles**: `.copilot/context/prompt-engineering/context-engineering-principles.md`
- **Tool Composition Guide**: `.copilot/context/prompt-engineering/tool-composition-guide.md`
- **Global Instructions**: `.github/copilot-instructions.md` (Dual YAML Metadata section)
- **Validation Prompts**: `.github/prompts/grammar-review.prompt.md`, etc.
