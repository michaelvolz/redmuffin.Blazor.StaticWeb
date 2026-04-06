---
title: feat: Verify test locations mirror production structure
type: feat
status: completed
date: 2026-04-06
---

# feat: Verify test locations mirror production structure

## Overview

Audit and correct test file locations to ensure they mirror production code folder and filename structure as closely as possible. Special attention required for NewTests folder (created by AI model) to preserve test quality and avoid losing coverage through improper merging of duplicates.

## Problem Frame

Test files should be organized to mirror production code structure for maintainability and discoverability. Current analysis shows general mirroring but with deviations in NewTests folder and missing tests for some source areas. The NewTests folder contains potentially valuable tests that may have different styles or coverage than existing tests - these require careful analysis before any relocation to prevent loss of test quality or duplication issues.

## Requirements Trace

- R1. Every test file should be located in a path that mirrors its corresponding production code
- R2. Test filenames should match production filenames with "Tests" suffix
- R3. All production code areas should have corresponding test coverage locations
- R4. Preserve test quality and coverage when relocating tests from NewTests folder

## Scope Boundaries

- Focus on file/folder structure mirroring, not test content quality
- Exclude integration and code quality test folders from mirroring requirements
- NewTests folder requires separate analysis before relocation

## Key Technical Decisions

- Analyze NewTests folder thoroughly before any moves to assess test style, quality, and potential duplicates
- Do not merge duplicate tests automatically - require manual review
- Move contents from NewTests folder to proper mirrored locations only after analysis confirms safety
- Add test directories for unmapped source areas like Configuration/ and Services/
- Maintain existing test project structure (redmuffin.Blazor.StaticWeb.Tests, etc.)

## Implementation Units

- [ ] **Unit 1: Audit current test locations**

**Goal:** Create comprehensive list of all test files and their current locations

**Requirements:** R1, R2, R3

**Dependencies:** None

**Files:**

- Create: `docs/audit/test-location-audit-2026-04-06.md`

**Approach:**

- List all files in tests/ folder recursively
- Map each test file to its expected production counterpart
- Document deviations and missing mappings

**Patterns to follow:**

- Use existing mirrored structure as reference

**Test scenarios:**

- Test expectation: none -- this is an audit/documentation unit

**Verification:**

- Audit document contains complete list of test files with location analysis

- [ ] **Unit 2: Identify deviations and corrections needed (excluding NewTests)**

**Goal:** Analyze audit results to identify specific moves and additions required for all tests except NewTests folder

**Requirements:** R1, R2, R3

**Dependencies:** Unit 1

**Files:**

- Modify: `docs/audit/test-location-audit-2026-04-06.md`

**Approach:**

- Review each deviation from mirroring (excluding NewTests)
- Determine correct target location for each misplaced test
- Identify source areas lacking test directories

**Patterns to follow:**

- Follow established mirroring patterns (e.g., Features/Home/ → Features/Home/)

**Test scenarios:**

- Test expectation: none -- this is analysis/documentation unit

**Verification:**

- Audit document includes specific action plan for each deviation (excluding NewTests)

- [ ] **Unit 2.5: Analyze NewTests folder content and style**

**Goal:** Thoroughly analyze NewTests folder to assess test quality, style, duplicates, and relocation safety

**Requirements:** R4

**Dependencies:** Unit 1

**Files:**

- Create: `docs/audit/newtests-analysis-2026-04-06.md`

**Approach:**

- Review each test file in NewTests for style, quality, and coverage
- Compare with existing tests for the same functionality to identify duplicates
- Assess whether tests use newer/better patterns or older/worse patterns
- Document findings and recommendations for each file

**Patterns to follow:**

- Compare against current test standards in the codebase

**Test scenarios:**

- Test expectation: none -- this is analysis/documentation unit

**Verification:**

- Analysis document provides clear assessment and relocation recommendations for each NewTests file

- [ ] **Unit 3: Move misplaced tests to correct locations**

**Goal:** Relocate test files from NewTests (after analysis) and other incorrect locations to proper mirrored paths

**Requirements:** R1, R2, R4

**Dependencies:** Unit 2, Unit 2.5

**Files:**

- Move: Files from NewTests/ (only approved ones) and other incorrect locations to correct mirrored paths
- Update: Any references to moved test files in project files or scripts

**Approach:**

- Create target directories as needed
- Move files preserving relative structure where possible
- Update namespace declarations if needed
- For NewTests: Only move files explicitly approved in analysis, handle duplicates manually

**Patterns to follow:**

- Match production structure exactly (e.g., Services/PerformanceMetricsService.cs → Services/PerformanceMetricsServiceTests.cs)

**Test scenarios:**

- Happy path: Test file moves successfully and builds
- Edge case: Namespace conflicts resolved during move
- Error path: Missing target directory created automatically
- Integration: Duplicate tests from NewTests handled appropriately (manual review required)

**Verification:**

- All identified misplaced tests are in correct locations
- NewTests files only moved if approved in analysis
- Project builds successfully after moves

- [ ] **Unit 4: Add test directories for unmapped source areas**

**Goal:** Create test directory structure for production areas currently lacking tests

**Requirements:** R3

**Dependencies:** Unit 2

**Files:**

- Create: Test directories mirroring unmapped source directories (e.g., Configuration/, Services/)

**Approach:**

- Identify source directories without corresponding test directories
- Create empty test directories following mirroring pattern
- Add placeholder test files if needed for structure

**Patterns to follow:**

- Use same directory names as production

**Test scenarios:**

- Test expectation: none -- directory creation only

**Verification:**

- All production directories have corresponding test directories

- [x] **Unit 5: Verify complete mirroring**

**Goal:** Confirm all test locations now properly mirror production structure

**Requirements:** R1, R2, R3

**Dependencies:** Unit 3, Unit 4

**Files:**

- Modify: `docs/audit/test-location-audit-2026-04-06.md`

**Approach:**

- Re-audit test locations against production
- Verify no remaining deviations
- Document final state

**Patterns to follow:**

- Complete mirroring as established in codebase

**Test scenarios:**

- Happy path: All tests in correct mirrored locations
- Edge case: New source files added during process are accounted for

**Verification:**

- Audit document shows 100% mirroring compliance
- No unmapped source areas remain

## System-Wide Impact

- **Unchanged invariants:** Test project structure and build process remain compatible

## Risks & Dependencies

| Risk                                | Mitigation                                                               |
| ----------------------------------- | ------------------------------------------------------------------------ |
| Breaking builds during file moves   | Test build after each move                                               |
| Namespace conflicts                 | Review and update namespaces as needed                                   |
| Losing test coverage from NewTests  | Require analysis before any moves, manual review of duplicates           |
| Degrading test quality              | Assess test styles in NewTests analysis, preserve better implementations |
| Improper merging of duplicate tests | Never auto-merge, require explicit manual decisions                      |

## Sources & References

- Related code: tests/ folder structure, src/ folder structure
