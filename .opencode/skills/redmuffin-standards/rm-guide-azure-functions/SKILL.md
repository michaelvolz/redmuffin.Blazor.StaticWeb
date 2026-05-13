---
name: rm-guide-azure-functions
description: "Use when creating or reviewing Azure Functions isolated worker code."
---

# rm-guide-azure-functions

## CRITICAL

- Use isolated worker patterns.
- Keep functions small and single-purpose.
- Validate inputs early and return clear status codes.
- Use structured `ILogger` logging.

## WHEN TO LOAD

- Creating or changing function handlers, bindings, or host setup.

## GUIDANCE

- Use async all the way through I/O paths.
- Keep configuration in environment variables or options.
- Make event-driven handlers idempotent when duplicates are possible.

### Thin Wrapper Pattern (Shared Static Handler)

When two or more Azure Functions share near-identical HTTP logic (differing
only in collection ID, log messages, or Function name), extract a shared
static handler class and keep Functions as 5-line thin wrappers:

```csharp
// Shared handler in Api/Core/RaindropListFetcher.cs:
public static class RaindropListFetcher
{
    public static async Task<HttpResponseData> FetchAsync(
        HttpRequestData request, string collectionId, string bearerToken,
        IHttpClientFactory httpClientFactory, ILogger logger,
        Action<ILogger, string> logFetch, Action<ILogger, Exception> logError,
        CancellationToken cancellationToken)
    {
        // All HTTP, JSON parsing, error handling lives here
    }
}

// Thin wrapper Function — 5 lines:
[Function("RaindropListArticles")]
public Task<HttpResponseData> RunAsync(
    [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData request)
{
    Log_FunctionProcessed(logger);
    return RaindropListFetcher.FetchAsync(
        request, TargetCollectionId, _settings.RainDropTestToken ?? string.Empty,
        _httpClientFactory, logger, Log_FetchArticles, Log_ErrorFetchingArticles,
        request.FunctionContext.CancellationToken);
}
```

**Rules:**

- Log delegates inject via `Action<ILogger, string>` / `Action<ILogger, Exception>`
  so each Function's `[LoggerMessage]` partial methods are preserved
- Bearer token: `_settings.RainDropTestToken ?? string.Empty` (null-safe)
- Cancellation: pass `request.FunctionContext.CancellationToken` through

### Duplicate Elimination

When two Azure Functions have near-identical logic (≥50% overlap), extract
shared logic into a static helper. Follow the Rule of Three: extract only
when 2+ instances of duplication exist and the shared behavior contract is
clear. Examples:

- `RaindropListFetcher.FetchAsync` — shared HTTP+JSON parsing for list endpoints
- `RaindropBackgroundRefreshHelper.TryFetchFreshDataAsync` — shared background
  refresh for collection views

## TESTING

API test projects **must NOT** reference `Microsoft.Azure.Functions.Worker.Sdk`.
The SDK's build target replaces `dotnet run` with `func start`, blocking TUnit.

Use `ControlledHttpHandler_Fake` (see `rm-guide-testing`) for deterministic
HTTP responses with zero real HTTP calls. Wire through `HttpClientFactory_Fake`.

Install Azure Functions Core Tools for integration tests:

```
yay -S azure-functions-core-tools-bin
```

## NEVER

- Do not block on async work.
- Do not let HTTP handlers return opaque failures.
