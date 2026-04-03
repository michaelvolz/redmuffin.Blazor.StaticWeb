# Product Requirements Document: HttpClientFactory Simplification

## Introduction/Overview

Simplify the HttpClient configuration across the entire codebase by eliminating all named HttpClient instances and implementing a single, default HttpClient configuration through IHttpClientFactory. This modernization will reduce cognitive overhead, eliminate the need to remember client names, and provide sensible defaults for 90% of use cases (internal API calls) while maintaining flexibility for external services.

## Goals

1. **Eliminate Complexity**: Remove all named HttpClient configurations and usage patterns
2. **Optimize for Common Case**: Configure default HttpClient for internal API calls with base address and timeout
3. **Maintain Flexibility**: Allow external API consumers to modify HttpClient properties as needed
4. **Modernize Codebase**: Apply C# 13/.NET 9 best practices throughout the migration
5. **Zero Build Warnings**: Ensure all changes comply with StyleCop/Meziantou analyzer rules
6. **Complete Migration**: Update all projects including Blazor app, Azure Functions API, and test projects

## User Stories

1. **As a developer**, I want to inject IHttpClientFactory without specifying client names so that I can focus on business logic rather than configuration details.

2. **As a developer**, I want the default HttpClient to be pre-configured with the application's base address and reasonable timeout so that internal API calls work immediately without additional setup.

3. **As a developer**, I want to easily modify HttpClient properties for external API calls so that I can customize configuration when needed without breaking the default behavior.

4. **As a maintainer**, I want consistent HttpClient usage patterns across all projects so that the codebase is easier to understand and maintain.

5. **As a tester**, I want simplified HttpClient mocking so that test setup is cleaner and more maintainable.

## Functional Requirements

1. **FR-001**: The system must configure a single default HttpClient through IHttpClientFactory with:
   - BaseAddress = builder.HostEnvironment.BaseAddress
   - Timeout = TimeSpan.FromSeconds(30)

2. **FR-002**: The system must eliminate all named HttpClient registrations ("DefaultClient", etc.)

3. **FR-003**: All services must use IHttpClientFactory.CreateClient() without parameters to get the default configured client

4. **FR-004**: External API consumers must be able to modify HttpClient properties after creation for custom configurations

5. **FR-005**: All Blazor components must inject IHttpClientFactory instead of HttpClient directly in the CodeBehind files

6. **FR-006**: All Azure Functions must use IHttpClientFactory pattern for HTTP operations

7. **FR-007**: All test projects must use IHttpClientFactory mocking patterns established in tests before in the NewTests folder

8. **FR-008**: The migration must maintain existing functionality without breaking changes to API behavior

9. **FR-009**: All code must comply with C# 13/.NET 9 modern patterns including primary constructors where appropriate

10. **FR-010**: All changes must pass zero build warnings policy

11. **FR-011**: All test code must be organized in `NewTests/` folder structure in both test projects

12. **FR-012**: Follow established TestScope and .Helpers.cs partial class pattern for all test files

13. **FR-013**: All test classes must use partial class pattern with main file containing only `[Test]` methods and .Helpers.cs containing TestScope, mocks, and utilities

## Non-Goals (Out of Scope)

1. **NG-001**: Creating multiple named HttpClient configurations
2. **NG-002**: Implementing complex HttpClient pooling or advanced configuration scenarios
3. **NG-003**: Changing existing API endpoint URLs or response formats
4. **NG-004**: Modifying existing error handling patterns beyond HttpClient usage
5. **NG-005**: Adding new HTTP features or middleware

## Design Considerations

### Blazor WebAssembly Configuration

- Use IHttpClientFactory with single default configuration in Program.cs
- Leverage builder.HostEnvironment.BaseAddress for internal API calls
- Maintain compatibility with existing Azure Static Web Apps deployment

### Component Injection Pattern

```csharp
public partial class ExampleComponent : ComponentBase
{
    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        ArgumentNullException.ThrowIfNull(HttpClientFactory);
        using var httpClient = HttpClientFactory.CreateClient();
        // Use pre-configured client for internal APIs
    }
}
```

### Service Constructor Pattern

```csharp
public sealed class ExampleService(IHttpClientFactory httpClientFactory, ILogger<ExampleService> logger)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

    public async Task<T> GetDataAsync<T>(CancellationToken cancellationToken = default)
    {
        using var httpClient = _httpClientFactory.CreateClient();
        // Client is pre-configured with base address and timeout
        return await httpClient.GetFromJsonAsync<T>("api/data", cancellationToken).ConfigureAwait(false);
    }
}
```

### External API Pattern

```csharp
public async Task<T> GetExternalDataAsync<T>(string externalBaseUrl, CancellationToken cancellationToken = default)
{
    using var httpClient = _httpClientFactory.CreateClient();
    httpClient.BaseAddress = new Uri(externalBaseUrl); // Override for external APIs
    httpClient.Timeout = TimeSpan.FromMinutes(5); // Custom timeout if needed
    return await httpClient.GetFromJsonAsync<T>("api/external", cancellationToken).ConfigureAwait(false);
}
```

## Technical Considerations

### Blazor WebAssembly .NET 9 Specific

- **Primary Constructors**: Use modern C# 13 primary constructor syntax where appropriate
- **Collection Expressions**: Apply `int[] values = [1, 2, 3];` syntax for collections
- **Using Declarations**: Prefer `using var client = factory.CreateClient();` pattern
- **ConfigureAwait**: Maintain `ConfigureAwait(false)` on all async operations
- **Null Validation**: Use `ArgumentNullException.ThrowIfNull()` consistently

### Azure Functions .NET 8 Integration

- Update existing Azure Functions to use IHttpClientFactory pattern
- Maintain isolated worker model compatibility
- Preserve existing dependency injection configuration

### Testing with custom Mocks or LightMock.Generator

- Replace HttpClient mocks with IHttpClientFactory mocks
- Maintain existing test behavior while simplifying setup
- **IMPORTANT**: All new test code must be created in `NewTests/` folder structure
- Create `tests/redmuffin.Blazor.StaticWeb.Api.Tests/NewTests/` structure if it doesn't exist

### Performance Considerations

- Single HttpClient configuration reduces memory overhead
- Connection pooling benefits from unified configuration
- Reduced cognitive load improves developer productivity

### Build and Deployment

- Ensure compatibility with WebAssembly AOT compilation
- Maintain Azure Static Web Apps deployment compatibility
- Preserve existing CSP and security configurations

## Success Metrics

1. **Code Simplification**: Eliminate 100% of named HttpClient references
2. **Build Quality**: Maintain zero build warnings
3. **Test Coverage**: All existing tests pass with new HttpClient patterns
4. **Performance**: No degradation in HTTP request performance
5. **Developer Experience**: Reduced lines of code for HttpClient setup
6. **Maintainability**: Single configuration point for default HttpClient settings

## Implementation Notes

### Migration Strategy

1. **Phase 2**: Update Program.cs configuration in both Blazor and API projects
2. **Phase 3**: Migrate all service constructors to use IHttpClientFactory
3. **Phase 4**: Update all Blazor components to inject IHttpClientFactory
4. **Phase 5**: Migrate all test projects to use IHttpClientFactory mocking (only in NewTests folders)
5. **Phase 1**: Ensure NewTests folder structure follows established TestScope and .Helpers.cs partial class pattern
6. **Phase 6**: Clean up old HttpClient registrations and references

### Code Quality Standards

- Apply StyleCop SA1402 (one type per file)
- Use Meziantou MA0004 (ConfigureAwait(false))
- Implement CA1848 (LoggerMessage delegates)
- Follow SA1201-1214 (member ordering)
- Apply MA0053 (sealed classes where possible)

### Testing Approach

- Use TUnit framework with `[Test]` attributes
- Implement custom Mocks or LightMock.Generator for HttpClient mocking
- Maintain Arrange-Act-Assert pattern
- Test behavior, not implementation details

### File Organization

- Maintain feature-based structure under `src/redmuffin.Blazor.StaticWeb/Features/`
- Update corresponding test files under `tests/*/NewTests/` folders only
- Preserve existing SCSS and static asset organization

### Test Directory Structure

```
tests/
├── redmuffin.Blazor.StaticWeb.Tests/
│   └── NewTests/
│       ├── Core/
│       │   ├── ServiceTests.cs
│       │   └── ServiceTests.Helpers.cs
│       ├── Features/
│       │   └── Pages/
│       │       └── ComponentTests.cs
│       │       └── ComponentTests.Helpers.cs
│       └── Integration/
└── redmuffin.Blazor.StaticWeb.Api.Tests/
    └── NewTests/          # ✅ Created
        ├── FunctionTests.cs
        └── FunctionTests.Helpers.cs
```

### Test File Pattern (Established Convention)

- **Main Test File**: Contains ONLY `[Test]` methods
- **Helper Partial File**: Contains TestScope classes, factory methods, mocks, and utilities
- **Naming**: `TestClassName.cs` and `TestClassName.Helpers.cs`
- **Pattern**: `public partial class TestClassName` in both files

### Error Handling

- Maintain existing error handling patterns
- Use structured logging with LoggerMessage delegates
- Implement proper exception handling for HTTP operations
- Preserve existing user experience for error scenarios

## Open Questions

1. **Q**: Should we maintain any HttpClient configuration for specific scenarios?
   **A**: No, the goal is complete simplification with manual configuration for edge cases.

2. **Q**: How should we handle HttpClient disposal in long-running operations?
   **A**: Use `using` declarations for automatic disposal, following .NET best practices.

3. **Q**: Should we update existing documentation and PRDs that reference named clients?
   **A**: Yes, update all references to reflect the new simplified pattern, except old PRDs -> do not touch them.

4. **Q**: How should we handle backward compatibility during the migration?
   **A**: Complete migration in a single change to avoid mixed patterns in the codebase.

## Files to be Modified

### Blazor WebAssembly Project

- `src/redmuffin.Blazor.StaticWeb/Program.cs`
- `src/redmuffin.Blazor.StaticWeb/Features/Raindrop/Services/RaindropAPI.cs`
- `src/redmuffin.Blazor.StaticWeb/Features/Raindrop/Services/DummyRaindropAPI.cs`
- `src/redmuffin.Blazor.StaticWeb/Features/Pages/MarkdownExamplesPage/MarkdownExamples.razor.cs`
- `src/redmuffin.Blazor.StaticWeb/Features/Pages/WeatherPage/Weather.razor`
- `src/redmuffin.Blazor.StaticWeb/Features/Pages/WeatherPage/Weather.razor.cs`
- All other components currently injecting HttpClient directly

### Azure Functions API Project

- `src/redmuffin.Blazor.StaticWeb.Api/Program.cs`
- All Azure Functions using HttpClient

### Test Projects

- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Raindrop/Services/RaindropAPITests.Helpers.cs`
- `tests/redmuffin.Blazor.StaticWeb.Api.Tests/NewTests/HttpClientFactoryTests.cs` (new)
- `tests/redmuffin.Blazor.StaticWeb.Api.Tests/NewTests/HttpClientFactoryTests.Helpers.cs` (new)
- All test files using HttpClient mocking (only in NewTests folders following .Helpers.cs pattern)

### Documentation

- Update existing PRD files that reference named HttpClient patterns
- Update any technical documentation with HttpClient examples
