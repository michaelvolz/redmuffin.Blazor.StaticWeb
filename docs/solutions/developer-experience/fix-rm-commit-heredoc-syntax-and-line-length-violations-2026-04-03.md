---
module: agent-tooling
date: 2026-04-03
problem_type: developer_experience
component: development_workflow
severity: high
symptoms:
  - "ParserError when AI uses heredoc syntax in OpenCode bash tool"
  - "Commitlint retry loops (~5 retries) due to body line length violations"
  - "AI ignores skill instructions and falls back to global commits skill"
root_cause: inadequate_documentation
resolution_type: documentation_update
tags:
  - git-commit
  - skill-optimization
  - agent-instructions
  - shell-compatibility
  - commitlint
---

# Fix rm-commit Skill Heredoc Syntax and Line Length Violations

## Problem

The `rm-commit` skill instructed AI agents to use bash heredoc syntax (`git commit -m "$(cat <<'EOF'...)"`) for commit messages. This syntax fails in OpenCode's bash tool, which runs PowerShell (`pwsh`), causing immediate `ParserError`. Additionally, the skill's vague line-length guidance ("wrap at 72 chars") led to body lines exceeding the 100-character commitlint limit, triggering ~5 retry loops per commit.

## Symptoms

- `ParserError: Missing file specification after redirection operator` when AI attempts heredoc commit
- Commitlint rejects commits with body lines >100 characters
- AI retries commit 5+ times before succeeding, wasting tokens
- User must always invoke `/rm-commit` command manually because auto-trigger is unreliable

## What Didn't Work

- **Prose-only line-length guidance** — Phrases like "wrap at 72 chars" were ignored by the AI, which would write long lines and expect the shell to wrap them
- **Heredoc syntax** — The `<<'EOF'` syntax is bash-specific and incompatible with PowerShell, which OpenCode uses as its bash tool shell
- **Pre-commit validation scripts** — Adding `awk` or other pre-validation was redundant with commitlint and didn't fix the root cause (poor first-pass generation)

## Solution

### 1. Replace Heredoc with Multiple `-m` Flags

Changed the commit command from:

```bash
git commit -m "$(cat <<'EOF'
type(scope): subject

Body explaining why.
EOF
)"
```

To:

```bash
git commit -m "type(scope): subject" -m "Body explaining why the change exists."
```

**Why this works:** Multiple `-m` flags are supported identically in both bash and PowerShell, have no parsing quirks, and make it easier for the AI to validate line lengths since each `-m` argument is a separate string.

### 2. Add Explicit Quoting Rules

Added guidance for shell metacharacters in commit messages:

- Use double quotes for `-m` values unless the message contains `$`, backticks, or `!`
- If the message contains shell metacharacters, use single quotes instead
- If the message contains both single and double quotes, escape the inner ones with `\`

### 3. Add Multi-Paragraph and Footer Examples

```bash
git commit -m "refactor(core): extract validation logic" \
  -m "Move input validation into a dedicated service so controllers stay thin." \
  -m "This also makes it easier to reuse validation across API and web endpoints." \
  -m "Refs: #123"
```

### 4. Enforce Line Length with Concrete Examples

Added good/bad examples showing the 100-character boundary:

**Good** (body line ≤100 chars):

```bash
git commit -m "fix(frontend): prevent duplicate form submits" \
  -m "Disable submit immediately so rapid clicks cannot queue duplicate requests."
```

**Bad** (body line >100 chars):

```bash
git commit -m "fix(frontend): prevent duplicate form submits" \
  -m "Disable submit immediately so rapid clicks cannot queue duplicate requests because the submit button stays enabled for too long and users can trigger duplicate server calls."
```

### 5. Linearize the Skill Flow

Restructured the skill as a numbered FLOW section (6 steps) that is harder for the AI to skip:

1. Check repo rules first
2. Inspect the working tree and recent history
3. Decide whether changes belong in one commit or several
4. Stage only the intended files or hunks
5. Commit with Conventional Commits using multiple `-m` flags
6. Verify the result with `git status`

## Why This Works

The root cause was **inadequate documentation** — the skill used syntax incompatible with the platform and gave vague guidance that the AI couldn't follow reliably. The fix addresses both:

1. **Shell compatibility** — Multiple `-m` flags work in any shell, eliminating the ParserError entirely
2. **Concrete examples** — Good/bad examples with actual character counts give the AI a clear boundary to respect
3. **Linear flow** — Numbered steps are harder for the AI to skip than prose descriptions
4. **Quoting rules** — Explicit guidance prevents shell expansion issues with `$`, backticks, and `!`

## Prevention

- **Always test skill syntax against the target platform** — Don't assume bash syntax works in all shells
- **Use concrete examples over prose** — AI agents follow examples more reliably than abstract rules
- **Include good/bad examples for every constraint** — Show the boundary, don't just describe it
- **Linearize critical workflows** — Numbered steps reduce the chance of AI skipping important steps
- **Review skills after platform changes** — When the shell or tooling changes, verify all skill commands still work
