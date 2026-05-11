# Quality Gates Toolchain

A local `dotnet tool` that runs the full Uncle Bob agentic coding metric suite
against this repo. Every gate must pass before work is considered done.

## Why this exists

The [rm-uncle-bob-martin-agentic-coding][uncle-bob-skill] skill mandates
running a full metric suite after every significant change. Coverage alone is
insufficient — it must be paired with cyclomatic complexity (CRAP score),
mutation kill rate, structural test analysis (SCRAP), and dependency
architecture checks.

This toolchain automates all four gates as a single, unified command.

## Gates

| Gate             | Subcommand | Description                                                                                                              | Exit Codes                    |
| ---------------- | ---------- | ------------------------------------------------------------------------------------------------------------------------ | ----------------------------- |
| **CRAP**         | `crap`     | Cyclomatic complexity × coverage risk. `CC² × (1 − cov)³ + CC`. Threshold: ≤ 8. Uses Roslyn + Cobertura XML.             | 0=pass, 1=error, 2=breach     |
| **SCRAP**        | `scrap`    | Test structural analyzer. Jaccard similarity on Roslyn-normalized test bodies. Outputs STABLE/LOCAL/SPLIT.               | 0=pass, 1=error, 2=violations |
| **Architecture** | `arch`     | Dependency graph + cycle detection. YAML config with allowed-dependencies, component-map, fail-on-cycles.                | 0=pass, 1=error, 2=violations |
| **Mutation**     | `mutate`   | Mutation testing with 6 categories (19 rules). In-place source mutation via Roslyn. Differential mode via JSON manifest. | 0=pass, 1=error               |
| **All**          | `all`      | Runs all gates in sequence. All gates execute regardless of failures (run-all policy). Returns worst exit code.          | worst of all gates            |

### CRAP (Complexity Risk Analysis)

Replicates Uncle Bob's `crap4clj` / `crap4java`.

- `CyclomaticComplexity.Analyze(projectPath)` walks all `.cs` files via
  `CSharpSyntaxWalker`, counts decision points per method
- `CoverageParser.Parse(coveragePath)` reads Cobertura XML
- `MethodMapper.Map(methods, coverage)` joins line coverage to methods
- `CrapHandler.Run(results, maxCrap)` formats output, returns exit code

Flags: `--max-crap N` (default 8), `--changed` (only files modified since HEAD).

### SCRAP (Structural Analyzer)

Replicates Uncle Bob's `scrap`. Analyzes test files only.

- `TestMethodParser.FindTests(dir)` discovers TUnit test methods via Roslyn
- `ScrapDuplication.Analyze(methods)` normalizes test bodies via
  `CSharpSyntaxRewriter`, computes Jaccard similarity (threshold 0.5, n-gram
  size 3, skip ≤3 token norm)
- `ExtractionPressure.ComputeFilePressure(dupReport)` computes extraction
  pressure
- `ScrapRecommender.Decide(report)` classifies each file: STABLE, LOCAL, or
  SPLIT with AI-actionability

All thresholds locked to Uncle Bob's
[`policy.clj`](https://github.com/unclebob/scrap/blob/master/src/scrap/policy.clj).

Flags: `--verbose`, `--json`, `--write-baseline`, `--compare-path`,
`--stability-threshold` (default 12.0).

### Architecture (Dependency Checker)

Replicates Uncle Bob's `dependency-checker` + `arch-view`.

Requires a YAML config file with:

- `component-map`: project-name → component-name
- `allowed-dependencies`: component → [allowed targets]. Same-component
  references are always allowed.
- `ignored-components`: components to skip
- `fail-on-cycles` / `fail-on-violations` (defaults: true)

Pipeline: `ArchConfig` (YAML via YamlDotNet) → `ProjectGraph.From` (`.csproj`
reference extraction) → `ComponentGraph.Resolve` (project-to-component mapping)
→ `ArchHandler.Run` (violation + cycle detection) → `ArchOutputFormatter`
(text or JSON).

Flags: `--arch-config <path>`, `--json`.

Cross-platform note: `.csproj` files use Windows `\` path separators.
`ProjectGraph.From` normalizes them via `.Replace('\\', Path.DirectorySeparatorChar)`.

### Mutation

Replicates Uncle Bob's `clj-mutate`. Mutates source files in-place, runs the
test project, and classifies each mutant as killed or survived.

**6 mutation categories (19 rules):**

- Arithmetic: `+` ↔ `-`, `*` → `/`, `++` ↔ `--` (pre/post)
- Comparison: `>` ↔ `>=`, `<` ↔ `<=`
- Equality: `==` ↔ `!=`
- Boolean: `true` ↔ `false`
- Conditional: negate `if` condition (`!(x)`)
- Constant: `0` ↔ `1`

**Pipeline:** `MutationDiscoverer.FindSites(source)` (CSharpSyntaxWalker) →
`CoverageReader.PartitionByCoverage(sites, coveredLines)` (Cobertura XML
line-level) → `MutationApplicator.Apply(source, index, site)`
(CSharpSyntaxRewriter, node-identified by Span) → `MutationRunner.RunAsync`
(executes `dotnet run --project`, classifies killed/survived, crash-safe backup).

**Differential mode:** `MutationManifest` embeds a JSON footer comment block
(not Clojure EDN — format fork for C# interop) with per-method SHA256 hashes.
Subsequent runs only mutate forms whose hash changed.

**Worker isolation:** `--max-workers N > 1` creates N temp directories cloning
the test project, dispatching mutations in parallel (matching clj-mutate's
worker design).

Flags: `--scan`, `--max-workers` (default 1), `--since-last-run`,
`--mutate-all`, `--lines`, `--mutation-warning` (default 50),
`--timeout-factor` (default 10), `--reuse-coverage`.

Test execution uses `dotnet run --project <test-project>` (NOT `dotnet test` —
TUnit + Microsoft.Testing.Platform in AOT mode discovers zero tests with
`dotnet test`).

## Architecture Decisions

All decisions documented in [ADR-0002][adr]. Key points:

- **Separate solution** (`tools/redmuffin.Tools.sln`) — keeps Roslyn-heavy
  builds out of the main Blazor WASM AOT build path.
- **Monolith with subcommands** — single tool with `all` subcommand gives one
  command, one report, one exit code.
- **Roslyn + Cobertura XML** — computes cyclomatic complexity directly via
  `Microsoft.CodeAnalysis` and maps line-level coverage from Cobertura output.
- **Command/Handler separation** — every gate has a `Command` class for CLI
  wiring (thin wrapper) and a `Handler` class with `public static` methods for
  testability (no `InternalsVisibleTo`).
- **Run-all policy** — `AllCommand` executes every gate regardless of
  intermediate failures, returns the worst exit code. Architecture gate skips
  when `--arch-config` not provided.
- **TOP REQUIREMENT** (§1.1 of Uncle Bob skill): Algorithm, CLI flags, exit
  codes, and scope must replicate original tools exactly. Research-before-
  implementing is mandatory.

## Development

### Critical operational rules

1. **Always run from `tools/` directory.** The repo root `global.json` pins
   SDK 9.0; the tool targets .NET 10 (pinned in `tools/global.json`). Wrong
   directory → `NETSDK1045`.
2. **Use `dotnet run --project`, not `dotnet test`.** TUnit +
   Microsoft.Testing.Platform in AOT mode discovers zero tests via
   `dotnet test`. The only working test command is:
   `dotnet run --project tests/redmuffin.Tools.QualityGates.Tests`.
3. **Never suppress analyzer warnings with pragmas.** Warnings are signals
   that code is not following best practices. Fix the root cause properly. If
   a fix is truly impossible, stop and ask before suppressing.
4. **Use `dotnet format` for style fixes.** `dotnet format src/<project> --severity info`
   auto-fixes ~75% of StyleCop and Roslyn-analyzer violations. Only manually
   fix what remains.
5. **No `InternalsVisibleTo`.** Handlers are `public static` for testability.
   Test the Handler directly, not the Command.

### Build and test

```bash
cd tools

# Build
dotnet build src/redmuffin.Tools.QualityGates --verbosity quiet

# Test (187 tests as of 2026-05-09)
dotnet run --project tests/redmuffin.Tools.QualityGates.Tests

# Smoke test a gate
dotnet run --project src/redmuffin.Tools.QualityGates -- <gate> [options]

# Full verify cycle
dotnet clean src/redmuffin.Tools.QualityGates
dotnet build src/redmuffin.Tools.QualityGates --verbosity quiet
dotnet run --project tests/redmuffin.Tools.QualityGates.Tests
```

### Generating coverage (required for CRAP, Mutation, and all gates)

CRAP, Mutation, and the `all` command require a Cobertura XML coverage file.
TUnit provides native coverage via `Microsoft.Testing.Extensions.CodeCoverage`
— no extra NuGet packages needed.

**Generate from the repo root** (not the `tools/` directory — this runs the
main project's tests, not the tools tests):

```bash
# Generate coverage (Debug config — Release has lock file drift issues)
dotnet run --project tests/redmuffin.Blazor.StaticWeb.Tests \
  --coverage \
  --coverage-output-format cobertura \
  --coverage-output coverage/blazor-cobertura.xml
```

The actual file lands at:
`tests/redmuffin.Blazor.StaticWeb.Tests/bin/Debug/net9.0/TestResults/coverage/blazor-cobertura.xml`

A convenience script exists at `scripts/Generate-CoverageReport.ps1`.

**Then run CRAP from the `tools/` directory:**

```bash
cd tools
dotnet run --project src/redmuffin.Tools.QualityGates -- crap \
  --project ../src/redmuffin.Blazor.StaticWeb \
  --coverage-file ../tests/redmuffin.Blazor.StaticWeb.Tests/bin/Debug/net9.0/TestResults/coverage/blazor-cobertura.xml
```

### TDD pattern (enforced by rm-tdd)

- Write ONE failing test → minimal production code → refactor → next test.
- Test the Handler directly, never the Command:
  ```csharp
  var exitCode = SomeHandler.Run(results, options, output);
  await Assert.That(exitCode).IsEqualTo(0);
  ```
- Plan document defines test scenarios per unit. Supplement missing categories
  (happy path, edge cases, error paths) before writing tests.

### TUnit assertion API

Use `.IsEqualTo(n)`, `.Contains(str)`, `.IsLessThan(n)`, `.Count.IsEqualTo(n)`.
Never `.HasCount()` or `.SequenceEqual(features).IsTrue()` — these fail silently.

### Command pattern

New gates follow `CrapCommand` / `CrapHandler` exactly:

- **Command**: `public static Command Create()` returns `Command` with options.
  `internal static int Execute(...)` wires the pipeline. Uses `SetAction`
  (not `SetHandler`).
- **Handler**: `public static` class. `Run()` with `TextWriter? output` param.
  Returns exit code (0=pass, 1=error, 2=threshold breach).
- **Options**: `public sealed record` in `Commands/`. Mutable options via
  `with` expressions.

### Analysis pipeline pattern

Every gate follows: **Parser → Normalizer → Analyzer → Scorer → Recommender → Handler**.

### Roslyn patterns

- **CSharpSyntaxWalker subclass** — for tree traversal (CRAP, mutation
  discovery). Override specific `Visit*` methods.
- **CSharpSyntaxRewriter subclass** — for tree mutation (SCRAP normalization,
  mutation applicator). Override `Visit(SyntaxNode?)`.
- **Per-file parsing** via `CSharpSyntaxTree.ParseText(source)` — not
  `MSBuildWorkspace`.
- **Span-based node identity** — store `SyntaxNode` references in data models
  and identify nodes by `Span` in rewritten trees. `CSharpSyntaxWalker` and
  `CSharpSyntaxRewriter` have incompatible base classes — use Span identity,
  not index counting.

### Adding a new gate

1. Research the original Uncle Bob tool repo (see References). Confirm
   existence, scope, thresholds, and CLI interface. Never build from memory.
2. Create: `Analysis/` classes, `Commands/XxxCommand.cs`,
   `Commands/XxxHandler.cs`, `Commands/XxxOptions.cs`, test files.
3. Wire into `Program.cs`: `rootCommand.Subcommands.Add(XxxCommand.Create());`
4. Hook into `AllCommand.cs` with appropriate flags.
5. Add test fixture projects under `tests/.../Fixtures/` if the gate needs
   real project execution. Add `Content Include` to the test `.csproj` for
   output copying and `Compile Remove` to avoid duplicating source.
6. Update the gates table and project structure in this README.

### Known issues

- `dotnet tool install` broken with .NET 10 packaging (`DotnetToolSettings.xml`
  path). Smoke test with `dotnet run` instead.
- Generate coverage before running CRAP/all gates (see Generating Coverage below).
- The Release build can fail with lock file drift (`NU1004`). Use Debug config
  for coverage generation — it works reliably.
- Mutation runner tests (`MutationRunnerTests`) use `[NotInParallel]` because
  they modify shared fixture files. Keep this attribute on any test that
  mutates disk state.
- **CRAP false positives — production→production call chains:** The
  `Microsoft.Testing.Extensions.CodeCoverage` instrumenter cannot attribute
  coverage for calls from one production method to another. Only direct
  test→production calls are recorded. The following methods are called and
  tested by integration tests (`CommandIntegrationTests`), verified by passing
  test runs, but always report 0% coverage with CRAP 12.0 (CC=3). They have
  no extractable seams — each is a thin pipeline wrapper around already-tested
  components:
  - `CrapCommand.RunAnalysis` — calls CyclomaticComplexity.Analyze →
    CoverageParser.Parse → MethodMapper.Map → CrapHandler.Run
  - `GitFileFilter.GetChangedFiles` — spawns `git diff`, infrastructure method
  - `MutateHandler.RunMutationCoreAsync` — pipeline orchestration
  - `MutateHandler.DiscoverSitesAsync` — delegates to MutationDiscoverer +
    CoverageReader
  - `ArchHandler.Run` — try/catch wrapper around RunConfigPipeline. Both
    branches (missing config, DirectoryNotFoundException) tested by
    direct tests that pass, but instrumenter reports 0% for the catch
    blocks.
  - `CrapCommand.Execute`, `CrapCommand.ValidatePaths`,
    `ScrapCommand.Execute`, `DupesHandler.Run` — pipeline orchestrators
    in the Commands layer. Each delegates to tested Handlers. Same
    production→production attribution gap.
    These are accepted as justified exceptions per rm-guide-cleanup §3. If
    `coverlet` or another coverage tool that supports cross-assembly attribution
    becomes compatible with `dotnet run` (TUnit AOT), these methods should be
    re-evaluated.
- **CRAP structural exceptions — Roslyn switch dispatchers:** `DupesNormalizer
.NormalizeStatement` (CC=13, 93%) and `DupesNormalizer.NormalizeNode` (CC=11,
  75%) are Roslyn pattern-match switch dispatchers. Each arm delegates to an
  already-tested sub-dispatcher. The CRAP formula (`CC²(1−cov)³+CC`) requires
  CC ≤ 8 to pass at any coverage level. These methods have been split into
  category-level sub-dispatchers (NormalizeControlFlowStatement,
  NormalizeBlockStatement, NormalizeLeafStatement, NormalizeLoopStatement,
  NormalizeExpression, NormalizeStatement, NormalizeMemberList, etc.) — the
  smallest decomposition that preserves readability. Further atomization would
  create 12 single-line pass-through methods. Accepted as justified structural
  exceptions.
- **Dupes structural candiates — not semantic duplicates:** `NormalizeMemberList`
  / `NormalizeSwitch` (1.00) share "tagged list + iterate + normalize" pattern
  but operate on different node types. `ComputeSharedForms` /
  `ComputeVariablePoints` (0.82) share HashSet iteration but compute
  intersection vs union−intersection. Merging either would couple unrelated
  concerns. dry4clj identifies candidates; the human decides. Accepted.

## Project Structure

```
tools/
├── global.json                        .NET 10 SDK pin
├── redmuffin.Tools.sln               Separate solution
├── nuget.config                       Local NuGet feed
├── README.md                          This file
├── src/redmuffin.Tools.QualityGates/
│   ├── Program.cs                     CLI root
│   ├── Commands/
│   │   ├── AllCommand.cs              Runs all gates
│   │   ├── CrapCommand.cs             CRAP gate CLI
│   │   ├── CrapHandler.cs             CRAP output formatting
│   │   ├── ScrapCommand.cs            SCRAP gate CLI
│   │   ├── ScrapHandler.cs            SCRAP output formatting
│   │   ├── ScrapOptions.cs            SCRAP options record
│   │   ├── ArchCommand.cs             Architecture gate CLI
│   │   ├── ArchHandler.cs             Architecture orchestration
│   │   ├── ArchOutputFormatter.cs     Text/JSON output
│   │   ├── MutateCommand.cs           Mutation gate CLI
│   │   ├── MutateHandler.cs           Mutation orchestration
│   │   └── MutateOptions.cs           Mutation options record
│   ├── Analysis/
│   │   ├── CyclomaticComplexity.cs    CRAP: Roslyn CC walker
│   │   ├── CoverageParser.cs          CRAP/Mutation: Cobertura XML
│   │   ├── CoverageReader.cs          Mutation: line-level coverage
│   │   ├── MethodMapper.cs            CRAP: maps coverage to methods
│   │   ├── MethodComplexity.cs        CRAP: method analysis record
│   │   ├── MethodCrap.cs              CRAP: final score record
│   │   ├── TestMethodParser.cs        SCRAP: TUnit test discovery
│   │   ├── TestNormalizer.cs          SCRAP: syntax normalization
│   │   ├── ScrapDuplication.cs        SCRAP: Jaccard similarity
│   │   ├── ExtractionPressure.cs      SCRAP: D_before formula
│   │   ├── ScrapScorer.cs             SCRAP: per-file scoring
│   │   ├── ScrapRecommender.cs        SCRAP: classification
│   │   ├── MutationRules.cs           Mutation: 19 rule definitions
│   │   ├── MutationDiscoverer.cs      Mutation: site discovery via walker
│   │   ├── MutationApplicator.cs      Mutation: apply via rewriter
│   │   ├── MutationRunner.cs          Mutation: process execution + backup
│   │   ├── MutationManifest.cs        Mutation: JSON footer + differential
│   │   ├── MutationCategory.cs        Mutation: category enum
│   │   ├── MutationRule.cs            Mutation: rule record
│   │   └── MutationSite.cs            Mutation: site record (carries SyntaxNode)
│   └── Models/
│       └── ArchConfig.cs              Architecture: YAML config parsing
├── tests/redmuffin.Tools.QualityGates.Tests/
│   ├── Commands/                      Handler + composition tests
│   ├── Analysis/                      Per-gate unit tests (187 total)
│   └── Fixtures/
│       ├── coverage-basic.xml         CoverageReader fixture
│       └── MutationTarget/            MutationRunner fixture project
└── nupkgs/                            Local NuGet feed (gitignored)
```

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
- [Uncle Bob's dependency-checker](https://github.com/unclebob/AIR-J/blob/master/dependency-checker.edn)
- [Uncle Bob's clj-mutate](https://github.com/unclebob/clj-mutate)

[adr]: ../docs/adr/0002-quality-gates-toolchain.md
[uncle-bob-skill]: ../.opencode/skills/rm-uncle-bob-martin-agentic-coding/SKILL.md
