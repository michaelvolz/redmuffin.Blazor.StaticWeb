---
title: fix: Update chrome-devtools-launch.mjs for strict Brave path checking
type: fix
status: active
date: 2026-04-18
---

# fix: Update chrome-devtools-launch.mjs for strict Brave path checking

## Overview

Update the `chrome-devtools-launch.mjs` script to enforce strict Brave browser path checking with descriptive errors, removing all fallback logic to Chrome/Chromium. Keep it KISS with one hardcoded path per OS.

## Problem Frame

The current script includes fallback logic to Chrome/Chromium on Linux, which adds complexity. We want a simpler approach that only checks for Brave paths and throws clear errors if not found, ensuring users install Brave correctly without silent fallbacks.

## Requirements Trace

- R1. Use only one path per OS (Windows, Linux, macOS) for Brave browser.
- R2. Provide descriptive error messages if Brave is not found at the expected path.
- R3. Remove all fallback logic to Chrome/Chromium.
- R4. Keep the script simple and maintainable (KISS principle).

## Scope Boundaries

- Only modify the `getExecutablePath()` function and related logic.
- Do not change other parts of the script (e.g., spawning, logging).
- No new features or additional platforms.

## Context & Research

### Relevant Code and Patterns

- Existing script: `scripts/mcp/chrome-devtools-launch.mjs`
- Uses `existsSync` for path checking.
- Current paths are correct for each platform.

### Institutional Learnings

- From `docs/solutions/`: No specific learnings on browser path handling, but simplicity is preferred.

### External References

- Brave installation docs confirm standard paths: `/usr/bin/brave` on Linux, `C:\Program Files\BraveSoftware\Brave-Browser\Application\brave.exe` on Windows, `/Applications/Brave Browser.app/Contents/MacOS/Brave Browser` on macOS.

## Key Technical Decisions

- Decision: Remove Chrome fallback logic to enforce Brave-only usage.
- Rationale: Simplifies the script and ensures users address missing Brave installations directly.

## Open Questions

### Resolved During Planning

- What paths to use? Use confirmed standard paths for each OS.

### Deferred to Implementation

- None.

## Implementation Units

- [x] **Unit 1: Update getExecutablePath function**

**Goal:** Modify the function to check only Brave paths and throw descriptive errors if not found.

**Requirements:** R1, R2, R3, R4

**Dependencies:** None

**Files:**

- Modify: `scripts/mcp/chrome-devtools-launch.mjs`

**Approach:**

- Keep `browserPaths` as an object with single strings per platform.
- In `getExecutablePath()`, directly check `existsSync(osBrowserPaths)`.
- If exists, return it.
- If not, throw `new Error(`Brave browser not found at ${osBrowserPaths}. Please ensure Brave is installed on ${platform()}.`)`
- Remove the entire `if (platform() === 'linux')` block and Chrome fallback logic.

**Patterns to follow:**

- Existing error handling in the script (e.g., `child.on('error')`).

**Test scenarios:**

- Test expectation: none -- this is a script update with no behavioral changes to test directly.

**Verification:**

- Script runs without errors when Brave is installed.
- Script throws descriptive error when Brave is not found.

## System-Wide Impact

- **Unchanged invariants:** Other MCP servers and functionality remain unaffected.

## Risks & Dependencies

| Risk                                             | Mitigation                                              |
| ------------------------------------------------ | ------------------------------------------------------- |
| Users without Brave installed get unclear errors | Provide descriptive error message guiding installation. |

## Documentation / Operational Notes

- Update any docs referencing the script if fallback behavior was mentioned.

## Sources & References

- Related code: `scripts/mcp/chrome-devtools-launch.mjs`
