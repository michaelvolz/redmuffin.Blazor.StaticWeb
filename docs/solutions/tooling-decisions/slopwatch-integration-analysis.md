---
title: Slopwatch Integration Analysis — LLM Anti-Cheat Gate
date: 2026-06-28
category: tooling-decisions
module: tools
problem_type: tooling_decision
component: tooling
severity: high
applies_when:
  - Integrating slopwatch as a new quality gate
  - Evaluating LLM reward-hacking detection for .NET projects
  - Deciding exception/justification policy for slopwatch findings
tags:
  [
    slopwatch,
    llm-anti-cheat,
    quality-gates,
    reward-hacking,
    baseline,
    static-analysis,
  ]
---

# Slopwatch Integration Analysis — LLM Anti-Cheat Gate

## What Belongs in This File

- **Viewpoint**: Agent evaluating slopwatch as a quality gate for this repo.
  Reader knows the existing 6-gate pipeline (Depth, Architecture, CRAP, SCRAP,
  Mutation, Dupes).
- **What belongs**: Tool identity, detection rules, findings breakdown,
  exception mechanisms, integration strategy, workflow recommendations.
- **What does NOT belong**: Per-issue fix instructions, implementation
  commits, gate source code, general quality-gate philosophy already covered
  by `rm-cleanup-session`.

## Context

Slopwatch v0.4.2 by [Aaron Stannard](https://github.com/Aaronontheweb/dotnet-slopwatch)
is a .NET global tool that detects LLM "reward hacking" — patterns where an
LLM satisfies the literal goal (green tests, passing build) by cheating:
disabling tests, suppressing warnings, adding arbitrary delays, emptying catch
blocks, or bypassing package management. The tool's philosophy: **the tool is
right, our code is wrong** — until proven otherwise with 100% certainty.

This analysis evaluates slopwatch as a new gate in the existing quality pipeline,
covering its detection rules, findings against our codebase, exception
mechanisms, and recommended workflow.

## Detection Rules (6 total)

| Rule | Severity | Description |
|------|----------|-------------|
| SW001 | Error | Disabled tests (`[Fact(Skip)]`, `[Ignore]`, `#if false`) |
| SW002 | Warning | Warning suppression (`#pragma`, `[SuppressMessage]`) |
| SW003 | Error | Empty catch blocks that swallow exceptions |
| SW004 | Warning | `Task.Delay` / `Thread.Sleep` in test code |
| SW005 | Warning | Project file slop (`<NoWarn>`, `<TreatWarningsAsErrors>false`) |
| SW006 | Error | CPM bypass (`VersionOverride`, inline `Version` attributes) |

## Findings — 99 Issues (All Warnings)

434 files analyzed in 0.5s. No errors, no info-level findings — all 99 issues
are warnings.

### SW004 — Test Timeout Jiggling (~95 issues)

`Task.Delay()` calls in test code, flagged because timing-dependent tests are
fragile and slow. Two sub-categories:

- **Real test delays (~85)**: Tests using `Task.Delay(50-200ms)` to wait for
  async cache operations, debounce timers, or polling loops. Each needs
  individual evaluation: can it be replaced with `TaskCompletionSource`,
  `SemaphoreSlim`, or a test hook? These are genuine code quality concerns.

- **False positives (~10)**: `ConfigureAwaitFixerTests.cs` contains
  `Task.Delay(100)` inside string literals (test fixture source code), not
  actual execution. The pattern fires on the literal text.

### SW002 — Warning Suppression (2 issues)

- `tools/src/.../ArchConfig.cs:46` — `[SuppressMessage("Design", "CA1812")]`
  for YamlDotNet reflection. Already documented in `rm-warnings` KNOWN
  CONFLICTS. **Legitimate exception** — the type IS instantiated, just not by
  compiler-visible code.
- `tools/src/.../ScrapDuplication.cs:1` — `#pragma warning disable CA1859`
  for the CA1859/MA0016 analyzer contradiction. Already documented in
  `rm-warnings`. **Known analyzer conflict.**

### SW005 — Project File Slop (2 issues)

- `Directory.Build.props:122` — `<NoWarn>1701;1702;CA1014;AD0001</NoWarn>`.
  MSBuild warnings + CA1014 (CLSCompliant). Needs investigation.
- `src/.../redmuffin.Blazor.StaticWeb.csproj:16` — `<NoWarn>$(NoWarn);WASM0001</NoWarn>`.
  Blazor WASM-specific warning. Needs investigation.

### Clean Rules (0 issues)

- SW001 (disabled tests): Clean — no `[Skip]`, `[Ignore]`, or `#if false` in tests.
- SW003 (empty catch blocks): Clean — no exception-swallowing patterns.
- SW006 (CPM bypass): Clean — no `VersionOverride` or inline `Version` attributes.

## Exception Mechanisms (3 Tiers)

Slopwatch provides three tiers of exception handling, ordered from most to
least acceptable:

### Tier A — Baseline (Bulk Grandfathering)

```bash
slopwatch init                    # Creates .slopwatch/baseline.json
git add .slopwatch/baseline.json  # Commit it
slopwatch analyze                 # Only NEW detections reported
```

The baseline acknowledges existing findings predate the tool. After committing
the baseline, only newly introduced violations fail the gate. `--update-baseline`
adds new detections to the baseline after intentional additions.

### Tier B — Config File (Pattern-Based Suppressions)

`.slopwatch/config.json`:

```json
{
  "suppressions": [
    {
      "ruleId": "SW004",
      "pattern": "**/ConfigureAwaitFixerTests.cs",
      "justification": "Task.Delay appears in test fixture source strings, not actual execution"
    }
  ]
}
```

For categorical false positives — entire files or directories where a rule
produces noise. Requires a written justification. Glob patterns match file paths.
Use `--config` flag to specify path.

### Tier C — Inline Suppression (Instance-Specific, Last Resort)

- C#: `[SlopwatchSuppress("SW004", "reason with 20+ chars minimum")]`
- XML/project files: `<!-- slopwatch-ignore: SW005 [justification here] -->`

For single, genuinely justified instances that cannot be fixed. The 20-character
minimum forces meaningful explanation. **This is the last resort.** Exceptions
must be the utmost rarity — finding another solution is 100% preferable.

## Suppression Decision Tree

Before applying any suppression (config or inline), answer three questions
(mirrors `rm-warnings` Pragma Decision Tree):

| Question | For Slopwatch |
|----------|---------------|
| Q1: Is this a genuine false positive or tool limitation? | Does the pattern fire on string literals, generated code, or reflection-only types? |
| Q2: Would fixing this make the code WORSE? | Would replacing `Task.Delay` in a timeout-test change the test's meaning? |
| Q3: Is this a one-off, not a recurring pattern? | One file → inline suppression. Many files → config pattern. |

All three must answer YES for suppression. If any answer is NO, fix the code.

## Integration with Existing Quality Gates

The existing gate pipeline runs: Depth → Architecture → CRAP → SCRAP → Mutation →
Dupes. Slopwatch is fundamentally different:

- **Speed**: 0.5s vs 17-600s for the full gate suite
- **Scope**: File-level pattern detection, not build/coverage-dependent
- **Philosophy**: Catches LLM cheating, not general code quality
- **Mode**: Designed as a pre-commit/hook check, not a post-build analysis

**Recommended gate order**: Slopwatch should run **first** — before any other
gate. It is the fastest gate and catches patterns that make other gates
meaningless (disabled tests, suppressed warnings). If slopwatch fails, stop
immediately.

Proposed new order: **Slopwatch → Depth → Architecture → CRAP → SCRAP →
Mutation → Dupes**

## Hook Mode (Real-Time LLM Guard)

The `--hook` flag enables integration as a PostToolUse hook:

- Only analyzes git-dirty files (near-instant on large repos)
- Outputs errors to stderr
- Suppresses all other output
- Fails on warnings and errors (exit code 2)
- Falls back to full analysis if git is unavailable

For Grok Build, the hook would live in `~/.grok/hooks/` as a PostToolUse hook
on Write/Edit/MultiEdit events, running `slopwatch analyze -d . --hook`.
Consult `rm-grok-build` for the exact hook JSON schema.

## Recommended Workflow

### Phase 1: Initialize (One-Time Setup)

1. Audit the 99 current findings — classify each as fixable, false positive,
   or justified exception.
2. Fix all fixable findings first (replace `Task.Delay` with proper
   synchronization, investigate `NoWarn` entries).
3. Document false positives in `.slopwatch/config.json` with pattern-based
   suppressions and written justifications.
4. For justified exceptions that cannot be pattern-matched, use inline
   `[SlopwatchSuppress]` only after the decision tree confirms no fix is
   possible.
5. Create the baseline from remaining detections:
   `slopwatch init --force`
6. Commit `.slopwatch/baseline.json` and `.slopwatch/config.json` to the repo.

### Phase 2: Ongoing (Gate Integration)

7. Add slopwatch as the first step in `scripts/Run-QualityGates.ps1`
   (exit early on failure).
8. Add slopwatch as a PostToolUse hook (optional but powerful — catches
   slop at edit time).
9. CI/CD: `slopwatch analyze` (with baseline) in the pipeline.

### Phase 3: Exception Documentation Standard

Every exception must answer the three decision-tree questions above.
Documentation lives in:
- Config file `justification` field (pattern-level)
- Inline suppression 20+ char reason (instance-level)
- `rm-warnings` KNOWN CONFLICTS table (cross-referenced)

## Open Questions

1. **SW005 NoWarn entries** — Are `1701;1702;CA1014;AD0001` and `WASM0001`
   fixable or justified? Needs investigation before baseline creation.
2. **Hook integration specifics** — Grok Build hook JSON schema differs from
   Claude Code's `.claude/settings.json`. Consult `rm-grok-build`.
3. **Config file auto-discovery** — Does slopwatch auto-discover
   `.slopwatch/config.json` in the project root, or must `--config` always be
   explicit? The docs show it as explicit.
4. **Baseline merge conflicts** — Multiple branches adding different
   suppressions to `baseline.json` could conflict. Standard JSON merge
   resolution applies.
5. **Tools solution coverage** — Slopwatch found 2 SW002 + 4 SW004 issues in
   `tools/`. The dogfooding principle says the tools solution must also pass.
6. **Baseline vs Config precedence** — When a finding is both in the baseline
   AND matched by a config suppression, which takes priority? Needs testing.

## Summary

Slopwatch is a fast, focused tool that complements the existing quality gates
without overlapping them. It catches LLM-specific cheating patterns that
structural gates (CRAP, Depth) cannot detect. The three-tier exception system
(baseline → config → inline) provides clear escalation that respects the
"exceptions are the utmost rarity" constraint. Integration as the first gate
in the pipeline, with an optional real-time hook, gives us defense-in-depth
against LLM slop at both edit time and pre-commit time.

## Related

- `docs/solutions/tooling-decisions/crap-quality-gates-pipeline.md`
- `docs/solutions/tooling-decisions/analyzer-warning-distribution-prioritization.md`
- `docs/solutions/best-practices/crap-driven-functional-refactoring.md`
- [Slopwatch GitHub](https://github.com/Aaronontheweb/dotnet-slopwatch)
