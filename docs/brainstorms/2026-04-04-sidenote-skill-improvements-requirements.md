---
date: 2026-04-04
topic: sidenote-skill-improvements
---

# Sidenote Skill Improvements: Performance and Title Format

## Problem Frame

The sidenote capture skill has two issues identified through real-world usage:

1. **Capture latency**: The subagent-based capture approach takes several seconds, which is noticeable and frustrating during fast-paced coding sessions. The goal is a fast, mostly silent note capture that immediately returns to the original task.
2. **Title redundancy**: Sidenote files currently store the title in both YAML frontmatter (`title:`) and as a markdown H1 (`# Title`). This is duplicated content that adds maintenance burden and file bloat without clear benefit.

## Requirements

**Performance**

- R1. Sidenote capture must feel instantaneous and avoid blocking the current task on subagent startup
- R2. The capture mechanism must write directly without using a subagent
- R3. Capture should return one short confirmation containing the exact filename that was created

**Title Format**

- R4. Each sidenote file must have exactly one title representation — not both frontmatter and markdown H1
- R5. The chosen title format must support programmatic extraction for `sidenotes list` display, and listing must read the frontmatter title
- R6. Existing sidenote files should be normalized to the new format: files with only an H1 gain frontmatter titles, files with both drop the H1, and frontmatter-only files remain unchanged

**Documentation**

- R7. The `rm-sidenotes` skill file must be updated to document the new capture mechanism and title format rules

## Success Criteria

- User captures a sidenote and the file appears before they finish their next thought
- The user gets one short confirmation with the exact filename created
- `sidenotes list` displays titles correctly after the format change
- All existing sidenote files are consistent — no mixed title formats
- The skill file itself is updated to reflect the new capture mechanism and title rules

## Scope Boundaries

- NOT a rewrite of the entire skill — only the capture flow and title format
- NOT changing the conversion or dismissal logic
- Retrieval/list display may change only as needed to read frontmatter titles
- NOT adding new features — this is purely an improvement to existing behavior

## Key Decisions

- **Direct write only**: Sidenote capture should be handled in the main flow without a subagent. The work is simple file I/O, and the user wants the interaction to stay fast and quiet.
- **Frontmatter title only**: The `title:` field in YAML frontmatter is sufficient. It's machine-readable for `sidenotes list`, and when a user opens a sidenote file, the body text is self-explanatory. Markdown H1 titles are removed from the template. Converted artifacts (brainstorms, plans) can have their own H1 titles — that's their concern, not the sidenote's.
- **Migration is inline**: Existing sidenotes are normalized as part of the skill update so the directory ends up with one consistent title format.

## Dependencies / Assumptions

- Assumes the main agent has direct file write access (standard for coding agents)
- Assumes `glob` and `write` tools are available to the main agent without subagent delegation

## Outstanding Questions

### Resolve Before Planning

- None

### Deferred to Planning

- [Affects R2][Technical] Should the verification subagent use a minimal prompt (just "verify file exists, retry if not") or keep the current full instructions trimmed down?

## Next Steps

→ /ce:plan for structured implementation planning
