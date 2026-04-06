---
name: rm-guide-async
description: "Shortcut: rm:guide-async. Use when writing async methods, cancellation flows, or Task-based APIs."
---

# rm-guide-async

## CRITICAL

- Use `Async` suffix on async methods.
- Return `Task` / `Task<T>`; avoid `async void` except event handlers.
- Never block on async work with `.Result`, `.Wait()`, or `GetAwaiter().GetResult()`.
- Apply `ConfigureAwait(false)` on awaited work in library code.

## WHEN TO LOAD

- Any method that awaits I/O, timers, HTTP, database, or background work.
- Any change involving cancellation tokens or concurrency coordination.

## GUIDANCE

```csharp
var response = await httpClient.GetAsync(url, token).ConfigureAwait(false);
```

- Use `Task.WhenAll` for independent parallel work.
- Use `Task.WhenAny` for first-completed or timeout patterns.
- Pass `CancellationToken` through long-running flows.

## NEVER

- Do not swallow exceptions.
- Do not start background work without a clear ownership model.
