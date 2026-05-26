---
module: quality-gates
date: 2026-05-13
problem_type: workflow_issue
component: tooling
severity: high
tags:
  - coverage
  - cobertura
  - crap
  - quality-gates
  - auto-coverage
  - multi-project
applies_when:
  - "Solution contains multiple test projects (e.g., Blazor WASM + Azure Functions API)"
  - "--auto-coverage flag is used with CRAP analysis"
  - "Some projects show 0% coverage despite having passing behavior tests"
symptoms:
  - "API project methods consistently show 0% coverage in CRAP analysis"
  - "Auto-coverage generates coverage from only one test project"
  - "CRAP reports FAIL for methods that are well-tested in other test projects"
root_cause: incomplete_setup
resolution_type: tooling_addition
---

# Multi-Test-Project Auto-Coverage With Merged Cobertura

## Context

The quality gates auto-coverage pipeline (`--auto-coverage`) originally supported
only a single test project. When the solution grew to two test projects (Blazor
WASM and Azure Functions API), the API test project's coverage was invisible.
The CRAP gate showed `RaindropListArticles.RunAsync` and `RaindropListVideos.RunAsync`
at CRAP 30.0 with 0% coverage — FAIL — despite both functions having 5
well-written behavior tests in the API test project.

The `dotnet coverage merge` CLI command does not exist. Merging Cobertura XML
had to be done manually.

## Guidance

Generate per-project coverage, merge all Cobertura XML files, then feed the
merged result to CRAP analysis. Three changes are required:

### 1. CoberturaMerger — XML merge utility

The merger collects all `<class filename="...">` elements from all input files,
sums `hits` for identical (filename, lineNumber) keys, and writes the merged
result. Single-file inputs are copied directly.

```csharp
public static void Merge(IReadOnlyList<string> inputPaths, string outputPath)
{
    if (inputPaths.Count == 0)
        throw new ArgumentException("...");
    if (inputPaths.Count == 1)
        File.Copy(inputPaths[0], outputPath, overwrite: true);
    else
        WriteMergedDocument(LoadAllClassLines(inputPaths), outputPath);
}
```

Key: the CRAP `CoverageParser` only reads `<class filename>` and `<line number
hits>` — aggregate attributes on `<package>` and `<coverage>` root are unused.
The merger only needs to get those two attributes correct per line.

### 2. Multi-project coverage generation

`CrapCommand.GenerateCoverageForAllProjects` iterates over all test projects,
runs `dotnet run --project <path> --coverage` per project, then merges:

```csharp
private static string? GenerateCoverageForAllProjects(
    IReadOnlyList<string> testProjectPaths)
{
    var tempFiles = new List<string>();
    foreach (var path in testProjectPaths)
    {
        var coveragePath = GenerateCoverage(path);
        if (coveragePath is null) { CleanupTempFiles(tempFiles); return null; }
        tempFiles.Add(coveragePath);
    }
    if (tempFiles.Count == 1) return tempFiles[0];
    var merged = Path.Combine(Path.GetTempPath(),
        Path.GetRandomFileName() + ".merged.cobertura.xml");
    CoberturaMerger.Merge(tempFiles, merged);
    CleanupTempFiles(tempFiles);
    return merged;
}
```

### 3. Discover all test projects from .slnx

`AllCommand.ResolveTestProjectPaths` returns all `<IsTestProject>true</IsTestProject>`
projects from the solution, not just the first one:

```csharp
var discovered = SlnxProjectDiscovery.DiscoverFromSlnx(solutionPath);
var testProjectDirs = discovered.TestProjects
    .Select(ProjectDir)
    .ToList();
```

The default coverage path changed from hardcoded `/tmp/coverage-data.xml` to
`null` when auto-coverage is enabled, so the pipeline correctly enters the
generate-then-merge path.

**Backward compatibility**: single-project solutions unchanged. The
`CrapCommand.Execute` overload accepts `IReadOnlyList<string>?` but null or
single-element lists produce the same behavior as before.

## Why This Matters

Without multi-project merge, any solution with 2+ test projects produces
**false CRAP failures**. Code that is well-tested in separate test projects
appears to have 0% coverage, inflating CRAP scores and blocking quality gates.

After the fix, `RaindropListFetcher.FetchAsync` dropped from CRAP 30.0 (FAIL,
0% coverage) to 6.3 (PASS, 63% coverage). The old `RunAsync` wrappers went
from 30.0 (FAIL) to 2.1 (PASS). The CRAP gate now accurately reflects
real test coverage across all projects.

## When to Apply

- Solution contains 2+ test projects targeting different assemblies
- Coverage reports are generated per test project via `dotnet run --coverage`
- Quality gates consume merged coverage data
- CI/CD or local gate runs must pass before commit/push

## Examples

**Before**: `dotnet run -- all --solution main.slnx` — CRAP shows API functions
at 30.0 / 0% / FAIL. Only Blazor WASM tests contributed coverage.

**After**: Same command — CRAP shows API functions at 6.3 / 63% / PASS. Both
test projects generate coverage, merged via CoberturaMerger.

**Regression safety**: 4 CoberturaMerger characterization tests (single file,
disjoint classes, overlapping lines, empty input). 287 tools tests pass.

## Related

- `tools/README.md` — Quality gates development and operational docs
- `tools/src/.../CoberturaMerger.cs` — XML merge implementation
- `tools/src/.../CrapCommand.cs` — `GenerateCoverageForAllProjects`
- `tools/src/.../AllCommand.cs` — `ResolveTestProjectPaths`
- `docs/solutions/tooling-decisions/crap-quality-gates-pipeline-2026-05-09.md` — CRAP pipeline design
- [CRAP Formula Measurement Gaps](/docs/solutions/developer-experience/crap-formula-cobertura-coverage-divergence-2026-05-16.md) — the other major class of CRAP accuracy issues (metric semantics vs. incomplete data)
