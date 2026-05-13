---
name: rm-guide-config
description: "Use when touching build commands, dev modes, package management, or repository configuration."
---

# rm-guide-config

## CRITICAL

- Keep package versions centralized in `Directory.Packages.props`.
- Use repo scripts for package updates and verification.
- Use `pwsh -NoProfile` for PowerShell commands.

## WHEN TO LOAD

- Editing build, test, package, or dev workflow docs/configuration.
- Updating repo-level conventions or startup scripts.

## GUIDANCE

- Prefer existing scripts over one-off commands.
- Keep config changes narrow and reversible.

## NEVER

- Do not scatter version literals across projects.
