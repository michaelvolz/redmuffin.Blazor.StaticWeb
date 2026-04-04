---
module: agent-tooling
date: 2026-04-04
problem_type: developer_experience
component: development_workflow
severity: high
symptoms:
  - "commitlint rejects commits with 'body may not be empty' even when body text is present"
  - "AI retries commit multiple times, unable to pass commitlint validation"
root_cause: conventional_commits_parser_bug
resolution_type: documentation_update
tags:
  - commitlint
  - git-commit
  - conventional-commits
  - issue-references
---

# commitlint Rejects Body When #NNNNN Appears in Body Text

## Problem

The `conventional-commits-parser` (used by commitlint) treats `#NNNNN` anywhere in the commit body as an issue reference. When it encounters `#NNNNN`, it considers everything from that point onward as a footer, leaving the parsed body empty. This triggers the `body-empty` rule even though the body text is clearly present.

This is a known, open bug in commitlint since 2020:

- [Issue #896](https://github.com/conventional-changelog/commitlint/issues/896) — footer-leading-blank complains with number sign in body (open since Jan 2020)
- [Issue #4099](https://github.com/conventional-changelog/commitlint/issues/4099) — body line with reference like `#123` is considered footer (open since Jul 2024)

## Symptoms

- commitlint rejects valid commits with `body may not be empty [body-empty]`
- The commit body is clearly non-empty when viewed in `git log`
- Overriding `issuePrefixes: []` in the local commitlint config does not fix it — the `@commitlint/config-conventional` preset's parser options take precedence during merge
- The bug has been open for 6+ years with no fix in sight

## What Didn't Work

- **`parserPreset.parserOpts.issuePrefixes: []`** — The local config override is loaded (confirmed via `--print-config`) but the preset's `issuePrefixes: ['#']` wins during the merge process
- **`referenceActions: null`** — Disabling reference actions did not prevent the parser from treating `#NNNNN` as a footer
- **CRLF vs LF line endings** — Not the cause; both produce the same error
- **`git commit -F <file>` vs `-m` flags** — Both code paths hit the same parser bug

## Solution

### Move Issue References to the Footer

Never put `#NNNNN` in the commit body. Always use the `Refs:` footer syntax:

```powershell
@"
docs: add feature brainstorm and plan

Brainstorm identifies the pain point and defines requirements
for capture, storage, retrieval, and conversion.

Refs: #26376
"@ | git commit -F -
```

### If You Must Reference an Issue in the Body

Write the issue number without the `#` prefix:

```
Brainstorm identifies the pain point (Claude Code issue 26376)
```

## Why This Works

The `Refs: #NNNNN` footer syntax is the correct conventional-commits format for issue references. The parser expects references in the footer, not the body. By keeping `#NNNNN` out of the body entirely, the parser correctly identifies the body text and the `body-empty` rule passes.

## Prevention

- **Updated `rm-commit` skill** — Added CRITICAL rule: "NEVER put `#NNNNN` (issue references) in the commit body — the conventional-commits-parser treats them as footers, making the body appear empty to commitlint. Always use `Refs: #NNNNN` in the footer instead."
- **Use `Refs:` footer** — This is the correct conventional-commits format anyway
- **Avoid `#` in body prose** — If you must mention an issue number, write it without the `#` prefix

## Related

- `docs/solutions/developer-experience/fix-rm-commit-heredoc-syntax-and-line-length-violations-2026-04-03.md` — PowerShell here-string approach for git commits
- `.opencode/skills/rm-commit/SKILL.md` — Updated with #NNNNN body warning
