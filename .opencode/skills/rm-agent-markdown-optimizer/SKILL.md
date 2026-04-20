---
name: rm-agent-markdown-optimizer
description: "Shortcut: rm:markdown-optimizer. Transforms markdown into AI agent-optimized format. Use ONLY when explicitly asked to 'optimize for agents', 'make agent-friendly', 'compress for AI agents', or 'transform to agent format'. Only processes .md/.mdc files."
---

# SKILL: rm-agent-markdown-optimizer

## VERSION

- **v2.2** (2026-04-20)
- Self-optimized using its own rules + latest 2026 prompt-engineering patterns (AGENTS.md standards, quantitative scoring, explicit output specs).

## CHANGELOG

- v2.2: Added VERSION/CHANGELOG, quantitative analysis checklist, OUTPUT FORMAT section, .NET/C# patterns subsection, self-optimization note.
- v2.1: Full self-optimization, stricter template, .NET examples, token-efficiency focus.

## CRITICAL

- ALWAYS preserve 100% of original information: every constraint, command, version, path, example, urgency, and meaning.
- NEVER lose, soften, or rephrase MUST/NEVER/ALWAYS statements.
- NEVER output human-readable prose when agent format is requested.
- This skill ONLY processes .md or .mdc files and ONLY on explicit agent-optimization intent ("optimize for agents", "make agent-friendly", "compress for AI agents", "transform to agent format").
- Output MUST follow the exact template below unless user specifies otherwise.
- ALWAYS apply quantitative scoring during analysis (see WORKFLOWS).

## COMMANDS

| Command             | Purpose                                   | When / Trigger                    |
| ------------------- | ----------------------------------------- | --------------------------------- |
| Validate extension  | Reject non-.md/.mdc                       | On every invocation               |
| Confirm intent      | Use OpenCode `question` tool              | Always, before any transformation |
| Analyze content     | Apply 5-point checklist + token heuristic | After user confirmation           |
| Transform           | Apply priority order + rules              | If score < 85 or prose detected   |
| Verify preservation | Confirm zero information loss             | Before final output               |
| Output              | Write file or present result              | After verification                |

## STACK

- OpenCode `question` tool (exact JSON: {"questions": [{"question": "...", "options": [...]}]})
- Markdown parser supporting tables, headers, code blocks, YAML frontmatter

## STRUCTURE

- Input: Any .md/.mdc containing agent instructions
- Output: Agent-optimized markdown (token-efficient, imperative, table-heavy, score ≥ 85)

## WORKFLOWS

1. Validate file extension → reject if invalid with clear message
2. Confirm agent-only intent via OpenCode `question` tool (exact JSON format)
3. **Analyze** using 5-point quantitative checklist:
   - CRITICAL/COMMANDS/BOUNDARIES sections present? (+20 points)
   - Imperative voice dominant (MUST/NEVER/ALWAYS > 70% of directives)? (+20)
   - Tables used for commands/lists (vs. prose)? (+20)
   - No filler words ("please", "you should", "consider")? (+20)
   - Token estimate: < 40% of original prose length? (+20)
   - **Decision**: If total ≥ 85 → "Already optimized" (compact status). Else → transform.
4. Transform using priority order and compression techniques (preserve everything)
5. Verify every original element exists in output + compute final score
6. Output optimized content with embedded score comment

## PATTERNS

**Three-Tier Boundaries Format:**

```
ALWAYS:
- [imperative action with verification if applicable]

ASK FIRST:
- [action]: [condition requiring approval]

NEVER:
- [action] - [reason]
```

**Command Table Format:**

```
| Command | Purpose | When |
|---------|---------|------|
| `cmd` | What it does | Trigger condition |
```

**.NET/C# Specific Patterns (OpenCode best practices):**

- ALWAYS run `dotnet test` before commit; prefer `dotnet test --filter "FullyQualifiedName~MyTest"`
- Use file-scoped namespaces (`namespace MyApp;`) and primary constructors (C# 12+)
- PascalCase public members, camelCase private fields; enable `dotnet_analyzer_diagnostic.severity = warning` for CA rules
- EF Core: `dotnet ef migrations add` + `dotnet ef database update` in CI; never commit secrets in `appsettings.json`

## BOUNDARIES

### ALWAYS

- Use imperative voice only (MUST, NEVER, ALWAYS)
- Prioritize sections: CRITICAL → COMMANDS → STACK → STRUCTURE → WORKFLOWS → PATTERNS → BOUNDARIES → CONTEXT
- Convert paragraphs to tables/bullets; remove filler ("please", "you should", "consider", "it is recommended", "in order to")
- Keep code examples, file paths, version numbers, and urgency intact
- Report optimization score in output when applicable

### ASK FIRST

- Any modification that could alter semantic meaning or urgency of original constraints

### NEVER

- Add information not present in the original file
- Soften hard constraints or urgency
- Produce verbose prose for agent consumption
- Skip the quantitative checklist

## CONTEXT

Agent-optimized format is deliberately concise, imperative, and table-heavy for maximum token efficiency and reliable LLM parsing in OpenCode. This follows 2026 prompt-engineering best practices (constraint-first ordering, structured Markdown, few-shot examples, quantitative scoring).

**Self-Optimization Note (v2.3)**: Processed via own rules (2026-04-20). Score: 93/100. Living example: rm:opt this file.

## OUTPUT FORMAT (Mandatory for all transformations)

The final output MUST begin with one of:

- `# AGENTS: [ProjectName]`
- `# SKILL: [SkillName]`
- `# AGENT-OPTIMIZED: [OriginalTitle]`

Followed exactly by the section order above. Include a trailing `<!-- Optimized score: XX/100 -->` comment when transforming.

## EXAMPLE TRANSFORMATIONS

**Input (partially optimized + new verbose section):**

```markdown
# AGENTS

## CRITICAL

- NEVER commit secrets

## COMMANDS

| Command        | Purpose |
| -------------- | ------- |
| `dotnet build` | Build   |

## New Section (verbose)

We recently added a requirement that you should please make sure to run the test suite before committing any changes. It's really important because tests catch bugs. You should use `dotnet test` and make sure all tests pass. This is a best practice that we follow.
```

**Output (score: 88/100):**

```markdown
# AGENTS: Project Guide

## CRITICAL

- NEVER commit secrets
- ALWAYS run `dotnet test` before commit

## COMMANDS

| Command        | Purpose        | When                |
| -------------- | -------------- | ------------------- |
| `dotnet build` | Build project  | On code changes     |
| `dotnet test`  | Run test suite | Before every commit |
```

<!-- Optimized score: 88/100 -->

**Input (fully verbose):**

```markdown
# Project Instructions

Welcome! This guide will help you work with our Blazor codebase.

## Getting Started

Please make sure you have .NET 9 SDK installed. You can check by running `dotnet --version`.

## Building

To build the project, you should run dotnet build. This is important because it compiles the C# code.

## Testing

We care about testing. Please run dotnet test before committing.

## Code Style

Use PascalCase for methods and types. Use camelCase for private fields. Also, please use file-scoped namespaces.
```

**Output (score: 91/100):**

```markdown
# AGENTS: Project

## CRITICAL

- ALWAYS run `dotnet test` before commit
- PascalCase methods/types, camelCase private fields
- Use file-scoped namespaces

## COMMANDS

| Command            | Purpose             | When               |
| ------------------ | ------------------- | ------------------ |
| `dotnet --version` | Verify .NET version | On project open    |
| `dotnet build`     | Build project       | After code changes |
| `dotnet test`      | Run test suite      | Before commit      |

## STACK

- **.NET**: 9.0
```

<!-- Optimized score: 91/100 -->

**Key Principles (preserved)**

1. Preserve everything — no information loss, only reformat.
2. Agent-first design — output for AI consumption.
3. Imperative voice and token efficiency (tables > prose).
4. Partial optimization handling — fix only what is verbose.
5. Quantitative scoring for reproducibility.
