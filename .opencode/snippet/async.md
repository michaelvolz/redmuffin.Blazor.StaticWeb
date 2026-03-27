---
aliases: [async, async-await]
description: C# async programming best practices
---
C# Async Programming Best Practices

## Naming
- Use 'Async' suffix for all async methods
- Match sync counterparts: `GetDataAsync()` for `GetData()`

## Return Types
- Return `Task<T>` when returning a value
- Return `Task` when no value
- Consider `ValueTask<T>` for high-performance to reduce allocations
- Avoid `void` except for event handlers

## Exception Handling
- Use try/catch around await expressions
- Use ConfigureAwait(false) to prevent deadlocks in library code
- NEVER swallow exceptions silently

## Performance
- Use Task.WhenAll() for parallel execution
- Use Task.WhenAny() for timeouts/first-completed
- Consider cancellation tokens for long-running operations

## Common Pitfalls (NEVER DO)
- Never use `.Wait()`, `.Result`, or `.GetAwaiter().GetResult()`
- Avoid mixing blocking and async code
- Don't create async void methods (except event handlers)
- Always await Task-returning methods

## Patterns
- Async command pattern for long-running operations
- IAsyncEnumerable<T> for async streams
- Task-based asynchronous pattern (TAP) for public APIs