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

- Never write a one-off command when an existing script covers the task.
- Keep config changes narrow and reversible.

## Build & Test

- **Before every commit**: Run `dotnet run --project tests/redmuffin.Blazor.StaticWeb.Tests` (All 325+ tests must pass).
- **After every C# edit**: Run `dotnet build --verbosity quiet`. SCSS is compiled by the dev server systemd unit (`sass --watch`).
- **Repeated failures**: Run `dotnet clean` first, then rebuild.
- **Zero warnings on build**: All analyzer warnings are improvement signals. Fix root causes, never suppress.

## Repository Conventions

- All workflows and Dependabot configuration go in `.github/`.
- Trunk-Based Development: `main` is the source of truth. Feature branches only for high-risk changes.
- New files in `docs/` must have `date: YYYY-MM-DD` frontmatter.
- Fast file search: Use `es.exe` when available; fall back to `grep` only if `es.exe` is unavailable.

## Pragma Warnings

- **Ask first** before changing any `#pragma warning disable` directive.
- All pragma warnings are deliberate. Goal: zero warnings on build.
- Reviewers must target the correct subfolder or "Local" only.

## NEVER

- Do not scatter version literals across projects.
