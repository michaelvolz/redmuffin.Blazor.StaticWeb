---
module: agent-tooling
date: 2026-04-04
last_updated: 2026-04-04
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

# commitlint Rejects Body When `#` + Identifier Appears in Body Text

## Problem

The `conventional-commits-parser` (used by commitlint) treats `#` followed by an identifier anywhere in the commit body as an issue reference. When it encounters such a pattern, it considers everything from that point onward as a footer, leaving the parsed body empty. This triggers the `body-empty` rule even though the body text is clearly present.

This is a known, open bug in commitlint since 2020:

- [Issue #896](https://github.com/conventional-changelog/commitlint/issues/896) — footer-leading-blank complains with number sign in body (open since Jan 2020)
- [Issue #4099](https://github.com/conventional-changelog/commitlint/issues/4099) — body line with reference like `#123` is considered footer (open since Jul 2024)

## Root Cause: Exact Regex from Source

The bug lives in `conventional-commits-parser`'s `getReferencePartsRegex` function (`dist/regex.js`, line 26):

```js
new RegExp(
  `(?:.*?)??\\s*([\\w-\\.\\/]*?)??(${join(issuePrefixes, "|")})([\\w-]+)(?=\\s|$|[,;)\\]])`,
  flags,
);
```

With the default `issuePrefixes: ['#']`, this becomes:

```
(?:.*?)??\s*([\w-\.\/]*?)??(#)([\w-]+)(?=\s|$|[,;)\]])
```

Breaking it down:

| Part               | Meaning                                                                                                       |
| ------------------ | ------------------------------------------------------------------------------------------------------------- |
| `(?:.*?)??`        | Non-greedy optional text before the reference                                                                 |
| `\s*`              | Optional whitespace                                                                                           |
| `([\w-\.\/]*?)??`  | Capture group 1: optional "action" prefix like `Refs:` (word chars, hyphens, dots, slashes)                   |
| `(#)`              | Capture group 2: the issue prefix (default `#`)                                                               |
| `([\w-]+)`         | Capture group 3: the identifier — **one or more word characters or hyphens** (`[A-Za-z0-9_-]`)                |
| `(?=\s\|$[,;)\]])` | Lookahead: must be followed by whitespace, end-of-string, comma, semicolon, closing paren, or closing bracket |

**The critical insight:** The identifier is `[\w-]+`, which matches **any** combination of letters, digits, underscores, and hyphens — not just digits. This means `#abc`, `#SN-0001`, `#123a`, and `#XYZ` all trigger the bug, not just `#1234`.

## Empirical Test Results

20+ test cases were run against commitlint v20.5.0 with `@commitlint/config-conventional`. Each wrote a commit message to a temp file and ran `npx commitlint --edit`. Results confirmed the regex analysis above.

### Patterns that TRIGGER the bug (body-empty fails):

| Pattern                        | Example                    | Why                                                       |
| ------------------------------ | -------------------------- | --------------------------------------------------------- |
| `#` + digits at end of body    | `text #123`                | `123` matches `[\w-]+`, end-of-string satisfies lookahead |
| `#` + digits mid-line          | `see #123 for details`     | `123` matches `[\w-]+`, space satisfies lookahead         |
| `#` + letters                  | `see #abc for details`     | `abc` matches `[\w-]+`, space satisfies lookahead         |
| `#` + mixed alphanumeric       | `see #SN-0001 for details` | `SN-0001` matches `[\w-]+`, space satisfies lookahead     |
| `#` + digits + trailing letter | `ref #123a here`           | `123a` matches `[\w-]+`, space satisfies lookahead        |
| `#` + digits + comma           | `see #123, which`          | `,` is in the lookahead set                               |
| `#` + digits + closing paren   | `see #123) for more`       | `)` is in the lookahead set                               |
| `#` + digits + closing bracket | `see #123] for more`       | `]` is in the lookahead set                               |
| `#` + digits + semicolon       | `see #123; and`            | `;` is in the lookahead set                               |
| Multiple `#` references        | `see #123 and #456`        | First match triggers it                                   |

### Patterns that do NOT trigger the bug (pass):

| Pattern                    | Example              | Why                                                                 |
| -------------------------- | -------------------- | ------------------------------------------------------------------- |
| `#` + space + text         | `this is # a note`   | `[\w-]+` requires at least one char after `#`                       |
| Bare `#`                   | `text with #`        | `[\w-]+` requires at least one char after `#`                       |
| `#` + digits + period      | `see #123. for info` | `.` is not in the lookahead set                                     |
| `#` + digits + slash       | `see #123/ref`       | `/` consumed by group 1, so `#` is part of the "action" prefix      |
| `#` + digits + colon       | `see #123: for info` | `:` is not `[\w-]` and not in the lookahead set                     |
| `#` + digits + exclamation | `see #123! wow`      | `!` is not `[\w-]` and not in the lookahead set                     |
| `#` + digits + backtick    | `` see #123` ``      | `` ` `` is not `[\w-]` and not in the lookahead set                 |
| `#` + digits + `@`         | `see #123@host`      | `@` is not `[\w-]` and not in the lookahead set                     |
| `Refs: #123` in footer     | `body\n\nRefs: #123` | Correct conventional-commits format — parser expects refs in footer |

## What Didn't Work

- **`parserPreset.parserOpts.issuePrefixes: []`** — The local config override is loaded (confirmed via `--print-config`) but the preset's `issuePrefixes: ['#']` wins during the merge process
- **`referenceActions: null`** — Disabling reference actions did not prevent the parser from treating `#` + identifier as a footer
- **CRLF vs LF line endings** — Not the cause; both produce the same error
- **`git commit -F <file>` vs `-m` flags** — Both code paths hit the same parser bug

## Solution

### Move Issue References to the Footer

Never put `#` followed by an identifier (`[A-Za-z0-9_-]+`) in the commit body. Always use the `Refs:` footer syntax:

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
Brainstorm identifies the pain point (issue 26376)
```

## Why This Works

The `Refs: #NNNNN` footer syntax is the correct conventional-commits format for issue references. The parser expects references in the footer, not the body. By keeping `#` + identifier out of the body entirely, the parser correctly identifies the body text and the `body-empty` rule passes.

## Real-World Impact: Sidenote Naming Convention

This bug directly influenced the design of our sidenote capture system (`rm-sidenotes`). Sidenotes use sequential IDs like `SN-0001`, `SN-0002`, etc. The natural instinct is to reference them as `#SN-0001` in commit messages — but this triggers the bug because `SN-0001` matches `[\w-]+`.

**Design decision:** Sidenotes are referenced as `SN-NNNN` (no `#` prefix) in commit bodies. This is semantically correct — sidenotes are local file IDs, not GitHub issue references. The `#` prefix is a GitHub convention that has no meaning for local markdown files.

```
# Good — no # prefix for sidenotes
feat(sidenotes): capture SN-0004

Agent instructions should surface es.exe as a primary search tool.
See SN-0004 for the full context.

# Bad — triggers commitlint body-empty
feat(sidenotes): capture SN-0004

Agent instructions should surface es.exe as a primary search tool.
See #SN-0004 for the full context.
```

This convention is documented in `.opencode/skills/rm-sidenotes/SKILL.md` under the `## COMMIT MESSAGES` section.

## Prevention

- **Updated `rm-commit` skill** — Added CRITICAL rule: "NEVER put `#` followed by an identifier (`[A-Za-z0-9_-]+`) in the commit body — the conventional-commits-parser treats them as footers, making the body appear empty to commitlint. Always use `Refs: #NNNNN` in the footer instead."
- **Use `Refs:` footer** — This is the correct conventional-commits format anyway
- **Avoid `#` in body prose** — If you must mention an issue number, write it without the `#` prefix
- **Remember: it's not just digits** — `#abc`, `#SN-0001`, `#XYZ` all trigger the bug, not just `#1234`

## Related

- `docs/solutions/developer-experience/fix-rm-commit-heredoc-syntax-and-line-length-violations-2026-04-03.md` — PowerShell here-string approach for git commits
- `.opencode/skills/rm-commit/SKILL.md` — Updated with `#` + identifier body warning
- `conventional-commits-parser` source: `dist/regex.js` line 26 — the exact regex that causes the bug
