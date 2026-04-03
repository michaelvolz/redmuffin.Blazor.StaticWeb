---
date: 2026-04-03
topic: rm-commits-skill-optimization
---

# rm-commits Skill Optimization

## Problem Frame

The `rm-commits` skill (129 lines) generates conventional commit messages for a trunk-based .NET/Blazor repo. It works in principle but has two chronic failures in practice:

1. **Body line length violations** — The AI consistently generates body lines exceeding 100 characters. Commitlint catches this. The AI retries ~5 times before succeeding. This wastes tokens and frustrates the workflow.
2. **Instruction drift** — The AI ignores or partially follows several skill rules, requiring the user to always invoke via `/rm-commit` command rather than trusting auto-trigger.

The global `git-commit` skill (from Compound Engineering package) contains useful patterns that `rm-commits` lacks, but also contains workflow assumptions (branch protection, detached HEAD, push safety) that are irrelevant for this trunk-based, manually-pushed repo.

## Requirements

**Message Generation Quality**

- R1. Body lines must not exceed 100 characters on the first attempt, eliminating the commitlint retry loop
- R2. The skill must use concrete, enforceable instructions for line length — not prose like "wrap to 100 chars" which the AI ignores
- R3. The skill must include good/bad examples that demonstrate the line length constraint visually

**Workflow Alignment**

- R4. The skill must explicitly state this is a trunk-based repo — no branch checks, no push attempts, no detached HEAD warnings
- R5. The skill must be optimized for invocation via the `/rm-commit` command (the only reliable trigger)
- R6. The skill must remove all instructions that waste tokens on irrelevant checks (branch protection, push safety, detached HEAD)

**Structural Clarity**

- R7. The skill must be organized as a linear execution flow that is hard for the AI to skip steps in
- R8. The skill description in frontmatter must be optimized for reliable auto-triggering when the command is not used
- R9. The skill name remains `rm-commits` — preserving the `rm-` namespace convention. Trigger reliability will be improved via description optimization, not renaming.

**Content Preservation**

- R10. All project-specific knowledge must be preserved: allowed types/scopes, lockfile paths, BuildWebCompiler2022 exception, commitlint rules
- R11. The skill must align with the repo's `commitlint.config.js` — body is always required (not just for "non-trivial" changes)

## Success Criteria

- Commit body lines ≤100 chars on first attempt ≥90% of the time (down from current ~20%)
- Commitlint retry count reduced from ~5 to ≤1
- Skill is shorter and more focused (target: ~80-100 lines, down from 129)
- No token waste on branch/push/detached-HEAD checks
- Skill overrides global `commits` skill by name match

## Scope Boundaries

- **In scope**: `rm-commits` skill content, `rm-commit` command content, skill naming
- **Out of scope**: `git-commit-push-pr` global skill (separate concern), commitlint config changes, git hooks configuration
- **Deferred**: Renaming to `commits` for global override — do this after the content is validated

## Key Decisions

**Decision: Remove branch protection and detached HEAD checks**
Rationale: This is a trunk-based repo with manual push control. These checks burn tokens on warnings the user never needs. They belong in the global `git-commit` skill (which serves many workflows), not in this project-specific skill.

**Decision: Fix line length via better generation, not pre-validation**
Rationale: Adding `awk` or other pre-commit validation is redundant with commitlint. The real fix is better first-pass generation: short sentence structure, good/bad examples, negative constraints, and removing "wrap" language (which implies write-long-then-break).

**Decision: Linear execution flow over categorized sections**
Rationale: The current skill mixes concerns (schema, gates, output contract, format, commitlint rules, allowed values, hard rules, valid message shape, lock paths, lockfile exception). A linear step-by-step flow is harder for the AI to skip steps in.

**Decision: Keep lockfile path list explicit, not glob-based**
Rationale: The explicit path list is precise and catches the BuildWebCompiler2022 exception correctly. A glob pattern would lose this precision. The list is short (8 paths) and easy to maintain.

**Decision: Keep `rm-commits` name, optimize description for trigger reliability**
Rationale: The `rm-` namespace is an established project convention from the instruction architecture overhaul. The real problem is that the AI sometimes auto-triggers the global `commits` skill instead. Fix this by strengthening the local skill's description with explicit trigger phrases, not by renaming.

**Decision: Body always required (match commitlint, not skill's current "non-trivial" language)**
Rationale: `commitlint.config.js` has `'body-empty': [2, 'never']` — body is required for ALL commits. The skill currently says "body is required for non-trivial changes" which contradicts commitlint and causes the AI to skip body on "obvious" changes.

## Dependencies / Assumptions

- `commitlint.config.js` remains the source of truth for commit message validation rules
- The `/rm-commit` command remains the primary invocation method
- Trunk-based workflow with manual push continues
- Global `git-commit` and `git-commit-push-pr` skills will eventually be removed

## Outstanding Questions

### Resolve Before Planning

- [Affects R8][User decision] The current skill description is ~200 chars. Do you want it shortened to ~100 chars with explicit trigger phrases (better for Opencode's matching), or keep the current verbose description?

### Deferred to Planning

- [Affects R2][Technical] What exact phrasing works best for LLM line-length compliance? Should we test with a few sample commits before finalizing?
- [Affects R7][Technical] Should the `rm-commit` command be expanded to include orchestration steps (context gathering, validation), or stay as a thin delegator to the skill?

## Alternatives Considered

| Approach                                                      | Pros                                                         | Cons                                                                      | Verdict         |
| ------------------------------------------------------------- | ------------------------------------------------------------ | ------------------------------------------------------------------------- | --------------- |
| **Add pre-commit awk validation**                             | Catches violations before commitlint                         | Redundant with commitlint, same token cost, different place               | Rejected        |
| **Keep branch/detached HEAD checks**                          | Safety net for edge cases                                    | Burns tokens on checks user never needs, contradicts trunk-based workflow | Rejected        |
| **Merge into git-commit-push-pr**                             | Single source of truth                                       | Over-scoped, mixes commit generation with PR creation                     | Rejected        |
| **Rename to `commits` to override global**                    | Eliminates name ambiguity                                    | Discards established `rm-` namespace convention                           | Rejected        |
| **Rewrite with linear flow + better generation instructions** | Fixes root cause, removes noise, preserves project knowledge | Requires careful instruction design                                       | **Recommended** |

## Related Documents

- [Instruction Architecture Overhaul Requirements](2026-04-03-instruction-architecture-overhaul-requirements.md) — Parent requirements for the full instruction restructure
- [Instruction Architecture Findings](2026-04-03-instruction-architecture-findings.md) — Complete audit results and migration map

## Next Steps

→ `/ce:plan` for structured implementation planning.
