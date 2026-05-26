# Plan: Migrate tasks/ to docs/ (Delete tasks/ folder)

**Date:** 2026-05-26
**Status:** planning

## Goal

Read all 75 files in `tasks/`, consolidate valuable content into knowledge docs under
`docs/`, then delete the entire `tasks/` folder.

## Decisions

| #   | Question               | Decision                                                                                                                   |
| --- | ---------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| 1   | Value criteria         | PRDs are valuable. Tasklists may have supplemental info worth consolidating into PRDs. Tasklists themselves are worthless. |
| 2   | Subfolder structure    | Merge into existing `docs/` subfolders by domain. No new `tasks/` directory under `docs/`.                                 |
| 3   | Naming convention      | Standard `YYYY-MM-DD-description-name.md`                                                                                  |
| 4   | Date source            | Earliest commit date for each PRD file from git history                                                                    |
| 5   | Content format         | Rewrite to knowledge format: problem, root cause, solution, prevention                                                     |
| 6   | Scope                  | ALL PRDs. All were completed. Migrate even if no longer visible in code.                                                   |
| 7   | Older tasks duplicates | Deduplicate by merging unique content, one copy survives                                                                   |
| 8   | Infrastructure PRDs    | Fit into existing categories (workflow-issues, architecture-patterns, tooling-decisions)                                   |
| 9   | Execution strategy     | Parallel subagent batches by domain category                                                                               |
| 10  | Final state            | `tasks/` folder fully deleted                                                                                              |

## Domain Categories (Target Folders)

| Category                          | Target folder                           | PRD count (est.) |
| --------------------------------- | --------------------------------------- | ---------------- |
| Blazor WASM performance / loading | `docs/solutions/performance-issues/`    | ~5               |
| Azure SWA / infrastructure        | `docs/solutions/architecture-patterns/` | ~4               |
| Testing / test doubles            | `docs/solutions/testing-patterns/`      | ~4               |
| CI/CD / GitHub Actions            | `docs/solutions/workflow-issues/`       | ~3               |
| SCSS / styling                    | `docs/solutions/developer-experience/`  | ~2               |
| Image handling / Raindrop API     | `docs/solutions/features/`              | ~4               |
| Code quality / standards          | `docs/solutions/architecture-patterns/` | ~3               |
| Miscellaneous older tasks         | various                                 | ~12              |

## Per-PRD Workflow

For each PRD:

1. Read PRD and its paired tasklist
2. Extract git history date: `git log --diff-filter=A --follow --format=%ad --date=short -- tasks/path/to/prd.md | tail -1`
3. Consolidate any unique tasklist info into the PRD content
4. Rewrite into knowledge format:
   - **Problem** — what needed solving (from PRD Overview/Requirements)
   - **Root cause** — why it was a problem (from PRD Background)
   - **Solution** — what was built (from PRD Implementation/Approach)
   - **Prevention** — patterns, rules, or tests created (from PRD Success Metrics/Risks)
5. Place in correct `docs/solutions/{category}/` subfolder
6. Delete both PRD and tasklist from `tasks/`

## Batches (Parallel Subagents)

### Batch 1 — Blazor WASM Performance

- PRD-006, PRD-007, PRD-014, PRD-018, PRD-020
- Target: `docs/solutions/performance-issues/`

### Batch 2 — Testing

- PRD-008, PRD-011, PRD-012, lightmock-migration
- Target: `docs/solutions/testing-patterns/`

### Batch 3 — Azure / CI/CD / Infrastructure

- PRD-005, PRD-009, PRD-015, PRD-016, PRD-017, HttpClientFactory-Migration
- Target: `docs/solutions/workflow-issues/`, `docs/solutions/architecture-patterns/`

### Batch 4 — Image Handling / API

- PRD-002, PRD-003, simple-image-validation, fixing-articles-image-delay-bug, open-graph-image-fallback
- Target: `docs/solutions/features/`

### Batch 5 — Code Quality / Standards

- PRD-001, PRD-004, PRD-013, cleanup-and-optimize-scss-code, fix-build-warnings, displaywarnings
- Target: `docs/solutions/architecture-patterns/`, `docs/solutions/developer-experience/`

### Batch 6 — Older Tasks (Changelog, SCSS, misc)

- All Changelog-\* PRDs, scss-component-validation-report, implementing-code-coverage, simple-integration-test, delete-opengraph-infrastructure, plan-instruction-architecture-overhaul, PRD-010
- Deduplicate first (uppercase vs lowercase), then migrate
- Target: various

## Verification

After all batches:

1. `find tasks/ -name '*.md' | wc -l` → `0`
2. All new docs have valid YAML frontmatter with `date`, `tags`, `problem_type`
3. All new docs cross-referenced from at least one existing doc or skill
4. Git history preserves originals
