---
title: OpenCode treats local plugin as npm package
date: 2026-04-18
category: docs/solutions/workflow-issues
module: opencode
problem_type: workflow_issue
component: tooling
severity: medium
applies_when:
  - Custom plugins exist in .opencode/plugins/ directory
  - Plugin name in opencode.json matches a local file without file:// prefix
tags: [opencode, plugin, npm, local-plugin]
---

# OpenCode treats local plugin as npm package

## Context

A custom plugin `block-push.js` was created in the project's `.opencode/plugins/` directory. It was added to the `plugin` array in `opencode.json` as a plain string:

```json
"plugin": ["opencode-snippets", "cc-safety-net", "block-push"]
```

OpenCode interpreted `"block-push"` as an npm package name and attempted to install it via Bun, which failed because no such npm package exists.

## Guidance

**Do not reference local plugins in the `plugin` array of `opencode.json`.**

Local plugins placed in `.opencode/plugins/` (project-level) or `~/.config/opencode/plugins/` (global) are **automatically loaded at startup** — no configuration needed.

Only add to the `plugin` array when:

1. Loading a specific npm package version (e.g., `"opencode-snippets"`)
2. Using an explicit file:// path for a local plugin that needs a specific entry point

## Why This Matters

When you reference a local plugin name in the plugin array without the `file://` prefix, OpenCode assumes it's an npm package and attempts to install it. This causes:

- Startup delays while Bun tries to resolve the package
- Installation failures for custom plugins not published to npm
- Confusion about whether the plugin loaded successfully

## When to Apply

- When you have custom plugins in `.opencode/plugins/` directory
- When the plugin name doesn't exist as an npm package

## Examples

**Before (broken):**

```json
"plugin": ["opencode-snippets", "cc-safety-net", "block-push"]
```

**After (working):**

```json
"plugin": ["opencode-snippets", "cc-safety-net"]
```

The `block-push.js` file and the local `.opencode/plugins/` directory have been removed (May 2026). Push blocking is now handled by the global `~/.config/opencode/plugins/` pushblocker. The lesson below remains valid for any future local plugins.

## Related

- OpenCode Plugin Documentation: https://open-code.ai/en/docs/plugins
- Existing solution: `docs/solutions/logic-errors/block-push-plugin-logic-errors-2026-04-03.md`
