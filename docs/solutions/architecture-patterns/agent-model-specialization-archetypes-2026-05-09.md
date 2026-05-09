---
title: Agent Model Specialization into Three Archetypes
date: 2026-05-09
category: architecture-patterns
module: opencode
problem_type: architecture_pattern
component: assistant
severity: medium
applies_when:
  - Designing agent rosters for multi-model agent harnesses
  - Consolidating overlapping agent definitions
  - Assigning agents to tasks based on structural archetypes rather than model names
tags: [agents, opencode, archetypes, model-selection, specialization, planmode]
---

# Agent Model Specialization into Three Archetypes

## Context

The original ~12 agent definitions (rm-build, rm-daily-dotnet-coder, rm-grokstrategist, rm-plan, etc.) were single-purpose with model assignments baked into each file. This made the agent roster fragile when models changed — updating a model required editing a dozen files. It also buried the system prompt differences in verbose agent definitions, making it hard to understand what made each agent distinct.

## Guidance

Consolidated to three specialized archetypes, each a self-contained agent definition:

| Archetype        | Role                                            | When to Use                                                |
| ---------------- | ----------------------------------------------- | ---------------------------------------------------------- |
| **rm-brilliant** | Primary coder, fast model                       | Routine implementation, straightforward tasks              |
| **rm-karpathy**  | Universal primary, research-grade deep thinking | Complex reasoning, research-heavy tasks, multi-domain work |
| **rm-rigurous**  | Verifier, planmode for structured verification  | Verification passes, quality checks, structured review     |

Each is in `.opencode/agents/`. Model changes update one file, not twelve.

A planmode variant (`rm-rigurous-verifier-planmode`) demonstrates how to derive specialized behavior from a base agent without duplication — the planmode wrapper adds structured thinking without copying the full prompt.

Compound-engineering agents remain under `compound-engineering/` for their specific review/analysis roles (ce-code-review, ce-doc-review, etc.).

## Why This Matters

Three well-understood archetypes are easier to assign correctly than twelve overlapping ones. The decision is structural (task type) not model-specific — "this is a reasoning task, use karpathy" rather than "this needs GPT-4, use agent X which happens to have GPT-4."

## When to Apply

- Assign `rm-karpathy` to research, architecture, and complex debugging
- Assign `rm-brilliant` to routine implementation, boilerplate, simple fixes
- Assign `rm-rigurous` to verification passes, code review, quality gates

## Related

- `.opencode/agents/rm-brilliant-primary-agent.md`
- `.opencode/agents/rm-karpathy-universal-primary-agent.md`
- `.opencode/agents/rm-rigurous-verifier.md`
- `.opencode/agents/rm-rigurous-verifier-planmode.md`
