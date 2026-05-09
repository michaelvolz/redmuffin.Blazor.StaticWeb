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
dotnet quality-gates all --project src/redmuffin.Blazor.StaticWeb

# Or a single gate
dotnet quality-gates crap --project src/redmuffin.Blazor.StaticWeb
```

After any tool code change, re-pack and re-install:

```bash
dotnet pack tools/redmuffin.Tools.QualityGates --output tools/nupkgs
dotnet tool update redmuffin.Tools.QualityGates \
  --tool-manifest .config/dotnet-tools.json \
  --add-source ./tools/nupkgs
```

## Gates

| Gate             | Subcommand | Description                                                                                                                                                                                 | Status  |
| ---------------- | ---------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------- |
| **CRAP**         | `crap`     | Cyclomatic complexity × coverage risk. Formula: `CC² × (1 − coverage)³ + CC`. Threshold: ≤ 8 per method. Exits 2 on breach. Uses Roslyn for CC, Cobertura XML for coverage.                 | Done    |
| **SCRAP**        | `scrap`    | Test structural analyzer. Detects setup duplication, long test chains, zero-assertion tests. Uses Jaccard similarity on normalized test bodies. Outputs STABLE/LOCAL/SPLIT recommendations. | Planned |
| **Duplication**  | `dupes`    | Fuzzy structural duplicate scanner. Normalizes source, computes Jaccard similarity on n-gram token sequences. Flags clusters ≥ 0.5 similarity.                                              | Planned |
| **Architecture** | `arch`     | Dependency graph and layer enforcement. Parses project references, checks for cycles, validates layered architecture rules (no upward references).                                          | Planned |
| **All**          | `all`      | Runs every gate in sequence. Unified pass/fail. Non-zero exit if any gate breaches.                                                                                                         | Planned |

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

## Project Structure

```
tools/
├── redmuffin.Tools.QualityGates/     Monolith project
│   ├── Commands/
│   │   ├── CrapCommand.cs            CRAP gate
│   │   ├── ScrapCommand.cs           SCRAP gate (future)
│   │   ├── DupeCommand.cs            Duplication gate (future)
│   │   ├── ArchCommand.cs            Architecture gate (future)
│   │   └── AllCommand.cs             Runs all gates in sequence
│   ├── Analysis/
│   │   ├── CyclomaticComplexity.cs   Roslyn-based CC computation
│   │   ├── CoverageParser.cs         Cobertura XML parser
│   │   └── MethodMapper.cs           Maps line coverage to methods
│   └── redmuffin.Tools.QualityGates.csproj
├── nupkgs/                           Local NuGet feed (gitignored)
├── redmuffin.Tools.sln               Separate solution
├── nuget.config                      Adds ./nupkgs/ as local source
└── README.md                         This file
```

## Adding a New Gate

1. Create a command class in `Commands/` implementing the subcommand pattern
2. Add the command to the CLI root in `Program.cs`
3. Hook it into `AllCommand.cs` so it runs with `all`
4. Update the gates table in this README
5. Re-pack and re-install

## CI Integration

In CI, the tool runs as part of the quality gate stage after tests:

```yaml
- name: Quality Gates
  run: |
    dotnet tool restore
    dotnet quality-gates all --project src/redmuffin.Blazor.StaticWeb
```

## References

- [ADR-0002: Quality Gates Toolchain][adr]
- [rm-uncle-bob-martin-agentic-coding skill][uncle-bob-skill]
- [Uncle Bob's crap4java](https://github.com/unclebob/crap4java)
- [Uncle Bob's SCRAP](https://github.com/unclebob/scrap)
- [Uncle Bob's arch-view](https://github.com/unclebob/arch-view)

[adr]: ../docs/adr/0002-quality-gates-toolchain.md
[uncle-bob-skill]: ../.opencode/skills/rm-uncle-bob-martin-agentic-coding/SKILL.md
