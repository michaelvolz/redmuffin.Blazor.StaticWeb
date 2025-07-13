---
description: 'Optimized Blazor component and application patterns for AI coding assistants'
applyTo: '**/*.razor, **/*.razor.cs, **/*.razor.css'
---

**FOR AI CODE ASSISTANTS ONLY** - Blazor WebAssembly .NET 9 development guidelines

## Code Style and Structure

- Write idiomatic and efficient Blazor C# code following .NET conventions
- **Structure:** UI (.razor), Logic (partial classes)
- Use `var` only when type is clearly apparent (e.g., `var items = new List<string>()`)
- Keep line length under 160 characters maximum
- Always use braces, even for single-line statements
- Group `using` directives with `System.*` first, then others alphabetically
- Always use `async`/`await` for non-blocking operations
- Avoid `async void`; use `async Task` instead

## Naming Conventions

- **PascalCase:** Component names, classes, methods, properties
- **camelCase:** Parameters, local variables, private fields
- **Prefix:** `_` for private fields (e.g., `_userService`)
- **Interfaces:** Prefix with "I" (e.g., `IUserService`)
- **Components:** File names must match component class names (e.g., `MyComponent.razor` contains `MyComponent`)

## Component Architecture

- **Organization:** Feature-based folders (`src/redmuffin.Blazor.StaticWeb/Features/`)
- **Structure:** Pages/, Components/, Shared/ within features
- **Patterns:** Follow MVU or MVVM when state becomes complex
- **Separation:** Split large components into smaller, reusable child components
- **Logic:** Keep UI markup and C# logic separate with partial classes for complexity
- **Dependency Injection:** Use `@inject` rather than service locators

## Blazor-Specific Best Practices

- Use `@code { }` instead of `@functions { }`
- Prefer `OnInitializedAsync()` over `OnInitialized()` when using `await`
- Use `EventCallback<T>` instead of `Action` or custom delegates for parameter events
- Avoid directly mutating bound parameters (`[Parameter]`) in child components
- Use `CascadingParameter` for authentication state, theme, or culture
- Prefer `RenderFragment` over `MarkupString` unless raw HTML rendering needed
- Use `@key` in `@foreach` loops to help Blazor track DOM elements
- Use `@ref` cautiously to avoid tight coupling

## Component Parameters and Events

- **Parameters:** Use `[Parameter]` with proper validation
- **Event Callbacks:** `[Parameter] public EventCallback<T> OnEvent { get; set; }`
- **Child Content:** `[Parameter] public RenderFragment? ChildContent { get; set; }`
- **Validation:** Favor strongly-typed parameters over dynamic

## Component Lifecycle

- Override `OnInitializedAsync()`, `OnParametersSetAsync()`, `OnAfterRenderAsync(bool firstRender)`
- Utilize Blazor's built-in lifecycle features appropriately
- Use data binding effectively with `@bind`
- Leverage Blazor DI for services, keep focused and small

## Performance Optimization

- **Rendering:** Minimize re-rendering using `ShouldRender()` or conditional UI logic
- **State:** Use `StateHasChanged()` sparingly, prefer parameter binding
- **Caching:** Implement appropriate caching strategies:
  - **Blazor Server:** `IMemoryCache` for in-memory caching
  - **Blazor WebAssembly:** localStorage/sessionStorage for client-side state
  - **Distributed:** Redis/SQL Server Cache for shared state
- **API Calls:** Cache responses to avoid redundant calls
- **Assets:** Optimize with bundling, minification, lazy loading, virtualization for large lists
- **Resources:** Implement `IDisposable` for event subscriptions and timers

## State Management

- **Built-in:** Cascading parameters, DI services, built-in Blazor patterns
- **Basic:** Use Cascading Parameters and EventCallbacks for basic state sharing
- **Advanced:** Implement libraries like Fluxor or BlazorState for complex applications
- **Client-side:** Use `Blazored.LocalStorage` or `Blazored.SessionStorage` for WebAssembly
- **Server-side:** Use Scoped Services and StateContainer pattern
- **Storage:** Use `IJSRuntime` for localStorage/sessionStorage via JS interop

## UI & Styling

- **Framework:** Zurb Foundation for all UI/layout
- **Styles:** Place in `wwwroot/css` or `wwwroot/scss`
- **Responsive:** Foundation grid/utilities or custom CSS
- **Accessibility:** Semantic HTML, ARIA roles, keyboard navigation, WCAG 2.1 AA compliance
- **Modern CSS:** Grid, Flexbox, variables, nesting, dark mode support
- **Images:** WebP/AVIF, `loading="lazy"`, `srcset`
- **Components:** Component-specific CSS using `.razor.css` files

## JavaScript Interop

- **Minimal:** Prefer C#/Blazor over JavaScript/HTML unless required
- **Interop:** Use `IJSRuntime.InvokeAsync<T>()` when necessary
- **Resources:** Dispose JS object references properly
- **Abstraction:** Use `IJSRuntime` abstraction for testing and mocking

## Forms and Validation

- **Forms:** Use `<EditForm>` with `EditContext` and validation attributes
- **Validation:** Implement with FluentValidation or DataAnnotations
- **Client-side:** Never trust client-side validation; always validate on server
- **UI:** Show loading indicators for async operations

## Error Handling and Security

- **Error Boundaries:** Wrap components in `<ErrorBoundary>` for error handling
- **Exceptions:** Handle with try/catch or error boundaries
- **Logging:** Use logging for backend error tracking
- **Security:** Sanitize inputs, enforce CSP, secure cookies, RBAC
- **Secrets:** Never expose sensitive logic or secrets in `.razor` files
- **Authentication:** Use `Microsoft.AspNetCore.Components.Authorization` for secure auth
- **Encoding:** Use proper encoding when injecting raw HTML or third-party content
- **XSS/CSRF:** Use Blazor built-ins and best practices

## API Integration

- **HTTP Client:** For Blazor WebAssembly: `[Inject] private HttpClient Http { get; set; } = default!;`
- **Server-side:** Use `IHttpClientFactory` for HTTP calls
- **Error Handling:** Implement error handling for API calls using try-catch
- **User Feedback:** Provide proper user feedback in UI for API operations
- **Minimal APIs:** Prefer minimal APIs for backend services

## Testing and Debugging

- **Unit Tests:** Use TUnit (NOT NUnit/xUnit/MSTest) for testing Blazor components
- **Attributes:** `[Test]` for methods, `[Tests]` with `[Arguments]` for data-driven tests
- **Mocking:** Use NSubstitute in test projects for mocking dependencies
- **Component Testing:** Use bUnit for unit testing Blazor components
- **Patterns:** Follow Arrange-Act-Assert pattern in unit tests
- **Debugging:** Use browser dev tools and Visual Studio debugging tools
- **Performance:** Use Visual Studio diagnostics tools for profiling
- **Accessibility:** Validate components for accessibility (ARIA, keyboard navigation)
- **Error Messages:** Enable detailed error messages in development mode

## Authentication and Authorization

- **Identity:** Use ASP.NET Core Identity or JWT tokens for authentication
- **Authorization:** Implement role-based access control (RBAC)
- **Communication:** Use HTTPS for all web communication
- **CORS:** Ensure proper CORS policies are implemented
- **Documentation:** Use Swagger/OpenAPI for API documentation
- **XML Docs:** Ensure XML documentation for models and API methods

## Reusability and Maintainability

- **Modularity:** Develop modular, reusable, testable components
- **Content Injection:** Prefer `RenderFragment` parameters for child content injection
- **Logic Isolation:** Isolate reusable logic in services or base classes
- **Feature Organization:** Use feature-based folders to group pages, components, and services
- **Interfaces:** Ensure all services are injected through interfaces
- **Static Classes:** Avoid static classes unless stateless and pure utility
- **UI Layer:** Keep logic out of UI layer when possible for easier testing

## Conditional Rendering

- **Directives:** Use `@if`, `@switch`, avoid complex logic in markup
- **Performance:** Minimize component render tree by avoiding unnecessary re-renders
- **Keys:** Use `@key` directive appropriately for dynamic content

## Modern C# Features (12/13)

- **Target:** .NET 9 Blazor WebAssembly, C# 12/13 features
- **Records:** Use record types for immutable data structures
- **Pattern Matching:** Leverage advanced pattern matching
- **Global Usings:** Use global usings appropriately
- **Collection Expressions:** Use `int[] nums = [1,2,3];` syntax
- **Primary Constructors:** Use when appropriate for cleaner code

## Development Workflow

- **References:** Reference code with filename and line numbers
- **Builds:** Keep builds and tests passing before merging
- **Documentation:** Update README/Wiki/OpenAPI for public APIs
- **Feature Structure:** Organize in `src/redmuffin.Blazor.StaticWeb/Features/FeatureName/`
- **Testing:** Mirror main project structure in test projects
- **File Encoding:** Always use UTF8 with BOM for Markdown files
