---
applyTo: "**/*.razor"
---

# Project coding standards

## General C# Coding Standards
- Use `var` only when the type is obvious; otherwise, use explicit types.
- Keep line length under 160 characters.
- Use consistent indentation and always include braces (`{}`) even for single-line statements.
- Group `using` directives with `System.*` first, then others in alphabetical order.

## Naming Conventions
- Use `PascalCase` for component names, classes, methods, and properties.
- Use `camelCase` for parameters and local variables.
- Prefix private fields with `_` (e.g., `_userService`).
- Blazor component files must match the component class name (e.g., `MyComponent.razor` must contain `MyComponent`).

## Blazor-Specific Best Practices
- Split large components into smaller, reusable child components.
- Use `@code { }` instead of `@functions { }`.
- Keep UI markup and C# logic separate when complexity grows (e.g., use partial classes).
- Avoid directly mutating bound parameters (`[Parameter]`) in child components.
- Use `EventCallback<T>` instead of `Action` or custom delegates for parameter events.
- Use `CascadingParameter` for passing data like authentication state, theme, or culture.
- Prefer `OnInitializedAsync()` over `OnInitialized()` when using `await`.

## Component Architecture
- Organize components by domain/feature in folders (e.g., `Pages/`, `Components/`, `Shared/`).
- Follow the MVU or MVVM pattern when the state becomes complex.
- Use `@inject` for dependency injection rather than service locators.
- Prefer `RenderFragment` over `MarkupString` unless you need raw HTML rendering.

##  Performance Best Practices
- Minimize re-rendering by using `ShouldRender()` or conditional UI logic.
- Use `@key` in `@foreach` loops to help Blazor track DOM elements.
- Avoid using `async void`; use `async Task` instead.
- Dispose components that use resources by implementing `IDisposable`.

## Security Guidelines
- Never trust client-side validation—always validate on the server.
- Avoid exposing sensitive logic or secrets in `.razor` files.
- Use `Microsoft.AspNetCore.Components.Authorization` for secure user authentication and role checking.
- Use proper encoding when injecting raw HTML or third-party content.

## Reusability and Maintainability
- Prefer `RenderFragment` parameters to allow child content injection (similar to slot in other frameworks).
- Isolate reusable logic in services or base classes.
- Use feature-based folders to group pages, components, and services.

## Testing & Tooling
- Use `bUnit` for unit testing Blazor components.
- Mock services using `Moq`, `FakeItEasy`, or `NSubstitute` in test projects.
- Use `IJSRuntime` abstraction for JavaScript interop, and mock it in tests.
- Validate components for accessibility (ARIA, keyboard navigation).

## Debugging and Diagnostics
- Use `@ref` cautiously to avoid tight coupling.
- Enable detailed error messages in development mode.
- Use browser dev tools and Blazor’s built-in error boundaries.

## General .NET Testability Guidelines
- Follow the Arrange-Act-Assert pattern in unit tests.
- Ensure all services are injected through interfaces.
- Avoid static classes unless stateless and pure utility.
- Keep logic out of the UI layer when possible for easier testing.