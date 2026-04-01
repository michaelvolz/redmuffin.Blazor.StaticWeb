---
date: 2026-04-01
topic: documentation-consolidation
---

# Documentation Consolidation: AGENTS.md Restructure

## Problem Frame

AGENTS.md is 379 lines of monolithic content that creates maintenance burden and human onboarding friction. An estimated 30% duplicates existing skills (csharp-standards, testing, dotnet), 30% belongs in new skills (dev-workflows, security-secrets, output-style), and 40% should remain as critical routing content. The skills system already exists with 11 skills but AGENTS.md still contains their content, creating redundancy between the main file and skill references.

## Requirements

**Content Migration**

- R1. Extract duplicated content from AGENTS.md into appropriate skill files (existing and new)
- R2. Update existing skills (csharp-standards, testing, dotnet) with content currently duplicated in AGENTS.md PATTERNS section
- R3. Create three new skills: dev-workflows, security-secrets, output-style
- R4. Ensure zero content loss — every rule, pattern, and workflow from AGENTS.md must appear in exactly one skill or the restructured AGENTS.md

**AGENTS.md Restructure**

- R5. Target AGENTS.md at ~150 lines: critical rules, key workflows, skill references
- R6. AGENTS.md becomes a hybrid document — routing index plus essential quick-reference content
- R7. Keep critical workflows in AGENTS.md: research-first protocol, infrastructure error protocol, frontend debugging protocol, dev server startup protocol
- R8. Keep boundaries section in AGENTS.md (ALWAYS, ASK FIRST, NEVER) as quick-reference guardrails
- R17. COMMANDS table and STACK table remain in AGENTS.md as quick-reference content

**Skill Creation**

- R9. dev-workflows skill: process management tables (VS-owned vs agent-owned), web search decision tree
- R10. security-secrets skill: secret management patterns, MCP env vars, zero-tolerance rules, devcontainer security reference
- R11. output-style skill: formatting rules, naming conventions, output style constraints, C# 12/13 features, nullable reference types

**Skill Updates**

- R12. csharp-standards: audit and reconcile naming conventions, nullable rules, formatting against AGENTS.md PATTERNS
- R13. testing: audit and reconcile test double naming convention, mock examples against AGENTS.md
- R14. dotnet: audit and reconcile dev modes table, test filter commands against AGENTS.md COMMANDS section

**Verification**

- R15. Content audit: diff AGENTS.md before/after to confirm line count reduction, verify each new skill file exists with expected content sections
- R16. Verify all skills load correctly (manual checklist: start opencode session, trigger each skill by description, confirm it loads)

## Success Criteria

- AGENTS.md reduced from 379 lines to ~150 lines
- Zero content loss — every rule/pattern/workflow preserved in exactly one location
- All 14 skills (11 existing + 3 new) are self-contained and non-overlapping
- AGENTS.md references skills correctly for routing
- No broken references or missing content

## Scope Boundaries

- Does NOT change any C# code, test files, or build configuration
- Does NOT modify README.md (separate effort)
- Does NOT change skill invocation mechanism or opencode.json
- Does NOT consolidate PRD documents (separate effort)
- Does NOT create `.github/copilot-instructions.md` (separate effort, but skill-based architecture is designed to be compatible with it)

## Key Decisions

- **Keep workflows in AGENTS.md**: Critical workflows (research-first, debugging, startup) stay in AGENTS.md for quick access rather than moving to dev-workflows skill. Rationale: These are the most frequently referenced sections during agent sessions.
- **Create 3 new skills**: dev-workflows, security-secrets, output-style. Rationale: Complete separation of concerns; each skill has a single trigger condition.
- **Target 150 lines**: Balance between readability and completeness. Rationale: Too short loses critical context; too long defeats the purpose.
- **Zero content loss policy**: Every line of AGENTS.md must appear somewhere after restructure. Rationale: Prevents accidental loss of critical rules during migration.
- **COMMANDS table stays in AGENTS.md**: 25-line commands table remains as quick reference. Rationale: Agents need these commands constantly during sessions.
- **STACK table stays in AGENTS.md**: 7-line stack table remains as essential context. Rationale: Core framework info needed for every agent session.

## Dependencies / Assumptions

- Skills system is functional and skills load correctly when referenced
- AGENTS.md is the single source of truth for agent behavior (not README.md)
- No other files reference specific AGENTS.md line numbers or sections

## Outstanding Questions

### Deferred to Planning

- [Affects R9][Technical] Should dev-workflows skill include the full process management tables (VS-owned vs agent-owned) or reference AGENTS.md?
- [Affects R11][Technical] Should output-style skill include the full C# 12/13 feature list or just reference official docs?
- [Affects R16][Needs research] How does opencode validate skill loading? Is there a test command?

## Next Steps

→ `/ce:plan` for structured implementation planning
