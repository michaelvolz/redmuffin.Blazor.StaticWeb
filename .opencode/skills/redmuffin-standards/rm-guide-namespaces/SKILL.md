---
name: rm-guide-namespaces
description: "Shortcut: rm:guide-namespaces. Use when creating new C# files or organizing namespaces."
---

# rm-guide-namespaces

## CRITICAL

- Use file-scoped namespaces in C# files.
- Namespace paths should mirror folder structure.

## WHEN TO LOAD

- Creating any new `.cs` file.
- Moving a type between folders or layers.

## GUIDANCE

```csharp
namespace redmuffin.Blazor.StaticWeb.Features.Home;
```

- Keep namespaces concise and predictable.
- Match the file's responsibility, not its historical origin.

## NEVER

- Do not add block-scoped namespaces in new code.
