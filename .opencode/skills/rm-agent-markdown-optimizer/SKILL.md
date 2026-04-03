---
name: rm-agent-markdown-optimizer
description: "Shortcut: rm:opt. Transforms markdown into AI agent-optimized format. Use ONLY when explicitly asked to 'optimize for agents', 'make agent-friendly', 'compress for AI agents', or 'transform to agent format'. Only processes .md/.mdc files."
---

# Agent Markdown Optimizer

Transforms markdown instruction files into formats optimized for AI coding agents/agents.

## Workflow

### Step 1: Validate File Extension

Check if file has `.md` or `.mdc` extension.

- If NO: Reject with "This skill only works with .md or .mdc files"
- If YES: Proceed to confirmation

### Step 2: Confirm Agent-Only Intent

Use the opencode `question` tool to ask:

```json
{
  "questions": [
    {
      "question": "This skill transforms markdown into AI agent-optimized format (concise, imperative, token-efficient, human-unreadable). Is [filename] an agent-only instruction file meant for AI agents, not humans?",
      "options": [
        {
          "label": "Yes",
          "description": "File is agent-only, proceed with optimization"
        },
        {
          "label": "No",
          "description": "File is for humans, cancel the operation"
        }
      ],
      "header": "Confirm File Type"
    }
  ]
}
```

- If user selects **No**: Stop immediately. Output: "Skill cancelled. This skill is for agent-only instruction files."
- If user selects **Yes**: Proceed to analysis

### Step 3: Analyze Current State

Read the entire file and categorize content:

1. **Check for agent-optimized indicators:**
   - CRITICAL/COMMANDS/BOUNDARIES sections present
   - Heavy use of tables over prose
   - Imperative voice (MUST/NEVER/ALWAYS)
   - Minimal filler words ("please", "you should", "consider")

2. **Identify optimization opportunities:**
   - Verbose sections that could be compressed
   - Prose that could become tables
   - Missing imperative voice
   - Filler words present

3. **Detect partial optimization:**
   - Some sections well-optimized
   - New verbose additions
   - Mixed formatting styles

### Step 4: Optimization Decision

**If file is already fully optimized:**
Output compact status (1-4 lines):

```
FILE STATUS: Already agent-optimized
- Structure: [CRITICAL/COMMANDS/BOUNDARIES present]
- Compression: [High - tables used, prose minimal]
- Action: No transformation needed
```

**If file can be improved:**
Proceed to transformation preserving ALL information.

### Step 5: Transform

Apply these rules preserving every piece of information:

**Information Preservation**: Every constraint, command, version, path, example MUST be preserved. Nothing lost, only reformatted.

**Priority Order (Most Important First):**

1. CRITICAL CONSTRAINTS (MUST/NEVER/ALWAYS)
2. EXECUTABLE COMMANDS
3. TECH STACK & VERSIONS
4. PROJECT STRUCTURE
5. WORKFLOW PATTERNS
6. CODE EXAMPLES
7. BOUNDARIES (ALWAYS/ASK FIRST/NEVER)
8. CONTEXT

**Compression Techniques:**

- Remove: "Please", "You should", "It's recommended", "Consider", "think about"
- Replace: "In order to" → "To", "Due to the fact that" → "Because"
- Convert paragraphs to tables where structured
- Use bullet points over numbered lists unless sequence matters
- Active voice only

**Three-Tier Boundaries Format:**

```
ALWAYS:
- [action with verification if applicable]

ASK FIRST:
- [action]: [condition requiring approval]

NEVER:
- [action] - [reason]
```

**Command Format:**

```
COMMANDS:
| Command | Purpose | When |
|---------|---------|------|
| `cmd` | What it does | Trigger condition |
```

**Output Template:**

```markdown
# [Type]: [Name]

## CRITICAL

[Hard constraints]

## COMMANDS

[Table]

## STACK

[Tech versions]

## STRUCTURE

[Paths with access]

## WORKFLOWS

[Step procedures]

## PATTERNS

[Code examples]

## BOUNDARIES

### ALWAYS

[List]

### ASK FIRST

[List]

### NEVER

[List]

## CONTEXT

[Remaining info]
```

### Step 6: Verify Preservation

Before outputting, confirm ALL information from input exists in output:

- Every constraint preserved
- Every command preserved
- Every version number preserved
- Every file path preserved
- Every code example preserved

### Step 7: Output

Write optimized content to file or present to user based on context.

## Example Transformations

**Input (Partially optimized with new verbose section):**

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

**Output:**

```markdown
# AGENTS: Project Guide

## CRITICAL

- NEVER commit secrets
- ALWAYS run `dotnet test` before commit

## COMMANDS

| Command        | Purpose        |
| -------------- | -------------- |
| `dotnet build` | Build project  |
| `dotnet test`  | Run test suite |
```

**Input (Verbose, needs full optimization):**

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

**Output:**

```markdown
# AGENTS: Project

## CRITICAL

- ALWAYS run `dotnet test` before commit
- PascalCase methods/types, camelCase private fields
- Use file-scoped namespaces

## COMMANDS

| Command            | Purpose             |
| ------------------ | ------------------- |
| `dotnet --version` | Verify .NET version |
| `dotnet build`     | Build project       |
| `dotnet test`      | Run test suite      |

## STACK

- **.NET**: 9.0
```

## Key Principles

1. **Preserve Everything**: No information loss, only reformat
2. **Agent-First**: Output is for AI consumption, not humans
3. **Imperative Voice**: Commands, not suggestions
4. **Token Efficiency**: Tables over prose, bullets over paragraphs
5. **Partial Optimization**: Detect and fix only what's verbose
