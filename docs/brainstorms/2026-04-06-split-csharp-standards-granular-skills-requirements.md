---
date: 2026-04-06
topic: split-csharp-standards-granular-skills
---

# Split C# Standards into Granular Skill Guides

## Problem Frame

The consolidated C# standards document (1,381 lines) is too large to load efficiently as a single skill, causing token waste and slower responses. We need a system to split it into granular skills that load precisely when needed, prioritizing zero false negatives (skills not loading when guidance is required).

## Requirements

**Core System**

- R1. Split C# standards document into separate skill guides with rm-guide-\* prefix
- R2. Skills load based on file types (_.cs, _.scss, \*.razor) and folders (test/)
- R3. Zero false negatives - skills must load when relevant guidance is needed
- R4. Expand same granularity system to all standards areas (markdown, UI styling, REST APIs, documentation, GitHub Actions, performance, security)
- R5. Use correct suffixes for reviewers and analyzers in subfolders

**Implementation Approach**

- R6. Start with Approach 1: File-type + folder-based triggers for C# standards proof of concept
- R7. Each skill has precise trigger description for reliable loading
- R8. Skills contain only essential content, no duplication
- R9. Reference skills in AGENTS.md with trigger conditions

## Success Criteria

- Skills load automatically and reliably for relevant tasks
- Token usage minimized compared to loading full document
- All essential C# standards preserved across skills
- System expandable to other standards without complexity increase

## Scope Boundaries

- Focus on C# standards first as proof of concept
- File-type and folder triggers only (no complex keyword detection initially)
- Maintain existing standards content accuracy

## Key Decisions

- Approach 1 (file-type + folder triggers) as starting point
- Consistent granularity across all standards areas
- rm-guide-\* naming convention
- Prioritize reliability over precision

## Dependencies / Assumptions

- OpenCode skill loading mechanisms support the required trigger patterns
- Existing C# standards document is the source of truth

## Next Steps

→ /ce:plan for structured implementation planning
