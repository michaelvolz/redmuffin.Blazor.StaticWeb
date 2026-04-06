---
name: rm-guide-blazor
description: "Shortcut: rm:guide-blazor. Use when building or reviewing Blazor components, render behavior, lifecycle code, or component DI."
---

# rm-guide-blazor

## CRITICAL

- Prefer component composition over inheritance.
- Use `required` injected properties in component code-behind.
- Keep UI state small and renders intentional.

## WHEN TO LOAD

- Creating or refactoring `.razor` components.
- Adjusting lifecycle, state, event, or render behavior.

## GUIDANCE

- Use `OnInitializedAsync` / `OnParametersSetAsync` for lifecycle work.
- Prefer `EventCallback` over custom delegate plumbing.
- Use `ShouldRender()` only when you can justify the optimization.

## NEVER

- Do not hide large logic blocks inside markup.
- Do not use unnecessary re-renders as a state strategy.
