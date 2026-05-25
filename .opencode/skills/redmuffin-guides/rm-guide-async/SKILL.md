---
name: rm-guide-async
description: "Use when writing async methods, cancellation flows, or Task-based APIs."
---

# rm-guide-async

## CRITICAL

- Use `Async` suffix on async methods.
- Return `Task` / `Task<T>`; avoid `async void` except event handlers.
- Never block on async work with `.Result`, `.Wait()`, or `GetAwaiter().GetResult()`.

## ConfigureAwait(false) Rules

| Context                              | Use ConfigureAwait(false)?                  |
| ------------------------------------ | ------------------------------------------- |
| Blazor WASM (single-threaded)        | No — no SynchronizationContext exists       |
| Library code (no UI context)         | Yes — avoid capturing unknown context       |
| Blazor Server (SignalR circuit)      | No — need the Blazor SynchronizationContext |
| ASP.NET Core (no HttpContext needed) | Yes — avoid capturing HttpContext           |
| Console apps, background services    | Yes                                         |
| Azure Functions (isolated)           | Not needed — no SynchronizationContext      |

In our Blazor WASM project: do not use `ConfigureAwait(false)` in
component code-behind. It is unnecessary and a noise indicator.

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
