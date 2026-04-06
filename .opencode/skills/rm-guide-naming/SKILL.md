---
name: rm-guide-naming
description: "Shortcut: rm:guide-naming. Use when creating, renaming, or reviewing C# names for types, members, namespaces, and test doubles."
---

# rm-guide-naming

## CRITICAL

- Use PascalCase for types, namespaces, methods, properties, events, enums.
- Use `camelCase` for locals and parameters.
- Use `_camelCase` for private fields.
- Prefix interfaces with `I`.
- Name test doubles as `[Class]_[Type]` (`Mock`, `Stub`, `Spy`, `Fake`, `Dummy`).

## WHEN TO LOAD

- Creating or renaming C# files, classes, records, enums, interfaces, methods.
- Reviewing names in new tests or test doubles.

## GUIDANCE

- Prefer explicit, intention-revealing names.
- Avoid abbreviations unless the domain already uses them.
- Match existing repo names exactly when extending a pattern.

## NEVER

- Do not invent new naming schemes inside a feature.
- Do not use Hungarian notation.
