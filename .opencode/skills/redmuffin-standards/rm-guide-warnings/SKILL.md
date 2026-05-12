---
name: rm-guide-warnings
description: "Shortcut: rm:guide-warnings. Use when fixing analyzer warnings, pragma directives, or zero-warning build issues."
---

# rm-guide-warnings

See also: `rm-guide-cleanup` §8 for the no-pragma-suppression policy.

## CRITICAL

- Target zero build warnings.
- **Never suppress warnings without investigation.** Warnings are signals
  that code is not following best practices. Fix the root cause first.
- When two enabled analyzer rules demand opposite things on the same code,
  a pragma may be the only correct resolution. Follow the decision tree
  below.
- Existing `#pragma warning disable` directives are grandfathered. Do not
  remove them unless explicitly instructed.

## WHEN TO LOAD

- Cleaning analyzer output or build warnings.
- Reviewing code that already contains pragmas.
- Investigating a build warning that needs a fix.

## GUIDANCE

### dotnet format Usage

Run one command per solution from the solution-level directory. No extra
flags needed — `dotnet format` auto-detects the solution and fixes all
auto-fixable violations:

```bash
# Main solution (from repo root)
dotnet format

# Tools solution (from tools/)
dotnet format tools/redmuffin.Tools.slnx
```

**What it auto-fixes (~75% of violations):**

- IDE code-style rules (IDE0028 collection initializer, IDE0290 primary
  constructor, IDE0305 collection expression, IDE0059 unused value)
- Most SA formatting rules (SA1507 blank lines, SA1513 closing brace
  spacing, SA1201 field ordering)
- Some MA rules (MA0002 string comparer, MA0076 culture on ToString)

**What it cannot auto-fix (requires manual work):**

- IDE1006 naming style — code fix exists but doesn't support
  "Fix All in Solution." Fix each occurrence individually.
- SA1600-SA1616 documentation rules — no code fix available, or fix
  doesn't support Fix All. Must write XML doc comments manually.
- MA0048 file name mismatch — requires extracting types or renaming files.
- SA1402 multiple types per file — requires extracting types.

**Do NOT use `--severity info` or target sub-projects.** These cause
`dotnet format` to analyze but not apply fixes ("0 files already
formatted"). The working command is just `dotnet format` at the
solution level.

- If a fix is truly impossible and the warning is a known false positive
  for our context, document it in the KNOWN FALSE POSITIVES table below
  rather than suppressing per-file.

### Pragma Decision Tree

A `#pragma warning disable` is acceptable ONLY when ALL three questions
answer YES:

**Q1: Is this a genuine analyzer conflict or false positive?**
Two enabled rules demand opposite things, OR the rule fires on code
that is correct by design and cannot be restructured around the check.
→ _If NO:_ fix the code. The warning found a real problem.

**Q2: Would satisfying this rule make the code WORSE by another metric?**
E.g., losing meaningful abstraction, adding false mutability signals,
reducing readability, or breaking a deliberate established pattern.
→ _If NO:_ satisfy the rule. The fix is a net improvement.

**Q3: Is this a one-off case, not a recurring pattern?**
→ _If YES:_ use a single targeted pragma with an explanatory comment.
→ _If NO (recurring pattern):_ do NOT scatter individual pragmas.
Document the pattern as a known exception below, then apply one
file-level or class-level pragma. If the pattern spans many files,
consult the user about `.editorconfig` configuration.

**Every pragma MUST include a comment** on the same line explaining
which competing rule takes priority and why:

```csharp
#pragma warning disable CA1859 // MA0016 (collection abstraction) takes priority
```

## KNOWN CONFLICTS AND FALSE POSITIVES

| Warning | Context                                                  | Resolution                                                                                                                                                                                                                                                                              |
| ------- | -------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| MA0015  | `[Inject]` properties in Blazor components               | Not method parameters — injected by DI container. Comment: `// Injected by DI container`.                                                                                                                                                                                               |
| CA1812  | `ArchConfig.ArchConfigDto` used via YAML deserialization | Instantiated by YamlDotNet at runtime.                                                                                                                                                                                                                                                  |
| CA1859  | Collection abstraction params (MA0016 conflict)          | CA1859 wants concrete types for perf; MA0016 wants abstractions. On .NET JIT devirtualizes `List<T>` interface calls — no real perf gain. Abstractions communicate intent (`IReadOnlyList` = read-only, `ICollection` = add-only). Use file-level pragma when both fire on same params. |

## NEVER

- Do not add pragmas without first investigating whether the code can be
  improved to satisfy the analyzer.
- Do not use pragmas as a shortcut to skip real work.
- Do not remove deliberate pragmas without explicit approval.
- Do not silence a warning by rewriting code to be worse.
- Never touch `.editorconfig` or analyzer configuration without explicit
  user consultation.
