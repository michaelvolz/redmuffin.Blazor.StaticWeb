---
name: rm-output-style
description: "Shortcut: rm:style. C# formatting, naming conventions, C# 12/13 features, and nullable reference types. Use when writing or reviewing C# code, formatting files, naming types/variables, or handling nullable types."
---

# Output Style and C# Standards

## Formatting

| File Type       | Indentation    | Notes             |
| --------------- | -------------- | ----------------- |
| C#              | Tab (4 spaces) | -                 |
| .razor, .cshtml | 4 spaces       | -                 |
| .csproj         | 2 spaces       | -                 |
| All             | Max 160 chars  | Brace on new line |

## Naming Conventions

| Type               | Convention       | Example                   |
| ------------------ | ---------------- | ------------------------- |
| Types/Namespaces   | PascalCase       | `HomePage`, `UserService` |
| Methods/Properties | PascalCase       | `GetUser()`               |
| Private fields     | camelCase        | `_userService`            |
| Static readonly    | UpperCamelCase\_ | `LogEvent`                |
| Interfaces         | Prefix "I"       | `IUserService`            |
| Test doubles       | `[Class]_[Type]` | `NavigationManager_Mock`  |

## C# 12/13 Features

- Primary constructors
- Collection expressions: `[1, 2, 3]`
- `ref readonly` parameters
- Pattern matching in switch expressions
- `nameof` not string literals

## Nullable Reference Types

- Declare non-nullable
- Check `null` at entry points
- `is null` / `is not null` (NOT `== null`)

## File-Scoped Namespaces

```csharp
namespace MyNamespace;
// Single-line using directives
// System.* first, then alphabetical
```

**Priority:** Tables > bullets > single-line > prose. ALL info preserved, verbosity removed.
