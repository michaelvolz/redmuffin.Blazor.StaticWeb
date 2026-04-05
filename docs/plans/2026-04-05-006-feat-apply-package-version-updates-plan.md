---
title: Apply package version updates in the updater script
type: feat
status: active
date: 2026-04-05
origin: docs/sidenotes/SN-0014.md
---

# Apply Package Version Updates in the Updater Script

## Overview

`scripts/Update-PackageVersions.ps1` already discovers outdated packages, but it only reports them. This plan makes the updater apply the version changes directly in `Directory.Packages.props` while keeping the final `dotnet clean && dotnet build --verbosity quiet && dotnet test` verification gate intact.

Apply mode should be exposed as an explicit `-Apply` switch so report-only behavior remains the default and discovery output stays available.

## Problem Frame

The current NuGet update workflow still leaves the actual version bump as a manual edit. That creates avoidable drift between what the script discovers and what lands in `Directory.Packages.props`, especially because the repo centralizes package versions through shared properties and a small number of exceptions.

## Requirements Trace

- R1. The updater must update package versions in `Directory.Packages.props`, not only report them.
- R2. The update must preserve the repo’s centralized package-version structure.
- R3. The script must continue to handle the existing safety checks and package-selection logic.
- R4. The workflow must still end with the clean build/test verification gate.
- R5. Script behavior must remain deterministic for both shared-property-backed versions and literal exceptions.

## Scope Boundaries

- No change to package selection policy or supported SDK baseline.
- No new package-management tool or alternate workflow.
- No change to the final verification command sequence.
- No attempt to auto-resolve unrelated restore/build failures beyond surfacing them.

## Context & Research

### Relevant Code and Patterns

- `scripts/Update-PackageVersions.ps1` currently reads the root `global.json`, dedupes outdated package sightings, and prints a report.
- `scripts/Update-PackageVersions.Tests.ps1` already uses offline JSON fixtures and `TestDrive`-backed repo roots, which is a good fit for apply-mode coverage.
- `Directory.Packages.props` stores shared version constants in a top property section, then maps package IDs through `PackageVersion` entries and a small number of literal/override exceptions.
- `docs/plans/2026-04-05-005-feat-safe-nuget-update-workflow-plan.md` covers the broader updater workflow and is the closest related prior plan.

### Institutional Learnings

- Centralize durable policy in repo config rather than scattering it across transient update steps.
- Keep the package-update workflow as one focused PowerShell entrypoint so it stays repeatable.
- Use `-NoProfile` for repo scripts to keep execution predictable.

## Key Technical Decisions

- The script should edit `Directory.Packages.props` directly instead of asking the user to transfer report output by hand.
- Shared-property-backed versions should be updated at the property level so dependent package entries stay consistent.
- CPM entries should be resolved deterministically: update a shared property when the package `Version` attribute points at that property; update an inline `Version` only when no shared property is involved; preserve explicit `Update=` overrides unless they are the intended source of truth for that package.
- The updater should fail clearly when an outdated package cannot be mapped to a CPM entry.
- The apply path should write through a temp file and replace the target atomically so formatting and encoding are preserved as much as practical.

## Open Questions

### Resolved During Planning

- Which file is the update target? `Directory.Packages.props` at repo root.
- What should remain unchanged? The final clean build/test gate.
- How is apply mode invoked? As an explicit `-Apply` switch.

### Deferred to Implementation

- The exact XML update strategy needed to preserve formatting while editing the props file.

## Implementation Units

- [ ] **Unit 1: Teach the updater to write CPM changes**

**Goal:** Extend `scripts/Update-PackageVersions.ps1` so it can apply version changes into `Directory.Packages.props` instead of stopping at a report.

**Requirements:** R1, R2, R3, R5

**Dependencies:** Existing report/dedup logic and root SDK validation

**Files:**

- Modify: `scripts/Update-PackageVersions.ps1`
- Modify: `Directory.Packages.props`

**Approach:**

- Reuse the existing package discovery flow as the source of truth for what should change.
- Resolve each package to its CPM entry by reading the props structure rather than doing blind text replacement.
- Update shared properties when the package version is property-backed.
- Update inline literal entries only when they are the effective source of truth.
- Preserve explicit `Update=` overrides unless the package mapping proves that node is the correct target.
- Preserve the current .NET 9 safety guard and conflict detection in the shared selection step used by both report and apply paths.
- Write changes atomically through a temporary file to avoid partial updates.

**Execution note:** Implement test-first coverage for the file-rewrite behavior and the failure path when a package cannot be mapped.

**Patterns to follow:**

- `Directory.Packages.props` property-first layout
- Existing report deduplication in `scripts/Update-PackageVersions.ps1`

**Test scenarios:**

- Happy path: an outdated shared-property-backed package updates the corresponding top-level property value and leaves the dependent `PackageVersion` entries intact.
- Happy path: a literal version entry updates in place when that package is outdated.
- Integration: the script still rejects `.NET 10` drift while applying updates for `.NET 9` packages.
- Edge case: duplicate package sightings across projects still resolve to a single applied change.
- Error path: an outdated package with no matching CPM entry fails clearly without writing partial changes.

**Verification:**

- After apply mode runs, `Directory.Packages.props` reflects the new version values and remains structurally valid.

- [ ] **Unit 2: Expand script tests for apply-mode behavior**

**Goal:** Cover the writer path, safety rails, and idempotence for the updater script.

**Requirements:** R1, R3, R4, R5

**Dependencies:** Unit 1 implementation shape

**Files:**

- Modify: `scripts/Update-PackageVersions.Tests.ps1`

**Approach:**

- Add fixtures for a writable CPM file and package list JSON that exercises shared-property-backed, literal, and override entries.
- Assert that report-only behavior still leaves the props file unchanged.
- Assert that apply behavior changes only the intended values and is repeatable when the file is already current.
- Assert that the explicit `-Apply` switch is required to write changes.
- Keep the tests offline and deterministic with `TestDrive`.

**Patterns to follow:**

- Existing JSON fixture style in `scripts/Update-PackageVersions.Tests.ps1`
- `TestDrive`-based repo root setup

**Test scenarios:**

- Happy path: report mode still prints outdated packages without modifying `Directory.Packages.props`.
- Happy path: apply mode writes the expected version into the correct CPM location.
- Happy path: a second apply against already-updated versions leaves the file unchanged.
- Edge case: the script preserves the existing override entry for `Microsoft.AspNetCore.Components.Analyzers` while applying the intended update path.
- Error path: conflicting latest-version data still fails before any write occurs.

**Verification:**

- The test suite proves both discovery and write paths, including no-op idempotence and failure isolation.

## System-Wide Impact

- **Interaction graph:** The script now sits on the path between package discovery, CPM file editing, and the repo’s restore/build/test gate.
- **Error propagation:** Any mapping or write failure should stop before partial version drift reaches the build.
- **State lifecycle risks:** A failed write must not leave `Directory.Packages.props` half-updated.
- **Unchanged invariants:** The root SDK pin stays on .NET 9, and the final clean build/test verification stays unchanged.

## Risks & Dependencies

| Risk                                                  | Mitigation                                                                  |
| ----------------------------------------------------- | --------------------------------------------------------------------------- |
| Naive replacement corrupts the props file             | Update through the parsed XML structure, not raw text substitution.         |
| Shared properties and literal exceptions diverge      | Treat property-backed entries and inline literals as separate update cases. |
| Partial writes create inconsistent package state      | Fail fast before write, and verify tests cover idempotence and error paths. |
| Restore/build failures surface after the version bump | Keep the final clean build/test gate as the last check.                     |

## Documentation / Operational Notes

- Keep the updater script as the single documented entrypoint for NuGet version bumps.
- Preserve the final verification gate exactly as documented in the sidenote.

## Sources & References

- **Origin document:** `docs/sidenotes/SN-0014.md`
- Related code: `scripts/Update-PackageVersions.ps1`, `scripts/Update-PackageVersions.Tests.ps1`, `Directory.Packages.props`
- Related plan: `docs/plans/2026-04-05-005-feat-safe-nuget-update-workflow-plan.md`
