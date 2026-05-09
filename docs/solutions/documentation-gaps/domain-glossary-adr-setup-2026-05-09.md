---
title: Domain Glossary and ADR Setup
date: 2026-05-09
category: documentation-gaps
module: docs
problem_type: documentation_gap
component: documentation
severity: medium
applies_when:
  - Setting up domain documentation for a new or undocumented project
  - Establishing Architecture Decision Record conventions
  - Defining shared terminology for multi-agent collaboration
tags:
  [domain-glossary, adr, documentation, context, shared-language, terminology]
---

# Domain Glossary and ADR Setup

## Context

The project lacked shared domain documentation — agents working in different sessions had no common vocabulary for concepts like "Raindrop" or "Dummy test doubles." Architecture decisions were made ad-hoc with no record of why. Without shared language, agents in parallel sessions would use different terms for the same concepts, causing confusion and inconsistency.

## Guidance

Created three documentation pillars in a grill-with-docs session:

1. **`CONTEXT.md`** — Single shared domain model for the entire repo. Defines the Common domain (shared types), the Raindrop data source (first-class concept), and the Dummy\* test double pattern. One file, one source of truth — not per-subproject contexts that would drift apart.

2. **`docs/adr/`** — Architecture Decision Records with numbered entries:
   - `0001-dummy-test-double-convention.md` — Dummy\* pattern as "fast standalone WASM mode for local dev" (corrected from initial framing as "testing dogma"). Enables local development without Azure Functions dependency.
   - `0002-quality-gates-toolchain.md` — Separate `tools/` solution, monolith with subcommands, local NuGet feed.

3. **`docs/agents/`** — Agent configuration files linked from the `## Agent skills` block in AGENTS.md. Required by Matt Pocock engineering skills for issue tracker, triage labels, and domain doc consumption rules.

## Why This Matters

Shared terminology is the foundation of reliable multi-agent collaboration. When one agent calls a concept "Raindrop" and another calls it "API data source," they produce inconsistent code. CONTEXT.md eliminates that ambiguity. ADRs prevent repeated arguments about already-decided architecture questions.

## When to Apply

- When multiple agents work on the same codebase
- Before scaling beyond a single developer
- When architecture decisions need durable rationale

## Related

- `CONTEXT.md` — Shared domain model
- `docs/adr/` — Architecture Decision Records
- `docs/agents/` — Agent configuration files
