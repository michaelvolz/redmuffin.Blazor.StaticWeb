---
title: Safe NuGet update workflow
type: feat
status: active
date: 2026-04-05
origin: docs/brainstorms/2026-04-05-nuget-cpm-dotnet9-upgrade-policy-requirements.md
---

# Safe NuGet Update Workflow

## Overview

Create one simple repo-local PowerShell script for updating CPM-managed NuGet
packages, keep the root `global.json` as the only SDK source of truth, and
remove the obsolete nested `global.json` files.

## Requirements Trace

- R1. Stay on .NET 9.
- R2. Avoid .NET 10 dependencies.
- R3. Keep Central Package Management intact.
- R4. Document the workflow.
- R5. Call out the .NET 9 baseline.
- R6. Keep all package version values centralized in the top property section of
  `Directory.Packages.props`.

## Scope Boundaries

- No new package manager.
- No custom .NET global tool.
- No enterprise policy gates.
- No extra workflow beyond what is needed to update packages safely.

## Implementation Units

- [ ] **Unit 1: Point docs at the script**

**Goal:** Make `AGENTS.md` and `README.md` direct people to the same updater
script.

**Files:**

- Modify: `AGENTS.md`
- Modify: `README.md`

**Test scenarios:**

- Test expectation: none -- docs-only.

- [ ] **Unit 2: Add the updater script**

**Goal:** Add `scripts/Update-PackageVersions.ps1`.

**Approach:**

- Read only the root `global.json`.
- Use `dotnet list package --outdated`.
- Update only `Directory.Packages.props`.
- Keep every package version value in the top property section; package items
  should reference those properties rather than hard-coded versions.
- Preserve both literal and shared-property-backed package versions.
- Dedupe repeated package sightings across projects/TFMs.

**Files:**

- Add: `scripts/Update-PackageVersions.ps1`

**Test scenarios:**

- Report mode lists outdated packages without changing files.
- Apply mode updates the correct CPM entry.
- Shared-property-backed versions update in place.
- Duplicate package sightings are deduped.

- [ ] **Unit 3: Test the script**

**Goal:** Cover the updater script with focused tests.

**Files:**

- Add: `scripts/Update-PackageVersions.Tests.ps1`

**Test scenarios:**

- Report mode leaves `Directory.Packages.props` unchanged.
- Apply mode writes the expected version.
- Missing CPM entries fail cleanly.

- [ ] **Unit 4: Remove obsolete nested SDK pins**

**Goal:** Delete the three nested `global.json` files.

**Files:**

- Delete: `src/redmuffin.Blazor.StaticWeb.Common/global.json`
- Delete: `src/redmuffin.Blazor.StaticWeb.Api/global.json`
- Delete: `tests/redmuffin.Blazor.StaticWeb.Api.Tests/global.json`

**Test scenarios:**

- Test expectation: none -- file deletion only.

## Verification

- Root `global.json` is the only SDK pin.
- The updater script can discover and apply package updates.
- CPM stays centralized in `Directory.Packages.props`.
- Final verification is `dotnet clean && dotnet build --verbosity quiet &&
dotnet test`.
