---
name: rm-docs
description: >
  Create and maintain long-living documentation in docs/. Use for specs,
  standalone guides, and manually created docs. Never use for sidenotes,
  commit messages, or transient notes.
---

# Custom Docs — Knowledge Base Authoring

Create long-living, manually authored documentation in `docs/`. Every
doc must be findable by future-you (or you) via frontmatter
metadata, consistent naming, and cross-references.

---

## 0 — CRITICAL: Doc Boundaries (READ THIS FIRST)

These `docs/` subfolders are managed by other skills. **You MUST NOT
create, modify, rename, or delete anything in them — not the files,
not the directories themselves.** Read-only.

| Subfolder                        | Action |
| -------------------------------- | ------ |
| `docs/solutions/`                | READ   |
| `docs/solutions/patterns/`       | READ   |
| `docs/plans/`                    | READ   |
| `docs/brainstorms/`              | READ   |
| `docs/ideation/`                 | READ   |
| `docs/residual-review-findings/` | READ   |
| `docs/sidenotes/`                | READ   |

**What IS ours to manage:**

| Subfolder      | Naming pattern                         |
| -------------- | -------------------------------------- |
| `docs/specs/`  | `YYYY-MM-DD-descriptive-name-spec.md`  |
| `docs/` (root) | `descriptive-name-guide-YYYY-MM-DD.md` |

### Hard rules

1. **Do not touch the folders above.** No creating, editing, renaming,
   deleting, or moving. Not the files, not the directories.
   Read-only. Period.
2. **Never write a new doc without first reading and referencing
   related docs in the managed folders.** You may read docs in those
   folders for context and link to them in `## Related` sections.
3. **Never manually write into a managed folder; always route through
   the skill that owns it.** If a topic belongs in one of those folders,
   use the skill that manages it.
4. **If you accidentally touch a restricted folder** — revert
   immediately.

### Design Constraints (MANDATORY)

Every doc created via this skill MUST include a
`## What Belongs in This File` section immediately after the first
`# Heading`. This section defines
what the file is for, what belongs, and what does NOT belong. Future
edits (by any agent or human) must respect these constraints.

**When creating a new doc:** include Design Constraints. Never create a
doc without them.

**Never edit a doc that has Design Constraints without reading those
constraints first.** If your edit would add something listed under
"What does NOT belong", reject it. If a new category of content is needed, update the
constraints to reflect the new scope.

**Never edit a doc lacking Design Constraints without first asking the
user to define them.** Show the template below and request the viewpoint,
what-belongs, and what-does-not-belong fields. Do not proceed without
them.

Template:

```markdown
## What Belongs in This File

- **Viewpoint**: [who this is for, what the reader already knows]
- **What belongs**: [categories of information allowed in this file]
- **What does NOT belong**: [categories that must not be added]
```

The constraints must be specific to the document, not generic. If the
document's scope changes, update the constraints to match.

---

## 1 — When to Create a Manual Doc vs Something Else

| Create a doc when…                              | Do NOT create a doc for…           |
| ----------------------------------------------- | ---------------------------------- |
| A process or format needs formal specification  | Trivial one-liner fixes            |
| You have a comprehensive, standalone guide      | Commit messages (use `rm-commit`)  |
| You'll search for this spec/guide months later  | Session-specific scratch notes     |
| You're defining conversion rules, API contracts | PR descriptions (use the PR skill) |
| The content is formal reference material        | Temporary debugging notes          |

**Test:** Would you search for this in 6 months? Is it a formal spec or
standalone guide? If yes to both → manual doc.

---

## 2 — Frontmatter (MANDATORY)

Every doc MUST open with YAML frontmatter between `---` fences. Never
skip this — without it, the doc is invisible to search tools.

### Core fields (required for ALL manual docs)

```yaml
---
date: 2026-05-03 # ISO 8601, YYYY-MM-DD
title: "Short descriptive title" # Optional if H1 duplicates it
---
```

If `title` is omitted, the first `# Heading` is treated as the title.

### Spec doc fields

```yaml
---
date: 2026-05-03
version: 1.0.0 # Semantic versioning for the spec itself
last_edited: "2026-05-03"
purpose: >
  What this spec defines and who should read it.
scope:
  - What is covered
exclude:
  - What is explicitly out of scope
---
```

**Version rules for specs:**

- Start at `1.0.0`
- MAJOR: incompatible rule/path/format changes
- MINOR: new sections, additional rules
- PATCH: clarifications, typos, rewording
- When superseding an old spec, add `original:` field pointing to the
  old file and mark it `(obsolete)`

### Guide doc fields

```yaml
---
date: 2026-05-03
last_updated: 2026-05-03
---
```

Guides are the least structured — keep frontmatter minimal.

---

## 3 — Naming Conventions

### Directory structure

```
docs/
├── specs/          # Formal specifications
└── *.md            # Standalone guides (root level)
```

Other subfolders are managed by other skills — see §0 for boundaries.

### Timestamp placement rule

Every filename includes a full `YYYY-MM-DD` timestamp. Where it goes
depends on what the doc represents:

| When                                                                    | Use        | Pattern                                |
| ----------------------------------------------------------------------- | ---------- | -------------------------------------- |
| The doc IS an event — it captures a decision at a point in time (specs) | **Prefix** | `YYYY-MM-DD-descriptive-name-spec.md`  |
| The doc is evergreen — the date tracks which version/revision (guides)  | **Suffix** | `descriptive-name-guide-YYYY-MM-DD.md` |

**Rationale:** Prefix sorts docs chronologically (good for specs — "what
order were specs written?"). Suffix keeps the topic name first (good for
guides — "find the networking guide" then pick version by date).

### File names

| Doc type | Pattern                                | Example                                    |
| -------- | -------------------------------------- | ------------------------------------------ |
| Spec     | `YYYY-MM-DD-descriptive-name-spec.md`  | `2026-05-03-conversion-format-spec.md`     |
| Guide    | `descriptive-name-guide-YYYY-MM-DD.md` | `omarchy-wsl-complete-guide-2026-04-28.md` |

### Rules

- Always lowercase kebab-case: hyphens between words, no spaces, no
  underscores
- Timestamps are full `YYYY-MM-DD` (ISO 8601 date), never abbreviated
- Specs: date prefix, kebab-case body, end with `-spec.md`
- Guides: kebab-case body, `-guide-YYYY-MM-DD.md` suffix
- Names must be descriptive but concise: 4-8 words before the
  timestamp
- No special characters except hyphens

---

## 4 — Document Structure

### Spec doc sections (in order)

```markdown
# Spec Title (H1)

## 0 — Critical Viewpoint (READ FIRST)

Who this spec is for, how to read it, what NOT to do.

## 1 — Scope and Definitions

What's covered. Define key terminology.

## 2 — Rules (numbered, testable)

Each rule must be verifiable. If it says "MUST", there must be a way
to check compliance.

## N — Migration / Conversion (if applicable)

If this spec replaces an earlier format or system.

## N+1 — Verification

How to validate compliance with this spec.

## Related

- Link to related docs in the knowledge base
```

### Guide sections (freeform)

Guides follow natural narrative structure. Common sections:

- Prerequisites
- Phase 1 / Phase 2 / etc.
- Troubleshooting
- File Inventory (table)

---

## 5 — Cross-Referencing

Always end docs with a `## Related` section linking to other docs in
the knowledge base. Use relative paths from repo root:

```markdown
## Related

- `docs/specs/2026-05-03-conversion-format-spec.md`
- `docs/omarchy-wsl-guide-2026-04-28.md`
```

**Rules:**

- Link at most 3 related docs (avoid link sprawl)
- Never add a new doc without back-linking from any related older doc
- Use full relative paths, not just filenames

---

## 6 — Tags and Searchability

Tags are the primary discovery mechanism. Put thought into them.

### Tag rules

```yaml
tags:
  - opencode # Product/tool name
  - conversion # Concept
  - plugin # Component
```

- Always lowercase
- Always a YAML list (each on its own line with `- `)
- Never use flow style `[a, b]` — inconsistent with YAML parsers
- Include: product name, component, technical concept
- 3-7 tags is the sweet spot
- Don't tag the obvious (e.g., don't tag "docs" on every doc)

### Tag taxonomy (informal, add as needed)

| Tag          | Meaning                                     |
| ------------ | ------------------------------------------- |
| `opencode`   | OpenCode IDE/agent platform                 |
| `dotfiles`   | Config file management                      |
| `backup`     | Backup/restore workflows                    |
| `gpg`        | GPG encryption and key management           |
| `conversion` | Format or platform migration                |
| `spec`       | Formal specification document               |
| `guide`      | Standalone tutorial or walkthrough          |
| `shell`      | Bash/zsh scripting and environment          |
| `security`   | Credentials, encryption, API key management |

---

## 7 — Revising Existing Docs

### When to edit vs create new

| Situation                          | Action                                  |
| ---------------------------------- | --------------------------------------- |
| Minor addition (new example, note) | Edit existing doc, add `last_updated`   |
| Correction (wrong command, typo)   | Edit existing doc, add `last_updated`   |
| Same topic, different doc type     | New doc, cross-reference from old       |
| Major rework (>30% changed)        | New doc, mark old as `(obsolete)` in H1 |

### Editing existing docs

1. Add or update `last_updated: YYYY-MM-DD` in frontmatter
2. Preserve existing frontmatter fields — add, don't remove
3. Keep the original `date:` (creation date never changes)
4. If content changed significantly, increment `version:` (specs only)

### Deprecating old docs

When a manual doc is fully superseded:

1. Add `status: deprecated` to frontmatter
2. Add `superseded_by: path/to/new-doc.md` field
3. Add a banner at top of H1: `# Old Title (OBSOLETE — see new-doc.md)`
4. Do NOT delete the old doc — it may still be referenced

---

## 8 — Writing Style

- **Imperative, direct.** "Run this command" not "You should run this
  command"
- **Code blocks over prose.** Show the command, then explain
- **Before/After** when showing transformations
- **One doc, one concern.** Don't bundle multiple unrelated topics
- **Explain why, not just what.**
- **80-char line limit** for readability in terminals and diffs

---

## 9 — Quick Templates

### Spec template

```markdown
---
date: YYYY-MM-DD
version: 1.0.0
last_edited: "YYYY-MM-DD"
purpose: >
  What this spec defines and who should read it.
scope:
  - Item covered
exclude:
  - Explicitly out of scope
---

# Specification Title

## 0 — Critical Viewpoint (READ FIRST)

Who this is for, how to read it, what NOT to do.

## 1 — Scope and Definitions

## 2 — Rules

## N — Migration (if converting from legacy format)

## N+1 — Verification

## Related

- `docs/specs/earlier-spec-2026-04-01.md` (obsolete)
- `docs/related-guide-2026-05-01.md`
```

### Guide template

```markdown
---
date: YYYY-MM-DD
last_updated: YYYY-MM-DD
---

# Guide Title

## Prerequisites

## Phase 1 — Setup

## Phase 2 — Configuration

## Troubleshooting

## File Inventory

## Related

- `docs/related-guide-2026-05-01.md`
```

---

## 10 — Usage

When you need to search the knowledge base, it queries
by frontmatter fields:

```
search docs/specs/ for version=2.0.0
search docs/ for tags=guide AND tags=opencode
```

Frontmatter fields are the API. Write them for machine readability
first, human readability second.

---

## COMMANDS (for this skill)

| Command                                            | Purpose                             |
| -------------------------------------------------- | ----------------------------------- |
| `skill({ name: "rm-docs-authoring" })`       | Load authoring rules before writing |
| `skill({ name: "rm-docs-authoring" }) spec`  | Load + create a new spec            |
| `skill({ name: "rm-docs-authoring" }) guide` | Load + create a new guide           |
