# Handoffs Pattern for Multi-Agent Orchestration

**Purpose**: Define patterns for coordinating work between specialized agents in multi-step workflows. This document establishes conventions for agent handoffs, intermediary reports, and workflow composition.

**Referenced by**: `.github/instructions/prompts.instructions.md`, `.github/instructions/agents.instructions.md`, orchestrator prompts

---

## Core Principles

### 1. Single Responsibility Agents

**Principle**: Each agent in a handoff chain MUST have exactly one responsibility.

**Agent Role Specialization**:

| Role | Responsibility | Access Level | Output |
|------|---------------|--------------|--------|
| **Researcher** | Gather context, discover patterns | Read-only (`agent: plan`) | Research report |
| **Builder** | Generate new files from specifications | Write (`agent: agent`) | Created file paths |
| **Validator** | Check quality, compliance, errors | Read-only (`agent: plan`) | Validation report |
| **Updater** | Fix identified issues | Write (`agent: agent`) | Updated file paths |

### 2. Explicit Handoff Contracts

**Principle**: Every handoff MUST define what information flows between agents.

**Contract Structure**:
```yaml
handoffs:
  - label: "Research Requirements"
    agent: prompt-researcher
    send: true          # Sends current context to agent
    receive: "report"   # Expects structured report back
    
  - label: "Build Prompt File"
    agent: prompt-builder
    send: true
    input_from: "Research Requirements"  # Uses output from previous step
    receive: "file_path"
```

### 3. Intermediary Reports

**Principle**: Use structured text reports (not JSON) between agent phases.

**Why Text Over JSON**:
- LLMs process natural language more reliably than structured data
- Semantic structure provides implicit prioritization
- Creates human-readable checkpoints for review
- Easier to debug and verify correctness

---

## Handoff Patterns

### Pattern 1: Linear Chain

**Use When**: Tasks have clear sequential dependencies.

```markdown
┌─────────────┐      ┌─────────────┐      ┌─────────────┐
│  Researcher │ ──→  │   Builder   │ ──→  │  Validator  │
└─────────────┘      └─────────────┘      └─────────────┘
   report.md          created_file         validation_report
```

**Orchestrator Configuration**:
```yaml
---
name: create-prompt-workflow
description: "Linear workflow: research → build → validate"
agent: plan
tools: ['read_file', 'semantic_search']
handoffs:
  - label: "Research Patterns"
    agent: prompt-researcher
    send: true
  - label: "Generate File"
    agent: prompt-builder
    send: true
  - label: "Validate Quality"
    agent: prompt-validator
    send: true
---
```

### Pattern 2: Parallel Research

**Use When**: Multiple independent information sources are needed.

```markdown
                ┌─────────────────┐
           ┌──→ │ Doc Researcher  │ ──┐
           │    └─────────────────┘   │
┌──────────┴─┐                      ┌─┴──────────┐
│Orchestrator│                      │  Aggregator │ ──→ Builder
└──────────┬─┘                      └─┬──────────┘
           │    ┌─────────────────┐   │
           └──→ │ Code Researcher │ ──┘
                └─────────────────┘
```

**Orchestrator Configuration**:
```markdown
## Parallel Research Phase

Launch BOTH researchers simultaneously:

### runSubagent: Documentation Research
**Prompt**: Research official VS Code Copilot documentation for {topic}
**Expected Output**: Summary of best practices with source links

### runSubagent: Pattern Research  
**Prompt**: Analyze existing prompts in this repository matching {pattern}
**Expected Output**: List of 3-5 similar prompts with key patterns

Wait for both to complete before proceeding.
```

### Pattern 3: Validation Loop

**Use When**: Quality gates require iteration until passing.

```markdown
┌──────────┐     ┌───────────┐     ┌─────────┐
│ Builder  │ ──→ │ Validator │ ──→ │ Updater │ ─┐
└──────────┘     └───────────┘     └─────────┘  │
                       ↑                         │
                       └─────────────────────────┘
                       (repeat until validation passes)
```

**Loop Control**:
```markdown
## Validation Loop

### Loop Constraints
- Maximum iterations: 3
- Exit conditions:
  - All validation checks pass
  - User explicitly approves with issues
  - Maximum iterations reached (escalate to user)

### Each Iteration
1. Validator checks current state
2. If issues found → Updater applies fixes
3. Return to Validator
4. If clean → Exit loop and proceed
```

### Pattern 4: Supervised Handoff

**Use When**: High-stakes operations require human approval.

```markdown
┌──────────┐     ┌──────────────┐     ┌──────────┐
│ Builder  │ ──→ │ User Review  │ ──→ │ Deployer │
└──────────┘     └──────────────┘     └──────────┘
                   (STOP - wait)
```

**Checkpoint Configuration**:
```markdown
## Phase 2: User Review Checkpoint

### STOP - Present Plan for Approval

**Summary**: 
- Files to be created: {list}
- Patterns applied: {patterns}
- Estimated changes: {count}

**Request**: Review the plan above and respond:
- "go ahead" - Proceed with file creation
- "modify X" - Adjust plan before proceeding  
- "cancel" - Abort workflow

**IMPORTANT**: Do NOT proceed until explicit user approval.
```

---

## Intermediary Report Format

### Research Report Template

```markdown
# Research Report: {Topic}

## Context Summary
[1-2 paragraph overview of findings]

## Key Patterns Discovered

### Pattern 1: {Name}
- **Location**: {file paths}
- **Usage**: {how it's used}
- **Relevance**: {why it matters for this task}

### Pattern 2: {Name}
...

## Recommendations

1. **MUST apply**: {critical patterns}
2. **SHOULD consider**: {best practices}
3. **AVOID**: {anti-patterns discovered}

## Source References
- {file path or URL 1}
- {file path or URL 2}

## Handoff Notes
**For Builder agent**: Focus on {specific aspects}
**Known constraints**: {limitations discovered}
```

### Validation Report Template

```markdown
# Validation Report: {File}

## Summary
- **Status**: {PASS / FAIL / NEEDS_REVIEW}
- **Issues Found**: {count}
- **Severity**: {Critical / Warning / Info}

## Checks Performed

### ✅ PASSED
- [x] YAML syntax valid
- [x] Required fields present
- [x] Tool list appropriate for agent mode

### ❌ FAILED
- [ ] Three-tier boundaries incomplete
  - **Issue**: Missing "Never Do" section
  - **Fix**: Add boundary section with file access restrictions
  
- [ ] Reference to non-existent file
  - **Issue**: Links to `.copilot/context/missing-file.md`
  - **Fix**: Create file or update reference

## Recommended Actions

### For Updater Agent
1. Add missing "Never Do" section
2. Fix broken file reference

### For User Review
- Approve fixes above
- Consider adding more examples (optional)
```

---

## Anti-Patterns

### ❌ Monolithic Agents

**Problem**: Single agent tries to research, build, AND validate.
```markdown
# Bad: One agent does everything
You are a prompt creator. Research existing patterns, create the new
prompt file, validate it meets all requirements, and fix any issues.
```

**Fix**: Split into specialized agents with explicit handoffs.

### ❌ Implicit Context Passing

**Problem**: Assuming agent B has access to what agent A discovered.
```markdown
# Bad: No explicit handoff
Phase 1: Use researcher to analyze patterns
Phase 2: Builder creates file  # Builder doesn't have Phase 1 context!
```

**Fix**: Explicit `send: true` in handoff or structured report passing.

### ❌ Missing Checkpoints

**Problem**: No stopping points for high-risk operations.
```markdown
# Bad: Auto-deploys without review
After validation passes, automatically deploy to production.
```

**Fix**: Insert user approval checkpoint before destructive operations.

### ❌ Unbounded Loops

**Problem**: Validation loop with no exit condition.
```markdown
# Bad: Can loop forever
Keep fixing issues until validator passes.
```

**Fix**: Maximum iteration count + escalation path.

---

## Implementation Checklist

When designing a multi-agent workflow:

- [ ] Each agent has single, clear responsibility
- [ ] Handoff contracts define send/receive expectations
- [ ] Intermediary reports use text format (not JSON)
- [ ] User approval checkpoints for high-risk operations
- [ ] Validation loops have maximum iteration limits
- [ ] Error handling path defined (what if agent fails?)
- [ ] Context size managed (don't pass entire conversation history)

---

## References

- **Context Engineering Principles**: [context-engineering-principles.md](context-engineering-principles.md)
- **Tool Composition Guide**: [tool-composition-guide.md](tool-composition-guide.md)
- **Prompt Templates**: `.github/templates/prompt-multi-agent-orchestration-template.md`
- **Agent Templates**: `.github/templates/prompt-implementation-template.md`
- **Example Workflows**: `.github/prompts/prompt-createorupdate-prompt-file-v2.prompt.md`

---

## Version History

| Version | Date | Changes | Author |
|---------|------|---------|--------|
| 1.0.0 | 2025-12-26 | Initial version | System |
