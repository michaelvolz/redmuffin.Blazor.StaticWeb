---
date: 2026-04-03
topic: rm-commit-skill-optimization
---

# rm-commit Skill Optimization

## Problem Frame

The `rm-commit` skill generates conventional commit messages for a trunk-based .NET/Blazor repo. It has three chronic failures in practice:

1. **Heredoc syntax fails in OpenCode** — The skill instructs AI to use `git commit -m "$(cat <<'EOF'...)"` which uses bash heredoc syntax. OpenCode's bash tool runs PowerShell (`Shell: pwsh`), where `<<'EOF'` is invalid. This causes immediate ParserError.

2. **Body line length violations** — The AI consistently generates body lines exceeding 100 characters. Commitlint catches this. The AI retries ~5 times before succeeding. This wastes tokens and frustrates the workflow.

3. **Instruction drift** — The AI ignores or partially follows several skill rules, requiring the user to always invoke via `/rm-commit` command rather than trusting auto-trigger.

The global `git-commit` skill (from Compound Engineering package) contains useful patterns that `rm-commit` lacks, but also contains workflow assumptions (branch protection, detached HEAD, push safety) that are irrelevant for this trunk-based, manually-pushed repo.

## Requirements

**Commit Command Syntax**

- R1. The skill must use `git commit -m "subject" -m "body"` syntax (multiple `-m` flags) instead of heredoc
- R2. The skill must include explicit, copy-pasteable command examples that the AI can generate verbatim
- R3. The skill must explain WHY multiple `-m` flags: works in both bash and PowerShell, no parsing quirks, AI can validate each line separately

**Message Generation Quality**

- R4. Body lines must not exceed 100 characters on the first attempt, eliminating the commitlint retry loop
- R5. The skill must use concrete, enforceable instructions for line length — not prose like "wrap to 100 chars" which the AI ignores
- R6. The skill must include good/bad examples that demonstrate the line length constraint visually

**Workflow Alignment**

- R7. The skill must explicitly state this is a trunk-based repo — no branch checks, no push attempts, no detached HEAD warnings
- R8. The skill must be optimized for invocation via the `/rm-commit` command (the only reliable trigger)
- R9. The skill must remove all instructions that waste tokens on irrelevant checks (branch protection, push safety, detached HEAD). ALL commitlint validation rules are mandatory and cannot be removed — body presence, line length, format, etc. are all enforced by the hook.

**Structural Clarity**

- R10. The skill must be organized as a linear execution flow that is hard for the AI to skip steps in
- R11. The skill description in frontmatter must be optimized for reliable auto-triggering when the command is not used
- R12. The skill name remains `rm-commit` — preserving the `rm-` namespace convention. Trigger reliability will be improved via description optimization, not renaming.

**Content Preservation**

- R13. All project-specific knowledge must be preserved: allowed types/scopes, lockfile paths, BuildWebCompiler2022 exception, commitlint rules
- R14. The skill must align with the repo's `commitlint.config.js` — body is always required (not just for "non-trivial" changes)

## Success Criteria

- Commit command executes without syntax errors on first attempt (no heredoc ParserError)
- Commit body lines ≤100 chars on first attempt ≥90% of the time (down from current ~20%)
- Commitlint retry count reduced from ~5 to ≤1
- Skill is shorter and more focused (target: ~80-100 lines, down from 129)
- No token waste on branch/push/detached-HEAD checks
- Skill overrides global `commits` skill by name match

## Scope Boundaries

- **In scope**: `rm-commit` skill content, `rm-commit` command content, skill naming
- **Out of scope**: `git-commit-push-pr` global skill (separate concern), commitlint config changes, git hooks configuration
- **Deferred**: Renaming to `commits` for global override — do this after the content is validated

## Key Decisions

**Decision: Use multiple `-m` flags instead of heredoc**
Rationale: The heredoc syntax `<<'EOF'` is bash-specific and fails in PowerShell (OpenCode's bash tool runs `pwsh`). Multiple `-m` flags (`git commit -m "subject" -m "body"`) work identically in bash and PowerShell, have no parsing quirks, and make it easier for the AI to validate line lengths since each `-m` argument is separate. This is simpler than PowerShell heredoc (`@'...'@`) and avoids file I/O needed for `--template`.

**Decision: Remove branch protection and detached HEAD checks**
Rationale: This is a trunk-based repo with manual push control. These checks burn tokens on warnings the user never needs. They belong in the global `git-commit` skill (which serves many workflows), not in this project-specific skill.

**Decision: Fix line length via better generation, not pre-validation**
Rationale: Adding `awk` or other pre-commit validation is redundant with commitlint. The real fix is better first-pass generation: short sentence structure, good/bad examples, negative constraints, and removing "wrap" language (which implies write-long-then-break).

**Decision: Linear execution flow over categorized sections**
Rationale: The current skill mixes concerns (schema, gates, output contract, format, commitlint rules, allowed values, hard rules, valid message shape, lock paths, lockfile exception). A linear step-by-step flow is harder for the AI to skip steps in.

**Decision: Keep lockfile path list explicit, not glob-based**
Rationale: The explicit path list is precise and catches the BuildWebCompiler2022 exception correctly. A glob pattern would lose this precision. The list is short (8 paths) and easy to maintain.

**Decision: Keep `rm-commit` name, optimize description for trigger reliability**
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

None — all questions deferred to planning.

### Deferred to Planning

- [Affects R11][Technical] After the skill is working, shorten the skill description to ~100 chars with explicit trigger phrases for better OpenCode matching
- [Affects R5][Technical] What exact phrasing works best for LLM line-length compliance? Should we test with a few sample commits before finalizing?
- [Affects R10][Technical] Should the `rm-commit` command be expanded to include orchestration steps (context gathering, validation), or stay as a thin delegator to the skill?

## Alternatives Considered

| Approach                                                  | Pros                                                     | Cons                                                                      | Verdict         |
| --------------------------------------------------------- | -------------------------------------------------------- | ------------------------------------------------------------------------- | --------------- |
| **Bash heredoc `<<'EOF'`**                                | Familiar to bash users                                   | Fails in PowerShell (OpenCode's bash tool), causes ParserError            | Rejected        |
| **PowerShell heredoc `@'...'@`**                          | Works in PowerShell                                      | Still complex, AI still struggles with line length                        | Rejected        |
| **Template file (`--template`)**                          | Works in both shells                                     | Requires temp file I/O, more complex for AI to execute                    | Rejected        |
| **Add pre-commit awk validation**                         | Catches violations before commitlint                     | Redundant with commitlint, same token cost, different place               | Rejected        |
| **Keep branch/detached HEAD checks**                      | Safety net for edge cases                                | Burns tokens on checks user never needs, contradicts trunk-based workflow | Rejected        |
| **Merge into git-commit-push-pr**                         | Single source of truth                                   | Over-scoped, mixes commit generation with PR creation                     | Rejected        |
| **Rename to `commits` to override global**                | Eliminates name ambiguity                                | Discards established `rm-` namespace convention                           | Rejected        |
| **Multiple `-m` flags + linear flow + better generation** | Works in bash/PowerShell, simple, easier line validation | Requires careful instruction design                                       | **Recommended** |

## Related Documents

- [Instruction Architecture Overhaul Requirements](2026-04-03-instruction-architecture-overhaul-requirements.md) — Parent requirements for the full instruction restructure
- [Instruction Architecture Findings](2026-04-03-instruction-architecture-findings.md) — Complete audit results and migration map

## Next Steps

→ `/ce:plan` for structured implementation planning.
