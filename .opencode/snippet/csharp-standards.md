---
aliases: [cs, csharp]
description: C# coding standards and conventions
---
Follow these C# conventions from the project AGENTS.md:

**Formatting:**
- Tab indentation (4 tabs = 4 spaces)
- Max line length: 160 characters
- Opening brace on new line

**Naming:**
- Types/Namespaces: PascalCase
- Methods/Properties: PascalCase
- Private fields: camelCase
- Static readonly fields: UpperCamelCase_underscore
- Interfaces: Prefix with "I"

**C# 12/13 Features:**
- Primary constructors
- Collection expressions ([1, 2, 3])
- ref readonly parameters
- Use nameof instead of string literals

**Nullable:**
- Declare variables non-nullable
- Use is null or is not null (NOT == null)

**Error Handling:**
- Use LoggerMessage delegates
- Throw specific exceptions with meaningful messages