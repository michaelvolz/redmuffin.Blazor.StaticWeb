---
title: "refactor: Move changelog config to .config folder"
type: refactor
status: completed
date: 2026-04-03
---

# refactor: Move changelog config to .config folder

## Overview

Move `config/changelog-config.json` to `.config/changelog-config.json` to consolidate tool configuration in the standard .NET location, then remove the now-empty `config` folder.

## Problem Frame

The project has two config locations:

- `.config/` - already contains `.config/dotnet-tools.json` (standard .NET tool config location)
- `config/` - contains only `changelog-config.json`

Having both creates unnecessary clutter. Moving the changelog config to `.config/` is cleaner and follows .NET conventions.

## Scope Boundaries

- Move only the changelog config file
- Update all hardcoded paths in PowerShell modules
- Update documentation references
- Delete the empty `config` folder after move
- **Do not** modify any other config files or the script logic itself

## Context & Research

### Relevant Code and Patterns

The config path is hardcoded as default parameter values in 5 modules:

| Module                    | Lines                          | Current Default                  |
| ------------------------- | ------------------------------ | -------------------------------- |
| FilterModule.psm1         | 17, 293                        | `"config/changelog-config.json"` |
| FileGenerator.psm1        | 109, 180, 278                  | `"config/changelog-config.json"` |
| ChangelogFormatter.psm1   | 17, 50, 89, 125, 170, 292, 352 | `"config/changelog-config.json"` |
| CategorizationModule.psm1 | 17, 87, 229                    | `"config/changelog-config.json"` |
| UpdateModule.psm1         | 214, 366                       | `"config/changelog-config.json"` |

Documentation in `Update-Changelog.ps1` also references the path (lines 22, 96, 102).

### Existing Pattern

The `.config/` folder is already used for .NET tool configuration:

- `.config/dotnet-tools.json` exists

## Key Technical Decisions

- **Target location `.config/changelog-config.json`**: Follows .NET convention, keeps tool configs consolidated
- **Update all 5 modules**: Each has the path as a default parameter value - need to update all occurrences
- **Preserve exact JSON content**: No changes to config structure, only location

## Open Questions

### Resolved During Planning

- **Q: What if other files exist in config/?** A: Only `changelog-config.json` exists. Safe to delete folder after move.

## Implementation Units

- [ ] **Unit 1: Move config file**

**Goal:** Relocate the config file to the new location

**Files:**

- Move: `config/changelog-config.json` → `.config/changelog-config.json`

**Approach:**

- Use file system move (not git move) to preserve file attributes

**Verification:**

- `.config/changelog-config.json` exists and contains valid JSON
- `config/changelog-config.json` no longer exists

---

- [ ] **Unit 2: Update FilterModule.psm1**

**Goal:** Update default config path in FilterModule

**Files:**

- Modify: `scripts/modules/FilterModule.psm1`

**Approach:**

- Replace all occurrences of `"config/changelog-config.json"` with `".config/changelog-config.json"`
- Lines 17 and 293

**Test scenarios:**

- Test expectation: none -- this is a config path change only, no behavioral change

**Verification:**

- Script runs without "config file not found" errors

---

- [ ] **Unit 3: Update FileGenerator.psm1**

**Goal:** Update default config path in FileGenerator

**Files:**

- Modify: `scripts/modules/FileGenerator.psm1`

**Approach:**

- Replace all occurrences of `"config/changelog-config.json"` with `".config/changelog-config.json"`
- Lines 109, 180, 278

**Test scenarios:**

- Test expectation: none -- this is a config path change only, no behavioral change

**Verification:**

- Script runs without "config file not found" errors

---

- [ ] **Unit 4: Update ChangelogFormatter.psm1**

**Goal:** Update default config path in ChangelogFormatter

**Files:**

- Modify: `scripts/modules/ChangelogFormatter.psm1`

**Approach:**

- Replace all occurrences of `"config/changelog-config.json"` with `".config/changelog-config.json"`
- Lines 17, 50, 89, 125, 170, 292, 352

**Test scenarios:**

- Test expectation: none -- this is a config path change only, no behavioral change

**Verification:**

- Script runs without "config file not found" errors

---

- [ ] **Unit 5: Update CategorizationModule.psm1**

**Goal:** Update default config path in CategorizationModule

**Files:**

- Modify: `scripts/modules/CategorizationModule.psm1`

**Approach:**

- Replace all occurrences of `"config/changelog-config.json"` with `".config/changelog-config.json"`
- Lines 17, 87, 229

**Test scenarios:**

- Test expectation: none -- this is a config path change only, no behavioral change

**Verification:**

- Script runs without "config file not found" errors

---

- [ ] **Unit 6: Update UpdateModule.psm1**

**Goal:** Update default config path in UpdateModule

**Files:**

- Modify: `scripts/modules/UpdateModule.psm1`

**Approach:**

- Replace all occurrences of `"config/changelog-config.json"` with `".config/changelog-config.json"`
- Lines 214, 366

**Test scenarios:**

- Test expectation: none -- this is a config path change only, no behavioral change

**Verification:**

- Script runs without "config file not found" errors

---

- [ ] **Unit 7: Update Update-Changelog.ps1 documentation**

**Goal:** Update doc comments that reference the config path

**Files:**

- Modify: `scripts/Update-Changelog.ps1`

**Approach:**

- Update lines 22, 96, 102 to reference `.config/changelog-config.json`
- These are only comments/documentation, not code

**Test scenarios:**

- Test expectation: none -- documentation only

**Verification:**

- Comments accurately reflect new path

---

- [ ] **Unit 8: Remove empty config folder**

**Goal:** Clean up the now-empty config directory

**Files:**

- Delete: `config/` folder

**Approach:**

- Verify folder is empty before deletion
- Use Remove-Item with -Force if needed

**Test scenarios:**

- Test expectation: none -- file system cleanup

**Verification:**

- `config/` folder no longer exists
- `.config/changelog-config.json` is accessible

---

- [ ] **Unit 9: Verify script works end-to-end**

**Goal:** Confirm the changelog script runs successfully with new config location

**Files:**

- Test: `scripts/Update-Changelog.ps1`

**Approach:**

- Run the script and verify it produces output without errors
- Check that config is loaded correctly

**Test scenarios:**

- Happy path: Run `.\scripts\Update-Changelog.ps1` and verify it completes without errors

**Verification:**

- Script completes successfully
- CHANGELOG.md is generated (or updated)
- No "config file not found" warnings

## System-Wide Impact

- **Interaction graph:** Only affects changelog generation script
- **Error propagation:** N/A - config path change only
- **Unchanged invariants:** Script behavior, output format, filtering logic all unchanged

## Risks & Dependencies

| Risk                    | Likelihood | Impact | Mitigation                                                  |
| ----------------------- | ---------- | ------ | ----------------------------------------------------------- |
| Missed path reference   | Low        | Medium | Searched all .psm1 and .ps1 files; verified all occurrences |
| Config folder not empty | Low        | Low    | Verified only changelog-config.json exists in config/       |

## Documentation / Operational Notes

- No user-facing documentation changes needed
- Internal code comments updated (Unit 7)
- Script behavior unchanged - purely organizational refactor
