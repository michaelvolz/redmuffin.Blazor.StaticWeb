---
title: "OpenCode Instruction Management: Operational Lessons Learned"
problem_type: knowledge
category: integration-issues
date: 2026-04-03
track: knowledge
component: opencode
module: instruction-architecture
tags:
  [
    opencode,
    skills,
    frontmatter,
    permissions,
    global-config,
    trigger-optimization,
  ]
applies_when: "Managing OpenCode skills, resolving trigger conflicts, or cleaning up instruction frontmatter"
---

# OpenCode Instruction Management: Operational Lessons Learned

## Context

During the instruction architecture overhaul, several operational lessons were discovered that aren't covered by the main architecture pattern doc. These are practical findings about OpenCode behavior, global skill conflicts, and frontmatter cleanup.

## Guidance

### 1. `invocable: false` Is Dead Frontmatter

OpenCode's official skill frontmatter only recognizes these fields:

- `name` (required)
- `description` (required)
- `license` (optional)
- `compatibility` (optional)
- `metadata` (optional)

**Unknown frontmatter fields are silently ignored.** The `invocable: false` field that appeared in 12 of our 13 skills does nothing. It was likely inherited from a template or copied from another tool's format.

**Action**: Strip it from all skills. It adds noise without effect.

### 2. Global Skill Conflicts — Deny via Config, Not File Edits

When multiple skills have overlapping triggers (e.g., three global commit skills all fire on "commit"), the agent picks one and the others are skipped. File edits to disable global skills are lost on package updates.

**Solution**: Use `permission.skill` in `~/.config/opencode/opencode.json`:

```json
{
  "permission": {
    "skill": {
      "git-commit": "deny",
      "git-commit-push-pr": "deny"
    }
  }
}
```

**Why this works:**

- Denied skills are **hidden from the agent** — they don't appear in `<available_skills>`
- Survives package updates — it's config, not file edits
- Supports wildcards: `"compound-*": "deny"`
- Per-skill granularity: `allow`, `deny`, `ask`

### 3. Procedural vs Declarative Skills — Conceptual, Not Structural

OpenCode doesn't formally distinguish between "command skills" and "guidance skills." Both are loaded the same way. But the distinction matters for content design:

| Dimension    | Procedural (`rm-commit`)               | Declarative (`rm-csharp-standards`)       |
| ------------ | -------------------------------------- | ----------------------------------------- |
| **Nature**   | Action/workflow to execute             | Rules/constraints to follow               |
| **Content**  | Steps, decision points, output format  | Standards, patterns, examples             |
| **Trigger**  | Action verbs: "commit", "save", "ship" | Context phrases: "writing C#", "new file" |
| **Lifespan** | One-shot execution                     | Persistent session context                |

This distinction should influence how you write skill content and design triggers, even though OpenCode treats them identically.

### 4. Skill Trigger Optimization — Match Global Skill Density

When a local skill competes with a global skill for the same trigger, the local skill's description must be at least as comprehensive as the global one. The global `git-commit` skill had this trigger:

> "Use when the user says 'commit', 'commit this', 'save my changes', 'create a commit', or wants to commit staged or unstaged work."

Our local `rm-commit` initially had:

> "Trigger on 'commit', 'commit this', 'save changes', 'create a commit', or any commit-related request."

**Fix**: Expand the local description to match and exceed the global trigger density:

> "Use when the user says 'commit', 'commit this', 'commit these changes', 'save my changes', 'save changes', 'create a commit', 'make a commit', 'git commit', 'check in', 'checkin', or wants to commit staged or unstaged work. Also trigger on any commit-related request, preparing to commit, writing commit messages, or commit-related questions."

### 5. `.github/guides/` Audit — Most Content Is Obsolete

The repo had 8 Copilot instruction files in `.github/guides/`. After auditing against current OpenCode skills:

| File                                  | Status                                 | Action                           |
| ------------------------------------- | -------------------------------------- | -------------------------------- |
| `blazor.md` (77 lines)                | **Real gap** — no Blazor skill exists  | Future: create `rm-blazor` skill |
| `security.md` (59 lines)              | ~80% covered by `rm-security-secrets`  | Future: merge remaining bits     |
| `azure-functions.md` (18 lines)       | ~70% covered by `rm-dotnet`            | Future: merge remaining bits     |
| `aspnet-rest-apis.md` (110 lines)     | Low relevance (static web app)         | Delete                           |
| `documentation.md` (180 lines)        | Static snapshot of dynamic Context7    | Delete                           |
| `high-focused-template.md` (23 lines) | One-off prompt, references unused libs | Delete                           |
| `performance.md` (420 lines)          | Generic multi-language, 5 lines .NET   | Delete                           |
| `github-actions.md` (595+ lines)      | Pipeline config, not coding guidance   | Delete                           |

**Key finding**: The biggest gap is Blazor. This is a Blazor WASM app with no Blazor-specific skill. Lifecycle patterns, render optimization, `ErrorBoundary`, state management — all missing from OpenCode.

### 6. Singular Naming for Skills and Commands

Renamed `rm-commits` → `rm-commit` to match the command name `rm-commit`. Skills and commands live in separate namespaces, so there's no conflict. Singular naming is more consistent and easier to reason about.

## Why This Matters

These lessons prevent common pitfalls:

- **Dead frontmatter** accumulates silently — every skill with `invocable: false` was carrying useless metadata
- **Global skill conflicts** cause unpredictable behavior — the agent picks whichever skill it sees first, not necessarily the right one
- **Weak triggers** cause skills to be skipped — if your local skill's description is less specific than a global competitor, it loses
- **Obsolete guides** create confusion — agents reading `.github/guides/` (if they could) would get outdated, generic advice

## When to Apply

- After installing new global skills that overlap with local ones
- When a skill isn't triggering as expected
- During periodic instruction file audits
- When migrating from Copilot/Claude Code to OpenCode
- When cleaning up frontmatter across multiple skills

## Examples

### Denying a Global Skill

```json
// ~/.config/opencode/opencode.json
{
  "permission": {
    "skill": {
      "git-commit": "deny",
      "git-commit-push-pr": "deny"
    }
  }
}
```

### Optimizing a Skill Description

```yaml
# Before — weak trigger
description: "Generate conventional commit payloads. Trigger on 'commit'."

# After — comprehensive trigger
description: "Shortcut: rm:commit. Generate conventional commit payloads. Use when the user says
  'commit', 'commit this', 'commit these changes', 'save my changes', 'save changes', 'create a
  commit', 'make a commit', 'git commit', 'check in', 'checkin', or wants to commit staged or
  unstaged work. Also trigger on any commit-related request, preparing to commit, writing commit
  messages, or commit-related questions. Produces well-structured conventional commit messages
  that follow this repo's conventions."
```

### Related Docs

- `docs/solutions/integration-issues/opencode-instruction-architecture-pattern-2026-04-03.md` — Main architecture pattern
- `docs/brainstorms/2026-04-03-instruction-architecture-overhaul-requirements.md` — Requirements document
- `docs/brainstorms/2026-04-03-instruction-architecture-findings.md` — Complete findings and recommendations
