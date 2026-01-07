# Tool Composition Guide for GitHub Copilot

**Purpose**: Comprehensive reference for tool selection, composition patterns, and priority rules when creating prompts and agents for GitHub Copilot.

**Referenced by**: `.github/instructions/prompts.instructions.md`, `.github/instructions/agents.instructions.md`, all builder and researcher agents

---

## Tool Priority Rules

### Priority Hierarchy

When GitHub Copilot has access to tools from multiple sources, it applies them in this order:

1. **Prompt-level tools** (highest priority)
   - Defined in `tools:` YAML frontmatter of `.prompt.md` files
   - Invoked via `/promptName` command
   - Scope: Only while that specific prompt is executing

2. **Agent-level tools**
   - Defined in `tools:` YAML frontmatter of `.agent.md` files
   - Active when that agent is selected in agent picker
   - Scope: Entire conversation while agent is active

3. **Default tools** (lowest priority)
   - Built-in VS Code Copilot tools
   - Always available regardless of prompt/agent
   - Scope: All conversations

**Example**:
```yaml
# .github/prompts/example.prompt.md
---
tools: ['read_file']  # ONLY read_file available during prompt execution
---

# .github/agents/example.agent.md  
---
tools: ['read_file', 'semantic_search', 'create_file']  # 3 tools when agent active
---

# Default tools when no prompt/agent active:
# read_file, semantic_search, create_file, replace_string_in_file, grep_search, etc.
```

### When to Define Tools at Each Level

| Level | Use When | Example |
|-------|----------|---------|
| **Prompt-level** | Task requires tool restriction beyond agent's normal scope | Grammar validator prompt needs `agent: plan` + read-only tools, but agent normally has write access |
| **Agent-level** | Role consistently needs specific toolset | Researcher agent always needs `semantic_search`, `grep_search`, `read_file` but never write tools |
| **Default** | General-purpose tasks with no special restrictions | Quick questions, exploratory code reviews |

---

## Tool Categories and Use Cases

### Read-Only Tools (Safe for `agent: plan`)

**Purpose**: Gather information without modifying workspace

| Tool | Best For | Returns | Cost |
|------|----------|---------|------|
| `read_file` | Reading specific known files | File content (with line range support) | Low |
| `semantic_search` | Finding relevant code/docs by meaning | Ranked file excerpts with context | Medium |
| `grep_search` | Finding exact strings/regex patterns | File paths + line numbers + excerpts | Low |
| `file_search` | Finding files by name/glob pattern | File paths | Very Low |
| `list_dir` | Listing directory contents | File/folder names | Very Low |
| `get_errors` | Checking compile/lint errors | Error messages with file locations | Low |
| `copilot_getNotebookSummary` | Analyzing notebook structure | Cell IDs, types, execution status | Low |

**Composition Patterns**:

```yaml
# Pattern 1: Research-first workflow
tools: ['semantic_search', 'read_file', 'grep_search']
# Workflow: semantic_search (find candidates) → read_file (deep dive) → grep_search (verify patterns)

# Pattern 2: Targeted analysis  
tools: ['file_search', 'read_file', 'get_errors']
# Workflow: file_search (find file) → read_file (inspect) → get_errors (validate)

# Pattern 3: Pattern discovery
tools: ['grep_search', 'read_file', 'list_dir']
# Workflow: grep_search (find all occurrences) → read_file (analyze context) → list_dir (check related files)
```

### Write Tools (Requires `agent: agent` or default)

**Purpose**: Modify workspace files

| Tool | Best For | Scope | Risk Level |
|------|----------|-------|------------|
| `create_file` | Creating new files | Single file | Medium |
| `replace_string_in_file` | Targeted single edit | One string replacement | Low |
| `multi_replace_string_in_file` | Batch editing | Multiple replacements across files | Medium |
| `edit_notebook_file` | Modifying Jupyter notebooks | Notebook cell operations | Medium |

**Composition Patterns**:

```yaml
# Pattern 1: Builder agent (research → create)
tools: ['semantic_search', 'read_file', 'create_file']
# Workflow: semantic_search (find similar) → read_file (analyze patterns) → create_file (generate new)

# Pattern 2: Updater agent (find → modify)
tools: ['grep_search', 'read_file', 'replace_string_in_file']
# Workflow: grep_search (find targets) → read_file (verify context) → replace_string_in_file (update)

# Pattern 3: Batch processor
tools: ['file_search', 'read_file', 'multi_replace_string_in_file']
# Workflow: file_search (find all targets) → read_file (validate) → multi_replace (apply changes)
```

### External Tools (Use with caution)

**Purpose**: Access external resources or execute commands

| Tool | Best For | Security Notes | Typical Use |
|------|----------|----------------|-------------|
| `fetch_webpage` | Retrieving documentation | Only public URLs | Research official docs, fetch examples |
| `github_repo` | Searching GitHub repos | Only public repos | Find best practices, analyze patterns |
| `run_in_terminal` | Executing shell commands | Full system access risk | Building projects, running tests |
| `runSubagent` | Complex multi-step research | Spawns independent agent | Deep analysis, extensive searches |

**Composition Patterns**:

```yaml
# Pattern 1: Research with external sources
tools: ['fetch_webpage', 'semantic_search', 'read_file']
# Workflow: fetch_webpage (get official docs) → semantic_search (find local context) → read_file (verify consistency)

# Pattern 2: Best practice discovery
tools: ['github_repo', 'semantic_search', 'grep_search']
# Workflow: github_repo (find patterns) → semantic_search (find similar local code) → grep_search (verify adoption)

# Pattern 3: Build and validate
tools: ['read_file', 'create_file', 'run_in_terminal']  
# Workflow: read_file (check config) → create_file (generate) → run_in_terminal (test build)
```

---

## Tool Selection by Agent Role

### Researcher Agent

**Role**: Analyze requirements, discover patterns, gather context

**Recommended Tools**:
```yaml
---
description: "Research specialist for requirements and pattern discovery"
agent: plan  # Read-only enforced
tools:
  - semantic_search  # Find relevant existing code
  - grep_search      # Locate specific patterns
  - read_file        # Deep dive into files
  - file_search      # Find files by name
  - fetch_webpage    # Research official docs (optional)
  - github_repo      # Find external best practices (optional)
---
```

**Typical Workflow**:
1. `semantic_search` to find 3-5 relevant similar files
2. `read_file` to analyze each candidate thoroughly
3. `grep_search` to identify common patterns (e.g., all files using specific YAML field)
4. `fetch_webpage` to verify against official documentation
5. Output: Research report with findings + recommendations

**Anti-patterns**:
- ❌ Including `create_file` or `replace_string_in_file` (violates read-only role)
- ❌ Including `run_in_terminal` (researchers shouldn't execute code)
- ❌ Too few tools (need at least `semantic_search` + `read_file` for effective research)

### Builder Agent

**Role**: Generate new files based on research and templates

**Recommended Tools**:
```yaml
---
description: "Prompt file generator following validated patterns"
agent: agent  # Default, allows file creation
tools:
  - read_file           # Access templates and context
  - semantic_search     # Find similar patterns for consistency
  - create_file         # Generate new prompts/agents
  - file_search         # Locate templates
---
```

**Typical Workflow**:
1. `read_file` to load template (e.g., `prompt-simple-validation-template.md`)
2. `read_file` to access research report from researcher agent
3. `semantic_search` to find 2-3 similar existing files for consistency
4. `create_file` to generate new prompt/agent with proper structure
5. Handoff to validator agent

**Anti-patterns**:
- ❌ Including `replace_string_in_file` (builders create, updaters modify)
- ❌ Including `run_in_terminal` (builders don't test/execute)
- ❌ Omitting `semantic_search` (builders need context for consistency)

### Validator Agent

**Role**: Quality assurance, syntax checking, best practice verification

**Recommended Tools**:
```yaml
---
description: "Quality assurance and optimization specialist"
agent: plan  # Read-only enforced
tools:
  - read_file     # Inspect prompt/agent files
  - grep_search   # Find potential issues across files
  - get_errors    # Check for syntax errors (if applicable)
---
```

**Typical Workflow**:
1. `read_file` to load file for validation
2. Parse YAML frontmatter (verify required fields)
3. Check three-tier boundaries structure
4. Verify tool list matches agent type
5. `grep_search` to check for anti-patterns
6. Output: Validation report with pass/fail + recommendations

**Anti-patterns**:
- ❌ Including any write tools (validators report, they don't fix)
- ❌ Using `agent: agent` (must be read-only)
- ❌ Including `semantic_search` (expensive, usually not needed for validation)

### Updater Agent

**Role**: Apply targeted modifications to existing files

**Recommended Tools**:
```yaml
---
description: "Specialized updater for existing prompt and agent files"
agent: agent  # Needs write access
tools:
  - read_file                    # Load current file
  - grep_search                  # Find modification targets
  - replace_string_in_file       # Single targeted update
  - multi_replace_string_in_file # Batch updates
---
```

**Typical Workflow**:
1. `read_file` to load file + validation report from validator
2. `grep_search` to locate specific sections needing updates
3. `replace_string_in_file` for single edit, or `multi_replace_string_in_file` for batch
4. Handoff to validator for re-validation

**Anti-patterns**:
- ❌ Including `create_file` (updaters modify, builders create)
- ❌ Using `agent: plan` (updaters need write access)
- ❌ Omitting `grep_search` (updaters need precise targeting)

### Orchestrator Prompt

**Role**: Coordinate handoffs, minimal direct operations

**Recommended Tools**:
```yaml
---
description: "Orchestrates prompt creation workflow via agent handoffs"
tools:
  - read_file        # For Phase 1 requirements analysis only
  - semantic_search  # For determining which agents to invoke
handoffs:
  - label: "Research Requirements"
    agent: prompt-researcher
    send: true
  - label: "Build Prompt"
    agent: prompt-builder
    send: false
  - label: "Validate Prompt"
    agent: prompt-validator
    send: true
---
```

**Typical Workflow**:
1. Phase 1: Gather requirements (minimal `read_file` if needed)
2. Handoff to `prompt-researcher` with requirements
3. Wait for research report
4. Handoff to `prompt-builder` with research
5. Wait for draft prompt
6. Handoff to `prompt-validator` with draft
7. Present validation report to user for approval

**Anti-patterns**:
- ❌ Including write tools (orchestrator delegates, doesn't implement)
- ❌ No handoffs defined (defeats purpose of orchestration)
- ❌ Too many tools (orchestrator should be minimal)

---

## Tool Composition Anti-Patterns

### ❌ Anti-Pattern 1: Over-Scoping (Tool Bloat)

**Problem**: Including every possible tool "just in case"

```yaml
# Bad: Researcher agent with unnecessary tools
---
agent: plan
tools:
  - semantic_search
  - grep_search
  - read_file
  - create_file         # ❌ Researchers don't create
  - replace_string_in_file  # ❌ Researchers don't modify
  - run_in_terminal     # ❌ Researchers don't execute
---
```

**Fix**: Only include tools essential for role
```yaml
# Good: Focused tool list
---
agent: plan
tools:
  - semantic_search
  - grep_search
  - read_file
---
```

### ❌ Anti-Pattern 2: Under-Scoping (Missing Critical Tools)

**Problem**: Omitting tools needed for role, forcing workarounds

```yaml
# Bad: Builder agent without read access
---
agent: agent
tools:
  - create_file  # ❌ How to access templates without read_file?
---
```

**Fix**: Include tools for complete workflow
```yaml
# Good: Builder can access templates and research
---
agent: agent
tools:
  - read_file       # Access templates and research reports
  - semantic_search # Find similar patterns
  - create_file     # Generate new files
---
```

### ❌ Anti-Pattern 3: Conflicting Agent Type + Tools

**Problem**: Declaring `agent: plan` but including write tools

```yaml
# Bad: Read-only agent type with write tool
---
agent: plan  # ❌ Says "read-only"
tools:
  - read_file
  - replace_string_in_file  # ❌ But has write tool
---
```

**Fix**: Align agent type with tool capabilities
```yaml
# Good: Read-only agent with read-only tools
---
agent: plan
tools:
  - read_file
  - grep_search
  - semantic_search
---
```

### ❌ Anti-Pattern 4: Redundant Tool Definitions

**Problem**: Defining same tools at prompt and agent level unnecessarily

```yaml
# Agent file
---
agent: agent
tools: ['read_file', 'semantic_search', 'create_file']
---

# Prompt file (same agent selected)
---
tools: ['read_file', 'semantic_search', 'create_file']  # ❌ Redundant
---
```

**Fix**: Only define prompt-level tools if restricting agent's normal scope
```yaml
# Prompt file (ONLY if restricting)
---
tools: ['read_file']  # ✅ Intentionally restricting to read-only for this task
agent: plan           # ✅ Enforce read-only behavior
---
```

### ❌ Anti-Pattern 5: External Tools Without Boundaries

**Problem**: Allowing `run_in_terminal` without constraints

```yaml
# Bad: Unrestricted terminal access
---
tools:
  - read_file
  - create_file
  - run_in_terminal  # ❌ No boundaries defined
---

## Process
1. Create file
2. Run terminal command  # Could execute anything!
```

**Fix**: Define explicit boundaries for dangerous tools
```yaml
# Good: Terminal access with boundaries
---
tools:
  - read_file
  - create_file
  - run_in_terminal
---

## Process
1. Create file
2. Run terminal command to validate syntax

## Boundaries

### ✅ Always Do
- Validate terminal commands before execution
- Use read-only operations when possible (e.g., `cat`, `ls`)

### ⚠️ Ask First  
- Run build commands (`dotnet build`, `npm install`)
- Execute test suites

### 🚫 Never Do
- NEVER run destructive commands (`rm -rf`, `format`, etc.)
- NEVER execute commands that modify system configuration
- NEVER run commands outside the workspace directory
```

---

## Tool Combination Recipes

### Recipe 1: Pattern Discovery (Read-Only)

**Goal**: Find and analyze common patterns across existing files

```yaml
tools: ['grep_search', 'semantic_search', 'read_file', 'list_dir']
```

**Workflow**:
```markdown
1. `grep_search` with regex to find all files matching pattern
   Example: Find all prompts using handoffs: `grep_search("handoffs:", ".github/prompts/**/*.md")`

2. `semantic_search` to understand context and variations
   Example: "What handoff patterns are used in validation prompts?"

3. `read_file` to deep dive into top 3-5 results
   Example: Read each file to extract handoff configurations

4. `list_dir` to check for related files
   Example: List templates directory to see what's available

5. Output structured report with findings
```

### Recipe 2: Template-Based Generation (Write)

**Goal**: Create new file based on template and research

```yaml
tools: ['read_file', 'file_search', 'semantic_search', 'create_file']
```

**Workflow**:
```markdown
1. `file_search` to locate appropriate template
   Example: Find "prompt-simple-validation-template.md"

2. `read_file` to load template content
   Example: Read template to understand structure

3. `semantic_search` to find 2-3 similar existing files
   Example: "Find validation prompts similar to grammar check"

4. `read_file` to analyze similar files for patterns
   Example: Extract common YAML fields and boundary structures

5. `create_file` to generate new file with customizations
   Example: Create new grammar validation prompt with proper structure
```

### Recipe 3: Targeted Update (Write)

**Goal**: Modify specific sections of existing file

```yaml
tools: ['read_file', 'grep_search', 'replace_string_in_file']
```

**Workflow**:
```markdown
1. `read_file` to load current file state
   Example: Read existing prompt to understand structure

2. `grep_search` to locate exact section needing update
   Example: Find "## Boundaries" section across file

3. `read_file` with narrow line range to get precise context
   Example: Read lines 45-60 to get 3 lines before/after target

4. `replace_string_in_file` to apply targeted change
   Example: Update "ask_first" boundary with new rule
```

### Recipe 4: Batch Consistency Update (Write)

**Goal**: Apply same change across multiple files

```yaml
tools: ['file_search', 'read_file', 'multi_replace_string_in_file']
```

**Workflow**:
```markdown
1. `file_search` to find all target files
   Example: "*.prompt.md" in .github/prompts/

2. `read_file` to load each file and validate change needed
   Example: Check if each has old YAML format

3. `multi_replace_string_in_file` to apply changes atomically
   Example: Update YAML frontmatter structure across all files in one operation
```

### Recipe 5: Research + Validation (Read-Only + External)

**Goal**: Verify local implementation against official sources

```yaml
tools: ['semantic_search', 'read_file', 'fetch_webpage', 'grep_search']
```

**Workflow**:
```markdown
1. `semantic_search` to find local implementation
   Example: "Find how we implement agent handoffs"

2. `read_file` to understand current approach
   Example: Read 2-3 agent files with handoffs

3. `fetch_webpage` to get official documentation
   Example: Fetch VS Code Copilot customization docs

4. `grep_search` to find discrepancies
   Example: Search for deprecated handoff patterns

5. Output comparison report with recommendations
```

---

## Security and Risk Management

### Tool Risk Matrix

| Tool Category | Risk Level | Mitigation Strategy |
|---------------|------------|---------------------|
| Read-only | ✅ Low | Use freely for research, always prefer over write tools |
| Single write | ⚠️ Medium | Validate target path before execution, use three-tier boundaries |
| Batch write | ⚠️ High | Require explicit user approval before `multi_replace_string_in_file` |
| Terminal | 🚫 Critical | Define explicit command whitelist, never allow without boundaries |
| External fetch | ⚠️ Medium | Only fetch from trusted domains, validate URLs |

### Recommended Restrictions by Environment

**Production Prompts/Agents** (used by team):
```yaml
# Strict: Read-only researcher
tools: ['semantic_search', 'read_file', 'grep_search']
agent: plan
```

**Development Prompts/Agents** (personal workspace):
```yaml
# Moderate: Builder with validation
tools: ['read_file', 'semantic_search', 'create_file', 'replace_string_in_file']
agent: agent
boundaries:
  never_do:
    - "Modify files outside .github/ and .copilot/ directories"
```

**Experimental Prompts/Agents** (testing):
```yaml
# Permissive: Full access with monitoring
tools: ['read_file', 'semantic_search', 'create_file', 'multi_replace_string_in_file', 'run_in_terminal']
agent: agent
boundaries:
  ask_first:
    - "Execute any terminal commands"
    - "Use multi_replace for changes affecting >5 files"
```

---

## Performance Considerations

### Tool Cost Hierarchy (Token Usage)

| Tool | Avg Token Cost | When to Use | When to Avoid |
|------|----------------|-------------|---------------|
| `read_file` (narrow range) | 100-500 | Known file, specific section | Exploring unknown files |
| `file_search` | 50-200 | Finding files by name pattern | Searching file content |
| `grep_search` | 200-1000 | Exact string/regex search | Semantic/meaning-based search |
| `semantic_search` | 500-2000 | Finding relevant code by meaning | Known exact location |
| `read_file` (full file) | 500-5000 | Need complete context | Only need small section |
| `list_dir` | 100-500 | Browsing directory structure | Finding specific file |
| `fetch_webpage` | 1000-10000 | Getting official docs | Have local equivalent |
| `runSubagent` | 5000-50000 | Complex multi-step research | Simple single query |

### Optimization Patterns

**Pattern 1: Narrow Before Wide**
```markdown
❌ Bad (expensive):
1. semantic_search for "validation patterns" (2000 tokens)
2. read_file on all results (10,000 tokens)
3. grep_search to find specific pattern (500 tokens)

✅ Good (efficient):
1. grep_search for specific pattern first (500 tokens)
2. read_file only matched files (2,000 tokens)
3. semantic_search only if grep finds nothing (2,000 tokens)
```

**Pattern 2: Lazy Loading**
```markdown
❌ Bad (eager loading):
1. Read all templates (5,000 tokens)
2. Read all context files (8,000 tokens)
3. Search all existing prompts (3,000 tokens)
[User only wanted to update one small section]

✅ Good (lazy loading):
1. Ask user what they want to update
2. Only read relevant template (500 tokens)
3. Only read target file (1,000 tokens)
```

**Pattern 3: Caching via Agent Context**
```markdown
✅ Best practice:
1. When agent is active, search results are cached in conversation
2. Subsequent `read_file` calls on same files reuse context
3. Switch agents only when role changes (preserves cache)
```

---

## Version History

| Version | Date | Changes | Author |
|---------|------|---------|--------|
| 1.0.0 | 2025-12-10 | Initial consolidated version from analysis | System |

---

## References

- **Context Engineering Principles**: `.copilot/context/prompt-engineering/context-engineering-principles.md`
- **Validation Caching Pattern**: `.copilot/context/prompt-engineering/validation-caching-pattern.md`
- **Official Tool Documentation**: VS Code Copilot API Reference
- **Repository Articles**: `.github/prompts/03. how_to_structure_content_for_copilot_prompt_files.md` (Tool Configuration section)
