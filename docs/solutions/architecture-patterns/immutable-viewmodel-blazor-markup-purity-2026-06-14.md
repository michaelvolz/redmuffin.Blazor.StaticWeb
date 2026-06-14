---
module: architecture-patterns
tags: [blazor, viewmodel, immutability, markup-purity, factory-method]
problem_type: pattern
date: 2026-06-14
---

# Immutable ViewModel + Static Factory Pattern for Blazor Markup Purity

## Problem

`@if` / `@foreach` / inline `@(expr)` in `.razor` files mix decision logic
with markup, making components untestable without bUnit and violating the
Humble Object pattern. Moving logic to `.razor.cs` with mutable fields
(`_isLoading = true; _data = response`) creates a different problem:
state transitions span multiple statements, each requiring `StateHasChanged`
or `InvokeAsync`.

## Solution

Move ALL display state into an immutable `sealed record` ViewModel.
State transitions are wholesale replacement via static factory methods.
The `.razor` file reads only pre-computed display properties.

### Pattern

1. **ViewModel record** — one `sealed record` per page component.
   All display state (bool flags, icons, text, lists, CSS classes) is
   pre-computed in the constructor or factory methods. Zero mutable
   properties. Zero domain objects exposed.

2. **Static factory methods** — one per logical page state:
   `Idle`, `Loading()`, `Success(data)`, `Failure(error)`.
   Each produces a fully-populated immutable instance.

3. **Snapshot replacement** — the code-behind holds one `_viewModel`
   field. State changes are assignment: `_viewModel = PageViewModel.Loading()`.
   No `StateHasChanged` calls needed — Blazor detects the field reference
   change automatically.

4. **Child types in separate files** — if the ViewModel references
   data records or display types, each goes in its own `.cs` file
   (StyleCop SA1402: one type per file).

### Canonical Example

`Features/ApiHealth/` — uses all six infrastructure components on one page:

- `ApiHealthViewModel.cs` — immutable record, 4 factory methods
- `ApiHealthData.cs` — pure data record
- `HealthCheckItem.cs` — display record with pre-computed `StatusIcon`, `RowCssClass`
- `ApiHealth.razor` — zero `@if` / `@foreach`. Pure component composition.
- `ApiHealth.razor.cs` — one field `_viewModel`, one async method `RunHealthCheckAsync`

### What Does Not Belong in the ViewModel

- Domain types (the ViewModel is the boundary — domain data becomes ViewModel input, never exposed)
- Business logic (computed properties project display state, never compute business decisions)
- Mutable setters (the record is immutable after construction)

### Why This Pattern

- **Testable without bUnit**: pass `new ViewModel()` to assert computed properties
- **No `StateHasChanged`**: Blazor detects reference change automatically
- **Single source of truth**: every display value traces to one `_viewModel` field
- **Factory methods are seams**: substitute any ViewModel variant in tests
- **Zero `@code` blocks**: all logic lives in `.cs` files

## See Also

- `rm-blazor` §Markup Purity — rules and replacement patterns
- `rm-blazor` §Immutable ViewModel Pattern — abbreviated reference
- `rm-naming` §CRITICAL — `_camelCase` fields, no abbreviations
