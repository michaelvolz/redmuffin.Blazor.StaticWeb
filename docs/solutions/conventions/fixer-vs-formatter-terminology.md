---
title: "ConfigureAwaitFixer is a fixer, not a formatter"
date: 2026-08-05
category: conventions
module: configureawait-fixer
problem_type: convention
component: tooling
severity: medium
applies_when:
  - "Diagnosing PostToolUse hangs or timeouts after .cs or .razor edits"
  - "Writing logs, solutions docs, or symptoms about ConfigureAwaitFixer"
  - "Choosing between ConfigureAwaitFixer, csharpier, and dotnet format"
tags:
  - configureawait
  - fixer
  - formatter
  - terminology
  - grok-hooks
  - code-formatters
  - csharpier
  - ca2007
  - dotnet-format
related_components:
  - development_workflow
  - documentation
---

# ConfigureAwaitFixer is a fixer, not a formatter

## Context

The Grok post-edit pipeline on this machine is defined in user harness config
`~/.grok/hooks/bin/code-formatters.json` (outside the repo). That file uses a
generic `formatters` array for every per-file tool. Current layout relevant to
this repo:

| Extension       | Pipeline steps (order)                                                     |
| --------------- | -------------------------------------------------------------------------- |
| `.cs`           | `ConfigureAwaitFixer.exe --fix {{file}}`, then `csharpier format {{file}}` |
| `.razor`        | `dotnet format --include {{file}}` only                                    |
| Other languages | ruff / gofmt / rustfmt / shfmt / taplo / prettier as configured            |

The config file’s name and the word “formatters” in the schema are harness
plumbing. They do **not** redefine ConfigureAwaitFixer as a formatter.
ConfigureAwaitFixer occupies a slot in that array; it remains a Roslyn CA2007
**fixer**.

Agents and docs repeatedly called CAF a “formatter” when diagnosing hangs.
That conflation pointed investigation at csharpier or `dotnet format` while the
real `.cs` failure mode was CAF cold MSBuildWorkspace open, Job Object kill of
the warm daemon, or an undeployed binary. User correction in the 2026-08 session:
“format hang” framing was largely a red herring for the `.cs` path.

## Guidance

**Reserve “formatter” for whitespace/style tools.** csharpier, `dotnet format`,
prettier, ruff format, and similar tools restyle layout. They do not apply
Roslyn CodeFixProviders. Exhaustive earlier research already established that
`dotnet format` never invokes `RegisterCodeFixesAsync` (see
`docs/solutions/developer-experience/automated-configureawait-fixer.md`).

**Call ConfigureAwaitFixer a fixer.** Preferred words: ConfigureAwaitFixer,
CAF, fixer, `--fix` client/daemon. It loads the official CA2007 analyzer and
rewrites source (add `.ConfigureAwait(false)` where appropriate). That is a
code fix, not a format pass.

**Name the executable when recording a hang.** Prefer
“`ConfigureAwaitFixer.exe --fix` stalled near the orchestrator limit” over
“the formatter hung.” If the stall is after CAF finishes, name csharpier. If
the file is `.razor`, name `dotnet format` — CAF never runs on that extension
in the current hooks.

**Do not invent a CAF issue from a generic “formatter” log.** Translate older
notes that say “PostToolUse formatter hangs” as “CAF hang on the `.cs` path”
unless the extension or command line proves otherwise.

## Why This Matters

1. **Wrong root cause.** “Format hang” steers agents into csharpier/`dotnet
format` while the load-bearing failure on `.cs` was CAF warm-path/Job Object
   behavior (see
   `docs/solutions/performance-issues/configureawait-daemon-job-object-detached-spawn.md`).
2. **Wrong tooling.** “Make the formatter fix CA2007” revisits a dead end:
   formatters do not apply CodeFixProviders; only a fixer client does.
3. **Wrong surface.** `.razor` failures cannot be CAF; `.cs` stalls must
   consider CAF first, then csharpier as the second step.

## When to Apply

- Debugging PostToolUse / Morpheus Host timeouts after an edit
- Writing symptoms, daemon logs, or solution docs about this pipeline
- Reading older docs that say “formatter” while discussing ConfigureAwaitFixer
- Deciding whether to touch CAF source, csharpier, or `dotnet format`

## Examples

**Loose (misleading):**

> PostToolUse formatter hangs until the ~30 s orchestrator kill.

**Precise:**

> `ConfigureAwaitFixer.exe --fix` on a `.cs` write stalls near the orchestrator
> limit; root cause is CAF cold open / daemon lifecycle, not csharpier.

**Extension split:**

- `.razor` format failure → `dotnet format` only; do not blame CAF.
- `.cs` stall → check CAF first (deploy, daemon under svchost, log, warm vs
  cold), then csharpier if CAF already completed.

**Harness plumbing (allowed wording):**

- “Entry in `code-formatters.json`” or “post-edit hook entry” — OK when naming
  the config mechanism.
- “CAF is a formatter” — not OK.

## Related

- `docs/solutions/performance-issues/configureawait-daemon-job-object-detached-spawn.md`
  — Job Object / WinExe / warm daemon. **Refresh:** several places call the CAF
  path a “formatter” (symptoms, guidance); reword to fixer while keeping the
  config filename where it is literal.
- `docs/solutions/developer-experience/automated-configureawait-fixer.md` —
  why `dotnet format` cannot apply CodeFixProviders; correctly uses “fixer.”
- `docs/solutions/tooling-decisions/configureawait-fixer-nuget-targets-removal.md`
  — hook-owned delivery of the fixer (not packaging).
- `docs/solutions/tooling-decisions/configureawait-auto-fix-research.md` —
  research path that separates script/fixer from MSBuild and format.
- `CONCEPTS.md` — ConfigureAwaitFixer (fixer), Formatter (post-edit), daemon,
  hook-owned delivery.
