---
title: Shell-Aware PowerShell Execution
date: 2026-04-11
category: developer-experience
module: ce:compound
problem_type: developer_experience
component: tooling
severity: medium
applies_when:
  - Executing PowerShell commands via the bash tool on Windows
  - Executing PowerShell commands via the bash tool on Linux/omarchy
tags:
  - powershell
  - bash-tool
  - execution
  - shell
---

# Shell-Aware PowerShell Execution

## Context

PowerShell commands executed via the bash tool failed with parse errors on Windows when the command contained `$`, `@{}`, `$_`, or `()` expressions. The root cause: the bash tool invokes `pwsh -NoProfile -Command "..."` internally, creating a nested shell that double-evaluated PowerShell syntax before it reached the target pwsh instance.

This manifested as silent data loss where variables and expressions were mangled or stripped entirely, producing incorrect results or cryptic parse failures with no obvious error message.

## Guidance

**Shell-aware PowerShell execution** is required to handle the different shells correctly:

- **On Windows** (shell = pwsh): Execute PowerShell commands **directly** without wrapping. The outer shell is already pwsh, so nesting another pwsh invocation causes double-evaluation.

  ```
  Get-ChildItem | ForEach-Object { $_.Name }
  ```

- **On Linux/omarchy** (shell = bash): Wrap PowerShell commands with **single quotes** to prevent bash from interpreting `$`, `@{}`, `$_`, `()` before passing them to the inner pwsh.

  ```
  pwsh -NoProfile -Command 'Get-ChildItem | ForEach-Object { $_.Name }'
  ```

- **For complex scripts**: Write a `.ps1` file, then execute with `-File` to avoid any shell interpolation issues.

  ```
  pwsh -NoProfile -File path/to/script.ps1
  ```

## Why This Matters

Without this guidance, every PowerShell command containing variables, hashtables, pipeline variables (`$_`), or subexpressions fails silently or produces parse errors. During this session, multiple commands failed with no meaningful feedback, causing debug loops and wasted time. Applying the correct shell-aware pattern ensures PowerShell syntax reaches the interpreter intact.

## When to Apply

- On Windows when the bash tool shell is pwsh
- On Linux/omarchy when the bash tool shell is bash (inverted execution model)
- Any time a PowerShell command contains `$variable`, `@{key=value}`, `$_`, or `(expression)` syntax

## Examples

**DO** (Windows - shell is already pwsh):

```
Get-ChildItem | ForEach-Object { $_.Name }
```

**DO** (Linux/omarchy - use single quotes to pass through):

```
pwsh -NoProfile -Command 'Get-ChildItem | ForEach-Object { $_.Name }'
```

**DON'T** (Windows - double-evaluation destroys `$` and `@`):

```
pwsh -NoProfile -Command "Get-ChildItem | ForEach-Object { $_.Name }"
```

**DON'T** (Linux/omarchy - double quotes let bash interpolate):

```
pwsh -NoProfile -Command "Get-ChildItem | ForEach-Object { $_.Name }"
```

## Related Issues

- `docs/solutions/developer-experience/bash-timeout-kills-long-running-dotnet-processes-2026-04-04.md` — complementary doc on bash tool limitations
