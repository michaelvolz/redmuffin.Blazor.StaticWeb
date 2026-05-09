# Quality Gates Toolchain

A local `dotnet tool` that runs the full Uncle Bob agentic coding metric suite
against this repo: CRAP, mutation, SCRAP, duplication, and architecture
checks. Every gate must pass before work is considered done.

## Why this exists

The [rm-uncle-bob-martin-agentic-coding][uncle-bob-skill] skill mandates
running a full metric suite after every significant change. Coverage alone is
insufficient — it must be paired with cyclomatic complexity (CRAP score),
mutation kill rate, structural test analysis (SCRAP), duplication detection,
and dependency architecture checks.

This toolchain automates all of those gates as a single, unified command.

## Architecture Decisions

All decisions are documented in [ADR-0002][adr]. Key points:

- **Separate solution** (`tools/redmuffin.Tools.sln`) — keeps Roslyn-heavy
  tool builds out of the main Blazor WASM AOT build path.
- **Monolith with subcommands** — the skill requires running all gates
  together. A single tool with an `all` subcommand gives one command, one
  report, one exit code.
- **Local NuGet feed** (`tools/nupkgs/`) — installs as a standard
  `dotnet tool` without ever touching nuget.org.
- **Roslyn + Cobertura XML** — computes cyclomatic complexity directly via
  `Microsoft.CodeAnalysis` and maps line-level coverage from TUnit's
  Cobertura output to per-method coverage.

## Quick Start

```bash
# 1. Pack the tool
dotnet pack tools/redmuffin.Tools.QualityGates \
  --output tools/nupkgs

# 2. Install to local tool manifest
dotnet tool install redmuffin.Tools.QualityGates \
  --tool-manifest .config/dotnet-tools.json \
  --add-source ./tools/nupkgs

# 3. Run all gates
dotnet quality-gates all \
  --project src/redmuffin.Blazor.StaticWeb \
  --test-project tests/redmuffin.Blazor.StaticWeb.Tests \
  --coverage-file testresults/coverage.cobertura.xml

# Or a single gate
dotnet quality-gates crap --project src/redmuffin.Blazor.StaticWeb
dotnet quality-gates scrap --test-project tests/redmuffin.Blazor.StaticWeb.Tests
```

After any tool code change, re-pack and re-install:

```bash
dotnet pack tools/redmuffin.Tools.QualityGates --output tools/nupkgs
dotnet tool update redmuffin.Tools.QualityGates \
  --tool-manifest .config/dotnet-tools.json \
  --add-source ./tools/nupkgs
```

## Gates

| Gate             | Subcommand | Description                                                                                                                                                                                                                                          | Status  |
| ---------------- | ---------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------- |
| **CRAP**         | `crap`     | Cyclomatic complexity × coverage risk. Formula: `CC² × (1 − coverage)³ + CC`. Threshold: ≤ 8 per method. Exits 2 on breach. Uses Roslyn for CC, Cobertura XML for coverage. Replicates Uncle Bob's `crap4clj`/`crap4java`.                           | Done    |
| **SCRAP**        | `scrap`    | Test structural analyzer. Detects zero-assertion tests, low-assertion smells, duplicated setup scaffolding. Uses Jaccard similarity on Roslyn-normalized test bodies. Outputs STABLE/LOCAL/SPLIT + AI-actionability. Replicates Uncle Bob's `scrap`. | Done    |
| **Architecture** | `arch`     | Dependency graph and layer enforcement. Parses project references, checks for cycles, validates layered architecture rules (no upward references). Replicates Uncle Bob's `dependency-checker` + `arch-view`.                                        | Next    |
| **Mutation**     | `mutate`   | Mutation testing with differential strategy. Generates mutants, runs tests, reports kill rate (target 100%). Supports --scan, --max-workers, manifest-based differential mode. Replicates Uncle Bob's `clj-mutate`.                                  | Planned |
| **All**          | `all`      | Runs every gate in sequence. Unified pass/fail. Non-zero exit if any gate breaches. Replicates Uncle Bob's combined workflow: structure-check → spec → cov → crap → mutate → check-dependencies.                                                     | Done    |

### CRAP details

The CRAP formula measures the risk of a method based on its complexity and how
well it is tested:

```
CRAP(m) = CC(m)² × (1 − coverage(m))³ + CC(m)
```

- CC = cyclomatic complexity (from Roslyn)
- coverage = fraction of lines covered (from Cobertura XML)

Methods scoring above 8 are flagged. The tool exits with code 2 if any method
breaches the threshold.

The `--changed` flag enables incremental mode: only methods in files modified
since the last commit are analyzed.

```
dotnet quality-gates crap --project src/Foo --changed --max-crap 8
```

### SCRAP details

SCRAP analyzes test structural quality — the _second_ quality gate after CRAP.
It detects test smells that CRAP's coverage-based scoring cannot see:

- **Zero-assertion tests** — tests that pass but verify nothing
- **Low-assertion tests** — single-assertion coverage-table entries
- **Duplicated setup scaffolding** — copy-pasted Arrange blocks across examples
- **Extraction pressure** — measures how much helper extraction would reduce duplication

SCRAP uses Roslyn syntax-node normalization (preserving AST shape while
abstracting identifier names) for fuzzy Jaccard similarity (threshold 0.5)
on test bodies. All thresholds are locked to Uncle Bob's scrap
[`policy.clj`][scrap-policy] values.

Output per test file: STABLE / LOCAL / SPLIT classification with
AI-actionability (LEAVE_ALONE, AUTO_TABLE_DRIVE, AUTO_REFACTOR,
MANUAL_SPLIT, REVIEW_FIRST). Exit code 2 if any file is SPLIT or needs
action. Exit code 0 if all files are STABLE.

```
dotnet quality-gates scrap --test-project tests/Foo
dotnet quality-gates scrap --test-project tests/Foo --verbose
dotnet quality-gates scrap --test-project tests/Foo --json
dotnet quality-gates scrap --test-project tests/Foo --changed
```

## Project Structure

```
tools/
├── redmuffin.Tools.QualityGates/     Monolith project
│   ├── Commands/
│   │   ├── CrapCommand.cs            CRAP gate
│   │   ├── CrapHandler.cs            CRAP output formatter
│   │   ├── ScrapCommand.cs           SCRAP gate
│   │   ├── ScrapHandler.cs           SCRAP output formatter
│   │   ├── ScrapOptions.cs           SCRAP CLI options record
│   │   ├── AllCommand.cs             Runs all gates in sequence
│   │   ├── ArchCommand.cs            Architecture gate (next)
│   │   └── MutateCommand.cs          Mutation testing gate (future)
│   ├── Analysis/
│   │   ├── CyclomaticComplexity.cs   Roslyn-based CC computation
│   │   ├── CoverageParser.cs         Cobertura XML parser
│   │   ├── MethodMapper.cs           Maps line coverage to methods
│   │   ├── TestMethodParser.cs       TUnit test method discovery
│   │   ├── TestNormalizer.cs         Syntax-node normalization
│   │   ├── ScrapDuplication.cs       Jaccard similarity + channel detection
│   │   ├── ExtractionPressure.cs     D_before formula + file pressure
│   │   ├── ScrapScorer.cs            Per-example + per-file SCRAP scoring
│   │   └── ScrapRecommender.cs       STABLE/LOCAL/SPLIT classification
│   └── redmuffin.Tools.QualityGates.csproj
├── nupkgs/                           Local NuGet feed (gitignored)
├── redmuffin.Tools.sln               Separate solution
├── nuget.config                      Adds ./nupkgs/ as local source
└── README.md                         This file
```

## Adding a New Gate

**TOP REQUIREMENT**: All gates must replicate Uncle Bob's original tools as
closely as possible. Before implementing, read the original repo's README,
AGENTS.md, and source code. Confirm existence, scope, thresholds, and CLI
interface. Never build from memory or third-party summaries.

1. Research the original Uncle Bob tool repo (see References below)
2. Create a command class in `Commands/` implementing the subcommand pattern
3. Add the command to the CLI root in `Program.cs`
4. Hook it into `AllCommand.cs` so it runs with `all`
5. Update the gates table in this README
6. Update the project structure below
7. Re-pack and re-install

## CI Integration

In CI, the tool runs as part of the quality gate stage after tests:

```yaml
- name: Quality Gates
  run: |
    dotnet tool restore
    dotnet quality-gates all --project src/redmuffin.Blazor.StaticWeb
```

## Development

> **All commands must run from the `tools/` directory.** The repo root
> `global.json` pins SDK 9.0, but this tool targets .NET 10 (pinned in
> `tools/global.json`). Running from the repo root produces
> `NETSDK1045: does not support targeting .NET 10.0`.

### Build

```bash
cd tools
dotnet build src/redmuffin.Tools.QualityGates --verbosity quiet
```

### Test

TUnit + Microsoft.Testing.Platform in AOT mode — `dotnet test` discovers
**zero tests**. Use `dotnet run` instead:

```bash
cd tools
dotnet run --project tests/redmuffin.Tools.QualityGates.Tests
```

### Smoke test a gate

```bash
cd tools
dotnet run --project src/redmuffin.Tools.QualityGates -- scrap --test-project ../tests/redmuffin.Blazor.StaticWeb.Tests
```

### TDD and command patterns

- Follow `rm-tdd` skill: write ONE failing test, minimal code to pass, refactor.
- Test the **Handler** directly (`ScrapHandler.Run()`), not `Command.Execute()`.
- New gates mirror `CrapCommand`/`CrapHandler` exactly.
- TUnit assertions: `.IsEqualTo(n)`, `.Contains("text")`, `Count.IsEqualTo(n)`.
- No `InternalsVisibleTo` — handlers are `public static`.

### Analysis pipeline

```
TestMethodParser → ScrapDuplication.Analyze (normalizes internally) →
ExtractionPressure → ScrapScorer → ScrapRecommender → ScrapHandler
```

### Known issues

- `dotnet tool install` broken with .NET 10 packaging. Use `dotnet run` for now.
- No coverage file available on dev machines — `all` command needs one.

## References

- [ADR-0002: Quality Gates Toolchain][adr]
- [ADR-0003: SCRAP Test Structural Analyzer](../docs/adr/0003-scrap-test-structural-analyzer.md)
- [Operational Gotchas & Development Workflow](../docs/solutions/developer-experience/quality-gates-tool-operational-gotchas-2026-05-09.md)
- [rm-uncle-bob-martin-agentic-coding skill][uncle-bob-skill]
- [AIR-J AGENTS.md — Uncle Bob's toolchain workflow](https://github.com/unclebob/AIR-J/blob/master/AGENTS.md)
- [Uncle Bob's crap4java](https://github.com/unclebob/crap4java)
- [Uncle Bob's SCRAP](https://github.com/unclebob/scrap)
- [Uncle Bob's SCRAP policy](https://github.com/unclebob/scrap/blob/master/src/scrap/policy.clj)
- [Uncle Bob's arch-view](https://github.com/unclebob/arch-view)
- [Uncle Bob's dependency-checker (pinned in AIR-J)](https://github.com/unclebob/AIR-J/blob/master/dependency-checker.edn)

[adr]: ../docs/adr/0002-quality-gates-toolchain.md
[uncle-bob-skill]: ../.opencode/skills/rm-uncle-bob-martin-agentic-coding/SKILL.md
