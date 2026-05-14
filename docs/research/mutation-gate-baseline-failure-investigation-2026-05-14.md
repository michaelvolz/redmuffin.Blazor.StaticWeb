---
date: 2026-05-14
title: Mutation Gate Coverage-Absent Fallback — Root Cause & Fix
tags: [mutation, quality-gates, coverage, bug-fix]
description:
  Root cause of mutation gate "FAIL — tests do not pass without mutations".
  The DiscoverSitesAsync method returned an empty sites list when no coverage
  file existed, causing 0 mutations to run. Fixed by falling back to all sites
  when coverage data is absent.
module: tools
problem_type: logic_error
---

# Mutation Gate Coverage-Absent Fallback — Root Cause

## Problem

```
FAIL — tests do not pass without mutations
```

Every mutation gate run produced this error. The baseline test run succeeded
(exit 0, 17-22 seconds), but 0 mutations were executed.

## Root Cause: Empty `sites` from Missing Coverage

`DiscoverSitesAsync` in `MutateHandler.cs` partitions mutation sites by
coverage: covered sites go into the `sites` list, uncovered sites are
reported but not mutated. When no Cobertura XML coverage file exists
(`LoadCoverage` returns empty `HashSet<int>`):

1. `CoverageReader.PartitionByCoverage(allSites, [])` → `covered = []`,
   `uncovered = allSites`
2. `sites = new List<MutationSite>(covered)` → `sites = []`
3. `RunAsync(sourcePath, [], ...)` → foreach loop iterates 0 times
4. `results.Count == 0` → "FAIL" message

The baseline passed. No exception was thrown. The code was working correctly
for its design — but the design required coverage data that wasn't present.

## Fix

In `MutateHandler.DiscoverSitesAsync`: when `covered` is empty and `uncovered`
contains all sites, use all sites for mutation:

```csharp
if (covered.Count == 0 && uncovered.Count == allSites.Count && !options.ReuseCoverage)
{
    await output.WriteLineAsync(
        "Note: No coverage data found for mutation. Mutating all sites.")
        .ConfigureAwait(false);
    // ... use allSites instead of covered
    return (allSites, allSites, allSites, [], ...);
}
```

This preserves the coverage-filtered path when a coverage file exists
(`--reuse-coverage` or pre-generated coverage). When coverage is absent,
every site is mutated unconditionally.

## Verification

- Fixture `Calculator.cs`: 6/9 mutants killed (66.7%), 3 survivors
- Main solution `RaindropListFetcher.cs`: 2/8 mutants killed (25.0%), 6 survivors
- Tools tests: 287/287 pass
- Build: 0 errors, 0 warnings

## Investigation Notes

The initial hypothesis of a DLL file lock was wrong. The subprocess spawned
`dotnet run --project <test>` correctly, with identical SDK resolution (SDK
10.0.104, global.json at repo root). Exit code 2 from earlier runs was from
TUnit test failures caused by instrumentation code, not the original gate bug.

The key diagnostic was adding `RunAsync` flow tracing showing `sites=0`:
