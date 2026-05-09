---
title: rm-git-sidebar TUI Plugin for OpenCode
date: 2026-05-09
category: developer-experience
module: opencode
problem_type: developer_experience
component: development_workflow
severity: low
applies_when:
  - Building OpenCode TUI plugins
  - Creating persistent sidebar views in the OpenCode terminal UI
  - Building Bun-based OpenCode plugins
tags: [tui, sidebar, git, opencode, solidjs, bun, plugin]
---

# rm-git-sidebar TUI Plugin for OpenCode

## Context

OpenCode's TUI lacked a persistent view of git state — agents and developers had to run `git status` repeatedly to see branch, sync state, recent commits, and uncommitted changes. This created cognitive load during long sessions where context-switching between code and git state was frequent.

## Guidance

Built `rm-git-sidebar` as a SolidJS-based TUI plugin using the `@opencode-ai/plugin/tui` API. Shows:

- **Current branch** with ahead/behind counts relative to remote
- **Recent commits** (last 10) with hash, author, and message
- **Uncommitted changes** by category: staged, unstaged, untracked
- **Session-scoped file tracking** — files touched during the current session

Uses porcelain git commands (`git status --porcelain=v2 --branch`, `git log --format=...`) for stable, machine-readable output that doesn't change across Git versions or user configurations.

Packaged as a Bun-based plugin with oxlint for linting. Lives at `.opencode/plugins/rm-git-sidebar/`.

## Why This Matters

Persistent git visibility reduces the cognitive load of context-switching during agent sessions. The session-scoped file tracking answers "what did this session touch?" without querying git blame or session history. Porcelain commands guarantee stable output regardless of user git config.

## When to Apply

- Enable in `tui.json` plugin list — starts automatically when OpenCode TUI loads
- Useful during any development session where git state awareness matters

## Related

- `.opencode/plugins/rm-git-sidebar/tui.tsx` — Plugin entry point
- `.opencode/plugins/rm-git-sidebar/git.ts` — Git command wrappers
