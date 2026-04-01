---
title: Restructure AGENTS.md into Skill-Referenced Modules
type: refactor
status: active
date: 2026-04-01
origin: docs/brainstorms/2026-04-01-documentation-consolidation-requirements.md
---

# Restructure AGENTS.md into Skill-Referenced Modules

## Overview

Extract duplicated content from AGENTS.md (379 lines) into existing and new skill files, reducing AGENTS.md to ~150 lines as a hybrid routing index plus essential quick-reference content. Zero content loss — every rule, pattern, and workflow preserved in exactly one location.

## Problem Frame

AGENTS.md contains ~30% content that duplicates existing skills (csharp-standards, testing, dotnet), ~30% that belongs in new skills, and ~40% that should remain as critical routing content. This creates two sources of truth, degrades agent accuracy, and increases maintenance burden. (see origin: docs/brainstorms/2026-04-01-documentation-consolidation-requirements.md)

## Requirements Trace

- R1. Extract duplicated content from AGENTS.md into appropriate skill files
- R2. Audit and reconcile existing skills (csharp-standards, testing, dotnet) against AGENTS.md PATTERNS
- R3. Create three new skills: dev-workflows, security-secrets, output-style
- R4. Zero content loss — every rule/pattern/workflow in exactly one location
- R5. Target AGENTS.md at ~150 lines
- R6. AGENTS.md becomes hybrid: routing index + essential quick-reference
- R7. Keep critical workflows in AGENTS.md (research-first, infra error, frontend debug, dev server startup)
- R8. Keep boundaries section in AGENTS.md (ALWAYS, ASK FIRST, NEVER)
- R9. dev-workflows skill: process management tables, web search decision tree
- R10. security-secrets skill: secret management patterns, MCP env vars, zero-tolerance rules
- R11. output-style skill: formatting rules, naming conventions, output style constraints, C# 12/13, nullable
- R12. csharp-standards: audit and reconcile naming, nullable, formatting
- R13. testing: audit and reconcile test double naming, mock examples
- R14. dotnet: audit and reconcile dev modes table, test filter commands
- R15. Content audit verification (diff before/after, verify skill files exist)
- R16. Manual skill loading verification (opencode session checklist)
- R17. COMMANDS table and STACK table remain in AGENTS.md

## Scope Boundaries

- Does NOT change C# code, test files, or build configuration
- Does NOT modify README.md
- Does NOT change opencode.json or skill invocation mechanism
- Does NOT consolidate PRD documents
- Does NOT create .github/copilot-instructions.md

## Context & Research

### Relevant Code and Patterns

- **11 existing skills** in `.opencode/skills/` with YAML frontmatter pattern (`name`, `description`, `invocable: false`)
- **8 snippets** in `.opencode/snippet/` — separate lighter-weight system, not affected
- **Skill auto-discovery** — no registration in opencode.json needed
- **AGENTS.md sections**: CRITICAL (17 lines), COMMANDS (27 lines), STACK (9 lines), STRUCTURE (29 lines), WORKFLOWS (86 lines), PATTERNS (127 lines), BOUNDARIES (51 lines), OUTPUT STYLE (11 lines), CONTEXT (8 lines)
- **Heavy overlap areas**: naming conventions, nullable rules, LoggerMessage, LightMock, test quality, file-scoped namespaces, nameof
- **Unique to AGENTS.md**: research-first protocol, infra error protocol, frontend debug protocol, dev server startup protocol, web search decision tree, secret management table, dev modes table, process management tables, output style rules, boundaries lists

### Institutional Learnings

- **Copilot Instructions Restructuring Plan** exists (docs/Copilot_Instructions_Restructuring_Plan.md) — 12-step migration, planned but incomplete. This restructure should be compatible.
- **Mock naming conflict** (docs/mock-naming-conventions.md) conflicts with AGENTS.md — flagged for reconciliation but out of scope for this plan.
- **Useless rules analysis** (docs/useless-rules-analysis.md) — 26 rules identified as AI-instinctive, useful for avoiding bloat in new skills.

## Key Technical Decisions

- **Workflows stay in AGENTS.md**: Research-first, infra error, frontend debug, dev server startup protocols remain in AGENTS.md for quick access. Rationale: Most frequently referenced sections during agent sessions.
- **3 new skills created**: dev-workflows, security-secrets, output-style. Rationale: Complete separation of concerns; each skill has single trigger condition.
- **COMMANDS and STACK tables stay in AGENTS.md**: Quick reference content agents need constantly.
- **BOUNDARIES section stays in AGENTS.md**: Quick-reference guardrails, even though it duplicates content from skills. Rationale: Deliberate summary layer exempt from "exactly one location" rule.
- **Content audit replaces dotnet build verification**: Skill files are markdown — cannot affect .NET build. Verification is content audit (diff before/after, verify files exist).

## Open Questions

### Resolved During Planning

- **Should dev-workflows skill include process management tables or reference AGENTS.md?**: Include directly. Since workflows stay in AGENTS.md, dev-workflows covers only process management tables and web search decision tree — reference content agents need when managing processes.
- **Should output-style skill include full C# 12/13 feature list or reference official docs?**: Include directly. It's 5 bullet points of quick reference content that agents need during coding sessions.
- **How does opencode validate skill loading?**: Skills are auto-discovered from `.opencode/skills/`. No registration needed. Verification: manual checklist — start opencode session, trigger each skill by description, confirm it loads.

## Implementation Units

- [ ] **Unit 1: Create output-style skill**

**Goal:** Create new skill file containing formatting rules, naming conventions, output style constraints, C# 12/13 features, and nullable reference types extracted from AGENTS.md PATTERNS and OUTPUT STYLE sections.

**Requirements:** R3, R4, R11

**Dependencies:** None

**Files:**

- Create: `.opencode/skills/output-style/SKILL.md`
- Modify: `AGENTS.md` (remove PATTERNS formatting/naming/C#/nullable content, remove OUTPUT STYLE section)

**Approach:**

1. Create skill directory and SKILL.md with proper YAML frontmatter
2. Extract formatting rules (tab indent, 4-space .razor, 2-space .csproj, max 160 chars, brace on new line)
3. Extract naming conventions (PascalCase, camelCase, UpperCamelCase*, I-prefix, [Class]*[Type])
4. Extract C# 12/13 features (primary constructors, collection expressions, ref readonly, pattern matching, nameof)
5. Extract nullable reference types rules (declare non-nullable, is null/is not null)
6. Extract output style rules (line width, empty lines, paragraphs, voice, priority)
7. Update AGENTS.md to remove these sections and add skill reference

**Patterns to follow:**

- Existing skill frontmatter pattern (name, description, invocable: false)
- Skill content structure from csharp-standards SKILL.md

**Test scenarios:**

- Edge case: Skill file has valid YAML frontmatter with all required fields
- Happy path: Skill description accurately triggers on formatting/naming/output style queries

**Verification:**

- `.opencode/skills/output-style/SKILL.md` exists with valid frontmatter
- AGENTS.md no longer contains PATTERNS formatting/naming/C#/nullable content or OUTPUT STYLE section
- Content audit: all extracted content appears in new skill file

- [ ] **Unit 2: Create security-secrets skill**

**Goal:** Create new skill file containing secret management patterns, MCP env vars, zero-tolerance rules, and devcontainer security reference extracted from AGENTS.md PATTERNS and CONTEXT sections.

**Requirements:** R3, R4, R10

**Dependencies:** None

**Files:**

- Create: `.opencode/skills/security-secrets/SKILL.md`
- Modify: `AGENTS.md` (remove secret management table, update CONTEXT section reference)

**Approach:**

1. Create skill directory and SKILL.md with proper YAML frontmatter
2. Extract secret management table (Environment Variables, VS Code DevContainer, VS Code Copilot MCP, GitHub Secrets, Azure Key Vault, User Secrets)
3. Extract MCP env var patterns (correct vs incorrect examples)
4. Extract zero-tolerance rules from BOUNDARIES NEVER section (commit secrets, hardcode secrets, file-based secrets)
5. Add reference to .devcontainer/SECURITY.md
6. Update AGENTS.md to remove secret management content and add skill reference

**Patterns to follow:**

- Existing skill frontmatter pattern
- Skill content structure from csharp-standards SKILL.md

**Test scenarios:**

- Edge case: Skill file has valid YAML frontmatter with all required fields
- Happy path: Skill description accurately triggers on secret/security/MCP env var queries

**Verification:**

- `.opencode/skills/security-secrets/SKILL.md` exists with valid frontmatter
- AGENTS.md no longer contains secret management table
- Content audit: all extracted content appears in new skill file

- [ ] **Unit 3: Create dev-workflows skill**

**Goal:** Create new skill file containing process management tables (VS-owned vs agent-owned) and web search decision tree extracted from AGENTS.md WORKFLOWS section.

**Requirements:** R3, R4, R9

**Dependencies:** None

**Files:**

- Create: `.opencode/skills/dev-workflows/SKILL.md`
- Modify: `AGENTS.md` (remove process management tables, remove web search decision tree)

**Approach:**

1. Create skill directory and SKILL.md with proper YAML frontmatter
2. Extract process management tables (VS-owned vs agent-owned indicators, safe to kill rules, PID tracking)
3. Extract web search decision tree (Context7, brave_web_search, websearch, sequentialthinking)
4. Update AGENTS.md to remove these sections and add skill reference

**Patterns to follow:**

- Existing skill frontmatter pattern
- Skill content structure from csharp-standards SKILL.md

**Test scenarios:**

- Edge case: Skill file has valid YAML frontmatter with all required fields
- Happy path: Skill description accurately triggers on process management/web search tool queries

**Verification:**

- `.opencode/skills/dev-workflows/SKILL.md` exists with valid frontmatter
- AGENTS.md no longer contains process management tables or web search decision tree
- Content audit: all extracted content appears in new skill file

- [ ] **Unit 4: Audit and reconcile existing skills**

**Goal:** Audit csharp-standards, testing, and dotnet skills against AGENTS.md PATTERNS content, updating only where AGENTS.md has newer/different information.

**Requirements:** R2, R4, R12, R13, R14

**Dependencies:** Units 1-3 (new skills created first to avoid content gaps)

**Files:**

- Modify: `.opencode/skills/csharp-standards/SKILL.md`
- Modify: `.opencode/skills/testing/SKILL.md`
- Modify: `.opencode/skills/dotnet/SKILL.md`
- Modify: `AGENTS.md` (remove duplicated PATTERNS content)

**Approach:**

1. **csharp-standards audit**: Compare AGENTS.md PATTERNS (naming, nullable, formatting, file-scoped namespaces, nameof, LoggerMessage) against existing skill content. Update only where AGENTS.md has newer/different info. Most content already exists — this is reconciliation, not addition.
2. **testing audit**: Compare AGENTS.md PATTERNS (test quality, mocking, test double naming) against existing skill content. Update only where AGENTS.md has newer/different info. Mock examples and test categorization already exist.
3. **dotnet audit**: Compare AGENTS.md COMMANDS (test filter commands, dev modes table) against existing skill content. Extract specific test filter commands (Smoke, Feature:Home, etc.) that don't exist in dotnet skill. Dev modes table already exists.
4. Update AGENTS.md to remove all PATTERNS content that is now covered by skills.

**Patterns to follow:**

- Existing skill content structure
- AGENTS.md PATTERNS section as source for any missing content

**Test scenarios:**

- Happy path: Each skill file contains all content from corresponding AGENTS.md PATTERNS section
- Edge case: No duplicate content within skill files after reconciliation

**Verification:**

- Each skill file contains all relevant content from AGENTS.md PATTERNS
- AGENTS.md PATTERNS section removed entirely
- Content audit: zero content loss — all PATTERNS content appears in exactly one skill

- [ ] **Unit 5: Restructure AGENTS.md**

**Goal:** Rewrite AGENTS.md as hybrid routing index (~150 lines) with skill references, keeping CRITICAL, COMMANDS, STACK, STRUCTURE, WORKFLOWS (minus process management/web search), BOUNDARIES, and CONTEXT (minus secret management).

**Requirements:** R4, R5, R6, R7, R8, R15, R17

**Dependencies:** Units 1-4 (all skills created and reconciled)

**Files:**

- Modify: `AGENTS.md` (complete restructure)

**Approach:**

1. Keep CRITICAL section (17 lines) — hard rules that apply to all agent sessions
2. Keep COMMANDS table (27 lines) — quick reference for build/test commands
3. Keep STACK table (9 lines) — essential framework context
4. Keep STRUCTURE section (29 lines) — directory layout, update skill list to include all 14 skills
5. Keep WORKFLOWS section minus process management tables and web search decision tree (moved to dev-workflows skill)
6. Keep BOUNDARIES section (51 lines) — quick-reference guardrails
7. Keep CONTEXT section minus secret management (moved to security-secrets skill)
8. Add skill references section pointing to all 14 skills with trigger conditions
9. Remove PATTERNS section entirely (content moved to skills)
10. Remove OUTPUT STYLE section entirely (moved to output-style skill)
11. Verify line count is ~150 lines

**Patterns to follow:**

- Existing AGENTS.md structure and formatting
- Skill reference pattern from existing skills

**Test scenarios:**

- Happy path: AGENTS.md is ~150 lines after restructure
- Edge case: All skill references are valid (skill files exist, descriptions match)
- Error path: No content loss — every rule/pattern/workflow from original AGENTS.md appears somewhere

**Verification:**

- AGENTS.md line count is ~150 lines (±20 lines acceptable)
- All 14 skills referenced correctly
- Content audit: diff original vs restructured AGENTS.md, verify zero content loss
- `dotnet build --verbosity quiet` passes (no C# changes expected)

- [ ] **Unit 6: Verification and cleanup**

**Goal:** Run content audit, verify skill files exist, update STRUCTURE section skill list, and perform manual skill loading verification.

**Requirements:** R4, R15, R16

**Dependencies:** Unit 5 (AGENTS.md restructured)

**Files:**

- Modify: `AGENTS.md` (update STRUCTURE section skill list)

**Approach:**

1. Run content audit: diff original AGENTS.md (from git) against restructured version
2. Verify each new skill file exists with expected content sections:
   - `.opencode/skills/output-style/SKILL.md`
   - `.opencode/skills/security-secrets/SKILL.md`
   - `.opencode/skills/dev-workflows/SKILL.md`
3. Update STRUCTURE section skill list from "csharp-standards, testing, ui-styling, dotnet, commits" to include all 14 skills
4. Manual skill loading verification: start opencode session, trigger each skill by description, confirm it loads
5. Run `dotnet build --verbosity quiet` to verify no build issues (should pass since no C# changes)

**Patterns to follow:**

- Content audit: git diff HEAD~1 AGENTS.md
- Skill verification: ls .opencode/skills/\*/SKILL.md

**Test scenarios:**

- Happy path: All 14 skill files exist and load correctly
- Error path: Content audit reveals no missing content

**Verification:**

- All 14 skill files exist in `.opencode/skills/`
- STRUCTURE section skill list updated to include all 14 skills
- Content audit shows zero content loss
- `dotnet build --verbosity quiet` passes with zero warnings (except IL2111)

## System-Wide Impact

- **Interaction graph:** AGENTS.md is listed in CI workflows (Azure Static Web Apps, CodeQL) as documentation trigger path. No workflow changes needed — file path unchanged.
- **Error propagation:** No runtime impact — documentation-only change.
- **State lifecycle risks:** None — no state changes.
- **API surface parity:** No API changes.
- **Integration coverage:** N/A — documentation-only change.
- **Unchanged invariants:** opencode.json, .mcp.json, skill invocation mechanism, README.md, PRD documents, .github/copilot-instructions.md (planned separately).

## Risks & Dependencies

| Risk                                               | Mitigation                                                                                |
| -------------------------------------------------- | ----------------------------------------------------------------------------------------- |
| Content loss during migration                      | Content audit (R15) — diff original vs restructured AGENTS.md before commit               |
| Skill description mismatch causing failed triggers | Manual skill loading verification (R16) — test each skill in opencode session             |
| BOUNDARIES section duplication with skills         | Accepted — BOUNDARIES is deliberate summary layer exempt from "exactly one location" rule |
| STRUCTURE section skill list becomes stale         | Updated in Unit 6 — includes all 14 skills after restructure                              |
| Copilot instructions restructuring conflict        | Out of scope — this restructure is compatible with future copilot-instructions.md effort  |

## Documentation / Operational Notes

- **Docs updated:** AGENTS.md (restructured), 3 new skill files created, 3 existing skills audited
- **Rollout:** No rollout needed — documentation-only change
- **Monitoring:** No monitoring needed
- **Support:** No support impacts

## Sources & References

- **Origin document:** [docs/brainstorms/2026-04-01-documentation-consolidation-requirements.md](docs/brainstorms/2026-04-01-documentation-consolidation-requirements.md)
- **Ideation document:** [docs/ideation/2026-04-01-open-ideation.md](docs/ideation/2026-04-01-open-ideation.md)
- **Copilot Instructions Restructuring Plan:** [docs/Copilot_Instructions_Restructuring_Plan.md](docs/Copilot_Instructions_Restructuring_Plan.md)
- **Skill Creator:** [.opencode/skills/skill-creator/SKILL.md](.opencode/skills/skill-creator/SKILL.md)
- **Existing skills:** `.opencode/skills/csharp-standards/`, `.opencode/skills/testing/`, `.opencode/skills/dotnet/`, `.opencode/skills/ui-styling/`, `.opencode/skills/commits/`, `.opencode/skills/markdown/`, `.opencode/skills/nuget-manager/`, `.opencode/skills/agent-markdown-optimizer/`, `.opencode/skills/create-prd/`, `.opencode/skills/generate-tasks/`, `.opencode/skills/skill-creator/`
