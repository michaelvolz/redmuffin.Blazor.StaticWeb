---
date: 2026-04-14
module: agent-configuration
tags:
  [
    opencode,
    agents,
    parallel-execution,
    compound-engineering,
    workflow-optimization,
  ]
problem_type: developer-experience
title: "Parallel Agent Execution Not Working in CE Skills"
---

# Parallel Agent Execution Not Working in CE Skills

## Problem

When CE skills (like `/ce/compound`, `/ce/document-review`, `/ce-review`) specify parallel agent dispatch via `<parallel_tasks>` XML tags or explicit "dispatch in parallel" instructions, the model was executing them **sequentially** instead of in parallel.

This made parallel workflows nearly useless — running multiple reviewers in parallel should be fast, but sequential execution made it slow.

## Symptoms

- Skills with `<parallel_tasks>` blocks still ran agents one-by-one
- Phase 1 subagents in `/ce/compound` ran sequentially instead of simultaneously
- Multiple reviewer agents in `/ce-review` executed in sequence, not parallel
- Expected speedup from parallel execution was not realized

## Root Cause

**Instruction conflict** between two sources:

| Source                                             | Instruction                                                          | Effect              |
| -------------------------------------------------- | -------------------------------------------------------------------- | ------------------- |
| `rm-reliable-dotnet-coder` agent persona           | "Never Guess, Never Loop" — research-first, step-by-step, deliberate | Sequential default  |
|                                                    | "Strict research-first, loop-free mode"                              | Be safe, avoid risk |
| CE skills (`ce-compound`, `document-review`, etc.) | "Dispatch in parallel" / `<parallel_tasks>`                          | Run simultaneously  |

The model default was **sequential** because:

1. The persona said "be deliberate, step-by-step"
2. XML tags (`<parallel_tasks>`) felt like documentation, not directives
3. Sequential is the "safer" choice when unsure

## Solution

Updated the custom `rm-reliable-dotnet-coder` agent to add explicit **Parallel Task Execution** priority over the sequential reasoning protocol.

### Files Changed

| File                                           | Change                                             |
| ---------------------------------------------- | -------------------------------------------------- |
| `.opencode/agents/rm-reliable-dotnet-coder.md` | Added "Parallel Task Execution (CRITICAL)" section |
| `.opencode/agents/rm-daily-dotnet-coder.md`    | Added same section                                 |

### New Instructions Added

```markdown
## Parallel Task Execution (CRITICAL)

When skills, agents, or other instructions specify **parallel execution**,
it MUST take priority over the sequential reasoning protocol. This is a
deliberate exception to ensure efficiency.

### How to Recognize Parallel Instructions

1. **XML tags**: `<parallel_tasks>...</parallel_tasks>` blocks in skills
   explicitly list agents to run in parallel
2. **Explicit directives**: Phrases like "dispatch in parallel", "spawn in
   parallel", "run simultaneously", "launch all at once"
3. **Skill instructions**: Skills that say "run X and Y in parallel" or
   "spawn multiple agents"

### Execution Rules

- When you see `<parallel_tasks>` or explicit parallel dispatch instructions,
  emit ALL agent/task calls **in the same response** — do not wait for one to
  complete before launching the next
- Multiple `task` tool calls in a single response = parallel execution
- Do NOT run agents sequentially "to be safe" when parallel is explicitly specified
- The sequential "research → plan → implement" protocol applies ONLY to your own
  reasoning, not to dispatching multiple independent sub-agents when instructed
```

## Why This Works

1. **Explicit priority** — The new instructions explicitly state that parallel takes priority over sequential
2. **Clear recognition signals** — Model now knows to look for `<parallel_tasks>` XML tags and explicit directives
3. **Actionable guidance** — Model knows to emit ALL task calls in the same response for parallel execution

## Lessons Learned

1. **Skills are not enough** — Skills can specify parallel execution, but if the agent persona conflicts, the model defaults to the persona
2. **Custom agents need explicit overrides** — When modifying third-party skills isn't an option, update your custom agents to align
3. **XML tags can be invisible** — The model may treat `<parallel_tasks>` as documentation markup rather than actionable instructions
4. **Sequential is the safe default** — Without explicit parallel priority, models default to sequential "to be safe"

## Related Documentation

- `docs/solutions/integration-issues/opencode-instruction-architecture-pattern-2026-04-03.md`
- `AGENTS.md` — Project instructions for agent behavior
- `.opencode/agents/rm-reliable-dotnet-coder.md` — Custom agent persona
