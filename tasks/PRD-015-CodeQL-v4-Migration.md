# PRD-015: CodeQL Action v3 to v4 Migration

## Overview

This PRD documents the migration of the CodeQL workflow from GitHub Actions v3 to v4. CodeQL Action v4 was released on October 7, 2025, and runs on Node.js 24 runtime. Version 3 is scheduled for deprecation in December 2026.

**Migration Complexity:** LOW - No breaking API changes, only Node.js runtime upgrade (v20 → v24)

---

## Goals

1. Upgrade all CodeQL Action references from `@v3` to `@v4` without breaking existing functionality
2. Preserve all custom workflow logic and optimizations
3. Maintain the documentation-only change detection feature
4. Ensure zero disruption to the existing CI/CD pipeline
5. Document all custom changes for future reference

---

## Current State Analysis

### File Location

`.github/workflows/codeql.yml` (242 lines)

### Custom Modifications (Documented for Preservation)

The current workflow contains **significant custom logic** that must be preserved:

#### 1. **Documentation-Only Change Detection Job** (Lines 23-132)

- **Job Name:** `check_changes`
- **Purpose:** Skip CodeQL analysis when only documentation/non-code files change
- **Logic:**
  - Runs on PRs and pushes (skips scheduled runs)
  - Checks out full git history (`fetch-depth: 0`)
  - Defines comprehensive list of files to skip (38+ patterns)
  - Compares changed files against skip patterns using regex
  - Outputs `should_skip=true` if only doc files changed
- **Key Feature:** Handles force pushes gracefully (falls back to HEAD~1 comparison)

#### 2. **Documentation-Only Notification Job** (Lines 133-151)

- **Job Name:** `docs_only_changed_job`
- **Purpose:** Provides clear feedback when analysis is skipped
- **Output:** Friendly message explaining why analysis was skipped

#### 3. **Smart Analysis Triggering** (Lines 152-158)

- **Condition:** Runs on schedule OR when non-doc files change
- **Logic:** `if: github.event_name == 'schedule' || needs.check_changes.outputs.should_skip != 'true'`

#### 4. **File Skip Patterns** (Lines 38-76)

- Comprehensive list of 38+ patterns including:
  - Documentation: `README.md`, `AGENTS.md`, `CHANGELOG.md`, `docs/**/*.md`
  - Tests: `tests/**/*`
  - Scripts: `scripts/**/*`
  - IDE Config: `.vscode/**/*`, `.trae/**/*`
  - Build Tools: `.editorconfig`, `.mcp.json`
  - Git Config: `.gitattributes`, `.gitignore`

#### 5. **Languages Matrix** (Lines 176-191)

- Analyzes: `actions` and `csharp` languages
- Build mode: `none` for both

#### 6. **Version References to Update**

- Line 212: `github/codeql-action/init@v3` → `@v4`
- Line 240: `github/codeql-action/analyze@v3` → `@v4`

---

## Functional Requirements

### FR-001: Version Bump

**Requirement:** Update all CodeQL Action references from v3 to v4.

**Specific Changes:**

- Change `github/codeql-action/init@v3` to `github/codeql-action/init@v4`
- Change `github/codeql-action/analyze@v3` to `github/codeql-action/analyze@v4`

**Lines to Modify:**

- Line 212: `uses: github/codeql-action/init@v3`
- Line 240: `uses: github/codeql-action/analyze@v3`

### FR-002: Preserve Custom Change Detection Logic

**Requirement:** All custom file detection and skip logic must remain intact.

**Must Preserve:**

- `check_changes` job with all 38+ skip patterns
- `docs_only_changed_job` for user feedback
- The conditional logic for running analysis
- Git history fetch (`fetch-depth: 0`)
- Force push handling logic

### FR-003: Maintain Language Matrix

**Requirement:** Keep the existing language configuration.

**Current Configuration:**

```yaml
matrix:
  include:
    - language: actions
      build-mode: none
    - language: csharp
      build-mode: none
```

### FR-004: Verify Workflow Functionality

**Requirement:** After migration, the workflow must:

- Run successfully on push/PR to master
- Properly detect documentation-only changes
- Skip analysis when appropriate
- Run full analysis on code changes
- Continue scheduled weekly runs (Wednesdays at 07:32 UTC)

---

## Non-Goals (Out of Scope)

1. **Adding new languages** - Not required, keep existing `actions` and `csharp`
2. **Changing build modes** - Keep `build-mode: none` for both languages
3. **Modifying skip patterns** - Keep existing 38+ patterns unless requested
4. **Adding new custom logic** - This is a pure version bump with preservation
5. **Changing trigger events** - Keep existing push/PR/schedule triggers
6. **Updating other Actions versions** - Focus only on CodeQL Actions

---

## Technical Considerations

### Node.js Runtime Change

- **v3:** Runs on Node.js v20
- **v4:** Runs on Node.js v24
- **Impact:** No breaking changes for workflow syntax or inputs

### No API Changes

The v3 → v4 migration is purely a runtime update. All inputs, outputs, and configuration options remain identical:

- `languages` input works the same
- `build-mode` input works the same
- `category` input works the same
- All outputs remain unchanged

### GitHub Enterprise Server Compatibility

- **Not applicable** - This repository is on GitHub.com
- If GHES were in use: v4 requires GHES 3.20+ or GHES 3.19 with GitHub Connect enabled

### Permissions

No changes required to workflow permissions:

- `security-events: write` (required)
- `packages: read` (required for CodeQL packs)
- `actions: read` (for private repos)
- `contents: read` (for private repos)

---

## Implementation Plan

### Step 1: Create Feature Branch

```bash
git checkout -b feature/PRD-015-codeql-v4-migration
```

### Step 2: Update Version References

Edit `.github/workflows/codeql.yml`:

**Line 212:**

```yaml
# BEFORE
uses: github/codeql-action/init@v3

# AFTER
uses: github/codeql-action/init@v4
```

**Line 240:**

```yaml
# BEFORE
uses: github/codeql-action/analyze@v3

# AFTER
uses: github/codeql-action/analyze@v4
```

### Step 3: Validate Syntax

```bash
# Check YAML syntax
cat .github/workflows/codeql.yml | head -250
```

### Step 4: Create Pull Request

1. Commit the changes
2. Push to origin
3. Create PR with detailed description
4. Link to this PRD

### Step 5: Test the Migration

#### Test Case 1: Code Changes Trigger Analysis

1. Create a test branch
2. Modify a C# file in `src/`
3. Push and create PR
4. **Expected:** CodeQL analysis runs for both `actions` and `csharp`

#### Test Case 2: Documentation Changes Skip Analysis

1. Modify `README.md`
2. Push and create PR
3. **Expected:**
   - `check_changes` job runs and detects docs-only change
   - `docs_only_changed_job` runs with skip message
   - `analyze` job is skipped

#### Test Case 3: Scheduled Run

1. Wait for Wednesday at 07:32 UTC OR
2. Manually trigger workflow via `workflow_dispatch` (if added)
3. **Expected:** Analysis runs regardless of changes

---

## Testing Checklist

- [ ] Workflow file YAML syntax is valid
- [ ] `init` step uses `github/codeql-action/init@v4`
- [ ] `analyze` step uses `github/codeql-action/analyze@v4`
- [ ] Code changes trigger full analysis
- [ ] Documentation-only changes skip analysis
- [ ] Scheduled runs execute normally
- [ ] Both `actions` and `csharp` languages are analyzed
- [ ] No errors in workflow execution
- [ ] Security events are uploaded successfully

---

## Success Metrics

1. **Zero Breaking Changes:** All existing functionality preserved
2. **Deprecation Warnings Resolved:** No more v3 deprecation warnings
3. **Performance:** No regression in workflow execution time
4. **Coverage:** All code changes still trigger security analysis
5. **Efficiency:** Documentation-only changes still skip analysis

---

## Rollback Plan

If issues are encountered:

```bash
# Revert the version change
git checkout .github/workflows/codeql.yml
git commit -m "Revert CodeQL to v3 due to [issue]"
git push
```

**Known Issues:** None expected - this is a non-breaking version bump.

---

## References

- [CodeQL Action v3 Deprecation Notice](https://github.blog/changelog/2025-10-28-upcoming-deprecation-of-codeql-action-v3/)
- [CodeQL Action Changelog](https://github.com/github/codeql-action/blob/main/CHANGELOG.md)
- [CodeQL Action Repository](https://github.com/github/codeql-action)

---

## Open Questions

1. Do we want to add `workflow_dispatch` trigger for manual testing? (Currently not in scope)
2. Should we consider adding any new skip patterns? (Currently not in scope)
3. Are there any new CodeQL v4 features we should enable? (Requires separate PRD)

---

## Document History

| Version | Date       | Author   | Changes                                                        |
| ------- | ---------- | -------- | -------------------------------------------------------------- |
| 1.0     | 2026-03-30 | opencode | Initial PRD creation with complete custom change documentation |

---

## Implementation Notes

**Estimated Effort:** 15 minutes (simple version bump)

**Risk Level:** VERY LOW

**Dependencies:** None - standalone workflow change

**Review Requirements:**

- At least 1 approval recommended
- CI/CD validation required
