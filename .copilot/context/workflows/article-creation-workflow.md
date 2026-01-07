# Article Creation Workflow

This workflow guides you through creating a new article from concept to publication using the automation tools in this repository.

## Workflow Overview

```
Idea → Planning → Drafting → Validation → Review → Publication
```

## Phase 1: Planning

### 1.1 Define the Topic

**Questions to answer:**
- What specific topic will this cover?
- Who is the target audience (beginner/intermediate/advanced)?
- What will readers be able to do after reading?
- What prerequisites should readers have?

**Document:**
- Topic: [clear, specific description]
- Audience: [skill level]
- Learning objectives: [3-5 bullet points]
- Prerequisites: [list existing articles or concepts]

### 1.2 Research and Outline

**Actions:**
- Search existing articles: `/correlated-topics` prompt
- Research authoritative sources
- Identify gaps: `/gap-analysis` on related content
- Create outline with main sections

**Deliverable:**
```markdown
# [Article Title]

## Outline
- Introduction: [key points]
- Section 1: [topic]
  - Subsection 1.1
  - Subsection 1.2
- Section 2: [topic]
- Conclusion: [summary]
- References: [sources to cite]
```

### 1.3 Select Template

Choose appropriate template from `.github/templates/`:
- **article-template.md**: General explanatory content
- **howto-template.md**: Step-by-step procedures
- **tutorial-template.md**: Hands-on learning with exercises

## Phase 2: Drafting

### 2.1 Create Article File

**Location:** Place in appropriate subject folder
- `tech/` for technical topics
- `howto/` for procedural guides
- `projects/` for project documentation

**Naming:** Use lowercase with hyphens
- Good: `understanding-webhooks.md`
- Bad: `Understanding Webhooks.md` or `understanding_webhooks.md`

### 2.2 Initialize Metadata

**Run prompt:**
```
/metadata-init
```

**Provide:**
- Article file path
- Title
- Author (your name)
- Tags (3-5 relevant tags)
- Series info (if part of series)

**Result:** Creates `article-name.metadata.yml` adjacent to article

### 2.3 Write First Draft

**Option A: Manual Writing**
- Copy template structure
- Fill in sections based on outline
- Include examples and code as you go

**Option B: AI-Assisted**
```
/article-writing topic="Your Topic" outline="key points" template="article-template"
```

**Tips:**
- Focus on content over perfection
- Include placeholder citations (fill in later)
- Mark areas needing examples with TODO
- Don't worry about polish yet

### 2.4 Add Code Examples

**Requirements:**
- Specify language for syntax highlighting
- Add comments explaining logic
- Test that code works
- Show realistic usage

**Example:**
````markdown
```javascript
// Connect to database with error handling
async function connectDB() {
  try {
    await mongoose.connect(process.env.DB_URL);
    console.log('Database connected');
  } catch (error) {
    console.error('Connection failed:', error);
    process.exit(1);
  }
}
```
````

### 2.5 Gather References

**As you write:**
- Note sources for factual claims
- Bookmark official documentation
- Save URLs of examples
- Track version numbers

**Format for References section:**
```markdown
## References

- [Official Documentation](URL) - Primary technical reference
- [Tutorial Name](URL) - Helpful guide
- [Academic Paper](URL) - Research foundation
```

## Phase 3: Validation

### 3.1 Structure Validation

**Run prompt:**
```
/structure-validation
```

**Checks:**
- All required sections present
- Heading hierarchy correct
- Markdown formatting valid
- TOC generated (if > 500 words)
- Code blocks have language specified

**Actions:**
- Fix any structural issues
- Add missing sections
- Correct Markdown errors

### 3.2 Grammar Check

**Run prompt:**
```
/grammar-review
```

**Reviews:**
- Spelling
- Grammar
- Punctuation
- Consistency

**Actions:**
- Accept/reject suggested corrections
- Fix identified issues
- Run `/metadata-update` with results

### 3.3 Readability Review

**Run prompt:**
```
/readability-review
```

**Analyzes:**
- Reading level
- Sentence complexity
- Paragraph structure
- Redundancy

**Actions:**
- Simplify complex sentences
- Break up long paragraphs
- Remove redundant content
- Improve transitions

### 3.4 Understandability Check

**Run prompt:**
```
/understandability-review target_audience="[level]"
```

**Evaluates:**
- Appropriate for audience
- Jargon explained
- Examples helpful
- Concepts clear

**Actions:**
- Add definitions for jargon
- Include more examples if needed
- Clarify confusing sections

### 3.5 Logic Analysis

**Run prompt:**
```
/logic-analysis
```

**Verifies:**
- Concepts in correct order
- Prerequisites introduced first
- Smooth flow between sections
- Conclusion follows from content

**Actions:**
- Reorder sections if needed
- Add transitions
- Fill logical gaps
- Ensure prerequisites covered

### 3.6 Fact-Checking

**Run prompt:**
```
/fact-checking
```

**Validates:**
- Factual accuracy
- Source credibility
- Version information
- Code examples work

**Actions:**
- Correct inaccurate information
- Add citations
- Update version numbers
- Test all code examples

### 3.7 Gap Analysis

**Run prompt:**
```
/gap-analysis
```

**Identifies:**
- Missing information
- Unanswered questions
- Related topics not covered
- Additional examples needed

**Actions:**
- Add critical missing information
- Consider scope for minor gaps
- Link to related topics
- Note gaps in conclusion for "Next Steps"

### 3.8 Update Metadata

**After each validation:**
```
/metadata-update
```

**Records:**
- Validation type
- Timestamp
- Model used
- Outcome
- Issues found
- Notes

## Phase 4: Review

### 4.1 Self-Review

**Read through completely:**
- As if you're the target reader
- Check all links work
- Verify code examples
- Ensure examples match explanations

**Checklist:**
- [ ] Title accurate and engaging
- [ ] Introduction sets expectations
- [ ] Each section delivers on promise
- [ ] Examples clarify concepts
- [ ] Conclusion summarizes well
- [ ] References complete

### 4.2 Cross-Reference Check

**Run prompt:**
```
/correlated-topics
```

**Reviews:**
- Related articles to link
- Prerequisites to reference
- Advanced topics to suggest
- Series opportunities

**Actions:**
- Add internal links to related articles
- Update "See Also" section
- Add prerequisite links
- Consider series structure

### 4.3 Update Article Status

**In metadata file:**
```yaml
article:
  status: "in-review"
  last_updated: "[today's date]"
```

## Phase 5: Final Check

### 5.1 Run Publish-Ready Checklist

**Run prompt:**
```
/publish-ready
```

**Comprehensive check:**
- All validations current
- No critical issues
- Metadata complete
- Links working
- Ready determination

**Result:**
- ✅ READY TO PUBLISH: Proceed to publication
- ⚠️ NEEDS ATTENTION: Address warnings, optional re-check
- ✗ NOT READY: Fix critical issues, re-run checks

### 5.2 Address Any Issues

**If not ready:**
1. Review list of critical issues
2. Fix each one
3. Re-run relevant validation
4. Update metadata
5. Run `/publish-ready` again

### 5.3 Series Integration (if applicable)

**If part of series:**
```
/series-validation
```

**Ensures:**
- Consistent with other articles
- Proper navigation
- Non-redundant
- Logical position in series

**Actions:**
- Update series metadata
- Add prev/next navigation
- Update series index
- Cross-reference other articles

## Phase 6: Publication

### 6.1 Final Metadata Update

**Set publication metadata:**
```yaml
article:
  status: "published"
  published_date: "[today's date]"
  last_updated: "[today's date]"
```

### 6.2 Generate Series Navigation (if applicable)

**Run script:**
```powershell
.\.copilot\scripts\generate-series-index.ps1
```

### 6.3 Update Cross-References

**In related articles:**
- Add links to new article
- Update "Related Topics" sections
- Update series indexes

### 6.4 Commit and Deploy

**Git operations:**
```bash
git add [article-file] [metadata-file]
git commit -m "Add article: [title]"
git push
```

**Deployment:**
- Automatic via GitHub Actions (if configured)
- Or manual deployment process

## Post-Publication

### Monitor and Maintain

**Schedule reviews:**
```yaml
publication:
  next_review_date: "[90 days from now]"
  review_frequency: "quarterly"
```

**React to:**
- Reader feedback
- Technology updates
- Broken links
- New related content

### Continuous Improvement

**Quarterly:**
- Re-run fact-checking
- Update version information
- Check links
- Assess if content needs refresh

**When technology updates:**
- Run fact-checking
- Update examples
- Note version changes
- Re-test code

## Time Estimates

| Phase | Estimated Time | Notes |
|-------|---------------|-------|
| Planning | 30-60 min | Research and outline |
| Drafting | 2-4 hours | Depends on topic complexity |
| Validation | 1-2 hours | Running prompts and fixing issues |
| Review | 30-60 min | Self-review and refinement |
| Final Check | 15-30 min | Publish-ready checklist |
| Publication | 15 min | Metadata and deployment |

**Total:** 5-8 hours for a comprehensive article

## Tips for Efficiency

1. **Batch similar tasks**: Run all validations together
2. **Use validation caching**: Don't re-run if content unchanged
3. **Template adherence**: Following template saves validation time
4. **Incremental validation**: Run checks as you write sections
5. **Reuse research**: Build on existing articles rather than starting fresh

## Common Pitfalls

❌ **Skipping planning**: Results in unfocused content  
✅ **Start with clear outline**: Saves time and improves quality

❌ **Writing without validation**: Rework needed at end  
✅ **Validate incrementally**: Catch issues early

❌ **Ignoring metadata**: Makes tracking and maintenance hard  
✅ **Keep metadata current**: Enables smart automation

❌ **Publishing without fact-check**: Credibility risk  
✅ **Always verify facts**: Build trust with readers
