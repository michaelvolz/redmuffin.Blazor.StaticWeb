---
title: "Agent Skill Library Naming and Organization Conventions"
date: 2026-05-25
module: opencode
problem_type: best_practice
component: development_workflow
severity: medium
applies_when:
  - Designing or refactoring an rm-* skill naming strategy and subfolder organization
  - Adding a new skill and determining where it belongs in the directory tree
  - Reviewing a skill whose name may overlap with existing skills
tags:
  - agent-skills
  - naming-conventions
  - skill-organization
  - opencode
  - skill-library
  - best-practices
  - knowledge-management
---

# Agent Skill Library Naming and Organization Conventions

## Context

The repo's OpenCode agent skill library grew to 22+ `rm-*` skills with no naming strategy, inconsistent subfolder placement, overlapping names (3 skills with "cleanup" in the title), and generic names that fail to communicate what the skill does. The library has no decision tree for where new skills should live or what pattern their names should follow.

At scale, skill discovery relies on two mechanisms: the `description` field (the routing trigger the agent sees) and the skill name (the human-facing identifier). When names overlap, descriptions blur, and folders lack clear boundaries, routing becomes unreliable, the skill library resists maintenance, and every new skill adds tax rather than capability.

Web research across 6 authoritative sources—the Agent Skills spec (agentskills.io / Microsoft), Perplexity's internal design docs, enterprise guides from noqta.tn, Lalit Madan's agent-engineering patterns, mgechev/skills-best-practices, and laguagu/agents-best-practices—produced consistent principles that ground the conventions below.

This doc extends the implemented architecture in [OpenCode Instruction Architecture Pattern](../integration-issues/opencode-instruction-architecture-pattern-2026-04-03.md) with external research and a formal decision tree. The earlier doc defines the namespace strategy (`rm-*` prefix, vendor isolation, `redmuffin-standards/` subfolder) under `~/.config/opencode/`. This doc formalizes the naming rules, folder placement decision tree, and rename plan for the current library.

## Guidance

### Folder Architecture

The skill library uses two buckets, separated by a single question:

**Is this skill triggered by the file a developer is editing?**

| Answer | Bucket                                 | Folder                 | Name pattern        |
| ------ | -------------------------------------- | ---------------------- | ------------------- |
| Yes    | Guide (domain-specific reference)      | `redmuffin-standards/` | `rm-guide-{domain}` |
| No     | Action (capability the agent performs) | root `skills/`         | `rm-{action}`       |

Guides are reference material loaded when a specific technology, language, or filetype is in play—they don't do things, they inform decisions. Actions are tasks the agent performs: commit, cleanup, research, audit. This distinction is the single axis that determines folder placement.

**Subfolders**: The spec default is flat. Subfolders are appropriate only for groups of 4+ skills that share a clear conceptual boundary. `redmuffin-standards/` qualifies (14+ guides). If the root ever accumulates 10+ action skills, a flat `workflows/` subfolder may become warranted, but no subfolder with fewer than 4 skills is justified. Precede suspicion until scale demands structure.

### Decision Tree for New Skills

```
New skill needed
  │
  ├─ Is it triggered by a filetype? (.cs, .razor, .scss, .md, .html, .csproj, etc.)
  │   Yes → rm-guide-{domain}
  │          Place in redmuffin-standards/ subfolder
  │          Done.
  │
  └─ Is it an action the agent performs?
      Yes → rm-{action} (verb or verb-noun)
             Place in root skills/ folder
             Done.
```

If neither bucket fits cleanly, the skill is probably trying to do too much. Split it. A skill that both acts and references is two skills—a guide for the domain and an action for the workflow.

### Naming Rules

**Structure**

- Guide names: `rm-guide-{domain}` — domain is the technology, concept, or filetype the guide covers (e.g., `rm-guide-blazor`, `rm-guide-testing`, `rm-guide-css`)
- Action names: `rm-{action}` — action is a verb or verb-noun describing what the agent does (e.g., `rm-commit`, `rm-cleanup`, `rm-research`)

**Constraints**

| Rule                                                                     | Source                | Rationale                                                                              |
| ------------------------------------------------------------------------ | --------------------- | -------------------------------------------------------------------------------------- |
| ≤ 30 characters preferred, ≤ 64 hard max                                 | noqta (30), spec (64) | Routing triggers parse the full name; long names slow matching                         |
| Lowercase, hyphens only, no consecutive hyphens                          | Agents spec           | Conventional commit compatibility, shell safety                                        |
| Never: `helper`, `utils`, `misc`, `tool`, `manager`                      | Lalit Madan           | These are capability vacuums—they grow indefinitely because they have no defined scope |
| Verb or verb-noun for actions                                            | noqta                 | Verbs communicate what the agent will _do_, not what the skill _is_                    |
| `{domain}-{action}` for guides                                           | noqta                 | Domain scopes the knowledge; action communicates intent                                |
| No two skills share the same primary word unless in different categories | Derived               | Prevents routing ambiguity and naming collisions                                       |

**Disambiguation heuristic**: If two skills exist where one could plausibly be triggered instead of the other based on a user's ambiguous phrasing, the names are too close. Rename the newer one.

### Renaming Plan

The following renames resolve the problems identified in the current library:

| Current name                         | Problem                                                                                         | New name                                        | Rationale                                                                                                 |
| ------------------------------------ | ----------------------------------------------------------------------------------------------- | ----------------------------------------------- | --------------------------------------------------------------------------------------------------------- |
| `rm-guide-cleanup`                   | "cleanup" overlaps with `rm-gates-cleanup` and `rm-cleanup-session`; 3 skills all say "cleanup" | `rm-guide-code-quality`                         | Clarifies this is the universal code quality standards reference, not a cleanup workflow                  |
| `rm-gates-cleanup`                   | Doesn't communicate it's about quality gate protocols                                           | `rm-guide-quality-gates`                        | Makes explicit this is a guide for gate tool protocols, not a general cleanup skill                       |
| `rm-cleanup-session`                 | "session" is redundant (it _is_ the session)                                                    | `rm-cleanup`                                    | Master entry point for cleanup sessions; brevity communicates authority                                   |
| `rm-uncle-bob-martin-agentic-coding` | 37 chars (exceeds 30-char target), contains a person name                                       | `rm-guide-metrics`                              | Captures the skill's actual purpose: enforcing metrics-driven development standards                       |
| `rm-code-philosophy`                 | Not a capability; "philosophy" is a content label, not a trigger pattern                        | Merge into `rm-guide-architecture`              | Ousterhout's principles are architectural design guidance; one guide is cleaner than two overlapping ones |
| `rm-css`                             | In root folder but IS a guide (triggered by `.css`/`.scss` filetype)                            | Move to `redmuffin-standards/rm-guide-css`      | Corrects folder placement; harmonizes name with guide pattern                                             |
| `rm-html`                            | In root folder but IS a guide (triggered by `.html`/`.razor` filetype)                          | Move to `redmuffin-standards/rm-guide-html`     | Same as above                                                                                             |
| `rm-markdown`                        | In root folder but IS a guide (triggered by `.md` filetype)                                     | Move to `redmuffin-standards/rm-guide-markdown` | Same as above                                                                                             |

`rm-guide-review-heuristics` was created under these conventions and is correctly placed. No action needed.

### Description Standards

The `description` field in each `SKILL.md` is the routing key—the agent matches user intent to skill descriptions. Write descriptions that answer three questions:

1. **What triggers this skill?** (filetype, command, phrase)
2. **What does the agent do with it?** (the action or knowledge loaded)
3. **What must NOT trigger it?** (negative constraints prevent routing collisions)

Example:

```
description: "Use when writing, editing, reviewing, or auditing CSS — any .css or .scss file, any style tag, any styling decision. Contains the definitive anti-patterns list and Widely-available Baseline definition for this repo. DO NOT USE FOR: SCSS architecture decisions (see rm-scss), general UI framework selection (see rm-ui-styling)."
```

The negative constraint block (`DO NOT USE FOR`) is mandatory when more than one skill could plausibly match a given context. Without it, the agent guesses between near-miss skills and gets the wrong reference.

## Why This Matters

**Routing reliability.** The agent matches skills by scanning descriptions against the user's current context (open file, command issued, task described). When two skills have "cleanup" as their primary word, the agent faces a 1-in-3 guess. When a guide lives in the root folder, it competes with action skills for the same namespace. Each ambiguity is a silent routing failure—the agent loads the wrong skill and gives an answer that looks right but references the wrong constraints.

**Maintenance velocity.** A skill library with no naming convention resists refactoring. Renaming a skill means updating every reference in every description. Without conventions, the cost of that audit scales with library size. With conventions, the rename is predictable and grep-assisted.

**Onboarding cost.** Every new contributor must learn what 22+ skills do. Generic names (`rm-css` — is that a guide? an action? a tool?) force them to read every `SKILL.md` to build a mental map. Conventions collapse that map to two rules: "guides go in `redmuffin-standards/`, actions go in root."

**Token budget.** Skill descriptions are loaded into context. Descriptions that must enumerate every possible trigger pattern because the name doesn't communicate scope consume more tokens than names that carry semantic weight.

## When to Apply

- When creating a new skill: follow the decision tree before writing a single line
- When a skill's description grows past 5 lines of trigger text: the name is too broad, rename it
- When two skills have the same primary word: one must be renamed (disambiguation heuristic)
- When a skill exceeds 30 characters: shorten it unless the extra characters carry unique disambiguation value
- When reviewing PR descriptions that reference skills: verify the skill name matches these conventions before merging
- Before any compound-engineering workflow that generates a new skill (ce-compound, skill-creator)

## Examples

**Good names (convention-compliant)**

| Name               | Category | Why it works                                                                                                            |
| ------------------ | -------- | ----------------------------------------------------------------------------------------------------------------------- |
| `rm-guide-blazor`  | Guide    | Domain (`blazor`) is precise; immediately tells agents this loads when editing `.razor` files                           |
| `rm-guide-testing` | Guide    | Domain (`testing`) covers the entire test practice; description narrows to TUnit + bUnit specifics                      |
| `rm-guide-async`   | Guide    | Domain (`async`) is a capability area, not a filetype; still a guide because it informs decisions, not performs actions |
| `rm-commit`        | Action   | Verb communicates the action; short, unambiguous                                                                        |
| `rm-dev-shutdown`  | Action   | Verb-noun communicates the action; dev environment teardown                                                             |
| `rm-research`      | Action   | Verb communicates the action; loads the research persona                                                                |

**Bad names (pre-convention)**

| Name                                           | Problem                                                                                            | Fix                                                   |
| ---------------------------------------------- | -------------------------------------------------------------------------------------------------- | ----------------------------------------------------- |
| `rm-cleanup`                                   | Verb doesn't communicate scope; "cleanup" overlaps with `rm-cleanup-session` (master orchestrator) | `rm-dev-shutdown`                                     |
| `rm-uncle-bob-martin-agentic-coding`           | 37 chars, person name, doesn't communicate purpose                                                 | `rm-guide-metrics`                                    |
| `rm-helper` (hypothetical)                     | Forbidden word; scope grows indefinitely                                                           | Split into the specific actions it performs           |
| `rm-tools` (hypothetical)                      | Forbidden word; no defined scope                                                                   | Split by tool domain (e.g., `rm-guide-quality-gates`) |
| `rm-guide-config` but `rm-guide-configuration` | Inconsistent suffix for same concept                                                               | Pick one; stick with the existing convention          |

## Related

- [OpenCode Instruction Architecture Pattern](../integration-issues/opencode-instruction-architecture-pattern-2026-04-03.md) — The implemented skill namespace strategy that this doc extends with external research
- Agent Skills specification (agentskills.io) — Canonical reference for folder structure, description format, and routing behavior
- `rm-instruction-standards` — Governs when and how to update `SKILL.md` instruction files under these naming conventions
- `rm-guide-cleanup` → `rm-guide-code-quality` — Universal code quality principles
- `rm-gates-cleanup` → `rm-guide-quality-gates` — Quality gate remediation protocols
- `rm-guide-architecture` — Absorbs `rm-code-philosophy`; Ousterhout's principles belong with architectural design patterns
