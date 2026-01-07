# Context Engineering Principles for GitHub Copilot

**Purpose**: Consolidated best practices for crafting effective instructions, prompts, and agent personas for GitHub Copilot. This document serves as the single source of truth for context engineering principles across all file types.

**Referenced by**: `.github/instructions/prompts.instructions.md`, `.github/instructions/agents.instructions.md`, all prompt and agent files

---

## Core Principles

### 1. Narrow Scope, Specific Purpose

**Principle**: Each prompt, agent, or instruction file should have a single, well-defined responsibility.

**Why it matters**: 
- Reduces ambiguity in AI interpretation
- Makes files easier to maintain and debug
- Enables better composition through handoffs
- Improves caching and reuse efficiency

**Application**:
- ❌ **Bad**: "Create or update a prompt file, validate it, and update documentation"
- ✅ **Good**: Three separate agents: prompt-builder, prompt-validator, documentation-updater

**Guidelines**:
- One primary user goal per file
- Related sub-tasks are acceptable if they serve the primary goal
- Use handoffs for sequential workflows
- Use composition for parallel capabilities

### 2. Early Commands, Clear Structure

**Principle**: Place the most important instructions and commands at the beginning of your content.

**Why it matters**:
- AI models give more weight to earlier content
- Critical constraints must be established before detailed instructions
- Early structure aids AI's mental model formation

**Application**:
```markdown
<!-- HIGH PRIORITY - Place at top -->
1. Core objective statement
2. Critical constraints (NEVER/ALWAYS rules)
3. Primary workflow/process
4. Tool configuration
5. Handoff definitions

<!-- LOWER PRIORITY - Place after -->
6. Examples and edge cases
7. Quality checklists
8. Background context
9. Troubleshooting tips
```

**YAML Frontmatter Priority**:
Always place critical configuration in YAML frontmatter, not buried in Markdown:
```yaml
---
description: "Single-sentence role definition"  # AI reads this first
tools: ['read_file', 'editor']  # Tool scope defined immediately
agent: plan  # Behavioral constraints upfront
---
```

### 3. Imperative Language

**Principle**: Use direct, commanding language that leaves no room for interpretation.

**Why it matters**:
- Reduces probabilistic behavior in AI responses
- Creates enforceable boundaries
- Establishes clear expectations

**Command Hierarchy**:

| Strength | Phrase | Use Case | Example |
|----------|--------|----------|---------|
| **Absolute** | `NEVER`, `MUST NOT` | Hard boundaries, safety rules | "NEVER modify files outside .github/prompts/" |
| **Required** | `MUST`, `ALWAYS`, `WILL` | Core requirements | "You MUST validate YAML syntax before saving" |
| **Expected** | `SHOULD`, `PREFER` | Best practices, soft preferences | "You SHOULD use handoffs for multi-step workflows" |
| **Optional** | `CAN`, `MAY`, `CONSIDER` | Suggestions, alternatives | "You MAY ask for clarification if requirements are vague" |

**Examples**:

❌ **Weak** (probabilistic):
```markdown
It would be helpful if you could check for syntax errors.
Try to keep the file under 200 lines.
Consider using semantic search to find similar patterns.
```

✅ **Strong** (imperative):
```markdown
You MUST validate YAML syntax before saving any changes.
You WILL keep the file under 200 lines; factor into multiple files if needed.
You MUST use semantic_search to analyze at least 3 similar prompts before building.
```

### 4. Three-Tier Boundary System

**Principle**: Explicitly define what the AI MUST do, what requires approval, and what is forbidden.

**Why it matters**:
- Prevents overstepping boundaries (e.g., editing wrong files)
- Clarifies decision authority
- Reduces user interruptions for trivial decisions
- Establishes trust through predictable behavior

**Structure**:
```yaml
boundaries:
  always_do:  # No approval needed - execute immediately
    - "Validate YAML syntax before saving"
    - "Create backup files when updating"
    - "Use semantic_search before building new prompts"
  
  ask_first:  # Requires user approval before proceeding
    - "Modify existing validated prompts"
    - "Change agent handoff workflows"
    - "Delete any prompt files"
  
  never_do:  # Hard boundaries - refuse with explanation
    - "Modify instruction files without explicit request"
    - "Remove validation metadata from bottom YAML"
    - "Create prompts that violate repository conventions"
```

**Application to File Types**:

| File Type | Always Do | Ask First | Never Do |
|-----------|-----------|-----------|----------|
| **Prompts** | Validate syntax, check tool availability | Modify workflow logic, change tool lists | Break YAML format, remove metadata |
| **Agents** | Validate handoff targets, check tool scope | Add new tools, modify persona | Grant unrestricted file access |
| **Instructions** | Verify `applyTo` patterns | Change core principles | Create circular dependencies |

### 5. Context Minimization

**Principle**: Include only the context necessary for the specific task; reference external sources for additional details.

**Why it matters**:
- Reduces token usage and execution cost
- Improves AI focus on relevant information
- Makes files easier to maintain (single source of truth)
- Enables better composition and reuse

**Techniques**:

**Use References Instead of Duplication**:
```markdown
❌ **Bad**: Embed 100 lines of context engineering principles in every prompt

✅ **Good**: 
For context engineering principles, see `.copilot/context/prompt-engineering/context-engineering-principles.md`
For tool composition patterns, see `.copilot/context/prompt-engineering/tool-composition-guide.md`
```

**Selective File Reads**:
```markdown
❌ **Bad**: "Read all files in .github/prompts/ before starting"

✅ **Good**: "Use semantic_search with query 'validation workflow' to find 3 relevant prompts"
```

**Lazy Loading Pattern**:
```markdown
## Process

1. **Gather Requirements** - Minimal context, user interaction only
2. **Research Phase** - NOW use semantic_search, read_file, grep_search
3. **Build Phase** - Access templates and examples
4. **Validate Phase** - Load validation rules and checklists
```

### 6. Tool Scoping and Security

**Principle**: Grant only the minimum tools necessary for the agent's role; use `agent: plan` for read-only operations.

**Why it matters**:
- Prevents accidental file modifications
- Reduces risk of cascading errors
- Makes agent behavior more predictable
- Aligns with principle of least privilege

**Tool Categories**:

| Category | Tools | Risk Level | Use For |
|----------|-------|------------|---------|
| **Read-Only** | `read_file`, `grep_search`, `semantic_search`, `list_dir`, `file_search` | ✅ Low | Research, analysis, validation |
| **Inspection** | `get_errors`, `copilot_getNotebookSummary` | ✅ Low | Quality checks, diagnostics |
| **Write** | `create_file`, `replace_string_in_file`, `multi_replace_string_in_file` | ⚠️ Medium | Content creation, targeted updates |
| **Destructive** | File deletion, workspace restructuring | 🚫 High | Avoid; use terminal with approval |
| **External** | `fetch_webpage`, `github_repo`, `run_in_terminal` | ⚠️ Medium | Research, environment setup |

**Agent Type Guidelines**:

```yaml
# Read-only agent (researcher, validator)
---
agent: plan
tools: ['read_file', 'semantic_search', 'grep_search', 'file_search']
---

# Builder agent (needs write access)
---
agent: agent  # Default, allows file creation
tools: ['read_file', 'semantic_search', 'create_file', 'replace_string_in_file']
---

# Orchestrator prompt (delegates, minimal tools)
---
tools: ['read_file', 'semantic_search']  # Only for Phase 1 analysis
handoffs:
  - label: "Research Requirements"
    agent: prompt-researcher
    send: true
---
```

**Security Boundaries**:
```markdown
## Tool Boundaries

✅ **ALWAYS ALLOWED**:
- Read any file in `.github/` directories
- Search workspace with semantic_search or grep_search
- Validate syntax without modifying files

⚠️ **ASK FIRST**:
- Create new files in `.github/agents/` or `.github/prompts/`
- Modify existing prompts or agents
- Install packages or extensions

🚫 **NEVER ALLOWED**:
- Modify files outside `.github/` and `.copilot/` directories
- Delete prompt or agent files without explicit user request
- Execute terminal commands that modify system configuration
```

---

## Application by File Type

### For Prompt Files (.prompt.md)

**Focus**: Task-specific workflows with clear start and end conditions

**Context Engineering Priorities**:
1. **Narrow Scope**: Single task (e.g., "grammar review" not "full validation")
2. **Early Commands**: Process steps first, examples last
3. **Imperative Language**: "You MUST validate..." not "It's good to validate..."
4. **Three-Tier Boundaries**: Define always_do, ask_first, never_do
5. **Context Minimization**: Reference context files, don't embed principles
6. **Tool Scoping**: Only tools needed for this specific task

**Template Section Order**:
```markdown
---
# YAML frontmatter with tools, agent type
---

# [Prompt Name]

**YOUR ROLE**: [One sentence, imperative]

## Process
[Numbered steps, imperative verbs]

## Boundaries
### ✅ Always Do
### ⚠️ Ask First
### 🚫 Never Do

## Output Format
[Specific structure requirements]

## Quality Checklist
[Verification steps]

## Examples
[Edge cases and patterns]
```

### For Agent Files (.agent.md)

**Focus**: Persistent persona with role-specific expertise

**Context Engineering Priorities**:
1. **Narrow Scope**: Single role (e.g., "researcher" not "researcher and builder")
2. **Early Commands**: Persona definition first, then capabilities
3. **Imperative Language**: "You MUST handoff to..." not "Consider handing off..."
4. **Three-Tier Boundaries**: Especially critical for file access
5. **Context Minimization**: Reference style guides, don't embed full guides
6. **Tool Scoping**: Minimal set for role (researcher = read-only)

**Agent Persona Elements**:
1. **Core Identity**: "You are a research specialist..."
2. **Expertise**: "You excel at pattern discovery and requirement analysis..."
3. **Boundaries**: "You NEVER create or modify files; you prepare research reports..."
4. **Collaboration**: "You handoff to prompt-builder when research is complete..."
5. **Communication Style**: "You present findings in structured reports with evidence..."

### For Instruction Files (.instructions.md)

**Focus**: Context-specific rules auto-applied based on glob patterns

**Context Engineering Priorities**:
1. **Narrow Scope**: Rules for one file type or directory (via `applyTo`)
2. **Early Commands**: Critical constraints first, conventions last
3. **Imperative Language**: Universal rules that apply to all matching contexts
4. **Three-Tier Boundaries**: Universal never_do rules (e.g., "NEVER remove validation metadata")
5. **Context Minimization**: HIGH PRIORITY - Instructions should be concise, reference context files
6. **Tool Scoping**: Not applicable (instructions don't define tools directly)

**Optimal Length**: 80-120 lines (down from 200-400 with context consolidation)

**Content Guidelines**:
```markdown
✅ **INCLUDE IN INSTRUCTIONS**:
- Repository-specific conventions (e.g., "All prompts use dual YAML metadata")
- File type patterns (e.g., "Prompt files must end with .prompt.md")
- Universal boundaries (e.g., "NEVER modify top YAML block from validation prompts")
- Quality standards (e.g., "All agent files MUST have a description field")

❌ **MOVE TO CONTEXT FILES**:
- Context engineering principles (→ context-engineering-principles.md)
- Tool composition patterns (→ tool-composition-guide.md)
- Detailed examples and templates (→ templates/)
- Process workflows (→ prompts or agents)
```

---

## Common Anti-Patterns

### ❌ Anti-Pattern 1: Vague Instructions
```markdown
Try to keep prompts focused.
It's generally better to use handoffs.
Consider checking for errors.
```
**Problem**: Probabilistic language creates inconsistent behavior

**Fix**: Use imperative language with clear requirements
```markdown
You MUST limit prompts to one primary task; use handoffs for multi-step workflows.
You WILL validate YAML syntax before saving.
```

### ❌ Anti-Pattern 2: Scope Creep
```markdown
# prompt-validator.agent.md
You are a validation specialist. You check prompts for errors,
suggest improvements, update the files with fixes, and also
generate documentation about validation results.
```
**Problem**: Agent has 4 responsibilities (validate, suggest, update, document)

**Fix**: Split into specialized agents
- `prompt-validator`: Check and report (read-only)
- `prompt-updater`: Apply fixes (write access)
- `documentation-updater`: Generate docs (separate domain)

### ❌ Anti-Pattern 3: Embedded Documentation
```markdown
# Inside a prompt file:

## Context Engineering Principles
[100 lines of principles that belong in context files]

## Tool Usage Guide
[50 lines of tool documentation]

## Validation Rules
[80 lines of validation logic]
```
**Problem**: Duplicates content, increases maintenance burden, wastes tokens

**Fix**: Reference external context
```markdown
## Prerequisites

Before starting, review:
- Context engineering principles: `.copilot/context/prompt-engineering/context-engineering-principles.md`
- Tool usage patterns: `.copilot/context/prompt-engineering/tool-composition-guide.md`
```

### ❌ Anti-Pattern 4: Weak Boundaries
```markdown
## Boundaries
- Try not to modify files outside the target directory
- Probably should ask before deleting things
```
**Problem**: "Try not to" and "probably should" are not enforceable

**Fix**: Use three-tier system with imperative language
```markdown
## Boundaries

### 🚫 Never Do
- NEVER modify files outside `.github/prompts/` directory
- NEVER delete any files without explicit user approval
```

### ❌ Anti-Pattern 5: Late Critical Information
```markdown
[300 lines of examples and context]

## Important: NEVER modify the top YAML block
[Critical constraint buried at end]
```
**Problem**: AI may miss critical information placed late in content

**Fix**: Place critical constraints at the top
```yaml
---
description: "Validation specialist - read-only operations"
agent: plan  # Establishes read-only constraint immediately
---

# Prompt Validator Agent

**CRITICAL CONSTRAINTS**:
- You NEVER modify files; you only analyze and report
- You NEVER touch the top YAML block under any circumstances
```

---

## Validation Checklist

Use this checklist when creating or reviewing prompts, agents, or instructions:

### Scope and Purpose
- [ ] File has single, well-defined responsibility
- [ ] Purpose is stated in first sentence/YAML description
- [ ] Related to other files via handoffs/references, not duplication

### Structure and Priority
- [ ] YAML frontmatter contains critical configuration
- [ ] Most important instructions are in first 20% of content
- [ ] Process/workflow defined before examples
- [ ] Quality checklist or success criteria included

### Language and Clarity
- [ ] Uses imperative verbs (MUST, WILL, NEVER)
- [ ] Avoids weak phrases (try to, consider, probably)
- [ ] Commands are specific and actionable
- [ ] No ambiguous pronouns or references

### Boundaries and Safety
- [ ] Three-tier boundaries explicitly defined
- [ ] File access scope is clear (which directories)
- [ ] Destructive operations require approval
- [ ] Handoff targets are valid agent/prompt names

### Context and Efficiency
- [ ] References context files instead of embedding content
- [ ] Only includes context necessary for this specific task
- [ ] Uses lazy loading (tools called when needed, not preemptively)
- [ ] No duplicated content from other repository files

### Tools and Capabilities
- [ ] Tool list matches role (read-only for validators, write for builders)
- [ ] `agent: plan` used for analysis-only agents
- [ ] No unnecessary tools that increase risk
- [ ] External tools (fetch, terminal) have clear boundaries

---

## Version History

| Version | Date | Changes | Author |
|---------|------|---------|--------|
| 1.0.0 | 2025-12-10 | Initial consolidated version from analysis | System |

---

## References

- **Official Documentation**: [VS Code Copilot Custom Agents](https://code.visualstudio.com/docs/copilot/copilot-customization)
- **Repository Articles**: `tech/PromptEngineering/` series
- **Related Context**: 
  - [tool-composition-guide.md](tool-composition-guide.md) - Tool selection patterns
  - [validation-caching-pattern.md](validation-caching-pattern.md) - 7-day caching rules
  - [handoffs-pattern.md](handoffs-pattern.md) - Multi-agent orchestration
