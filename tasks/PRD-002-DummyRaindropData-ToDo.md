# Product Requirements Document: Dummy RaindropIO Data for Local Design Testing - To Do

## Relevant Files

### Blazor Components

- `src/redmuffin.Blazor.StaticWeb/Features/Pages/VideosPage/Videos.razor.cs` - Updated code-behind for Videos component to use IRaindropAPI service.
- `src/redmuffin.Blazor.StaticWeb/Features/Pages/ArticlesPage/Articles.razor.cs` - Updated code-behind for Articles component to use IRaindropAPI service.

### Services (Feature-Oriented with Partial Class Structure)

- `src/redmuffin.Blazor.StaticWeb/Features/Raindrop/Services/IRaindropAPI.cs` - Interface abstraction for RaindropIO API operations with GetVideosAsync and GetArticlesAsync methods.
- `src/redmuffin.Blazor.StaticWeb/Features/Raindrop/Services/IRaindropAPIFactory.cs` - Factory interface for environment-based service resolution with CreateRaindropAPI and ShouldUseDummyData methods.
- `src/redmuffin.Blazor.StaticWeb/Features/Raindrop/Services/RaindropAPI.cs` - Real API implementation using HttpClient for Azure Functions calls.
- `src/redmuffin.Blazor.StaticWeb/Features/Raindrop/Services/RaindropAPI.Logging.cs` - LoggerMessage delegates for RaindropAPI.
- `src/redmuffin.Blazor.StaticWeb/Features/Raindrop/Services/RaindropAPIFactory.cs` - Factory implementation for creating appropriate IRaindropAPI instances with environment detection logic.
- `src/redmuffin.Blazor.StaticWeb/Features/Raindrop/Services/RaindropAPIFactory.Logging.cs` - LoggerMessage delegates for RaindropAPIFactory with comprehensive logging.
- `src/redmuffin.Blazor.StaticWeb/Features/Raindrop/Services/DummyRaindropAPI.cs` - Dummy API implementation using JSON files for local development.
- `src/redmuffin.Blazor.StaticWeb/Features/Raindrop/Services/DummyRaindropAPI.Logging.cs` - LoggerMessage delegates for DummyRaindropAPI.

### Shared/Common

- `src/redmuffin.Blazor.StaticWeb.Common/Raindrop/RaindropJsonSerializerContext.cs` - Enhanced JSON serialization context for improved parsing.
- `src/redmuffin.Blazor.StaticWeb/Program.cs` - Updated service registration for dependency injection.

### Mock Data

- `wwwroot/mockdata/videos.json` - Realistic dummy video data for local development testing.
- `wwwroot/mockdata/articles.json` - Realistic dummy article data for local development testing.

### Tests (Feature-Oriented with Partial Class Structure)

- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Raindrop/Services/IRaindropAPITests.cs` - TUnit tests for IRaindropAPI interface implementations.
- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Raindrop/Services/IRaindropAPITests.Helpers.cs` - TestScope and helper methods for IRaindropAPI tests.
- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Raindrop/Services/RaindropAPIFactoryTests.cs` - TUnit tests for factory pattern and environment detection.
- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Raindrop/Services/RaindropAPIFactoryTests.Helpers.cs` - TestScope and helper methods for factory tests.
- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Raindrop/Services/DummyRaindropAPITests.cs` - TUnit tests for dummy API implementation.
- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Raindrop/Services/DummyRaindropAPITests.Helpers.cs` - TestScope and helper methods for dummy API tests.
- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Raindrop/Services/RaindropAPITests.cs` - TUnit tests for real API implementation.
- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Raindrop/Services/RaindropAPITests.Helpers.cs` - TestScope and helper methods for real API tests.
- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Pages/VideosPageTests.cs` - TUnit tests for Videos component with mocked IRaindropAPI.
- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Pages/VideosPageTests.Helpers.cs` - TestScope and helper methods for Videos component tests.
- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Pages/ArticlesPageTests.cs` - TUnit tests for Articles component with mocked IRaindropAPI.
- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Pages/ArticlesPageTests.Helpers.cs` - TestScope and helper methods for Articles component tests.

### HttpClientFactory Best Practices

#### Base Address Configuration Strategy

**Current Approach (Recommended)**: Centralized base address configuration in Program.cs works well for this project because:

```csharp
// Program.cs - Single base address configuration
builder.Services.AddHttpClient(client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

**Why This Works**:

- **Internal Services** (Weather, RaindropAPI): Use relative URLs that work with configured base address
- **External Services** (ImageValidationService): Use absolute URLs that override base address
- **DummyRaindropAPI**: Uses HttpClient to load JSON files from wwwroot/mockdata via HTTP requests
- **Simplicity**: Single configuration point, consistent behavior

#### Service-Specific Usage Patterns

1. **Internal API Calls** (RaindropAPI, Weather):

   ```csharp
   var httpClient = _httpClientFactory.CreateClient();
   var response = await httpClient.GetAsync("/api/RaindropListVideos", cancellationToken);
   ```

2. **External API Calls** (ImageValidationService, SimpleImageValidationService):

   ```csharp
   var httpClient = _httpClientFactory.CreateClient();
   var response = await httpClient.GetAsync("https://external-api.com/image.jpg", cancellationToken);
   ```

3. **Mock Data Access** (DummyRaindropAPI):

   ```csharp
   // Uses HttpClient to load JSON files via HTTP requests
   var httpClient = _httpClientFactory.CreateClient();
   var response = await httpClient.GetAsync("mockdata/videos.json", cancellationToken);
   ```

#### Alternative Approaches (Not Used)

**Named HttpClients** (More complex, not needed for current use case):

```csharp
builder.Services.AddHttpClient("Internal", client => {
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
});
builder.Services.AddHttpClient("External", client => {
    // No base address for external calls
});
```

**Service-Specific Configuration** (Increases complexity):

```csharp
// Each service configures its own HttpClient
var httpClient = _httpClientFactory.CreateClient();
httpClient.BaseAddress = GetServiceSpecificBaseAddress();
```

#### Key Benefits of Current Approach

- **Flexibility**: External services can use absolute URLs to override base address
- **Simplicity**: Single configuration point reduces complexity
- **Consistency**: All internal services use same base address
- **Testability**: Easy to mock with single HttpClient configuration
- **Performance**: Reuses connections efficiently

### Notes

- **MANDATORY**: Follow partial class organization standards from copilot instructions.
- **Services**: Split into main logic (.cs) and LoggerMessage delegates (.Logging.cs) files.
- **Tests**: Split into test methods (.cs) and TestScope/helpers (.Helpers.cs) files.
- **TestScope Architecture**: ALL test classes must use TestScope pattern with fluent configuration.
- **TUnit Framework**: Use `[Test]` attribute with fluent chaining for related assertions.
- **Test Naming**: Use `Component_Behavior_ExpectedOutcome` pattern (underscores only in tests).
- **Mocking**: LightMock.Generator ONLY with explicit parameter specification (NSubstitute deprecated).
- **LoggerMessage**: Use delegates instead of direct Logger calls for performance.
- **Build Verification**: `dotnet clean && dotnet build --no-restore --verbosity quiet` (zero warnings except IL2111).
- **Test Execution**: `dotnet test` or `dotnet test --filter "FullyQualifiedName~[TestClassName]"`.
- **Architecture**: Feature-based organization under `src/redmuffin.Blazor.StaticWeb/Features/`.
- **Azure Functions**: Isolated worker model with dependency injection.
- **UI Framework**: Zurb Foundation classes for consistent styling.
- **Async Patterns**: `ConfigureAwait(false)` on ALL awaits with proper error handling.
- **Code Quality**: StyleCop/Meziantou analyzer compliance (zero tolerance).
- **Environment Detection**: `NavigationManager.BaseUri` for localhost:5233 (dummy) vs localhost:4280 (real API).
- **JSON Serialization**: Enhanced context with robust parsing and error handling.
- **Factory Pattern**: Clean separation between dummy and real API implementations.
- **Test Location**: All new tests in `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/` with proper structure.
- **Resource Management**: Proper disposal via using statements and IDisposable implementation.
- **Test Quality**: Arrange-Act-Assert pattern with comprehensive error scenario testing.
- **HttpClient Usage**: Use default HttpClient without named clients, leverage base address for internal calls, absolute URLs for external calls.

#### Critical JSON Serialization Fix (Resolved)

**Issue**: `System.NotSupportedException` with message "JsonTypeInfo metadata for type 'System.Collections.Generic.List<redmuffin.Blazor.StaticWeb.Common.Raindrop.RaindropItem>' is not available" occurred during JSON deserialization in both `DummyRaindropAPI.cs` and `RaindropAPI.cs`.

**Root Cause**: The `DeserializeWithFallbackAsync<T>` method was incorrectly calling `GetTypeInfo(typeof(T))` on `JsonSerializerOptions` instead of using the proper generic deserialization method with the context.

**Solution Applied**:

```csharp
// ❌ INCORRECT (caused NotSupportedException):
var result = JsonSerializer.Deserialize(jsonContent, RaindropJsonSerializerContext.DefaultOptions.GetTypeInfo(typeof(T))) as T;

// ✅ CORRECT (working solution):
var result = JsonSerializer.Deserialize<T>(jsonContent, RaindropJsonSerializerContext.DefaultOptions);
```

**Files Fixed**:

- `src/redmuffin.Blazor.StaticWeb/Features/Raindrop/Services/DummyRaindropAPI.cs` - Lines 149, 165, 181
- `src/redmuffin.Blazor.StaticWeb/Features/Raindrop/Services/RaindropAPI.cs` - Lines 151, 167, 183

**Key Learning**: When using `JsonSerializerContext` with source generation, always use the generic `JsonSerializer.Deserialize<T>(content, options)` method rather than attempting to manually retrieve `TypeInfo` from the options. The `RaindropJsonSerializerContext` already has proper `[JsonSerializable(typeof(List<RaindropItem>))]` attributes and `TypeInfoResolver = Default` configured in all options (DefaultOptions, LenientOptions, StrictOptions).

**Verification**: Both Videos and Articles pages now load successfully with proper JSON deserialization using all three fallback strategies.

#### Test Serialization Consistency Fix (Resolved)

**Issue**: Test failures due to serialization/deserialization mismatch between test helpers and API implementations.

**Root Cause**: Test helpers used `RaindropJsonSerializerContext.Default.RaindropItemList` for serialization while APIs used `RaindropJsonSerializerContext.DefaultOptions` for deserialization.

**Solution**: Updated test helpers to use consistent `RaindropJsonSerializerContext.DefaultOptions` for both serialization and deserialization.

**Files Modified**:

- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Raindrop/Services/IRaindropAPITests.Helpers.cs`
- `tests/redmuffin.Blazor.StaticWeb.Api.Tests/Functions/ArticlesApiVerification_Tests.cs`

#### Error Handling Improvements (Resolved)

**Issue**: Inconsistent exception handling between different test scenarios (missing files vs malformed JSON vs API failures).

**Solutions Applied**:

1. **DummyRaindropAPI**: Return empty collections for missing files (404) to support development scenarios
2. **RaindropAPI**: Let `HttpRequestException` pass through without wrapping for proper test assertions
3. **Both APIs**: Throw `InvalidOperationException` when JSON deserialization fails completely

**Test Results**: Reduced failed tests from 18 to 8, successfully resolving all IRaindropAPI-related test failures. Remaining failures are unrelated Home navigation tests.

## Tasks

- [ ] 1.0 Create Service Abstraction Layer
  - [x] 1.1 Create IRaindropAPI interface with GetVideosAsync and GetArticlesAsync methods
  - [x] 1.2 Add comprehensive XML documentation with exception scenarios for interface methods
  - [x] 1.3 Create IRaindropAPIFactory interface for environment-based service resolution
  - [x] 1.4 Ensure interface supports CancellationToken and proper async patterns
  - [x] 1.5 Follow partial class organization standards for all service interfaces
- [x] 1.0 Create Service Abstraction Layer
- [x] 2.0 Implement Environment Detection and Factory Pattern
  - [x] 2.1 Create RaindropAPIFactory class implementing IRaindropAPIFactory
  - [x] 2.2 Create RaindropAPIFactory.Logging.cs with LoggerMessage delegates
  - [x] 2.3 Implement environment detection logic using NavigationManager.BaseUri
  - [x] 2.4 Add logic to distinguish localhost:5233 (dummy) vs localhost:4280 (real API)
  - [x] 2.5 Add comprehensive logging using LoggerMessage delegates for performance
  - [x] 2.6 Update Program.cs with factory-based service registration pattern
  - [x] 2.7 Implement proper resource management with using statements
- [x] 3.0 Create Dummy Data and Implementation
  - [x] 3.1 Create realistic dummy data files (videos.json and articles.json) in wwwroot/mockdata folder
  - [x] 3.2 Implement DummyRaindropAPI class with HTTP-based data loading from wwwroot
  - [x] 3.3 Create DummyRaindropAPI.Logging.cs with LoggerMessage delegates
  - [x] 3.4 Add proper error handling for missing files and JSON parsing errors
  - [x] 3.5 Ensure dummy data structure matches RaindropItem model exactly
  - [x] 3.6 Include edge cases in dummy data (missing covers, long titles, various content types)
  - [x] 3.7 Implement IDisposable pattern if managing file resources
  - [x] 3.8 Update to use default HttpClient without named clients
- [x] 4.0 Update Blazor Components to Use Service Abstraction
  - [x] 4.1 Update Videos.razor.cs to inject IRaindropAPI instead of HttpClient
  - [x] 4.2 Update Articles.razor.cs to inject IRaindropAPI instead of HttpClient
  - [x] 4.3 Replace direct API calls with service method calls through IRaindropAPI
  - [x] 4.4 Add proper dependency validation with ArgumentNullException.ThrowIfNull
  - [x] 4.5 Implement LoggerMessage delegates for better performance
  - [x] 4.6 Preserve existing error handling patterns and UI consistency
  - [x] 4.7 Ensure all async calls use ConfigureAwait(false) pattern
  - [x] 4.8 Follow Component_Behavior_ExpectedOutcome naming for any helper methods
- [x] 5.0 Enhance JSON Serialization and Error Handling
  - [x] 5.1 Update RaindropJsonSerializerContext with enhanced configuration
  - [x] 5.2 Add support for edge cases and malformed JSON responses
  - [x] 5.3 Implement RaindropAPI class for real Azure Function API calls
  - [x] 5.4 Create RaindropAPI.Logging.cs with LoggerMessage delegates
  - [x] 5.5 Add comprehensive error handling with proper logging patterns
  - [x] 5.6 Ensure ConfigureAwait(false) usage throughout async operations
  - [x] 5.7 Implement proper HttpClient disposal and resource management
  - [x] 5.8 Follow StyleCop/Meziantou analyzer rules with zero tolerance
- [ ] 6.0 Implement Comprehensive Testing Suite
  - [ ] 6.1 Create TUnit tests for IRaindropAPI interface implementations
  - [ ] 6.2 Create IRaindropAPITests.Helpers.cs with TestScope and helper methods
  - [ ] 6.3 Create TUnit tests for RaindropAPIFactory and environment detection
  - [ ] 6.4 Create RaindropAPIFactoryTests.Helpers.cs with TestScope and helper methods
  - [ ] 6.5 Create TUnit tests for DummyRaindropAPI with HttpClient mocking
  - [ ] 6.6 Create DummyRaindropAPITests.Helpers.cs with TestScope and helper methods
  - [ ] 6.7 Create TUnit tests for RaindropAPI with HttpClient mocking using LightMock.Generator
  - [ ] 6.8 Create RaindropAPITests.Helpers.cs with TestScope and helper methods
  - [ ] 6.9 Create TUnit tests for updated Videos and Articles components
  - [ ] 6.10 Create VideosPageTests.Helpers.cs and ArticlesPageTests.Helpers.cs with TestScope
  - [ ] 6.11 Ensure ALL tests use TestScope architecture with fluent configuration
  - [ ] 6.12 Use Component_Behavior_ExpectedOutcome naming pattern for test methods
  - [ ] 6.13 Implement Arrange-Act-Assert pattern with comprehensive error scenarios
  - [ ] 6.14 Ensure all tests use proper LightMock.Generator patterns with explicit parameters
  - [ ] 6.15 Use fluent chaining for related assertions in TUnit framework
  - [ ] 6.16 Verify test coverage >90% for all new service implementations
  - [ ] 6.17 Run dotnet clean && dotnet build to ensure zero build warnings (except IL2111)
