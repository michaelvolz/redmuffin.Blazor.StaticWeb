# Quality Gates Toolchain: Separate Solution, Monolith, Local NuGet Feed

The `rm-uncle-bob-martin-agentic-coding` skill mandates running a full metric
suite (CRAP, mutation, SCRAP, duplication, architecture) after every
significant change, with all gates passing before work is considered done.
This requires a developer toolchain that runs gates as a unified pipeline,
installable as a `dotnet tool` without publishing to NuGet.

## Decision

The quality gates live in a **separate solution** (`tools/redmuffin.Tools.sln`)
at the repo root, built as a **single monolith** project
(`redmuffin.Tools.QualityGates`) with subcommands (`crap`, `scrap`, `dupes`,
`arch`, `all`), installed via a **local NuGet feed** (`tools/nupkgs/`) that
never leaves the repo.

The CRAP analyzer uses Roslyn (`Microsoft.CodeAnalysis`) for cyclomatic
complexity and parses Cobertura XML from TUnit's native coverage for
per-method line coverage. The `all` subcommand runs every gate in sequence
and exits non-zero if any gate fails.

## Considered Options

**Same solution vs separate**: Adding tool projects to `redmuffin.Blazor.StaticWeb.sln`
would mean `dotnet build` compiles Roslyn-heavy tools on every build. Blazor
WASM AOT compilation is already slow; adding tool builds to the critical path
was unacceptable. A separate solution keeps tool builds out of the app build.

**Separate projects vs monolith**: Building each gate as a separate tool
(one `dotnet-tools.json` entry per gate) would let gates version independently,
but the skill explicitly requires running them all together. A monolith with
an `all` subcommand gives one command, one report, one exit code — eliminating
the orchestration script that would otherwise chain five tools and merge
results. Independent versioning is unnecessary since gates move together.

**Local NuGet feed vs `dotnet run`**: Running via `dotnet run --project`
avoids the pack/restore step but is verbose and requires knowing the project
path. A local NuGet feed gives the standard `dotnet tool` UX
(`dotnet quality-gates all`) at the cost of a `dotnet pack` step during
tool updates. The cleaner UX was preferred.

**Roslynator output vs Roslyn directly**: Roslynator is already installed for
linting, but its CLI output is not a stable machine-readable contract. Using
Roslyn directly gives full control over CC computation and method span
extraction, which is needed to map line-level Cobertura coverage to methods.

## Consequences

- Adding a new gate means adding a subcommand class and hooking it into `all`
- The `tools/nupkgs/` directory must be regenerated (`dotnet pack`) after
  any tool code change and before `dotnet tool restore` will pick it up
- All gates share the same Roslyn workspace — CC is computed once and reused
  across CRAP and SCRAP, saving parse time
- The tool is intentionally not in `src/` — it is not part of the application
