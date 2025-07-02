# Gemini Project Information

This file provides project-specific context and instructions for the Gemini AI assistant.

## Project Description

This project is a modern full-stack web application built with Blazor WebAssembly (.NET 9) and Azure Functions (.NET 8), featuring OAuth integration, real-time performance monitoring, and comprehensive testing infrastructure.

## Conventions

- **Frameworks**: Blazor WebAssembly (.NET 9), Azure Functions (.NET 8)
- **Languages**: C#
- **Style Guide**: .editorconfig
- **Testing**: TUnit, NSubstitute

## Commands

- **Build**: `dotnet build`
- **Run**: `dotnet run`
- **Test**: `dotnet test`

## Project Structure

- **src/**: Contains the source code for the Blazor application and the Azure Functions.
- **tests/**: Contains the tests for the application.
- **scripts/**: Contains various build and utility scripts.

## Commit Standards

- **Format**: `<type>(<scope>): <description>` (<72 chars>)
- **Types**: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`
- **Scopes**: `blazor`, `api`, `ui`, `db`, `auth`

## Coding Standards

- **Target**: .NET 9 Blazor WebAssembly, C# 12/13 features
- **Structure**: UI (.razor), Logic (partial classes)
- **Naming**: PascalCase (classes, methods, properties), camelCase (fields, variables, parameters), `_` prefix for private fields
- **var Usage**: Only when type is clearly apparent (e.g., `var items = new List<string>()`)
- **Line Length**: 160 characters maximum
- **Braces**: Always use braces, even for single-line statements
- **Async**: Always use `async`/`await`
- **DI**: Blazor DI for services, keep focused and small
- **HttpClient**: For Blazor WebAssembly: `[Inject] private HttpClient Http { get; set; } = default!;` For server-side: Use `IHttpClientFactory`
- **State**: Cascading parameters, DI services, built-in Blazor patterns
- **Storage**: Use IJSRuntime for localStorage/sessionStorage via JS interop

## UI & Styling

- **Framework**: Zurb Foundation for all UI/layout
- **Styles**: Place in `wwwroot/css` or `wwwroot/scss`
- **Responsive**: Foundation grid/utilities or custom CSS
- **Accessibility**: Semantic HTML, ARIA roles, keyboard navigation, WCAG 2.1 AA compliance, proper color contrast
- **Performance**: Optimize assets (bundling, minification), lazy loading, virtualization for large lists
- **Modern CSS**: Grid, Flexbox, variables, nesting, dark mode support
- **Images**: WebP/AVIF, `loading="lazy"`, `srcset`
- **JavaScript**: Minimal - prefer C#/Blazor, JS interop only when necessary
- **JS Interop**: Use `IJSRuntime.InvokeAsync<T>()`, dispose JS object references

## Security & API

- **Input Validation**: Always validate/sanitize user input
- **XSS/CSRF**: Use Blazor built-ins and best practices
- **Secrets**: Never expose in client code
- **API**: Use `IHttpClientFactory` for HTTP calls, prefer minimal APIs
- **CSP**: Enforce strong Content Security Policy
- **Authentication**: ASP.NET Core Identity, role-based access control

## Testing & Documentation

- **Unit Tests**: Use TUnit (NOT NUnit/xUnit), `[Test]` for methods, `[Tests]` with `[Arguments]` for data-driven
- **Documentation**: XML docs for public APIs, update README/Wiki/OpenAPI

## File & Directory Organization

- **Features**: `src/MainProject/Features/FeatureName/` - Include Razor components, code-behind, feature-specific CSS
- **Feature Subcomponents**: `src/MainProject/Features/FeatureName/Components/`
- **Static Assets**: `src/MainProject/wwwroot/` - Use subfolders: `css/`, `scss/`, `lib/`, `sample-data/`
- **Tests**: `tests/` - Mirror main project structure
- **Scripts**: `scripts/` - Deployment, setup, utility scripts
- **Subprojects**: `src/SubProjectName/`

## Blazor Patterns

- **Component Parameters**: Use `[Parameter]` with proper validation
- **Event Callbacks**: `[Parameter] public EventCallback<T> OnEvent { get; set; }`
- **Child Content**: `[Parameter] public RenderFragment? ChildContent { get; set; }`
- **Lifecycle**: Override `OnInitializedAsync()`, `OnParametersSetAsync()`, `OnAfterRenderAsync(bool firstRender)`
- **Conditional Rendering**: Use `@if`, `@switch`, avoid complex logic in markup
- **Forms**: Use `<EditForm>` with `EditContext` and validation attributes
- **Loading States**: Show loading indicators for async operations
- **Error Boundaries**: Wrap components in `<ErrorBoundary>` for error handling

## Best Practices

- Develop modular, reusable, testable components
- Favor strongly-typed parameters over dynamic
- Handle exceptions with try/catch or error boundaries (`<ErrorBoundary>`)
- Reference code with filename and line numbers
- Prefer C#/Blazor over JavaScript/HTML unless required
- Keep builds and tests passing before merging
- Use `StateHasChanged()` sparingly, prefer parameter binding
- Implement `IDisposable` for event subscriptions and timers

## AI Operational Guidelines

- Offer PowerShell scripts for complex/data-intensive tasks
- Use Context7 MCP Server for framework/library documentation - prioritize over training data
- **Workflow**:
  - Edit one file at a time to avoid conflicts
  - For large changes: outline plan, get approval, make incremental edits
  - Track progress (e.g., "Edit 2 of 5"), pause for clarification when blocked
  - Keep code buildable at each stage
- **Standards**: Follow all coding/testing practices, prefer Blazor over JavaScript
- **Repository**: Owner: `michaelvolz`, Name: `redmuffin.Blazor.StaticWeb`

## Azure Functions Best Practices

- **Dependency Injection**: Use `Startup.cs` to register services (`ILogger`, `IHttpClientFactory`) in C# Azure Functions for testability and maintainability. Example: `builder.Services.AddSingleton<IMyService, MyService>();`.
- **Cold Start Optimization**: Minimize assembly size by reducing dependencies in C# projects. Use .NET Isolated Worker for better control over startup logic and avoid heavy initialization in function code.
- **Error Handling**: Implement retry policies with Polly for transient failures. Use try-catch blocks to handle exceptions gracefully and return meaningful HTTP status codes (e.g., `400` for bad requests) for HTTP triggers.
- **Input Validation**: Validate HTTP trigger inputs using C# model validation (e.g., `System.ComponentModel.DataAnnotations`) or custom checks to ensure security and prevent errors.
- **Structured Logging**: Use `ILogger` for structured logging in C#, capturing only essential data (e.g., request IDs, errors) to avoid performance overhead. Example: `logger.LogInformation("Processing {RequestId}", requestId);`.
- **Asynchronous Programming**: Use `async`/`await` in C# functions for I/O-bound operations (e.g., HTTP calls, database queries) to improve scalability. Avoid blocking calls like `.Result` or `.Wait()`.
- **Function Granularity**: Write single-responsibility functions. Split complex logic into smaller, focused functions to improve maintainability and reusability. Example: Separate data retrieval and processing into distinct functions.
- **Configuration Management**: Access settings via environment variables using `Environment.GetEnvironmentVariable` in C#. Avoid hardcoding values to ensure flexibility across environments.
- **Unit Testing**: Write unit tests for function logic using frameworks like xUnit or MSTest. Mock dependencies (e.g., `ILogger`, `IHttpClientFactory`) with Moq to isolate function behavior.
- **Idempotency**: Ensure functions are idempotent, especially for event-driven triggers (e.g., Queue, Event Hub). Handle duplicate messages gracefully using unique identifiers or state checks.
- **Parameter Optimization**: Use strongly-typed bindings (e.g., `QueueTrigger`, `BlobInput`) in C# to reduce parsing logic and improve type safety. Avoid overusing dynamic `JObject` inputs.
- **Resource Cleanup**: Dispose of resources (e.g., database connections, HTTP clients) properly using `IDisposable` or `using` statements to prevent memory leaks in long-running functions.
- **Code Reusability**: Extract shared logic into class libraries or static methods in C#. Use NuGet packages for cross-function utilities to maintain DRY principles.
- **Performance Monitoring**: Instrument code with custom metrics via Application Insights SDK in C# (e.g., `TelemetryClient.TrackMetric`) to track function-specific performance indicators.
- **Versioning**: For HTTP-triggered functions, implement API versioning (e.g., via query parameters or headers) to support backward compatibility as function logic evolves.
- **Secure Coding**: Sanitize inputs and outputs to prevent injection attacks (e.g., SQL, XSS). Use libraries like `AntiXssEncoder` for output encoding in HTTP responses.
