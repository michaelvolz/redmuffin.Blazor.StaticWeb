---
title: "fix: Narrow and modernize rm-dev-workflows"
type: fix
status: superseded
date: 2026-04-03
origin: docs/brainstorms/2026-04-03-instruction-architecture-overhaul-requirements.md
superseded-by: docs/plans/2026-04-04-009-fix-prevent-about-blank-tabs-in-dev-chrome-plan.md
---

# Fix: Narrow and Modernize rm-dev-workflows

## Overview

`rm-dev-workflows` is carrying too much unrelated guidance and a few stale assumptions. The skill should stay the canonical repo-local reference for process management, port/Windows workflow, browser tab hygiene, and tool-selection guidance — but it needs to be trimmed so it favors OpenCode's builtin tools first, avoids deprecated Windows commands, and stops reading like a general "how to use the terminal" manual.

## Problem Frame

The current skill mixes four concerns that should be tighter and more explicit:

1. Windows process handling still leans on deprecated or brittle commands (`wmic`, `taskkill`, `netstat | findstr`).
2. Search/tool guidance gives `es.exe` too much importance even though OpenCode already provides `glob`, `grep`, `list`, and `read`.
3. Chrome DevTools tab guidance is useful but overlong for a workflow skill and risks overlapping with cleanup/browser lifecycle guidance.
4. The trigger language and cross-references need to stay precise so the skill fires for the right workflow tasks without becoming a catch-all.

The goal is to keep the skill useful while making it smaller, more current, and more faithful to how OpenCode actually works today.

## Requirements Trace

- R3. Use OpenCode's lazy-loaded skill model and keep the workflow content focused, not monolithic.
- R5. Base standard guidance on current OpenCode docs and current Windows guidance, not old command folklore.
- R6. Keep skill triggers specific and exclusive enough that the skill fires when needed and not otherwise.
- R7. Keep the root instruction surface small; do not spread workflow detail back into `AGENTS.md`.
- R9. Preserve `rm-dev-workflows` as the home for process-management tables and the search-tool decision tree, but tighten and modernize both.

## Scope Boundaries

- **In scope:** `.opencode/skills/rm-dev-workflows/SKILL.md`, `AGENTS.md`, `docs/OpencodeCatalog.md`
- **Out of scope:** app code, `rm-cleanup` internals, OpenCode source/config changes, and broad docs rewrites unrelated to this skill's references

## Context & Research

### Relevant Code and Patterns

- `.opencode/skills/rm-dev-workflows/SKILL.md` — current skill being refined.
- `.opencode/skills/rm-cleanup/SKILL.md` — already contains the stronger process-termination precedent to mirror.
- `AGENTS.md` — routing/index surface that should stay brief and consistent with the skill.
- `docs/OpencodeCatalog.md` — contains stale or legacy skill-path references that should be aligned if this skill is renamed or re-scoped.

### Institutional Learnings

- `docs/solutions/integration-issues/fix-skill-overtriggering-2026-04-03.md` — skill triggers should be specific + exclusive + escape hatch; broad absolute language causes over-triggering.
- `docs/solutions/integration-issues/opencode-instruction-management-lessons-2026-04-03.md` — dead frontmatter should be removed, and trigger density must compete with global skills.
- `docs/solutions/integration-issues/opencode-instruction-architecture-pattern-2026-04-03.md` — OpenCode instruction architecture should stay lazy-loaded, with clear namespace separation and small root guidance.
- `docs/solutions/integration-issues/opencode-instruction-management-lessons-2026-04-03.md` — workflow instructions belong in skills/commands, not in oversized reference docs.
- `docs/brainstorms/2026-04-03-instruction-architecture-findings.md` — `rm-dev-workflows` is the intended home for process-management and tool-selection guidance.

### External References

- https://opencode.ai/docs/tools/ — confirms builtin `bash`, `read`, `grep`, `glob`, and `list` tools are first-class.
- https://opencode.ai/docs/permissions — confirms tool permissions are native OpenCode behavior, not something the skill should re-explain at length.
- https://opencode.ai/docs/windows-wsl — recommends WSL on Windows and reinforces that modern guidance should avoid leaning on old Windows-only command folklore.
- https://support.microsoft.com/en-us/topic/windows-management-instrumentation-command-line-wmic-removal-from-windows-e9e83c7f-4992-477f-ba1d-96f694b8665d — WMIC is deprecated/removed on newer Windows builds.

## Key Technical Decisions

- **Use builtin OpenCode search/file tools as the first-line guidance.**
  - Rationale: `glob`, `grep`, `list`, and `read` already cover the common cases. `es.exe` should be optional at most, not a requirement.
- **Replace deprecated Windows process recipes with modern PowerShell-native wording.**
  - Rationale: `wmic` is no longer a safe default, and `taskkill`/`netstat | findstr` are more brittle than the current repo guidance merits.
- **Keep Chrome DevTools tab hygiene, but compress it to a short canonical rule.**
  - Rationale: tab reuse / no-blank-tab guidance belongs here, but detailed browser lifecycle behavior should stay with cleanup workflows.
- **Keep trigger language narrow and explicit.**
  - Rationale: the skill should trigger on process management, port checks, Windows dev workflow, and search-tool selection — not on every shell-adjacent task.
- **Sync any cross-references in the catalog and routing docs.**
  - Rationale: stale path/name references create discoverability drift even when the skill content is corrected.

## Open Questions

### Resolved During Planning

- The skill should remain `rm-dev-workflows`; this is a content/update pass, not a rename.
- The skill should keep a browser-tab hygiene note, but only as a concise canonical reminder.

### Deferred to Implementation

- Whether `es.exe` should be retained as a short optional escape hatch or removed entirely from the final wording.
- Whether any additional stale references beyond `docs/OpencodeCatalog.md` should be updated in the same pass or left for a broader instruction-architecture cleanup.

## Implementation Units

- [ ] **Unit 1: Modernize process-management guidance for Windows dev sessions**

**Goal:** Replace stale PID/process guidance with modern, Windows-appropriate instructions that match the repo's current cleanup precedent.

**Requirements:** R5, R6, R9

**Dependencies:** None

**Files:**

- Modify: `.opencode/skills/rm-dev-workflows/SKILL.md`
- Modify: `AGENTS.md` if the skill reference row needs to mirror the narrowed wording

**Approach:**

- Rewrite the process-management section around concrete Windows-native process identification and termination language.
- Remove or rephrase any `wmic`/`taskkill`/`netstat | findstr`-style examples that are now stale or overly specific.
- Keep the guidance aligned with the cleanup skill's process model so the two instructions do not disagree.

**Patterns to follow:**

- `.opencode/skills/rm-cleanup/SKILL.md` for the current PowerShell/CIM process-handling style.
- `docs/solutions/integration-issues/opencode-instruction-management-lessons-2026-04-03.md` for narrow, non-contradictory trigger phrasing.

**Test scenarios:**

- Happy path: a workflow about finding or stopping a dev process points the user toward modern Windows process identification, not WMIC.
- Edge case: a VS-owned process is clearly separated from an agent-owned one using the repo's current ownership rule.
- Error path: if the user is on a Windows setup where WMIC is unavailable, the skill still reads as valid and actionable.
- Integration: the process guidance stays consistent with `rm-cleanup` instead of contradicting it.

**Verification:**

- The section no longer depends on deprecated or brittle Windows command recipes.
- The guidance reads as current, concise, and compatible with the cleanup workflow.

- [ ] **Unit 2: Rebuild tool-selection guidance around builtin OpenCode tools first**

**Goal:** Make builtin OpenCode file/search tools the primary recommendation and demote `es.exe` to a niche optional helper, if it remains at all.

**Requirements:** R3, R5, R6, R9

**Dependencies:** None

**Files:**

- Modify: `.opencode/skills/rm-dev-workflows/SKILL.md`

**Approach:**

- Replace the current search-tool matrix with a shorter decision rule that starts with OpenCode's builtin tools.
- Keep `es.exe` only as an external-filesystem escape hatch if the final wording still needs it.
- Make the guidance read like practical routing, not a general tutorial on search tools.

**Patterns to follow:**

- `opencode.ai/docs/tools` for the builtin tool set and the intended first-line behavior.
- Existing repo style for short, trigger-driven operational guidance.

**Test scenarios:**

- Happy path: a workspace-local file search defaults to builtin `glob`/`grep`/`list`/`read` guidance.
- Edge case: a broader filesystem search outside the workspace can still mention `es.exe` as optional, not mandatory.
- Error path: the skill no longer claims `es.exe` must be on every contributor's PATH.
- Integration: the search guidance does not conflict with OpenCode's own builtin tool docs.

**Verification:**

- The skill no longer implies a hard dependency on external search tooling.
- The guidance is shorter and easier to trigger correctly.

- [ ] **Unit 3: Compress browser tab hygiene to one canonical note**

**Goal:** Keep the useful no-blank-tab / reuse-existing-tab guidance while removing the overgrown lifecycle commentary.

**Requirements:** R3, R6, R9

**Dependencies:** Unit 2

**Files:**

- Modify: `.opencode/skills/rm-dev-workflows/SKILL.md`
- Modify: `docs/OpencodeCatalog.md` if the catalog references this skill's browser guidance or path name

**Approach:**

- Reduce the Chrome DevTools section to a concise rule set: reuse existing tabs when possible, always target a URL, and never leave blank tabs open.
- Remove any incidental lifecycle detail that belongs in `rm-cleanup` or browser-specific tooling docs.
- Keep the section canonical, but clearly subordinate to the cleanup skill for teardown behavior.

**Patterns to follow:**

- The current `rm-dev-workflows` tab-management section.
- `docs/plans/2026-04-03-002-fix-chrome-devtools-tab-reopen-plan.md` for the canonical tab-hygiene boundary.

**Test scenarios:**

- Happy path: a browser-task prompt tells the agent to reuse an existing page instead of creating a blank one.
- Edge case: a new page is genuinely needed, and the guidance still says to target the final URL directly.
- Error path: cleanup/teardown behavior is not duplicated here, so the skill does not drift into browser lifecycle management.
- Integration: `rm-dev-workflows` and `rm-cleanup` remain aligned on tab hygiene boundaries.

**Verification:**

- The browser guidance is still present, but it is short enough to stay readable and trigger-relevant.
- The cleanup workflow remains the owner of teardown detail.

- [ ] **Unit 4: Sync skill references and catalog entries with the updated wording**

**Goal:** Prevent discoverability drift by aligning the skill reference table and catalog entries with the new canonical wording and path names.

**Requirements:** R6, R7

**Dependencies:** Units 1-3

**Files:**

- Modify: `AGENTS.md`
- Modify: `docs/OpencodeCatalog.md`

**Approach:**

- Update any `rm-dev-workflows` summary text in `AGENTS.md` so it matches the refined skill scope.
- Fix any stale `dev-workflows` path/name references in the catalog so they point at the current `rm-` skill.
- Keep both files short and routing-oriented; they should point to the skill, not duplicate it.

**Patterns to follow:**

- `AGENTS.md` as a compact routing/index file.
- `docs/OpencodeCatalog.md` as a discoverability reference, not a workflow manual.

**Test scenarios:**

- Happy path: the catalog and routing docs point to the same skill name and path.
- Edge case: a new contributor can find the skill without encountering the old `dev-workflows` path spelling.
- Integration: AGENTS, catalog, and skill text all tell the same story about where process/search/tab guidance lives.

**Verification:**

- No stale path names remain for this skill in the updated reference surfaces.
- The skill remains easy to find and its purpose is easy to distinguish from neighboring skills.

## System-Wide Impact

- **Interaction graph:** `opencode` skill matching → `rm-dev-workflows` trigger text → day-to-day terminal/process/browser/search workflows.
- **Cross-skill boundary:** `rm-cleanup` owns teardown; `rm-dev-workflows` owns the canonical day-to-day workflow hints.
- **Discoverability surface:** `AGENTS.md` and `docs/OpencodeCatalog.md` should point to the same skill name and scope.
- **Unchanged invariants:** no app code changes, no OpenCode config changes, and no change to the repo's existing cleanup skill ownership model.

## Risks & Dependencies

| Risk                                                                  | Mitigation                                                                                             |
| --------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------ |
| The skill gets trimmed so far that useful operational context is lost | Preserve the minimum canonical rules for process handling, search-tool choice, and tab hygiene         |
| `es.exe` removal makes some workflows harder outside the workspace    | Keep it only as an optional escape hatch if it still adds value                                        |
| Trigger wording becomes too broad again                               | Keep the description specific, exclusive, and aligned with the repo's instruction-architecture lessons |
| Catalog/routing docs drift from the skill after the rewrite           | Update the reference surfaces in the same pass                                                         |

## Documentation / Operational Notes

- Keep `rm-dev-workflows` as the single repo-local workflow reference for process management, search-tool selection, and tab hygiene.
- Avoid reintroducing startup/run guidance here; that belongs in the existing repo docs, not in this skill.
- If the path-name cleanup expands beyond `docs/OpencodeCatalog.md`, treat it as part of the broader instruction-architecture effort rather than this skill update alone.

## Sources & References

- **Origin document:** `docs/brainstorms/2026-04-03-instruction-architecture-overhaul-requirements.md`
- Related findings: `docs/brainstorms/2026-04-03-instruction-architecture-findings.md`
- Related code: `.opencode/skills/rm-dev-workflows/SKILL.md`
- Related code: `.opencode/skills/rm-cleanup/SKILL.md`
- Related docs: `AGENTS.md`, `docs/OpencodeCatalog.md`
- External docs: https://opencode.ai/docs/tools/
- External docs: https://opencode.ai/docs/permissions
- External docs: https://opencode.ai/docs/windows-wsl
- External docs: https://support.microsoft.com/en-us/topic/windows-management-instrumentation-command-line-wmic-removal-from-windows-e9e83c7f-4992-477f-ba1d-96f694b8665d
