---
title: "feat: Create opencode plugins, skills, and snippets catalog"
type: feat
status: active
date: 2026-04-02
---

# Create opencode Plugins, Skills, and Snippets Catalog

## Overview

Create a comprehensive markdown document that catalogs all custom opencode plugins, skills, and snippets available both locally in this project and globally in the opencode configuration. The document will serve as a quick reference guide for developers working with opencode in this repository.

## Problem Frame

Developers working with opencode need a single source of truth for available skills and snippets. Currently, this information is scattered across multiple directories (`~/.config/opencode/skills/`, `.opencode/skills/`, `.opencode/snippet/`) with no consolidated reference. A well-organized catalog will improve discoverability and usage of these tools.

## Requirements Trace

- **R1**: Document placed in `docs/` folder with normal filename (not ALL_CAPS)
- **R2**: Two distinct sections: Local (project-specific) and Global (system-wide)
- **R3**: Each entry includes title (name) and short description
- **R4**: Document written with "word perfect" quality - professional, clear, concise
- **R5**: Custom shortcuts (snippets aliases) ordered from shortest to longest within each section

## Scope Boundaries

- **Include**: All skills and snippets found in local `.opencode/` and global `~/.config/opencode/` directories
- **Include**: Snippet aliases (shortcuts) sorted by length
- **Exclude**: Detailed usage instructions (link to source files instead)
- **Exclude**: Chat modes, GitHub prompts, or other opencode configurations beyond skills and snippets

## Context & Research

### Discovered Assets

**Local Skills (14)** located in `.opencode/skills/`:

- `agent-markdown-optimizer` - Transform markdown for AI agents
- `commits` - Conventional commit guidelines
- `create-prd` - Generate Product Requirements Documents
- `csharp-standards` - C# coding standards and analyzers
- `dev-workflows` - Process management and port handling
- `dotnet` - .NET project configuration and deployment
- `generate-tasks` - Generate task lists from PRDs
- `markdown` - Markdown content standards
- `nuget-manager` - NuGet package management
- `output-style` - C# formatting and naming conventions
- `security-secrets` - Secret management patterns
- `skill-creator` - Create and manage opencode skills
- `testing` - TUnit testing patterns
- `ui-styling` - UI styling standards for Blazor

**Local Snippets (12)** located in `.opencode/snippet/`:

- `async.md` - C# async programming (aliases: async, async-await)
- `cleanup-devserver.md` - Kill dev server (aliases: cleanup-dev, cdev)
- `commit.md` - Create conventional commits (aliases: commit, c)
- `csharp-standards.md` - C# coding standards (aliases: cs, csharp)
- `design-patterns.md` - Design pattern review (aliases: design-patterns)
- `dotnet-best-practices.md` - .NET best practices (aliases: dotnet)
- `dotnet-build.md` - .NET build commands (aliases: build)
- `find-bug.md` - Evidence-based troubleshooting (aliases: find-bug, bug, debug, fb)
- `testscope.md` - TestScope architecture (aliases: tunit, test, csharp-testing, testscope)
- `tunit.md` - TUnit testing framework (aliases: tunit, test)
- `verify-plan.md` - Interview before building (aliases: vplan)
- `verify-work-so-far.md` - Verify work quality (aliases: vwork)

**Global Skills (41)** located in `~/.config/opencode/skills/`:
Includes agent-browser, ce:brainstorm, ce:compound, ce:ideate, ce:plan, ce:review, ce:work, ce:work-beta, changelog, claude-permissions-optimizer, deploy-docs, dhh-rails-style, document-review, dspy-ruby, every-style-editor, feature-video, frontend-design, gemini-imagegen, git-clean-gone-branches, git-commit, git-commit-push-pr, git-worktree, lfg, onboarding, orchestrating-swarms, proof, rclone, report-bug-ce, reproduce-bug, resolve-pr-feedback, setup, slfg, test-browser, test-xcode, todo-create, todo-resolve, todo-triage, and more.

### File Structure Patterns

- **Skills**: Each skill is a directory containing `SKILL.md` with YAML frontmatter
- **Snippets**: Each snippet is a `.md` file with YAML frontmatter containing `aliases` array
- **Frontmatter format**:
  ```yaml
  ---
  name: skill-name
  description: Short description
  ---
  ```
  or for snippets:
  ```yaml
  ---
  aliases:
    - alias1
    - alias2
  description: Short description
  ---
  ```

## Key Technical Decisions

- **Document Structure**: Hierarchical with clear section headers for Local vs Global
- **Ordering**: Snippets sorted by shortest alias length (ascending) for quick reference
- **Quality Standard**: Professional technical documentation tone, concise descriptions
- **File Location**: `docs/OpencodeCatalog.md` (PascalCase, follows repo conventions like TestingGuidelines.md)

## Implementation Units

- [ ] **Unit 1: Collect and Parse Local Skills Metadata**

**Goal**: Gather title and description from all 14 local skills

**Requirements**: R1, R2, R3

**Dependencies**: None

**Files:**

- Read: `.opencode/skills/*/SKILL.md`

**Approach:**

- Parse YAML frontmatter from each local skill's SKILL.md
- Extract `name` and `description` fields
- Store in structured format for document generation

**Patterns to follow:**

- YAML frontmatter parsing (standard markdown metadata)

**Test scenarios:**

- Happy path: All 14 skills parsed successfully with valid metadata
- Edge case: Handle skills without description field (use empty string)
- Error path: Handle missing SKILL.md files gracefully

**Verification:**

- All 14 local skills identified with name and description

- [ ] **Unit 2: Collect and Parse Snippets Metadata**

**Goal**: Gather aliases and descriptions from all 12 local snippets

**Requirements**: R1, R2, R3, R5

**Dependencies**: Unit 1

**Files:**

- Read: `.opencode/snippet/*.md`

**Approach:**

- Parse YAML frontmatter from each snippet file
- Extract `aliases` array and `description` field
- Calculate shortest alias length for each snippet
- Sort snippets by shortest alias length (ascending)

**Patterns to follow:**

- Frontmatter parsing with list/array handling for aliases

**Test scenarios:**

- Happy path: All 12 snippets parsed with aliases sorted by length
- Edge case: Snippet with single alias
- Edge case: Snippet with multiple aliases of same length
- Edge case: Missing description field

**Verification:**

- All 12 snippets parsed
- Aliases sorted correctly (shortest first)
- Ordering verified: `c` < `cs` < `cdev` < `vwork` < `vplan` < etc.

- [ ] **Unit 3: Collect and Parse Global Skills Metadata**

**Goal**: Gather title and description from all 41 global skills

**Requirements**: R1, R2, R3

**Dependencies**: Unit 1

**Files:**

- Read: `~/.config/opencode/skills/*/SKILL.md`

**Approach:**

- Parse YAML frontmatter from each global skill's SKILL.md
- Extract `name` and `description` fields
- No sorting required (alphabetical or as-discovered is fine)

**Patterns to follow:**

- Same parsing logic as Unit 1

**Test scenarios:**

- Happy path: All 41 global skills parsed successfully
- Edge case: Handle skills with complex descriptions

**Verification:**

- All 41 global skills identified with name and description

- [ ] **Unit 4: Generate Markdown Document**

**Goal**: Create the final catalog document with word-perfect quality

**Requirements**: R1, R2, R3, R4, R5

**Dependencies**: Units 1, 2, 3

**Files:**

- Create: `docs/OpencodeCatalog.md`

**Approach:**

- Generate professional markdown with clear hierarchy
- Section 1: Local Skills and Snippets
  - Subsection: Skills (14 items)
  - Subsection: Snippets (12 items, sorted by alias length)
- Section 2: Global Skills (41 items)
- Use consistent formatting: `### Name` followed by description
- Include table of contents
- Professional tone throughout

**Patterns to follow:**

- Existing docs in repo (e.g., `docs/TestingGuidelines.md`)
- Markdown best practices: clear headers, consistent formatting

**Test scenarios:**

- Happy path: Document generated with all 67 items (14 local skills + 12 snippets + 41 global)
- Happy path: Snippets correctly sorted by alias length
- Happy path: Filename uses correct PascalCase (OpencodeCatalog.md)
- Quality check: Descriptions are concise and professional

**Verification:**

- File exists at `docs/OpencodeCatalog.md`
- Document contains all items
- Snippets sorted by shortest alias
- Professional formatting throughout
- Filename uses correct PascalCase (not ALL_CAPS, not all lowercase)

## System-Wide Impact

- **New file**: Creates `docs/OpencodeCatalog.md` as a reference document
- **No changes**: No modifications to existing files
- **Documentation only**: No functional impact on code or tests

## Risks & Dependencies

| Risk                                     | Mitigation                                                   |
| ---------------------------------------- | ------------------------------------------------------------ |
| Missing skills if new ones added         | Document reflects state at creation time; can be regenerated |
| YAML parsing errors                      | Graceful handling with clear error messages                  |
| File permission issues for global skills | Verify path exists before reading                            |

## Documentation / Operational Notes

- The document should be regenerated periodically as skills/snippets are added
- Consider adding a "Last Updated" timestamp in the document
- Future enhancement: Add links to each skill/snippet source file

## Sources & References

- Local skills: `.opencode/skills/`
- Local snippets: `.opencode/snippet/`
- Global skills: `~/.config/opencode/skills/`
- Skill format reference: Any `SKILL.md` file frontmatter
- Snippet format reference: Any `.opencode/snippet/*.md` file frontmatter
