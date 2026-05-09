---
title: Compound-Engineering Plugin Port to OpenCode
date: 2026-05-09
category: tooling-decisions
module: opencode
problem_type: tooling_decision
component: development_workflow
severity: low
applies_when:
  - Porting Claude Code plugins to OpenCode
  - Converting CC-specific skill references to OpenCode equivalents
  - Adapting agent dispatch patterns across harnesses
tags: [compound-engineering, opencode, plugin-port, skills, agent-harness]
---

# Compound-Engineering Plugin Port to OpenCode

## Context

The compound-engineering skill pack was originally built for Claude Code. Porting to OpenCode required adapting CC-specific conventions (model names, tool names, file paths, agent dispatch syntax) to OpenCode equivalents while preserving all workflows.

## Guidance

Key adaptations made during the port:

1. **Model references.** CC model names (`sonnet`, `opus`, `haiku`) replaced with OpenCode agent archetypes (`rm-brilliant`, `rm-karpathy`, `rm-rigurous`). Agent dispatch updated from `@compound-engineering/ce-*` (CC syntax) to `ce-*` subagent types (OpenCode syntax).

2. **Tool mapping.** CC's `Task` tool → OpenCode's `task` tool with `subagent_type` parameter. CC's `Skill` tool → OpenCode's `skill` tool. CC's `AskUserQuestion` → OpenCode's `question` tool.

3. **File paths.** CC stores skills in `~/.claude/skills/`; OpenCode uses repo-local `.opencode/skills/`. All path references updated.

4. **Skill discovery.** Confirmed OpenCode's `**/SKILL.md` globstar supports unlimited nesting (from `packages/opencode/src/skill/index.ts`). Ce-compound skills preserved their 2-level nesting under `.opencode/skills/compound-engineering/`.

5. **Plugin coexistence.** The `ce-*` subagent types coexist with `rm-*` agents and vendor agents. Each namespace is independent.

## Why This Matters

The port proves that well-structured skills can move between agent harnesses with primarily mechanical changes (paths, tool names, model references). The workflow logic — research → analyze → assemble → write — is harness-agnostic.

## When to Apply

- When porting any CC plugin to OpenCode — follow the same adaptation checklist
- When adding a new agent harness — keep skill logic harness-agnostic, isolate harness-specific references to a single config layer

## Related

- `.opencode/skills/compound-engineering/` — All 20+ CE skills
- `.opencode/agents/compound-engineering/` — CE review agents (ported)
