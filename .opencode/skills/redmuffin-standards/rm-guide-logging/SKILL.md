---
name: rm-guide-logging
description: "Use when adding structured logging or LoggerMessage delegates."
---

# rm-guide-logging

See also: `rm-guide-cleanup` §7 for `[LoggerMessage]` source generator pattern.

## CRITICAL

- Use structured `ILogger<T>` logging.
- Never use the legacy `LoggerMessage.Define` delegate pattern.
- Put logging declarations in `*.Logging.cs` partial class files only.

## WHEN TO LOAD

- Adding logs to services, functions, components, or handlers.
- Splitting a partial class for logging declarations.

## GUIDANCE

```csharp
private static partial class Log
{
    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Image load failed for {Url}: {Error}")]
    public static partial void ImageLoadFailed(
        ILogger logger, string url, string error);
}
```

- Dynamic messages (with `$` interpolation) defeat structured logging.
  Use format templates with named placeholders.
- Keep log messages short and parameterized.
- Log the outcome, not noisy internal state.

## NEVER

- Do not place logging declarations in the main logic file.
- Do not log secrets or full payloads.
- Do not use ad hoc `_logger.LogError($"message {variable}")` —
  use source-generated methods.
