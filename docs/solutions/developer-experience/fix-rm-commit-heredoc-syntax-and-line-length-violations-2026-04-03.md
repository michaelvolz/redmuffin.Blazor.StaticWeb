---
module: agent-tooling
date: 2026-04-03
last_updated: 2026-04-03
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
  - powershell
  - here-string
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
- **Multiple `-m` flags** — Initially replaced heredoc with `-m` flags, but this provided no mechanical enforcement for line length. The AI could still dump a 150-character string into a single `-m` flag and get the same commitlint error. Line continuation (`\`) also caused issues in PowerShell.

## Solution

### Replace `-m` Flags with PowerShell Here-String Piped to `git commit -F -`

The working approach uses PowerShell's here-string syntax piped to `git commit -F -`:

```powershell
@"
type(scope): imperative subject

First body paragraph. Each line must be ≤ 100 characters.
Wrap manually at ~80 chars to stay safe.

Second body paragraph if needed. Same line length rule.

Refs: #123
"@ | git commit -F -
```

**Why this works:**

1. **Here-strings are native PowerShell** — `@"..."@` is PowerShell's multi-line string literal. No bash heredoc, no parsing errors.
2. **`git commit -F -` reads from stdin** — Git natively accepts `-` as a file path meaning "read from standard input." The piped here-string feeds the commit message directly.
3. **Exact line break preservation** — What you type in the here-string is exactly what git receives. No shell wrapping, no escaping surprises.
4. **Template structure** — The here-string naturally enforces a visual template. The AI sees the blank line between subject and body, the paragraph breaks, and the footer area. It's harder to accidentally produce malformed output.

### Quoting Rules

- **Double-quoted here-string** (`@"..."@`) — Default. Variables like `$var` and backticks will be expanded by PowerShell.
- **Single-quoted here-string** (`@'...'@`) — Use when the commit message contains `$`, backticks, or other PowerShell metacharacters that should be literal. No variable expansion, no escape sequences.

```powershell
@'
fix(api): handle null $userId gracefully

When $userId is null, return 401 instead of 500.
The backtick ` character is also safe here.
'@ | git commit -F -
```

### Line Length Enforcement

- Wrap every body line at **≤ 80 characters** (safe margin under the 100-char commitlint limit)
- Count characters if unsure — do NOT guess
- The here-string preserves your exact line breaks, so what you type is what commitlint sees

### Linearized Skill Flow

The skill is structured as a numbered FLOW section (6 steps) that is harder for the AI to skip:

1. Check repo rules first
2. Inspect the working tree and recent history
3. Decide whether changes belong in one commit or several
4. Stage only the intended files or hunks
5. Commit with Conventional Commits using PowerShell here-string piped to `git commit -F -`
6. Verify the result with `git status`

## Why This Works

The root cause was **inadequate documentation** — the skill used syntax incompatible with the platform and gave vague guidance that the AI couldn't follow reliably. The fix addresses both:

1. **Shell compatibility** — PowerShell here-strings are native to the platform OpenCode uses, eliminating ParserError entirely
2. **Template structure** — The here-string provides a visual template the AI fills in, not a command it constructs from parts
3. **Exact line break control** — `git commit -F -` reads the here-string verbatim, so line breaks are preserved exactly as written
4. **80-char wrap guidance** — Concrete safe margin under the 100-char limit, with explicit instruction to count when unsure
5. **Quoting rules** — Clear guidance on when to use single-quoted vs double-quoted here-strings prevents shell expansion issues

## Prevention

- **Always test skill syntax against the target platform** — Don't assume bash syntax works in all shells. OpenCode's bash tool runs PowerShell on Windows.
- **Use template structures over command construction** — Here-strings give the AI a fill-in-the-blank template rather than asking it to assemble command arguments
- **Include concrete line-length guidance** — "Wrap at ~80 chars" with explicit counting instruction is more effective than "keep lines under 100 chars"
- **Linearize critical workflows** — Numbered steps reduce the chance of AI skipping important steps
- **Review skills after platform changes** — When the shell or tooling changes, verify all skill commands still work

## Related

- `docs/solutions/integration-issues/opencode-instruction-management-lessons-2026-04-03.md` — Broader lessons on skill architecture and instruction management in OpenCode
- `docs/solutions/integration-issues/compound-engineering-plugin-installation-2026-04-01.md` — commitlint setup and configuration
