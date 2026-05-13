---
name: rm-guide-blazor
description: "Use when building or reviewing Blazor components, render behavior, lifecycle code, component DI, granularity decisions, or component architecture."
---

# rm-guide-blazor

See also: `rm-guide-cleanup` §5 for Blazor-specific false-positives and
lifecycle rules, §4 for ConfigureAwait in Blazor WASM.

## CRITICAL

- Prefer component composition over inheritance.
- Use `required` injected properties in component code-behind.
- Keep UI state small and renders intentional.
- **Smart/dumb pattern (MANDATORY):** Smart components orchestrate data;
  dumb components take `[Parameter]` props and render. Never mix data
  fetching and rendering in the same component.
- **Single responsibility:** A component does ONE thing. If the name
  contains "and", split it.
- **No copy-paste components:** Two 95%-identical files is the #1
  anti-pattern. Decompose into shared Lego bricks; never duplicate.

## WHEN TO LOAD

- Creating or refactoring `.razor` components.
- Adjusting lifecycle, state, event, or render behavior.
- Deciding component boundaries and granularity.

## GUIDANCE

- Use `OnInitializedAsync` / `OnParametersSetAsync` for lifecycle work.
- Prefer `EventCallback` over custom delegate plumbing.
- Use `ShouldRender()` only when you can justify the optimization.

### Blazor lifecycle methods (not dead code)

`OnInitialized`, `OnInitializedAsync`, `OnParametersSet`, `OnAfterRender`,
`OnAfterRenderAsync` are called by the runtime. Static analyzers may flag
them as dead code or uncovered — they are **false positives**. Do not
remove them or add pragmas.

### [Inject] properties and MA0015

The MA0015 warning on `[Inject]` properties is a **false positive** for
Blazor. These are injected by the DI container, not passed as method
parameters. Do not suppress; add the comment `// Injected by DI container`.

### ConfigureAwait in Blazor WASM

Blazor WASM runs single-threaded with no `SynchronizationContext`. Do NOT
use `ConfigureAwait(false)` in component code-behind — it is unnecessary.

### Fire-and-forget

Use `InvokeAsync(() => ...)` and call `StateHasChanged()` after async
operations that modify state outside lifecycle events.

## NEVER

- Do not hide large logic blocks inside markup.
- Do not use unnecessary re-renders as a state strategy.
