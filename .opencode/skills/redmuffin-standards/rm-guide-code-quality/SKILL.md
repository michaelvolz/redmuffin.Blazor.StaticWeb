---
name: rm-guide-code-quality
description: "Shortcut: rm:guide-code-quality. Use when reviewing style, readability, null handling, records, or general C# code quality."
---

# rm-guide-code-quality

**Note:** The comprehensive code quality standard is now `rm-guide-cleanup`.
Load it alongside this guide for superfluous code taxonomy, characterization
tests, method quality standards, async patterns, collection abstractions, and
the code review checklist. This guide covers style conventions only.

## CRITICAL

- Prefer clear, self-documenting code.
- Use records for immutable DTOs and value shapes.
- Use `is null` / `is not null` instead of `== null` / `!= null`.
- Keep methods small and focused.

## WHEN TO LOAD

- Reviewing style, maintainability, or readability.
- Shaping public APIs and DTOs.

## GUIDANCE

- Use expression-bodied members for trivial computed members only.
- Add XML docs to public APIs when they improve discoverability.
- Favor immutable defaults unless mutation is clearly required.

## NEVER

- Do not trade clarity for clever syntax.
- Do not introduce comments that explain obvious code.
