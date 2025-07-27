# Product Requirements Document: Hello World Mock Example - To Do

## Relevant Files

### Blazor Components

- `src/redmuffin.Blazor.StaticWeb/Features/Pages/ApiExamplePage/CallApiExample.razor.cs` - Updated code-behind for CallApiExample component to use IRaindropAPI service with automatic environment detection.

### Services

- `src/redmuffin.Blazor.StaticWeb/Features/Raindrop/Services/IRaindropAPI.cs` - Extended interface with GetHelloWorldAsync method.
- `src/redmuffin.Blazor.StaticWeb/Features/Raindrop/Services/DummyRaindropAPI.cs` - Extended with hardcoded Hello World mock implementation.
- `src/redmuffin.Blazor.StaticWeb/Features/Raindrop/Services/DummyRaindropAPI.Logging.cs` - Extended with LoggerMessage delegates for Hello World mock logging.
- `src/redmuffin.Blazor.StaticWeb/Features/Raindrop/Services/RaindropAPI.cs` - Extended with Azure Function Hello World implementation.
- `src/redmuffin.Blazor.StaticWeb/Features/Raindrop/Services/RaindropAPI.Logging.cs` - Extended with LoggerMessage delegates for Hello World API logging.

### Tests

- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Raindrop/Services/IRaindropAPITests.cs` - Extended with Hello World method tests.
- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Raindrop/Services/DummyRaindropAPITests.cs` - Extended with mock implementation tests.
- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Raindrop/Services/RaindropAPITests.cs` - Extended with real API implementation tests.
- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Pages/ApiExamplePage/CallApiExampleTests.cs` - New test file for component testing.
- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Pages/ApiExamplePage/CallApiExampleTests.Helpers.cs` - New test helpers file with TestScope and mocks.

### Important Notes

- **Avoid ConfigureAwait(false) on Assert Statements**: Never append `ConfigureAwait(false)` to `Assert` statements in unit tests to maintain context integrity.
- **Use Modern C# 13 Patterns**: Always apply contemporary C# 13 features and techniques for optimal code quality.
- **Custom Mock Naming**: Name custom mocks with a `Mock` suffix (e.g., `HttpHandlerMock`, `HttpResultMock`).
- **LightMock.Generator Mock Naming**: Apply `Mock` suffix to real LightMock.Generator mocks (e.g., `3rdPartyDependencyMock`, `Another3rdPartyDependencyMock`).
- **Post-Step Validation**: After each development step, verify adherence to:
  - Test scope pattern.
  - Custom mock naming pattern.
  - Absence of `ConfigureAwait(false)` where inappropriate.
- **Test Code Isolation**: Ensure code within the `NewTests` folder does not use any test code located outside `NewTests`. Do not modify existing code outside `NewTests`.
- **CRITICAL**: Test Behavior, NOT Implementation - Focus on public interfaces and contracts
- **EXAMINE HomeTests FIRST**: Before writing any tests, thoroughly examine all code in the HomeTests folder to understand existing patterns and conventions
- **NEW TESTS LOCATION**: All new tests MUST be placed in the `NewTests` subfolder only. Do NOT modify or touch any existing tests outside this folder as they are outdated
- **LightMock.Generator Usage**: Use LightMock.Generator ONLY for 3rd party external dependencies (e.g., HttpClient, external APIs). For internal services and components, use custom mocks following the patterns established in HomeTests
- **ConfigureAwait(false) Exception**: Assert statements in tests NEVER need ConfigureAwait(false) - only use it on actual async operations like service calls, not on test assertions
- Use TUnit testing framework with `[Test]` and `[Arguments]` attributes
- Enforce zero build warnings policy
- Follow partial class organization for logging with `LoggerMessage` delegates
- Adhere to existing patterns (environment detection, Azure Function endpoint)
- Maintain existing file structure and naming conventions
- Use `dotnet clean && dotnet build --no-restore --verbosity quiet` to verify zero build warnings (except IL2111)
- Use `dotnet test` to run all tests or `dotnet test --filter "FullyQualifiedName~[TestClassName]"` for specific test classes
- Blazor components follow feature-based organization under `src/redmuffin.Blazor.StaticWeb/Features/`
- All async methods must use `ConfigureAwait(false)` and proper error handling
- Follow StyleCop/Meziantou analyzer rules for code quality
- Extend existing Azure Function `/api/HelloWorld` endpoint - no new functions needed

## Implementation Guidelines

### Code Quality Requirements

- Use `dotnet clean && dotnet build --no-restore --verbosity quiet` to verify zero build warnings (except IL2111)
- Use `dotnet test` to run all tests or `dotnet test --filter "FullyQualifiedName~[TestClassName]"` for specific test classes
- Blazor components follow feature-based organization under `src/redmuffin.Blazor.StaticWeb/Features/`
- All async methods must use `ConfigureAwait(false)` and proper error handling
- Follow StyleCop/Meziantou analyzer rules for code quality
- Extend existing Azure Function `/api/HelloWorld` endpoint - no new functions needed

### Interface Extension Details

- Add `GetHelloWorldAsync()` method signature to interface:

  ```csharp
  Task<string> GetHelloWorldAsync(CancellationToken cancellationToken = default);
  ```

- Include comprehensive XML documentation with `<summary>`, `<param>`, `<returns>`, and `<exception>` tags
- Ensure method signature follows existing patterns with `Task<string>` return type

### DummyRaindropAPI Implementation Details

- Return exact response: "Hello World from Mock Data - Not from Azure Functions"
- Use `Task.FromResult()` for synchronous implementation that returns completed Task
- Add `ArgumentNullException.ThrowIfNull()` validation for parameters in implementation code (not needed in test code)
- Add logging call using LoggerMessage pattern (EventId 4, Information level)

### RaindropAPI Implementation Details

- Use existing HttpClient patterns consistent with `GetVideosAsync()` and `GetArticlesAsync()` methods
- Include proper error handling with try/catch for `HttpRequestException`
- Add success logging (EventId 4, Information) and error logging (EventId 5, Error) using LoggerMessage pattern
- Follow existing async patterns and forward `CancellationToken` parameter (CA2016 compliance)

### Component Integration Details

- **SIMPLIFIED APPROACH**: Follow the same pattern as Videos and Articles pages
- Add `[Inject] private IRaindropAPI RaindropAPI { get; set; } = default!;` property to `CallApiExample.razor.cs`
- Add `ArgumentNullException.ThrowIfNull(RaindropAPI);` validation in `OnInitializedAsync()` method
- **NO CHANGES** to existing direct API functionality - keep the current HttpClient implementation as-is
- Replace the existing API call with `RaindropAPI.GetHelloWorldAsync()` service call
- **Environment Detection**: Automatically uses DummyRaindropAPI on localhost:5233 and RaindropAPI on localhost:4280 via RaindropAPIFactory
- **Single Button**: Keep existing "Call Hello World API" button but use service instead of direct HttpClient
- Maintain existing UI layout and error handling patterns
- Ensure Zurb Foundation button styling consistency with existing UI

## Tasks

- [x] 1.0 Extend IRaindropAPI Interface
  - [x] 1.1 Add `GetHelloWorldAsync(CancellationToken cancellationToken = default)` method to `IRaindropAPI.cs`
  - [x] 1.2 Include comprehensive XML documentation with `<summary>`, `<param>`, `<returns>`, and `<exception>` tags
  - [x] 1.3 Ensure method signature follows existing patterns with `Task<string>` return type
  - [x] 1.4 Run `dotnet clean && dotnet build --no-restore --verbosity quiet` to verify zero build warnings
  - [x] 1.5 **MODERN C# 13**: Used default interface implementation to avoid breaking existing test mocks

- [x] 2.0 Implement DummyRaindropAPI Hello World Method
  - [x] 2.1 Implement `GetHelloWorldAsync()` method in `DummyRaindropAPI.cs` with hardcoded response
  - [x] 2.2 Return exact response: "Hello World from Mock Data - Not from Azure Functions"
  - [x] 2.3 Use `Task.FromResult()` for synchronous implementation that returns completed Task
  - [x] 2.4 Add `ArgumentNullException.ThrowIfNull()` validation for parameters in implementation code (not needed in test code)
  - [x] 2.5 Create `DummyRaindropAPI.Logging.cs` partial class with LoggerMessage delegate
  - [x] 2.6 Add logging call using LoggerMessage pattern (EventId 4, Information level)
  - [x] 2.7 Follow SA1201-1214 member order rules and ensure `ConfigureAwait(false)` compliance
  - [x] 2.8 Run `dotnet clean && dotnet build --no-restore --verbosity quiet` to verify zero build warnings

- [x] 3.0 Implement RaindropAPI Hello World Method
  - [x] 3.1 Implement `GetHelloWorldAsync()` method in `RaindropAPI.cs` calling `/api/HelloWorld` endpoint
  - [x] 3.2 Use existing HttpClient patterns consistent with `GetVideosAsync()` and `GetArticlesAsync()` methods
  - [x] 3.3 Include proper error handling with try/catch for `HttpRequestException`
  - [x] 3.4 Add `ArgumentNullException.ThrowIfNull()` validation for parameters in implementation code (not needed in test code)
  - [x] 3.5 Use `ConfigureAwait(false)` on all await calls (MA0004 compliance)
  - [x] 3.6 Create `RaindropAPI.Logging.cs` partial class with LoggerMessage delegates
  - [x] 3.7 Add success logging (EventId 4, Information) and error logging (EventId 5, Error) using LoggerMessage pattern
  - [x] 3.8 Follow existing async patterns and forward `CancellationToken` parameter (CA2016 compliance)
  - [x] 3.9 Run `dotnet clean && dotnet build --no-restore --verbosity quiet` to verify zero build warnings

- [x] 4.0 Update CallApiExample Component (SIMPLIFIED APPROACH)
  - [x] 4.1 Add `[Inject] private IRaindropAPI RaindropAPI { get; set; } = default!;` property to `CallApiExample.razor.cs`
  - [x] 4.2 Add `ArgumentNullException.ThrowIfNull(RaindropAPI);` validation in `OnInitializedAsync()` method
  - [x] 4.3 **PRESERVE EXISTING**: Keep all existing fields (_apiResponse, _errorMessage) and UI layout
  - [x] 4.4 **MODIFY EXISTING**: Update `CallApiAsync()` method to use `RaindropAPI.GetHelloWorldAsync()` instead of direct HttpClient
  - [x] 4.5 **NO NEW BUTTONS**: Keep existing single "Call Hello World API" button
  - [x] 4.6 **NO NEW FIELDS**: Reuse existing response and error message fields
  - [x] 4.7 **ENVIRONMENT DETECTION**: Automatically shows mock data on localhost:5233, real API on localhost:4280
  - [x] 4.8 Follow SA1201-1214 member order: fields→properties→constructors→methods
  - [x] 4.9 Ensure existing Zurb Foundation button styling is maintained
  - [x] 4.10 Run `dotnet clean && dotnet build --no-restore --verbosity quiet` to verify zero build warnings

- [x] 5.0 Create Comprehensive Test Suite (🧪 **BEHAVIOR-FOCUSED TESTING ONLY**)
  - [x] 5.1 **EXAMINE HomeTests**: Study all existing test patterns in HomeTests folder before implementation
  - [x] 5.2 **UPDATE COMPONENT TESTS**: Modify `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Pages/ApiExamplePage/CallApiExampleTests.cs` to test simplified IRaindropAPI integration
  - [x] 5.3 **UPDATE TEST HELPERS**: Modify `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Pages/ApiExamplePage/CallApiExampleTests.Helpers.cs` to support IRaindropAPI mocking
  - [x] 5.4 Implement TestScope with fluent configuration methods (`WithStandardServices()`, etc.) following HomeTests patterns
  - [x] 5.5 Create `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Raindrop/Services/DummyRaindropAPITests.cs` with Hello World method tests using TUnit framework
  - [x] 5.6 **Test BEHAVIOR**: Verify correct response returned and appropriate logging occurs (NOT internal implementation details)
  - [x] 5.7 Create `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Raindrop/Services/RaindropAPITests.cs` with Hello World method tests for Azure Function calls
  - [x] 5.8 **MOCKING STRATEGY**: Use custom mocks for internal services (IRaindropAPI) following HomeTests patterns; use LightMock.Generator only for 3rd party external dependencies like HttpClient
  - [x] 5.9 Use `CancellationToken.None`, `null`, `The<T>.IsAnyValue` for optional parameters in mock arrangements
  - [x] 5.10 **Test BEHAVIOR**: Verify error scenarios through public interface contracts, NOT internal exception handling logic
  - [x] 5.11 Use TUnit fluent chaining for related assertions: `await Assert.That(result).IsNotNull().And.Contains("expected")`
  - [x] 5.12 Use `Assert.Multiple()` for unrelated concerns (DOM structure vs logging)
  - [x] 5.13 Follow test naming convention: `Should_Return_HelloWorld_Response_When_Called` (describes expected BEHAVIOR)
  - [x] 5.14 **Test BEHAVIOR**: Focus on public method contracts, input/output behavior, NOT internal async implementation details
  - [x] 5.15 Ensure all async test methods use `ConfigureAwait(false)` on await calls (except Assert statements which never need it)
  - [x] 5.16 **UPDATE TESTS**: Run `dotnet test` to verify updated component tests pass with simplified approach
  - [x] 5.17 Run `dotnet clean && dotnet build --no-restore --verbosity quiet` to verify zero build warnings
