---
title: "Quality Gates Tool — Operational Gotchas and Development Workflow"
date: 2026-05-09
category: developer-experience
module: quality-gates-tool
problem_type: developer_experience
component: tooling
severity: medium
applies_when:
  - "Building, testing, or packing the redmuffin.Tools.QualityGates dotnet tool"
  - "Adding new quality gate subcommands to the tool"
  - "Running the tool via dotnet test or dotnet run"
tags:
  [
    quality-gates,
    dotnet-tool,
    tunit,
    roslyn,
    sdk-version,
    testing-workflow,
    scrap,
    crap,
  ]
---

# Quality Gates Tool — Operational Gotchas and Development Workflow

## Context

The `redmuffin.Tools.QualityGates` dotnet tool was built across 5 sessions on
branch `feat/scrap-test-structural-analyzer`, implementing U1-U10 (SCRAP test
structural analyzer, AllCommand composition, CLI wiring). The tool targets
.NET 10 and uses Roslyn for C# analysis, TUnit for its own tests, and
System.CommandLine for CLI parsing. During development, multiple operational
gotchas surfaced that are non-obvious to both developers and agents starting
fresh work on this tool.

## Guidance

### SDK version: always run from `tools/` directory

The repo root `global.json` pins SDK 9.0.100, but the tool requires .NET 10
(pinned in `tools/global.json` to 10.0.104). Running dotnet commands from the
repo root produces:

```
error NETSDK1045: The current .NET SDK does not support targeting .NET 10.0
```

**All commands must run with `tools/` as the working directory:**

```bash
cd tools
dotnet build src/redmuffin.Tools.QualityGates --verbosity quiet
```

### Test execution: use `dotnet run`, NOT `dotnet test`

TUnit + Microsoft.Testing.Platform in AOT mode causes `dotnet test` to
discover **zero tests**. The only working test command is:

```bash
cd tools
dotnet run --project tests/redmuffin.Tools.QualityGates.Tests
```

**Do not** attempt `dotnet test` — it silently finds nothing (session history).

### TDD pattern (enforced by rm-tdd)

- Write **exactly one** failing test first
- Minimal production code to make it pass
- Refactor, then next test
- Test the **Handler** directly, not the Command:

```csharp
// Correct: test Handler.Run() directly
var exitCode = ScrapHandler.Run(reports, options, output);
await Assert.That(exitCode).IsEqualTo(0);

// Avoid: calling Command.Execute() which requires CLI wiring and real files
```

### TUnit assertion API

Use `.IsEqualTo(n)`, `.Contains("text")`, `.IsLessThan(n)`.
**Never** use `.HasCount()` or `.SequenceEqual(features).IsTrue()` (session
history: these caused test failures). Use `.Count.IsEqualTo(n)` instead.

### Command pattern (follow CrapCommand exactly)

New gates follow `CrapCommand` / `CrapHandler` pattern:

- **Command**: static class, `Create()` returns `Command` with options,
  `internal static int Execute(...)` wires the pipeline
- **Handler**: separate static class, `Run()` with `TextWriter? output` param,
  returns exit code (0=pass, 1=error, 2=threshold breach)

### Analysis pipeline pattern

Every gate: **Parser → Normalizer → Analyzer → Scorer → Recommender → Handler**

For SCRAP specifically: `TestMethodParser.FindTests()` → `ScrapDuplication.Analyze()` (handles normalization internally) → `ExtractionPressure.ComputeFilePressure()` → `ScrapScorer.ScoreFile()` → `ScrapRecommender.Decide()` → `ScrapHandler.Run()`

### Pack and install

Pack works (from `tools/` directory):

```bash
dotnet pack src/redmuffin.Tools.QualityGates --output nupkgs
```

`dotnet tool install/update` currently broken with .NET 10 packaging issue
(`DotnetToolSettings.xml` path). Smoke testing must use:

```bash
dotnet run --project src/redmuffin.Tools.QualityGates -- scrap --test-project <path>
```

### Internal visibility

The tool project has **no `InternalsVisibleTo`**. Handlers are `public static`,
subagent types and records are `public`. Keep it this way unless strongly needed.

### Key architecture decisions (from ADR-0003)

- TUnit only (not multi-framework) — fits repo's test framework
- Roslyn syntax-node normalization (not token-stream) — preserves assertion shape
- Full SCRAP fidelity — Jaccard + extraction pressure + AI-actionability
- Per-file `CSharpSyntaxTree.ParseText()` (not `MSBuildWorkspace`) — mirrors CRAP
- SCRAP needs no `--coverage-file` — purely structural
- All thresholds locked to Uncle Bob's [`policy.clj`][scrap-policy]

## Why This Matters

Without this knowledge, every new session wastes time rediscovering:

- `dotnet test` silently finds zero tests
- Commands fail with SDK version errors from the wrong directory
- The test assertion API differs from handoff expectations
- `CombineExitCodes` was `internal` — needed to be `public` for testability
- Single quotes in interpolated string headers parse as character literals

The command-handler separation is critical for testability — commands are thin
wrappers around System.CommandLine wiring; handlers contain the business logic
tested directly. Missing this pattern leads to untestable code.

## When to Apply

- Building or extending the Quality Gates tool
- Adding new subcommands (follow CrapCommand + CrapHandler pattern)
- Running or debugging the tool (always from `tools/` directory)
- Setting up a new dev environment for the tool
- Investigating test failures (verify `dotnet run --project`, not `dotnet test`)
- Upgrading to a future .NET SDK where `dotnet test` may work again

## Examples

```bash
# WRONG — fails with SDK version error
dotnet build tools/src/redmuffin.Tools.QualityGates
# ERROR: The current .NET SDK does not support targeting .NET 10.0

# CORRECT
cd tools
dotnet build src/redmuffin.Tools.QualityGates --verbosity quiet

# WRONG — discovers 0 tests
dotnet test tools/tests/redmuffin.Tools.QualityGates.Tests

# CORRECT — runs all 104 TUnit tests
cd tools
dotnet run --project tests/redmuffin.Tools.QualityGates.Tests

# CORRECT — smoke test the scrap gate against real test files
cd tools
dotnet run --project src/redmuffin.Tools.QualityGates -- scrap --test-project ../tests/redmuffin.Blazor.StaticWeb.Tests

# CORRECT — pack for local distribution
cd tools
dotnet pack src/redmuffin.Tools.QualityGates --output nupkgs

# CORRECT — run with verbose per-example output
cd tools
dotnet run --project src/redmuffin.Tools.QualityGates -- scrap --test-project ../tests/redmuffin.Blazor.StaticWeb.Tests --verbose
```

## Related

- [CRAP Quality Gates Pipeline](tooling-decisions/crap-quality-gates-pipeline-2026-05-09.md) — primary solutions doc
- [ADR-0002: Quality Gates Toolchain](../../docs/adr/0002-quality-gates-toolchain.md)
- [ADR-0003: SCRAP Test Structural Analyzer](../../docs/adr/0003-scrap-test-structural-analyzer.md)
- [SCRAP Implementation Plan](../../docs/plans/2026-05-09-002-feat-scrap-test-structural-analyzer-plan.md)
- [Uncle Bob's scrap source](https://github.com/unclebob/scrap) — `policy.clj` thresholds
- [CRAP Formula Measurement Gaps](/docs/solutions/developer-experience/crap-formula-cobertura-coverage-divergence-2026-05-16.md) — metric semantics vs. Cobertura coverage
- [Design Changes Are the Point](/docs/solutions/conventions/design-changes-are-the-point-cleanup-philosophy-2026-05-16.md) — cleanup philosophy: design changes over mechanical extraction

[scrap-policy]: https://github.com/unclebob/scrap/blob/master/src/scrap/policy.clj
