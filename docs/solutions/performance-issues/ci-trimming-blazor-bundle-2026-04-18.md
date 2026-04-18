---
title: CI not applying Blazor trimming, causing large bundle size
date: 2026-04-18
last_updated: 2026-04-18
category: performance-issues
module: Blazor StaticWeb
problem_type: performance_issue
component: tooling
symptoms:
  - Production bundle size significantly larger than local (e.g., 7.5MB / 200+ assemblies)
root_cause: config_error
resolution_type: config_change
severity: high
tags: [blazor, trimming, ci, bundle-size, .net, publish, no-dependencies]
---

# CI not applying Blazor trimming, causing large bundle size

## Problem

The CI pipeline was failing to trim Blazor assemblies during production builds, resulting in significantly larger bundle sizes (200+ assemblies) compared to local development builds (50 assemblies), impacting page load performance.

## Symptoms

- Production bundles were excessively large and bloated
- Local development builds produced normal-sized bundles
- No trimming occurred in the CI environment despite expected configuration

## What Didn't Work

Initial CI builds without specifying an exact .NET version used a default version that lacked trimming support, causing assemblies to remain untrimmed regardless of configuration attempts. Attempts to modify trimming settings in the project file alone didn't resolve the issue, as the underlying runtime version was the root cause.

## Solution

Updated the GitHub Actions workflow to specify exact .NET version, remove invalid flags, and add trimming verification:

**Workflow publish command (before):**

```yaml
dotnet publish ... --no-restore --no-dependencies --verbosity quiet
```

**After:**

```yaml
dotnet publish ... --no-restore --verbosity quiet
```

1. Changed `dotnet-version: "9.0.x"` to `dotnet-version: "9.0.305"` + `workloads: wasm-tools`
2. Fixed TrimmerRootDescriptor path to `$(MSBuildProjectDirectory)/TrimmerRoots.xml`
3. Added verification: `jq '.resources.assembly | length' blazor.boot.json` → warn if >100

## Why This Works

CI was using inconsistent .NET versions where trimming support varied. Pinning the version ensures the SDK has proper trimming capabilities. The path fix ensures the trimmer descriptor is found in CI environment.

## Prevention

- Pin exact .NET SDK + `workloads: wasm-tools` in CI (`actions/setup-dotnet@v5`)
- Remove `--no-dependencies` from `dotnet publish` (breaks dep trimming)
- Post-publish: `jq '.resources.assembly | length' wwwroot/_framework/blazor.boot.json < 100`
- Test: Add Release publish assembly count test (<100)
- Monitor PR bundles via artifact analysis

## Related Issues

- docs/solutions/best-practices/csharp-standards-final-2026-04-02.md (IL2111 Blazor trimming warnings)
- docs/solutions/best-practices/csharp-standards-consolidation-2026-04-06.md (similar trimming guidance)
