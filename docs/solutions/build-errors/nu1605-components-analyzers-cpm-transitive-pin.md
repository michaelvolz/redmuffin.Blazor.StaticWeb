---
title: "NU1605 Components.Analyzers pin lag and NU1902 AngleSharp under CPM"
date: 2026-07-22
last_updated: 2026-07-22
category: build-errors
module: package-management
problem_type: build_error
component: tooling
severity: high
symptoms:
  - "NU1605 package downgrade on Microsoft.AspNetCore.Components.Analyzers (10.0.10 required, 10.0.9 pinned)"
  - "NU1902 AngleSharp 1.4.0 advisory via bunit transitive dependency"
  - "Dependabot PR #234 failed restore with TreatWarningsAsErrors"
root_cause: config_error
resolution_type: config_change
tags:
  - nu1605
  - nu1902
  - central-package-management
  - dependabot
  - directory-packages-props
  - components-analyzers
  - transitive-pinning
  - anglesharp
---

# NU1605 Components.Analyzers pin lag and NU1902 AngleSharp under CPM

## Problem

Dependabot PR #234 bumped the shared Microsoft 10.0.x package group and Meziantou.Analyzer, but restore failed with **NU1605** (Components.Analyzers left at a hardcoded older patch) and **NU1902** (bunit’s transitive AngleSharp 1.4.0 ignored the central safer pin). CI never reached compile.

## Symptoms

- **NU1605**: `Microsoft.AspNetCore.Components.Authorization` 10.0.10 → `Microsoft.AspNetCore.Components` 10.0.10 → requires `Microsoft.AspNetCore.Components.Analyzers` ≥ 10.0.10, while `Directory.Packages.props` still declared Analyzers at **10.0.9** (hardcoded `Include` plus a duplicate `Update` pin).
- **NU1902**: Package `AngleSharp` **1.4.0** (moderate advisory [GHSA-pgww-w46g-26qg](https://github.com/advisories/GHSA-pgww-w46g-26qg)) pulled transitively by **bunit 2.7.2**, while the repo already declared `AngleSharpVersion` **1.5.2** for direct references only.
- `dotnet restore` / `dotnet build` failed at restore under warning-as-error; no compilation.

## What Didn't Work

- **Merging the Dependabot bump as-is** — group update only touched packages Dependabot rewrote; the orphaned Analyzers literal never moved.
- **Assuming dual TFM (API net9 / Blazor net10) blocked Microsoft.Extensions 10.0.x** — net9 projects already consumed Extensions **10.0.9**; the failure was incomplete lockstep within the **10.0.x** line, not TFM incompatibility.
- **Fixing only one of NU1605 or NU1902** — each blocks restore independently when warnings are errors.

## Solution

All changes in `Directory.Packages.props`, shipped via PR #234 (Dependabot bumps + follow-up config fix).

### 1. Bind Components.Analyzers to the shared property

**Before:**

```xml
<PackageVersion Include="Microsoft.AspNetCore.Components.Analyzers" Version="10.0.9" />
<!-- ... -->
<PackageVersion Update="Microsoft.AspNetCore.Components.Analyzers" Version="10.0.9" />
```

**After** (single declaration, lockstep with Authorization/WASM):

```xml
<PackageVersion Include="Microsoft.AspNetCore.Components.Analyzers" Version="$(MicrosoftExtensionsVersion)" />
```

Remove the duplicate `PackageVersion Update=...` entry entirely.

### 2. Enable central transitive pinning

```xml
<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
<CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
```

With an existing:

```xml
<AngleSharpVersion>1.5.2</AngleSharpVersion>
<!-- ... -->
<PackageVersion Include="AngleSharp" Version="$(AngleSharpVersion)" />
```

NuGet forces bunit’s transitive AngleSharp edge up to the central version, clearing NU1902.

### 3. Accept the patch bumps in the same PR

- `MicrosoftExtensionsVersion` / `MicrosoftExtensionsDependencyInjectionAbstractionsVersion` → **10.0.10**
- `MeziantouAnalyzerVersion` → **3.0.123**

Verified: `dotnet build` clean; frontend TUnit suite 340 passed; CI Test/Build/Deploy and CodeQL green; PR #234 merged.

## Why This Works

1. **Analyzers are part of the Components restore graph.** Authorization 10.0.10 does not allow an Analyzers package still pinned at 10.0.9. Using `$(MicrosoftExtensionsVersion)` for Analyzers matches every other `Microsoft.AspNetCore.Components.*` entry in the AspNetCore item group so Dependabot property bumps cannot leave Analyzers behind.
2. **Duplicate `Update` pins hide the real source of truth.** Two declarations for the same package made audits and tooling (e.g. version-updater fixtures) treat Analyzers as a special case. One `Include` is enough under CPM.
3. **CPM without transitive pinning only constrains direct references.** Central `AngleSharp` 1.5.2 never applied to bunit’s transitive 1.4.0 until `CentralPackageTransitivePinningEnabled` is true — the standard NuGet fix for “we already pin a safer version.”
4. **Dual TFM is orthogonal.** API stays **net9.0** for Azure SWA Functions; Blazor stays **net10.0**. Microsoft.Extensions **10.0.x** packages multi-target and already ran on net9 projects. Do not block 10.0.x package patches solely because some projects still target net9.

## Prevention

- Never hardcode a patch for a package that belongs in a shared MSBuild version group (`Microsoft.AspNetCore.Components.*` → `$(MicrosoftExtensionsVersion)`).
- After Dependabot group PRs fail restore, inspect **all** `PackageVersion` entries for the same family — especially analyzers and `Update=` overrides — not only the lines Dependabot rewrote.
- Keep `CentralPackageTransitivePinningEnabled=true` whenever CPM is on and audit advisories (NU19xx) treat warnings as errors.
- When reviewing dual-TFM package bumps: ask “is this incomplete lockstep within one major line?” before “can net9 use this package?”

## Related Issues

- PR #234 — deps bump + CPM alignment (merged).
- `docs/solutions/tooling-decisions/nuget-package-update-strategy.md` — Dependabot/CPM policy (includes CPM lockstep gaps table).
- `docs/solutions/test-failures/tunit-testing-platform-version-compatibility.md` — similar “compatible pair / pin lag” pattern for TUnit vs Testing.Platform.
- `scripts/Update-PackageVersions.Tests.ps1` still fixtures a dual Include+Update for Components.Analyzers; production `Directory.Packages.props` no longer has the `Update=` row — tests describe a historical shape, not current props.
