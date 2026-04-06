---
name: rm-guide-logging
description: "Shortcut: rm:guide-logging. Use when adding structured logging or LoggerMessage delegates."
---

# rm-guide-logging

## CRITICAL

- Use structured `ILogger<T>` logging.
- Put `LoggerMessage` declarations in `*.Logging.cs` files only.
- Prefer `LoggerMessage` over ad hoc `LogError`/`LogInformation` calls in hot paths.

## WHEN TO LOAD

- Adding logs to services, functions, components, or handlers.
- Splitting a partial class for logging declarations.

## GUIDANCE

```csharp
private static readonly Action<ILogger, string, Exception?> LogProcessing
    = LoggerMessage.Define<string>(LogLevel.Information, new EventId(1), "Processing {Item}");
```

- Keep log messages short and parameterized.
- Log the outcome, not noisy internal state.

## NEVER

- Do not place `LoggerMessage` declarations in the main logic file.
- Do not log secrets or full payloads.
