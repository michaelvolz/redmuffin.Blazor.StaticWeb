---
title: TUnit and Microsoft.Testing.Platform Version Incompatibility
date: 2026-04-07
last_updated: 2026-07-22
category: test-failures
module: testing
problem_type: test_failure
component: testing_framework
severity: high
tags:
  - tunit
  - nuget
  - version-incompatibility
  - ci-cd
  - deployment-failure
symptoms:
  - MissingMethodException during test execution
  - Method 'PlatformResources.get_UnexpectedExceptionDuringByteConversionErrorMessage' not found
  - GitHub Actions deployment pipeline blocked at test stage
root_cause: config_error
resolution_type: dependency_update
---

# TUnit and Microsoft.Testing.Platform Version Incompatibility

## Problem

Deployment failed when **Microsoft.Testing.Platform** moved ahead of a compatible **TUnit** pair. TUnit depends on Testing.Platform APIs; a mismatched upgrade produced a `MissingMethodException` at test runtime and blocked CI.

## Symptoms (original incident)

- Tests fail with `MissingMethodException: PlatformResources.get_UnexpectedExceptionDuringByteConversionErrorMessage`
- GitHub Actions blocked at the test stage
- Affected both `redmuffin.Blazor.StaticWeb.Api.Tests` and `redmuffin.Blazor.StaticWeb.Tests`

## What Didn't Work

- Assuming a TUnit bump alone fixed the pipeline (wrong dependency identified first)
- Treating Testing.Platform as freely floatable without checking the TUnit release notes / runtime pair

## Solution

### Original fix (2026-04)

At the time of the incident:

| Package | Compatible pin |
| ------- | -------------- |
| TUnit | 1.28.7 |
| Microsoft.Testing.Platform | **2.1.0** (not 2.2.1) |

`scripts/Update-PackageVersions.ps1` gained `Get-IncompatiblePackageConstraints` so bulk updates would not reintroduce MTP **2.2+** while TUnit was still on **1.28.7**.

### Current tree (2026-07-22)

Both packages have advanced **in lockstep**. Central pins in `Directory.Packages.props`:

| Property | Version |
| -------- | ------- |
| `TUnitVersion` | **1.53.0** |
| `MicrosoftTestingPlatformVersion` | **2.2.3** |

This pair is what the solution builds and runs tests against today. The durable rule is **not** “never use MTP 2.2+”; it is **keep TUnit and Microsoft.Testing.Platform on a known-compatible pair** and verify with a full test run after either side moves.

**Script drift:** `Get-IncompatiblePackageConstraints` still lists `Microsoft.Testing.Platform` `MaxVersion = '2.1.0'` with a reason tied to TUnit **1.28.7**. That constraint is **stale relative to `Directory.Packages.props`** (already on 2.2.3). Dependabot/CPM pins win for restore; the constraint only affects the local updater script. When next touching `Update-PackageVersions.ps1`, raise or remove that max to match the current TUnit major/minor line, with a reason that names the TUnit version the cap was validated against.

## Why This Works

TUnit and Microsoft.Testing.Platform ship as a **runtime couple**. Jumping Testing.Platform while TUnit still expects older platform types fails at test execution, not always at restore. Pinning both via shared version properties in CPM makes the pair visible in one file and reviewable in Dependabot diffs.

## Prevention

1. After any bump of `TUnitVersion` or `MicrosoftTestingPlatformVersion`, run the full frontend (and API) test projects — do not stop at restore/build.
2. Keep both versions in `Directory.Packages.props` properties; never let only one side float via an ad-hoc override.
3. Maintain `Get-IncompatiblePackageConstraints` **only with caps validated against the current TUnit line**; update or delete caps when TUnit is upgraded deliberately.
4. Prefer Dependabot grouped updates so TUnit-family and platform packages land in the same PR when possible.

## Related

- `docs/solutions/tooling-decisions/nuget-package-update-strategy.md` — Dependabot/CPM update policy
- `docs/solutions/build-errors/nu1605-components-analyzers-cpm-transitive-pin.md` — sibling “shared property / incomplete lockstep” failure mode
- `Directory.Packages.props` — `TUnitVersion`, `MicrosoftTestingPlatformVersion`
