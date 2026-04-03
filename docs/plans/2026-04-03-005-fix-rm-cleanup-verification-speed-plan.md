---
title: "fix: Speed up rm-cleanup verification"
type: fix
status: completed
date: 2026-04-03
---

# Fix: Speed Up rm-cleanup Verification

## Overview

The cleanup workflow is already safe and parallel, but its final verification still spends extra time on two final process-family queries. This plan trims that last step down to a single source-of-truth snapshot so the teardown is less chatty and materially faster without weakening the safety rules.

## Problem Frame

The current `rm-cleanup` skill verifies cleanup with two final process-family queries. That is unnecessary overhead for a task that runs often and is already constrained to local Windows process identification.

The recommended fix is to keep the cleanup teammates unchanged and make the background verifier faster by:

- using one process snapshot for verification instead of two final queries
- filtering that snapshot in memory for remaining `dotnet.exe`, MCP-owned Brave, and `devenv.exe` ownership data
- preserving the existing no-probe, warnings-only success contract

## Requirements Trace

- R1. Reduce verification latency by collapsing the two final checks into one snapshot-based pass.
- R2. Preserve the existing safety rules: never touch Visual Studio-owned `dotnet.exe` and never confuse the user’s main Brave with the MCP-owned Brave.
- R3. Keep the existing no-chatter contract intact: successful verification stays silent, and only the existing already-closed notices, warnings, or errors appear when something is already absent or actually wrong.
- R4. Keep the workflow Windows/PowerShell/CIM based and avoid browser-page probing.

## Scope Boundaries

- In scope: `.opencode/skills/rm-cleanup/SKILL.md`
- In scope: wording and structure inside the background verification teammate only
- Out of scope: browser-cleanup behavior, server-cleanup behavior, `opencode.json`, and unrelated tab-hygiene guidance

## Context & Research

### Relevant Code and Patterns

- `.opencode/skills/rm-cleanup/SKILL.md` — current cleanup workflow and the Phase 2 verifier that still performs separate final checks
- `.opencode/skills/rm-dev-workflows/SKILL.md` — confirms teardown belongs in `rm-cleanup`, not the general workflow skill
- `docs/plans/2026-04-03-003-fix-rm-cleanup-rewrite-plan.md` — prior completed rewrite that established the no-probe, process-ID-based direction

### Institutional Learnings

- `docs/solutions/logic-errors/wasm-metrics-showing-zero-bytes-2026-04-03.md` reinforces the pattern of verifying against the source of truth directly instead of an indirect or derived surface.

### External References

- Microsoft Learn: `Get-CimInstance` supports `-Filter`, `-Query`, `-Property`, and returns a snapshot of CIM instances.
- Microsoft Learn: `Get-CimInstance` can reduce transferred data when only a small property set is requested.

## Key Technical Decisions

- Keep the background verifier as a separate teammate: the parallel cleanup shape is useful, and only the final checks are slow.
- Use a single CIM snapshot for the verifier with a selective filter: the query should cover the process families needed for verification and return only the properties needed to determine remaining `dotnet.exe`, Brave, and `devenv.exe` ownership.
- Keep the property set minimal: `ProcessId`, `ParentProcessId`, `Name`, and `CommandLine` are sufficient for the remaining-process decision.
- Preserve the current output contract: no success summary, no tables, and no new polling loop.

## Open Questions

### Resolved During Planning

- Should the cleanup teammates themselves change? No. The slowdown is in Phase 2 verification, so the cleaners stay untouched.
- Should verification remain a background teammate? Yes. It preserves the current parallel teardown shape.

### Deferred to Implementation

- Exact WQL shape (`-Filter` versus `-Query`) and the final wording for the single verifier message stay open until the skill text is edited; the plan only requires a single snapshot and minimal properties.

## Implementation Units

- [ ] **Unit 1: Collapse Phase 2 into a single verification snapshot**

**Goal:** Replace the current two-step final verification with one CIM snapshot that can answer both “what dotnet remains?” and “what Brave remains?” from the same result set.

**Requirements:** R1, R2, R4

**Dependencies:** None

**Files:**

- Modify: `.opencode/skills/rm-cleanup/SKILL.md`

**Approach:**

- Keep Phase 1 parallel cleanup unchanged.
- Rewrite the Phase 2 verifier so it queries `Win32_Process` once, then filters the returned snapshot in memory for the remaining-process checks.
- Keep the query focused on the exact process families needed for verification: `dotnet.exe`, `brave.exe`, and `devenv.exe`.
- Request only the properties the verifier actually needs.
- Do not add retry loops, port checks, or browser-page probing.

**Patterns to follow:**

- The current `rm-cleanup` phase structure: 3 cleanup teammates plus one background verifier.
- The existing CIM-based process identification pattern already used in the skill.
- The no-probe cleanup rule already present in the skill frontmatter and body.

**Test scenarios:**

- **Happy path:** one MCP Brave process and one agent-owned `dotnet.exe` are present before cleanup; the verifier uses one snapshot and reports only the remaining-process outcome, not a second scan.
- **Edge case:** no matching Brave or `dotnet.exe` processes remain; the verifier emits no success summary and exits after the single snapshot.
- **Edge case:** only Visual Studio-owned `dotnet.exe` remains; the verifier keeps the VS-owned process out of the removable set and does not misclassify it as cleanup residue.
- **Error path:** the snapshot query fails; the verifier surfaces the error once and does not fall back to a slower retry or port-based check.
- **Integration:** the verifier still waits for the three cleanup teammates before it performs the single snapshot.

**Verification:**

- The edited skill should contain exactly one final verification snapshot step and no repeated final checks.
- The verifier should still be able to explain both remaining-process results from one returned snapshot.
- Clean runs should stay quiet; already-closed cases should keep their existing terse notice without introducing summary chatter.

## System-Wide Impact

- **Interaction graph:** only `rm-cleanup` changes; the general workflow skill remains untouched.
- **Error propagation:** query failures should surface immediately in the verifier instead of being hidden behind retries.
- **Unchanged invariants:** Visual Studio-owned `dotnet.exe` stays protected, the user’s main Brave stays protected, and browser-page probing remains forbidden.

## Risks & Dependencies

| Risk                                                                                      | Mitigation                                                                                                       |
| ----------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------- |
| The one-shot snapshot might become too broad and slow if it pulls unnecessary properties. | Keep the property list minimal and use a selective filter for only the process families needed for the verifier. |
| A simplified verifier could accidentally misclassify a surviving process.                 | Preserve the existing ownership rules and keep the safety filters explicit.                                      |
| Fewer messages might make failures harder to diagnose.                                    | Keep warnings/errors explicit even though success stays quiet.                                                   |

## Documentation / Operational Notes

- No rollout or migration work is needed.
- Keep any future cleanup guidance consistent with the same no-chatter contract.

## Sources & References

- Related code: `.opencode/skills/rm-cleanup/SKILL.md`
- Related plan: `docs/plans/2026-04-03-003-fix-rm-cleanup-rewrite-plan.md`
- External docs:
  - https://learn.microsoft.com/en-us/powershell/module/cimcmdlets/get-ciminstance?view=powershell-7.6
  - https://learn.microsoft.com/en-us/powershell/scripting/samples/getting-wmi-objects--get-ciminstance-?view=powershell-7.6
