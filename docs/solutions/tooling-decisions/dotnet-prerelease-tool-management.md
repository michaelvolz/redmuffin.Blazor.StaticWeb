---
date: 2026-05-24
module: tooling-decisions
tags:
  - dotnet
  - topgrade
  - nuget
  - prerelease
  - omarchy
problem_type: configuration
title: Two-Tier .NET Tool Management for Prerelease Packages
---

## What Belongs in This File

The architecture and rationale for splitting .NET global tools into two installation tiers: stable tools managed by `--global` (auto-updated by topgrade) and prerelease-only tools managed by `--tool-path ~/.local/bin` (curated via topgrade `[post_commands]`).

What does NOT belong: NuGet package version management (see `rm-nuget-manager`), Omarchy package installation policies (see `rm-omarchy`), general topgrade configuration, or any tool-specific troubleshooting unrelated to the prerelease split.

## Problem

NuGet packages tagged as prerelease are opt-in: every `dotnet tool` command that should see them requires an explicit `--prerelease` flag. Without it, prerelease-only packages are invisible.

Topgrade's built-in `.NET` step runs:

```bash
dotnet tool update --global --all
```

This command cannot see prerelease-only packages. If a .NET global tool ships only prerelease versions on NuGet, topgrade silently skips it on every system update. The tool drifts, security patches are missed, and the user has no signal that anything is wrong.

`roslyn-language-server` is such a package. It is a Microsoft .NET global tool required by OpenCode's built-in LSP server, and every version on NuGet is prerelease. Topgrade's built-in step will never update it.

Topgrade has no `[dotnet]` config section — there is no way to customize the .NET update command to inject `--prerelease`. The only integration points are `[pre_commands]` and `[post_commands]`.

## Two-Tier Architecture

The solution establishes two installation tiers, separated by installation mechanism rather than by tool identity alone:

### Tier 1: Stable tools (`--global`)

- Installed via `dotnet tool install --global`
- Reside in `~/.dotnet/tools/`
- Automatically updated by topgrade's built-in `.NET` step
- Must have at least one stable (non-prerelease) version on NuGet
- Examples: `dotnet-reportgenerator-globaltool`, `roslynator.dotnet.cli`

### Tier 2: Prerelease-only tools (`--tool-path`)

- Installed via `dotnet tool install --tool-path ~/.local/bin`
- Reside in `~/.local/bin/`
- Updated via a curated topgrade `[post_commands]` entry that passes `--prerelease`
- Only used for tools that ship exclusively as prerelease on NuGet
- Example: `roslyn-language-server`

The tier assignment is permanent for a given tool unless its NuGet release status changes.

## PATH Ordering Guarantee

`~/.local/bin` is at PATH position 19. `~/.dotnet/tools/` is at position 20. Because `~/.local/bin` appears first, the tool-path install shadows the global install if the same tool exists in both locations.

This ordering is enforced by `~/.bashrc` and validated by `rm-system-startup`. The invariant is:

```
~/.local/bin/     (pos 19) → tool-path installs, prerelease-only tools
~/.dotnet/tools/  (pos 20) → global installs, stable tools
```

A tool installed to `--tool-path` will always be the one resolved by the shell, regardless of whether a stale global copy exists.

## Topgrade Configuration

In `~/.config/topgrade.toml`:

```toml
[post_commands]
"dotnet prerelease tools" = "dotnet tool update roslyn-language-server --prerelease --tool-path ~/.local/bin"
```

This runs after the built-in `.NET` step completes. The `--prerelease` flag is explicit. The `--tool-path` argument matches the install location. If additional prerelease-only tools are adopted in the future, append them as separate `dotnet tool update` calls.

## Migration Steps

When a tool is discovered to be prerelease-only and is currently installed globally:

1. **Uninstall the global copy:**

   ```bash
   dotnet tool uninstall --global <tool-name>
   ```

2. **Install to tool-path:**

   ```bash
   dotnet tool install --prerelease --tool-path ~/.local/bin <tool-name>
   ```

3. **Add post-command to `~/.config/topgrade.toml`:**

   ```toml
   [post_commands]
   "dotnet prerelease tools" = "dotnet tool update <tool-name> --prerelease --tool-path ~/.local/bin"
   ```

   If other prerelease-only tools already have entries, append the new tool to the same command string or add a separate entry.

4. **Verify PATH ordering** — `~/.local/bin` must appear before `~/.dotnet/tools/` in `$PATH`. If it does not, fix `~/.bashrc` and re-source.

5. **Run topgrade once** to confirm the post-command executes and the tool updates cleanly.

## Why mise Was Rejected

mise supports `dotnet` as a runtime backend but managing dotnet tools through mise adds an abstraction layer with no benefit. The `dotnet tool` commands manage installation and updates natively. Routing them through mise would replace one working mechanism with a more complex one that introduces shim naming, PATH ordering, and tool discovery unknowns.

The Omarchy simplicity principle governs: use the tool's native mechanism (`dotnet tool`) for tool management, and use mise only for runtime version management. The two-tier pattern is the simplest solution that solves the problem with zero new dependencies.

## Future Considerations

If a prerelease-only tool ships a stable version on NuGet, it should be migrated from Tier 2 to Tier 1:

1. Uninstall the tool-path copy: `dotnet tool uninstall --tool-path ~/.local/bin <tool-name>`
2. Install globally: `dotnet tool install --global <tool-name>`
3. Remove the tool from the topgrade `[post_commands]` entry

The reverse migration (stable → prerelease-only) is covered by the migration steps above. The trigger in both cases is a change in the tool's NuGet release status — this is a manual audit step, not an automated one.
