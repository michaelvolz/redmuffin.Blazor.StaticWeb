# Product Requirements Document: Hello World Mock Example

## Introduction/Overview

This PRD extends the existing `IRaindropAPI` interface and its implementations to include a Hello World example that demonstrates the mock data pattern already established in the project. The feature will add a `GetHelloWorldAsync()` method to the existing service abstraction, allowing developers to see how the environment-based service resolution works with a simple example.

The goal is to provide a clear, working example of the mock data pattern without creating new infrastructure, leveraging the existing `IRaindropAPI`, `DummyRaindropAPI`, and `RaindropAPI` implementations.

## Goals

1. **Extend Existing Pattern**: Add Hello World functionality to the current `IRaindropAPI` interface without breaking existing implementations
2. **Demonstrate Mock Pattern**: Provide a clear example of how mock vs real API calls work in the current architecture
3. **Maintain Consistency**: Follow all existing patterns, naming conventions, and architectural decisions
4. **Zero Breaking Changes**: Ensure all existing functionality continues to work without modification
5. **Educational Value**: Create a simple example that helps developers understand the service abstraction pattern

## User Stories

### Primary User Stories

- **US-001:** As a developer, I want to call a Hello World service method and see different responses based on environment (localhost:5233 vs localhost:4280) so that I can understand how the mock pattern works
- **US-002:** As a developer, I want the mock version to clearly indicate it's returning mock data so that I can distinguish between real and fake responses
- **US-003:** As a developer, I want to use the same service injection pattern as existing components so that the implementation is consistent with the codebase
- **US-004:** As a developer, I want the existing `CallApiExample` component to demonstrate both direct API calls and service-based calls so that I can see different approaches

### Secondary User Stories

- **US-005:** As a developer, I want comprehensive unit tests for the new functionality so that I can understand how to test service abstractions
- **US-006:** As a developer, I want the new method to follow the same async patterns and error handling as existing methods

## Functional Requirements

1. **IRaindropAPI Interface Extension**
   - Add `GetHelloWorldAsync(CancellationToken cancellationToken = default)` method to the existing interface
   - Method must return `Task<string>` to match the simple string response pattern
   - Method must support cancellation tokens and proper async patterns
   - Method must include comprehensive XML documentation with exception scenarios

2. **DummyRaindropAPI Implementation**
   - Implement `GetHelloWorldAsync()` method that returns hardcoded mock response
   - Response must clearly indicate it's from mock data: "Hello World from Mock Data - Not from Azure Functions"
   - Implementation must be synchronous (no HTTP calls) but return a completed Task
   - Must include appropriate logging using existing LoggerMessage patterns

3. **RaindropAPI Implementation**
   - Implement `GetHelloWorldAsync()` method that calls the existing `/api/HelloWorld` Azure Function
   - Use existing HttpClient patterns and error handling
   - Maintain consistency with existing `GetVideosAsync()` and `GetArticlesAsync()` implementations
   - Must include appropriate logging using existing LoggerMessage patterns

4. **Component Integration**
   - Update `CallApiExample.razor.cs` to inject `IRaindropAPI` service
   - Add a new button and method to demonstrate service-based Hello World call
   - Maintain existing direct API call functionality for comparison
   - Display both responses to show the difference between approaches

5. **Service Registration**
   - No changes required to existing service registration in `Program.cs`
   - Existing factory pattern automatically handles the new method
   - Environment detection continues to work as established

## Non-Goals (Out of Scope)

1. **New Infrastructure**: No new interfaces, factories, or service registration patterns
2. **Warmup Service Changes**: No modifications to the existing `WarmupService`
3. **New Azure Functions**: No new Azure Function endpoints (use existing HelloWorld function)
4. **JSON Data Files**: No new mock data files (hardcoded response is sufficient)
5. **UI/UX Changes**: No styling or layout changes to existing components
6. **Breaking Changes**: No modifications that would affect existing functionality

## Design Considerations

### UI/UX Requirements

- Use existing Zurb Foundation button styles and layout patterns
- Maintain consistency with current `CallApiExample` component design
- Display responses in the same format as existing API response display
- No new SCSS files or styling required

### Component Structure

- Follow existing Blazor component patterns with code-behind files
- Use existing dependency injection patterns with `[Inject]` attributes
- Maintain existing error handling and display patterns
- Follow established async/await patterns with `ConfigureAwait(false)`

## Technical Considerations

### Blazor WebAssembly .NET 9 Specific

- **Interface Extension**: Add method to existing `IRaindropAPI` interface with proper XML documentation
- **Service Implementation**: Extend both `DummyRaindropAPI` and `RaindropAPI` classes with new method
- **Dependency Injection**: Use existing service registration and factory patterns
- **Async Patterns**: Implement proper async/await with `ConfigureAwait(false)` and `CancellationToken` support
- **Error Handling**: Follow existing error handling patterns in both implementations
- **Logging**: Use existing LoggerMessage delegate patterns for consistent logging

### File Structure (Extending Existing)

```
src/redmuffin.Blazor.StaticWeb/
├── Features/
│   ├── Raindrop/
│   │   └── Services/
│   │       ├── IRaindropAPI.cs (extended)
│   │       ├── RaindropAPI.cs (extended)
│   │       ├── RaindropAPI.Logging.cs (extended)
│   │       ├── DummyRaindropAPI.cs (extended)
│   │       └── DummyRaindropAPI.Logging.cs (extended)
│   └── Pages/
│       └── ApiExamplePage/
│           └── CallApiExample.razor.cs (updated)

tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/
└── Features/
    ├── Raindrop/
    │   └── Services/
    │       ├── IRaindropAPITests.cs (extended)
    │       ├── DummyRaindropAPITests.cs (extended)
    │       └── RaindropAPITests.cs (extended)
    └── Pages/
        └── ApiExamplePage/
            └── CallApiExampleTests.cs (new)
```

### Integration with Existing Azure Functions

- **Existing Endpoint**: Use existing `/api/HelloWorld` Azure Function endpoint
- **No Changes Required**: The existing `HelloWorld.cs` function already returns "Welcome to Azure Functions!"
- **HTTP Client**: Use existing HttpClient patterns and configuration
- **Error Handling**: Follow existing error handling patterns for API calls

### Environment Detection

- **Existing Pattern**: Use existing `RaindropAPIFactory` environment detection
- **localhost:5233**: Automatically uses `DummyRaindropAPI` with hardcoded response
- **localhost:4280**: Automatically uses `RaindropAPI` with real Azure Function call
- **Production**: Uses `RaindropAPI` with real Azure Function call

## Success Metrics

1. **Functionality**: Hello World method works correctly in both mock and real environments
2. **Consistency**: New implementation follows all existing patterns and conventions
3. **Zero Regressions**: All existing functionality continues to work without issues
4. **Test Coverage**: Comprehensive unit tests for new functionality achieve >90% coverage
5. **Code Quality**: Zero build warnings and full compliance with existing analyzer rules
6. **Documentation**: Clear XML documentation and code examples for future reference

## Implementation Notes

### Blazor-Specific Guidance

#### Interface Extension Pattern

```csharp
public interface IRaindropAPI
{
    // Existing methods...
    Task<List<RaindropItem>> GetVideosAsync(CancellationToken cancellationToken = default);
    Task<List<RaindropItem>> GetArticlesAsync(CancellationToken cancellationToken = default);

    // New method
    /// <summary>
    /// Retrieves a Hello World message from the service.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A Hello World message string.</returns>
    /// <exception cref="HttpRequestException">Thrown when API request fails (RaindropAPI only).</exception>
    Task<string> GetHelloWorldAsync(CancellationToken cancellationToken = default);
}
```

#### Component Integration Pattern

```csharp
public partial class CallApiExample
{
    [Inject]
    private IHttpClientFactory HttpClientFactory { get; set; } = default!;

    [Inject]
    private IRaindropAPI RaindropAPI { get; set; } = default!;

    private async Task CallServiceHelloWorldAsync()
    {
        ArgumentNullException.ThrowIfNull(RaindropAPI);

        try
        {
            _serviceResponse = await RaindropAPI.GetHelloWorldAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _serviceErrorMessage = $"Service error: {ex.Message}";
        }
    }
}
```

#### Testing Considerations

- **TUnit Framework**: Write tests using `[Test]` attribute for new functionality
- **LightMock.Generator**: Mock `IRaindropAPI` interface for component testing
- **Test Coverage**: Test both dummy and real API implementations
- **Integration Tests**: Verify service method works correctly in both environments
- **Component Tests**: Test `CallApiExample` component with mocked service

#### Code Quality Standards

- **StyleCop Compliance**: Follow SA1402, SA1208, SA1201-1214 rules
- **Meziantou Analyzers**: Use `ConfigureAwait(false)`, proper async patterns
- **Microsoft Analyzers**: Use LoggerMessage delegates, proper exception handling
- **Zero Warnings**: Maintain zero build warnings policy (except IL2111)

### Logging Implementation

#### DummyRaindropAPI Logging

```csharp
[LoggerMessage(4, LogLevel.Information, "Returning hardcoded Hello World response from mock data")]
public static partial void LogHelloWorldMockResponse(ILogger logger);
```

#### RaindropAPI Logging

```csharp
[LoggerMessage(4, LogLevel.Information, "Successfully retrieved Hello World response from Azure Function")]
public static partial void LogHelloWorldSuccess(ILogger logger);

[LoggerMessage(5, LogLevel.Error, "Failed to retrieve Hello World response from Azure Function")]
public static partial void LogHelloWorldError(ILogger logger, Exception exception);
```

## Open Questions

1. **Response Format**: Should the mock response include any additional metadata or just the simple string?
2. **Error Simulation**: Should the mock implementation include any error simulation capabilities?
3. **Logging Level**: Should the Hello World calls use Information or Debug level logging?
4. **Component Layout**: Should the new service-based call be prominently displayed or secondary to the direct API call?

---

**Target Audience**: Junior developers familiar with Blazor WebAssembly and .NET 9
**Implementation Complexity**: Low - extends existing patterns without new infrastructure
**Estimated Effort**: 2-4 hours for implementation + testing
**Dependencies**: None - uses existing codebase patterns and infrastructure
