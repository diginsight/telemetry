# Global GitHub Copilot Instructions for Learning Documentation Site

## Purpose
This repository is a **learning and personal development documentation site** focused on creating high-quality, accurate, and accessible educational content.

## Core Principles

### Dual Metadata Block Structure
**All articles use two metadata blocks for clean separation:**

1. **Top YAML Block** (Quarto Metadata):
   - Location: Beginning of file (lines 1-X)
   - Format: Standard YAML frontmatter (`---` delimiters)
   - Contains: `title`, `author`, `date`, `categories`, `description`
   - Purpose: Quarto rendering and site generation
   - Visibility: Visible in source, used by Quarto
   - Modified by: Authors manually (NOT by validation prompts or watcher)
   
2. **Bottom HTML Comment Block with YAML** (Article additional metadata):
   - Location: End of file (after References section)
   - Format: HTML comment containing YAML (`<!-- \n---\nYAML\n---\n-->`)
   - Contains: `validations`, `article_metadata`, `cross_references`
   - Purpose: Quality tracking, analytics, cross-referencing
   - Visibility: **Completely hidden from rendered output**
   - Modified by: Validation prompts and content management tools (eg.IQPilot)

**Critical Rules:**
- ❌ Validation prompts must NEVER modify top YAML block
- ✅ Validation prompts update only their section in bottom metadata
- ✅ IQPilot tools updates `article_metadata.filename` in bottom metadata
- ✅ Bottom metadata wrapped in HTML comment for complete invisibility
- ✅ All metadata travels with the article 

**Example Structure:**
```markdown
---
title: "Article Title"
author: "Author Name"
date: "2025-11-21"
---

# Article Content

...

<!-- 
---
validations:
  grammar: {...}
article_metadata:
  filename: "article.md"
---
-->
```

See `.copilot/context/dual-yaml-helpers.md` for complete parsing guidelines.

**Note on Metadata:** Some articles may contain an HTML comment block at the end with YAML metadata managed by validation tools. This metadata tracks validation history and quality metrics. Validation prompts should update only this bottom metadata block, never the top YAML frontmatter.

### Content Quality Standards
- **Accuracy First**: Always verify facts against authoritative sources before publishing
- **Citation Required**: Include references section with links to all sources used
- **Up-to-Date Information**: Check that information is current; flag outdated content
- **Evidence-Based**: Support claims with verifiable evidence or documentation

### Writing Standards
- **Clarity**: Use clear, concise language appropriate for the target audience
- **Structure**: Follow standard article templates with TOC, introduction, body, conclusion, and references
- **Consistency**: Maintain consistent terminology, formatting, and style across articles
- **Readability**: Aim for Grade 9-10 reading level unless technical depth requires higher complexity
- **Non-Redundancy**: Avoid repeating information; link to existing content instead

### Technical Standards
- **Markdown Format**: All content in Markdown with proper heading hierarchy
- **Code Examples**: Include syntax highlighting, explanations, and working examples
- **Accessibility**: Use descriptive link text, alt text for images, and semantic HTML
- **Cross-References**: Link related articles and maintain series navigation

### Validation Requirements
- Check grammar and spelling before finalizing content
- Verify logical flow and concept connections
- Ensure all required sections are present (TOC, references, etc.)
- Validate metadata is complete and up-to-date
- Run fact-checking against official documentation sources

### Metadata Management
- Metadata may be embedded in articles using dual YAML blocks
- Top YAML: Document properties (title, author, date) - manual only
- Bottom YAML (in HTML comment): Article additional metadata - updated by validation prompts
- Automatic sync: Some tools may update filename on rename

## Tools and Automation
- Use prompt files from `.github/prompts/` for consistent automation
- Follow templates from `.github/templates/` for new content
- Reference context materials in `.copilot/context/` for guidance
- Leverage validation caching to avoid redundant checks
- Use PowerShell scripts in `.copilot/scripts/` for programmatic tasks

## Preferred Models and Modes
- Default to Claude Sonnet 4.5 for complex analysis and generation
- Use agent mode for multi-step workflows
- Use ask mode for analysis and review tasks
- Use edit mode for inline content improvements

## When Creating Content
1. Start with appropriate template from `.github/templates/`
2. Initialize metadata file using `metadata-init` prompt
3. Draft content following structure guidelines
4. Run validation prompts (grammar, readability, structure)
5. Verify facts using `fact-checking` prompt
6. Update metadata with validation results
7. Check for gaps and logical flow
8. Run final `publish-ready` checklist

## When Reviewing Content
1. Check metadata for last validation dates
2. Skip validations if article unchanged and recent
3. Focus on modified sections for efficiency
4. Update metadata with new validation results
5. Suggest improvements with clear rationale
6. Maintain constructive, educational tone

## File Organization
- Articles stored in subject-specific folders (e.g., `tech/`, `howto/`)
- Metadata files adjacent to articles (`.metadata.yml`)
- Images in article-specific subdirectories
- Series navigation maintained in parent folders
