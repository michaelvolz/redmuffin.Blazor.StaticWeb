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

This toolchain automates all five gates as a single, unified command:
`dotnet run -- all`.

## Gates

| Gate             | Subcommand     | Description                                                                                                     | Exit Codes                |
| ---------------- | -------------- | --------------------------------------------------------------------------------------------------------------- | ------------------------- |
| **CRAP**         | `crap`         | Cyclomatic complexity × coverage risk. `CC² × (1 − cov)³ + CC`. Threshold: ≤ 8. Uses Roslyn + Cobertura XML.    | 0=pass, 1=error, 2=breach |
| **SCRAP**        | `scrap`        | Test structural analyzer. Jaccard similarity on Roslyn-normalized test bodies. Outputs STABLE/LOCAL/SPLIT.      | 0=pass, 1=error, 2=breach |
| **Architecture** | `architecture` | Dependency graph + cycle detection. YAML config with allowed-dependencies, component-map, fail-on-cycles.       | 0=pass, 1=error, 2=breach |
| **Mutation**     | `mutation`     | 6 mutation categories (19 rules). In-place source mutation via Roslyn. Differential mode via JSON manifest.     | 0=pass, 1=error           |
| **Duplicates**   | `duplicates`   | Structural duplicate detection. Roslyn tree normalization, pairwise Jaccard similarity (threshold 0.82).        | 0=pass, 1=error, 2=breach |
| **All**          | `all`          | Runs all gates in sequence. All gates execute regardless of failures (run-all policy). Returns worst exit code. | worst of all gates        |

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
