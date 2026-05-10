---
name: rm-guide-warnings
description: "Shortcut: rm:guide-warnings. Use when fixing analyzer warnings, pragma directives, or zero-warning build issues."
---

# rm-guide-warnings

See also: `rm-guide-cleanup` §8 for the no-pragma-suppression policy.

## CRITICAL

- Target zero build warnings.
- **Never add `#pragma warning disable` to new code.** Warnings are signals
  that code is not following best practices. Fix the root cause.
- Existing `#pragma warning disable` directives are grandfathered. Do not
  remove them unless explicitly instructed.

## WHEN TO LOAD

- Cleaning analyzer output or build warnings.
- Reviewing code that already contains pragmas.
- Investigating a build warning that needs a fix.

## GUIDANCE

- Use `dotnet format src/<project> --severity info` to auto-fix ~75% of
  StyleCop and Roslyn-analyzer violations before manual work.
- Fix root causes before considering any workaround.
- If a fix is truly impossible and the warning is a known false positive
  for our context (e.g., MA0015 on `[Inject]` properties in Blazor, or
  Blazor lifecycle methods flagged as dead code), document it in
  `rm-guide-cleanup` as an explicit exception rather than suppressing
  per-file.

## KNOWN FALSE POSITIVES (do not suppress)

| Warning | Context                                                  | Handling                                                                                  |
| ------- | -------------------------------------------------------- | ----------------------------------------------------------------------------------------- |
| MA0015  | `[Inject]` properties in Blazor components               | Not method parameters — injected by DI container. Comment: `// Injected by DI container`. |
| CA1812  | `ArchConfig.ArchConfigDto` used via YAML deserialization | Instantiated by YamlDotNet at runtime.                                                    |

## NEVER

- Do not add new `#pragma warning disable` lines.
- Do not remove deliberate pragmas without explicit approval.
- Do not silence a warning by rewriting code to be worse.
