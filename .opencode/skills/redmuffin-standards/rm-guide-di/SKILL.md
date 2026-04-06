---
name: rm-guide-di
description: "Shortcut: rm:guide-di. Use when injecting dependencies, registering services, or shaping component/service constructors."
---

# rm-guide-di

## CRITICAL

- Use constructor injection for required dependencies.
- In Blazor components, use `[Inject]` with `required`.
- Use `IOptions<T>` for configuration; avoid raw `IConfiguration` in business code.
- Avoid service locator patterns.

## WHEN TO LOAD

- Adding services, handlers, or components that depend on app services.
- Registering new services in DI.

## GUIDANCE

```csharp
public partial class Home : ComponentBase
{
    [Inject] public required ILogger<Home> Logger { get; set; }
}
```

- Prefer small, focused services.
- Use keyed services only when multiple implementations are genuinely needed.

## NEVER

- Do not instantiate dependencies inside methods when DI can supply them.
- Do not inject scoped services into singletons.
