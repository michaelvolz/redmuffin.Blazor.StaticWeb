---
title: feat: Split C# Standards into Granular Skills
type: feat
status: completed
date: 2026-04-06
origin: docs/brainstorms/2026-04-06-split-csharp-standards-granular-skills-requirements.md
---

# feat: Split C# Standards into Granular Skills

## Overview

Split the consolidated C# standards document (1,381 lines) into 14 granular skills with rm-guide-\* prefix, each loading precisely when relevant C# development tasks occur, prioritizing zero false negatives and minimizing token usage.

## Problem Frame

The consolidated C# standards document is too large to load efficiently as a single skill, causing token waste and slower responses. We need a system to split it into granular skills that load precisely when needed.

## Requirements Trace

- R1. Split C# standards document into separate skill guides with rm-guide-\* prefix
- R2. Skills load based on file types (_.cs, _.scss, \*.razor) and folders (test/)
- R3. Zero false negatives - skills must load when relevant guidance is needed
- R4. Expand same granularity system to all standards areas (markdown, UI styling, REST APIs, documentation, GitHub Actions, performance, security)
- R5. Use correct suffixes for reviewers and analyzers in subfolders
- R6. Start with Approach 1: File-type + folder-based triggers for C# standards proof of concept
- R7. Each skill has precise trigger description for reliable loading
- R8. Skills contain only essential content, no duplication
- R9. Reference skills in AGENTS.md with trigger conditions

## Scope Boundaries

- Focus on C# standards first as proof of concept
- File-type and folder triggers only (no complex keyword detection initially)
- Maintain existing standards content accuracy

## Context & Research

### Relevant Code and Patterns

- Existing skill loading system in .opencode/skills/
- AGENTS.md skill references table
- Consolidated C# standards in docs/solutions/best-practices/csharp-standards-final-2026-04-06.md

### Institutional Learnings

- Previous successful skill splitting implementations
- Zero false negatives prioritized in skill design
- Essential content distillation patterns

### External References

- OpenCode skill loading mechanisms
- AI agent framework best practices for conditional loading

## Key Technical Decisions

- Use rm-guide-\* prefix for consistency with existing guides
- Precise trigger descriptions to ensure zero false negatives
- Essential content only to minimize token usage
- File-type and folder-based triggers for reliability

## Open Questions

### Resolved During Planning

- Skill naming: rm-guide-\* prefix
- Trigger precision: File-type + folder-based
- Content scope: Essential guidance only

### Deferred to Implementation

- Exact trigger wording optimization
- Cross-skill reference accuracy

## Implementation Units

- [x] **Unit 1: Analyze Source Document**

**Goal:** Identify sections in the consolidated C# standards document suitable for splitting into granular skills

**Requirements:** R1, R6

**Dependencies:** None

**Files:**

- Read: docs/solutions/best-practices/csharp-standards-final-2026-04-06.md

**Approach:**

- Review each section for independence and trigger conditions
- Identify 14 logical skill boundaries

**Patterns to follow:**

- Existing skill structure in .opencode/skills/

**Test scenarios:**

- Test expectation: none -- analysis only

**Verification:**

- 14 skill boundaries identified with clear trigger conditions

- [x] **Unit 2: Create Individual Skills**

**Goal:** Create 14 granular rm-guide-\* skills with precise triggers and essential content

**Requirements:** R1, R2, R3, R7, R8

**Dependencies:** Unit 1

**Files:**

- Create: .opencode/skills/rm-guide-naming/SKILL.md
- Create: .opencode/skills/rm-guide-csharp-features/SKILL.md
- Create: .opencode/skills/rm-guide-async/SKILL.md
- Create: .opencode/skills/rm-guide-namespaces/SKILL.md
- Create: .opencode/skills/rm-guide-logging/SKILL.md
- Create: .opencode/skills/rm-guide-di/SKILL.md
- Create: .opencode/skills/rm-guide-testing/SKILL.md
- Create: .opencode/skills/rm-guide-warnings/SKILL.md
- Create: .opencode/skills/rm-guide-blazor/SKILL.md
- Create: .opencode/skills/rm-guide-azure-functions/SKILL.md
- Create: .opencode/skills/rm-guide-architecture/SKILL.md
- Create: .opencode/skills/rm-guide-config/SKILL.md
- Create: .opencode/skills/rm-guide-dotnet9/SKILL.md
- Create: .opencode/skills/rm-guide-code-quality/SKILL.md

**Approach:**

- Extract essential content from consolidated document
- Add precise trigger descriptions
- Include code examples and patterns

**Patterns to follow:**

- Existing skill templates
- AGENTS.md skill reference format

**Test scenarios:**

- Test expectation: none -- skill creation

**Verification:**

- 14 skills created with proper structure and triggers

- [x] **Unit 3: Update AGENTS.md**

**Goal:** Add references to all new skills in AGENTS.md skill references table

**Requirements:** R9

**Dependencies:** Unit 2

**Files:**

- Modify: AGENTS.md

**Approach:**

- Add entries for each rm-guide-\* skill with trigger conditions

**Patterns to follow:**

- Existing skill reference format in AGENTS.md

**Test scenarios:**

- Test expectation: none -- documentation update

**Verification:**

- All 14 skills referenced in AGENTS.md

- [x] **Unit 4: Test Skill Loading**

**Goal:** Verify skills load correctly and build passes

**Requirements:** R3, R4

**Dependencies:** Unit 3

**Files:**

- Test: dotnet build and dotnet test

**Approach:**

- Run build and tests to ensure no regressions
- Verify skill loading mechanisms work

**Patterns to follow:**

- Project build and test procedures

**Test scenarios:**

- Happy path: Build succeeds with all tests passing
- Edge case: Skills load on relevant triggers
- Error path: No false positives in loading

**Verification:**

- Build successful, all tests pass, skills load as expected

## System-Wide Impact

- **Interaction graph:** Skill loading system enhanced with granular triggers
- **Error propagation:** No impact on existing error handling
- **State lifecycle risks:** None
- **API surface parity:** Consistent with existing skill system
- **Integration coverage:** Skill loading integration tested
- **Unchanged invariants:** Existing skills and loading mechanisms unchanged

## Risks & Dependencies

| Risk                                             | Mitigation                                 |
| ------------------------------------------------ | ------------------------------------------ |
| Skill triggers too broad causing false positives | Precise trigger descriptions tested        |
| Essential content omitted                        | Cross-reference with consolidated document |
| Build failures from skill changes                | Run build and tests after each unit        |

## Documentation / Operational Notes

- Consolidated document remains as archival reference
- Skills provide targeted guidance for C# development
- Expandable system for other standards areas

## Sources & References

- **Origin document:** docs/brainstorms/2026-04-06-split-csharp-standards-granular-skills-requirements.md
- Related code: .opencode/skills/ structure
- Related PRs/issues: None
- External docs: OpenCode skill loading documentation
