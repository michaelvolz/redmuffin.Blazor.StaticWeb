# Quality Gates Toolchain

A local `dotnet` tool that runs the full Uncle Bob agentic coding metric
suite — CRAP, SCRAP, Architecture, and Mutation — against any .NET solution.
Every gate must pass before work is considered done.

## Why this exists

The [rm-uncle-bob-martin-agentic-coding][uncle-bob-skill] skill mandates
running a full metric suite after every significant change. Coverage alone is
insufficient — it must be paired with cyclomatic complexity (CRAP score),
mutation kill rate, structural test analysis (SCRAP), and dependency
architecture checks.

This toolchain automates all seven gates as a single command sequence:
`slopwatch analyze && dotnet run -- all`.

## Gates

### Pre-Gate: Slopwatch (LLM Anti-Cheat)

Runs before all other gates. A global dotnet tool that catches LLM reward-hacking
patterns (disabled tests, warning suppression, arbitrary delays, empty catches,
project file slop). Runs in ~0.5s on 434 files.

```bash
slopwatch analyze -d . --fail-on warning
```

| Gate         | Tool          | Description                                                                          | Exit Codes                          |
| ------------ | ------------- | ------------------------------------------------------------------------------------ | ----------------------------------- |
| **Slopwatch** | `slopwatch`   | LLM anti-cheat — 6 rules detecting reward-hacking patterns in code and project files | 0=pass, 1=issues≥fail-on, 2=hook fail |

[Full analysis and exception policy](../docs/solutions/tooling-decisions/slopwatch-integration-analysis.md)

### Main Gates (Quality Gates Toolchain)

| Gate             | Subcommand     | Description                                                                                                     | Exit Codes                |
| ---------------- | -------------- | --------------------------------------------------------------------------------------------------------------- | ------------------------- |
| **CRAP**         | `crap`         | Cyclomatic complexity × coverage risk. `CC² × (1 − cov)³ + CC`. Threshold: ≤ 8. Uses Roslyn + Cobertura XML.    | 0=pass, 1=error, 2=breach |
| **SCRAP**        | `scrap`        | Test structural analyzer. Jaccard similarity on Roslyn-normalized test bodies. Outputs STABLE/LOCAL/SPLIT.      | 0=pass, 1=error, 2=breach |
| **Architecture** | `architecture` | Dependency graph, cycles, zone metrics (A/I/D; healthy/pain/useless). YAML: allowed-dependencies (`all`), component-map, forbidden-dependencies, allowed-exceptions, ignored-components, healthy-threshold (0.3), fail-on-cycles. | 0=pass, 1=error, 2=breach |
| **Depth**        | `depth`        | Structural quality — detects shallow methods, parameter bloat, wrong abstractions, and entanglement             | 0=pass, 2=fail, 1=error   |
| **Mutation**     | `mutation`     | 9 mutation categories (22 rules + null-rvalue discovery). In-place Roslyn mutation; embedded differential manifest (skip proven forms); `--mutation-warning` default 100 (**STRONG SIGNAL → split now**, mandatory); `--update-manifest`. | 0=pass, 1=error           |
| **Duplicates**   | `duplicates`   | Production structural DRY (dry4clj/dry4java port). Roslyn normalization, pairwise Jaccard (threshold 0.82).     | 0=pass, 1=error, 2=breach |
| **All**          | `all`          | Runs all 6 main gates in sequence. All gates execute regardless of failures (run-all policy). Returns worst exit code. | worst of all gates        |

## Usage

The `all` command auto-discovers source and test projects from the nearest
`.slnx` file. No flags are required:

```bash
cd tools
dotnet run -- all
```

Specify a different solution with `--solution`:

```bash
dotnet run -- all --solution ../redmuffin.Blazor.StaticWeb.slnx
```

Project classification uses `<IsTestProject>true</IsTestProject>` in
`.csproj` files. Coverage auto-generates to `/tmp/quality-gates-coverage.xml`.

**SDK versioning**: The tools project targets .NET 10 (latest C# features,
single-file app publishing). The main solution targets .NET 9 (Azure SWA
constraint — no .NET 10 Oryx support).

## Architecture Decisions

All decisions documented in [ADR-0002][adr]:

- **Separate solution** (`tools/redmuffin.Tools.slnx`) — keeps Roslyn-heavy
  builds out of the main Blazor WASM AOT build path.
- **Monolith with subcommands** — single tool with `all` subcommand gives one
  command, one report, one exit code.
- **Roslyn + Cobertura XML** — computes cyclomatic complexity via
  `Microsoft.CodeAnalysis` and maps line-level coverage from Cobertura output.
- **Command/Handler separation** — every gate has a `Command` class for CLI
  wiring and a `Handler` class with `public static` methods for testability.
- **Run-all policy** — `AllCommand` executes every gate regardless of
  failures, returns the worst exit code.
- **TOP REQUIREMENT**: Algorithm, CLI flags, exit codes, and scope must
  replicate the original Uncle Bob tools exactly. Research-before-implementing
  is mandatory.

## Project Structure

```
tools/
├── global.json                                   .NET 10 SDK pin
├── redmuffin.Tools.slnx                          Separate solution
├── nuget.config                                  Local NuGet feed
├── src/redmuffin.Tools.QualityGates/             Tool source (Commands, Analysis, Models)
├── tests/redmuffin.Tools.QualityGates.Tests/     Unit tests + fixture data
└── quality-gates/                                Architecture rules config
```

## Development

Development conventions, operational rules, build commands, test patterns,
and known issues live in the `tools-guide` skill for agents working on this
solution.

## Known Issues

| Area | Status | Notes |
| --- | --- | --- |
| Mutation applicator ArgumentSyntax NO-OP | **Fixed in 0.1.1** | `Apply` uses `ReplaceNode` on the span+kind resolved target (no span-only rewriter). |
| Mutation applicator post/pre inc/dec NO-OP | **Fixed in 0.1.1** | Unary arithmetic kinds map via `PostfixUnaryExpression` / `PrefixUnaryExpression`. |
| Mutation result taxonomy | **Fixed in 0.1.2** | `MutantResultType.NoOp` when Apply leaves source unchanged; summary lists no-ops separately from survivors; kill rate excludes no-ops. |
| `mutation --lines` binder | **Fixed in 0.1.2** | `--lines` binds as string (`10,20`); `MutateCommand.ParseLines` builds the set (0-based Roslyn lines). |
| CoverageReader fixture `coverage-basic.xml` | **Fixed in 0.1.2** | Fixture restored under `tests/.../Fixtures/coverage-basic.xml`. |

## Cleanup residual (tools dogfood, 2026-08-03)

Level-1 classification after characterization + P0 mutation. **Do not thrash** open residual rows for score-only greens.

### Closed this session (not residual)

| Class | Items | Action |
| --- | --- | --- |
| **CRAP fixed** | `FormatText`, `FindTests`, `FindDuplicates` | Characterization raised coverage past threshold. |
| **Mutation P0 (100% kill)** | `ArchOutputFormatter`, `TestMethodParser`, `CyclomaticComplexity`, `ArchConfig`, `CoverageGapDetector` | Manifest footers written; differential skip proven forms. |
| **Mutation STRONG SIGNAL** | Was `ScrapDuplication.cs` (102 sites) | Split by real seams: `JaccardClustering` (39), `DuplicationChannelClassifier` (45), `SubjectRepetition` (4), `ScrapDuplication.Analyze` orchestrator (14). All under 100 — signal cleared. |
| **Mutation kill-rate (post-split)** | Split modules above | Boundary tests added. Kill rates: SubjectRepetition **100%**; ScrapDuplication **92.9%** (1 equivalent: `Count == 0`→`1` early-exit); DuplicationChannelClassifier **84.4%** (loop-start `1`→`0` and dead complexity-score band under `BranchCount <= 0`); JaccardClustering **79.5%** (both-empty guard `&&`/`==0` equivalents; `InitUnionFind` start `0`→`1` with default zero array; `j = i+1`→`i` self-union no-op). No production thrash. |

### Open residual (leave)

| Class | Items | Action |
| --- | --- | --- |
| **CRAP formula-bound (CC ≥ 8)** | `ArchConfig.Parse`, `ComputeAbstractnessByComponent`, `AnalyzeMethod`, `NormalizeNode`, `ApplyMutation` | Accept residual; more coverage cannot green CC ≥ 8. |
| **CRAP harness / I/O** | `AllCommand.ExecuteAsync` / `ResolveProjectPath`, `MutateHandler.RunMutationCoreAsync`, `MutationRunner.RunAsync` / `RunTestsAsync` | Cobertura theater on CLI/process harness — leave. |
| **SCRAP** | Local AutoRefactor only (0 SPLIT) | **Leave Local.** Inline arrange/data *is* the test; factories would be metric gaming. |
| **Depth** | Shallow pattern helpers; wrong-abstraction/entangled algorithm methods | KEEP per Depth decision tree (visitor/pattern, algorithm branching). No shallow thrash reverse. |
| **Mutation P0 (hardened, residual OK)** | `ScrapScorer` (~93%), `DupesDetector` (~76%) | Boundary tests raised kill rates. Remaining: `* 1.0` / `>2` vs `>=2` **equivalent** on ScrapScorer; Dupes sort/line/Jaccard edges. No production thrash. |

Consumer evidence (Morpheus handoff, optional):
`redmuffin.Morpheus` `docs/solutions/developer-experience/quality-gates-mutation-tool-handoff-2026-08-02.md`.

## References

- [ADR-0002: Quality Gates Toolchain][adr]
- [ADR-0003: SCRAP Test Structural Analyzer](../docs/adr/0003-scrap-test-structural-analyzer.md)
- [AIR-J AGENTS.md — Uncle Bob's toolchain workflow](https://github.com/unclebob/AIR-J/blob/master/AGENTS.md)
- [Uncle Bob's crap4java](https://github.com/unclebob/crap4java)
- [Uncle Bob's SCRAP](https://github.com/unclebob/scrap)
- [Uncle Bob's dependency-checker](https://github.com/unclebob/AIR-J/blob/master/dependency-checker.edn)
- [Uncle Bob's clj-mutate](https://github.com/unclebob/clj-mutate)
- [Uncle Bob's dry4clj](https://github.com/unclebob/dry4clj)

[adr]: ../docs/adr/0002-quality-gates-toolchain.md
[uncle-bob-skill]: ../.opencode/skills/rm-uncle-bob-martin-agentic-coding/SKILL.md
