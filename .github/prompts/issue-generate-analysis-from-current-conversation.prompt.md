---
name: issue-generate-analysis
description: Generate analysis from current conversation
agent: agent
model: claude-sonnet-4.5
tools: ['codebase', 'fetch']
argument-hint: 'topic="Your Article Topic" outline="key points to cover"'
---

# Generate analysis from current conversation

## Goal
Generate a comprehensive issue markdown document using the enhanced structure defined in the file `ISSUE Template.md`.
The issue markdown document should be created in the folder `/src/docs/90. Issues/YYYYMM/<DatePrefix> - <IssueTitle>/` and named as `00. ISSUE Overview.md`.

## Input Sources (Collect from all available sources)

**Gather information from ALL available sources:**
- User-provided information in chat message (structured sections or placeholders like `{{IssueTitle}}` `{{Severity}}` `{{Component}}`)
- Active file or selection (detect content type: issue draft, error log, debug output, code file with errors)
- Attached files with `#file` (detect content type and extract relevant issue information)
- Workspace context files (existing issue reports, log files, error output files)
- Conversation analysis (fallback when no external sources available)
- Explicit file paths provided as arguments

**Content Detection (don't rely solely on filenames):**
- **Issue draft content**: Contains metadata (Date, Author, Severity, Component), structured sections (DESCRIPTION, CONTEXT, ANALYSIS)
- **Error log content**: Contains stack traces, exception messages, timestamps, error codes
- **Debug output content**: Contains variable values, execution flow, diagnostic information
- **Code file content**: Contains problematic code, inline comments about issues
- Analyze file content to determine type, not just the filename

**Information Priority (when conflicts occur):**
1. **Explicit user input** - User-provided details in chat message override everything
2. **Active file/selection** - Content from open file or selected text
3. **Attached files** - Files explicitly attached with `#file`
4. **Workspace context** - Files found in workspace (logs, existing issues)
5. **Conversation analysis** - Information extracted from debugging conversation
6. **Inferred/derived** - Information calculated or inferred from sources

**Workflow:**
1. Check for user-provided information in chat message (highest priority for conflicting data)
2. Check active file or selection - analyze content to identify issue details
3. Check attached files with `#file` - analyze content to extract metadata and context
4. Search workspace for related files (error logs, previous issue reports)
5. If insufficient data, analyze current conversation for issue details
6. **Merge information from all sources** using priority rules for conflicts
7. Extract or infer: IssueTitle, DatePrefix, Author, Severity, Component, Framework
8. Populate comprehensive issue sections from gathered evidence
9. Generate structured issue following `.github/templates/ISSUE Template.md`

## Expected Input Content

When analyzing source files, look for:

### In Issue Drafts:
- **Metadata**: Date, Author, Status, Severity, Component, Framework version
- **Structured sections**: Description, Context, Analysis, Reproduction Steps, Solution
- **Evidence**: Error messages, code snippets, stack traces

### In Error Logs:
- **Exception details**: Exception type, message, inner exceptions
- **Stack traces**: Call stack with line numbers and method names
- **Timestamps**: When errors occurred
- **Environment info**: Framework version, OS, dependencies

### In Debug Output:
- **Variable states**: Values at time of error
- **Execution flow**: Sequence of operations leading to issue
- **Performance metrics**: Timing, memory usage
- **Diagnostic messages**: Custom logging output

### In Code Files:
- **Problematic code**: Methods/functions with issues
- **Error context**: Surrounding code that may contribute
- **Comments**: Developer notes about known issues
- **Version info**: Framework targets, package versions

### In Conversation:
- **Issue symptoms**: What's going wrong
- **Attempted solutions**: What's been tried
- **Root cause analysis**: Conclusions reached during debugging
- **Resolution details**: How issue was solved (if applicable)

## Instructions

### 1. Gather Information from All Sources
Collect issue details from all available sources following the workflow above:
- **IssueTitle**: A concise, descriptive title about the issue
- **DatePrefix**: The current date in the format `YYYYMMDD` (e.g., `20251125`)
- **Author**: The current user name (e.g., "Dario Airoldi")
- **Severity**: Assess the severity level (Low/Medium/High/Critical)
- **Component**: Identify the affected component or project
- **Framework**: Determine the target framework version

If source files not found or incomplete, ask user to:
- Provide missing metadata as arguments
- Attach relevant files with `#file:path/to/file`
- Open the file containing issue details and re-run
- Confirm inferred values from conversation analysis

### 2. Read and Understand Template Structure
Read the template file located at:
`.github/templates/issue-template.md`

Understand the enhanced structure including:
- **Header with metadata** (Date, Author, Status, Severity, Component, Framework)
- **Table of Contents** with emoji navigation
- **Comprehensive sections** with detailed subsections
- **Modern formatting** with tables, code blocks, and checklists

### 3. Create New Issue Document
Create a new issue document in the folder:
`/src/docs/90. Issues/YYYYMM/<DatePrefix> - <IssueTitle>/` (create folders if they don't exist)

Where:
- `YYYYMM` is the year-month from DatePrefix (e.g., `202511` for November 2025)
- `<DatePrefix>` is the full date in format `YYYYMMDD` (e.g., `20251125`)
- `<IssueTitle>` is the concise issue title

Name the document as:
`00. ISSUE Overview.md`

This folder structure allows grouping issues by month while keeping related files (logs, screenshots, code samples) together in the issue-specific subfolder.

### 4. Fill Content from All Gathered Sources
Analyze ALL gathered sources (files, conversation, user input) and fill ALL sections of the issue report:

#### Required Sections to Complete:
- **📝 DESCRIPTION**: Brief description, error messages, and impact points
- **🔍 CONTEXT INFORMATION**: Environment details, exception details, call stack, variable values
- **🔬 ANALYSIS**: Root cause analysis, impact assessment, affected workflows
- **🔄 REPRODUCTION STEPS**: Step-by-step reproduction and affected code locations
- **✅ SOLUTION IMPLEMENTED**: Fix overview, code changes, solution features (if solution was discussed)
- **📚 ADDITIONAL INFORMATION**: Testing recommendations, migration considerations, performance impact
- **✔️ RESOLUTION STATUS**: Current status, verification checklist, follow-up actions
- **🎓 LESSONS LEARNED**: What went wrong/right, improvements for future
- **📎 APPENDIX**: Additional reference materials and examples

#### Content Guidelines:
- Use **emojis** in section headers for visual appeal
- Include **comprehensive tables** for structured data
- Add **code snippets** with proper syntax highlighting
- Use **checkboxes** for actionable items
- Include **links and references** where applicable
- Maintain **professional technical writing** style
- **Cite sources**: Reference which source provided each piece of information
- **Merge conflicting info**: Use priority rules when sources disagree
- **Mark inferred data**: Clearly indicate information derived from analysis vs explicitly stated

### 5. Quality Assurance
Ensure the generated document:
- ✅ Follows the exact template structure
- ✅ Includes Table of Contents with proper anchor links
- ✅ Contains all emoji headers as specified
- ✅ Has comprehensive content in each section
- ✅ Uses consistent formatting throughout
- ✅ Includes actionable follow-up items
- ✅ Provides clear reproduction steps
- ✅ Documents lessons learned for future prevention
- ✅ **Accurately merges information from all sources**
- ✅ **Uses priority rules correctly when sources conflict**
- ✅ **Clearly indicates source of each piece of information when helpful**

## Example Invocations

### Scenario 1: Working with error log file

#### Source Materials

**Active File:**
error.log (contains stack trace and exception details)

**User Input:**
Severity: High, Component: Authentication Service

**Conversation Context:**
Discussion about intermittent login failures

#### Output

**Issue Title:**
Intermittent NullReferenceException in Authentication Service

**Filename:**
`/src/docs/90. Issues/202511/20251125 - Intermittent NullReferenceException in Authentication Service/00. ISSUE Overview.md`

**Metadata Sources:**
- Title: Inferred from error log + conversation
- Date: Current date (20251125)
- Severity: User input (High)
- Component: User input (Authentication Service)
- Framework: Extracted from error log (.NET 8.0)

---

### Scenario 2: Working with existing issue draft

#### Source Materials

**Active File:**
draft-issue.md (partial issue report with some sections filled)

**Attached Files:**
`#file:logs/debug-output.txt` (detailed diagnostic information)

#### Output

**Issue Title:**
From draft-issue.md metadata

**Filename:**
`/src/docs/90. Issues/202511/20251125 - [Title from Draft]/00. ISSUE Overview.md`

**Content Strategy:**
- Use existing draft sections as base
- Enhance with details from debug-output.txt
- Fill missing sections from conversation analysis

---

### Scenario 3: Pure conversation analysis (no files)

#### Source Materials

**Conversation Only:**
Debugging session discussing memory leak in background worker

**User Input:**
Component: BackgroundWorkerService

#### Output

**Issue Title:**
Memory Leak in BackgroundWorkerService

**Filename:**
`/src/docs/90. Issues/202511/20251125 - Memory Leak in BackgroundWorkerService/00. ISSUE Overview.md`

**Content Strategy:**
- Extract all details from conversation history
- Mark sections as "based on conversation analysis"
- Recommend attaching logs/diagnostics for verification

   
