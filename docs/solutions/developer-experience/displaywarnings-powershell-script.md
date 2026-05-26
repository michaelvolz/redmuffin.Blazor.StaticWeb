---
module: Developer Tooling
date: 2026-04-03
problem_type: developer_experience
component: scripts
severity: low
symptoms:
  - Build output buried in hundreds of lines making warnings hard to find
  - No quick way to see warning distribution by type
  - IL warnings (expected in Blazor WASM) mixed with fixable warnings
root_cause: missing_tooling
resolution_type: tooling_addition
tags:
  - powershell
  - build-warnings
  - developer-tooling
  - scripts
---

# DisplayWarnings PowerShell Script &mdash; Build Warning Dashboard

## Problem

Running `dotnet build` on a project with many warnings produces hundreds of lines of output. Warnings are interleaved with compilation messages, making it hard to see which warning types are most frequent or identify the specific files that need attention. IL warnings (expected in Blazor WASM projects) are indistinguishable from fixable warnings.

## Root Cause

No tool existed to parse, categorize, and summarize build output. Developers had to manually scan verbose build logs.

## Solution

A PowerShell script (`scripts/DisplayWarnings.ps1`) that:

1. Runs `dotnet clean` then `dotnet build` in the repository root.
2. Captures and hides the full build output.
3. Parses warnings from the output stream.
4. Groups warnings by type and sorts by frequency (highest count first).
5. Displays a color-coded summary with emojis for readability.
6. Separates IL\* warnings (displayed last with a softened color) to distinguish expected vs fixable warnings.

### Features

- **Frequency-sorted output**: Most common warning types appear first, guiding cleanup priority.
- **Color differentiation**: Fixable warnings in one color, IL warnings in a softer color.
- **Emoji-enhanced readability**: Visual markers for different warning categories.
- **Progress indicators**: Shows build status as it runs.
- **Edge case handling**: Works correctly with zero warnings, build errors, etc.

## Prevention

The script provides continuous visibility into the warning state. Run it as part of the pre-commit workflow or as a quick health check before starting a cleanup session. When paired with the systematic cleanup approach (see `systematic-build-warning-cleanup-2026-04-03.md`), it provides both detection and resolution.
