# HttpClientFactory Simplification - To Do

## Relevant Files

### Blazor Components

- `src/redmuffin.Blazor.StaticWeb/Program.cs` - Main Blazor WebAssembly configuration for IHttpClientFactory.
- `src/redmuffin.Blazor.StaticWeb/Features/Pages/MarkdownExamplesPage/MarkdownExamples.razor.cs` - Code-behind for MarkdownExamples component.
- `src/redmuffin.Blazor.StaticWeb/Features/Pages/WeatherPage/Weather.razor` - Weather component template.
- `src/redmuffin.Blazor.StaticWeb/Features/Pages/WeatherPage/Weather.razor.cs` - Code-behind for Weather component.

### Services

- `src/redmuffin.Blazor.StaticWeb/Features/Raindrop/Services/RaindropAPI.cs` - Main Raindrop API service implementation.
- `src/redmuffin.Blazor.StaticWeb/Features/Raindrop/Services/DummyRaindropAPI.cs` - Mock Raindrop API service for testing.
- `src/redmuffin.Blazor.StaticWeb/Features/Raindrop/Services/IRaindropAPI.cs` - Interface for Raindrop API services.

### Azure Functions (API)

- `src/redmuffin.Blazor.StaticWeb.Api/Program.cs` - Azure Functions host configuration for IHttpClientFactory.
- `src/redmuffin.Blazor.StaticWeb.Api/Functions/RaindropListArticles.cs` - Azure Function for listing articles.
- `src/redmuffin.Blazor.StaticWeb.Api/Functions/RaindropListVideos.cs` - Azure Function for listing videos.

### Tests

- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Raindrop/Services/RaindropAPITests.cs` - TUnit tests for RaindropAPI service.
- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Raindrop/Services/RaindropAPITests.Helpers.cs` - TestScope and utilities for RaindropAPI tests.
- `tests/redmuffin.Blazor.StaticWeb.Api.Tests/NewTests/HttpClientFactoryTests.cs` - TUnit tests for HttpClientFactory integration.
- `tests/redmuffin.Blazor.StaticWeb.Api.Tests/NewTests/HttpClientFactoryTests.Helpers.cs` - TestScope and utilities for HttpClientFactory tests.
- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Pages/WeatherPage/WeatherTests.cs` - TUnit tests for Weather component.
- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Pages/WeatherPage/WeatherTests.Helpers.cs` - TestScope and utilities for Weather tests.

### Notes

- Examine all HomeTests*.* files before creating or changing tests. Let these tests guide you in the right direction.
- Tests use TUnit framework with `[Test]` attribute for test methods and `[Arguments]` for data-driven tests.
- Use `dotnet clean && dotnet build --no-restore --verbosity quiet` to verify zero build warnings (except IL2111).
- Use `dotnet test` to run all tests or `dotnet test --filter "FullyQualifiedName~[TestClassName]"` for specific test classes.
- All new test code must be created in `NewTests/` folder structure following TestScope and .Helpers.cs partial class pattern.
- Blazor components follow feature-based organization under `src/redmuffin.Blazor.StaticWeb/Features/`.
- Azure Functions use isolated worker model with dependency injection.
- All async methods must use `ConfigureAwait(false)` and proper error handling.
- Follow StyleCop/Meziantou analyzer rules for code quality.
- Use C# 13 primary constructors where appropriate.
- Apply modern patterns: `using var client = factory.CreateClient();` and `ArgumentNullException.ThrowIfNull()`.
- Eliminate ALL named HttpClient configurations in favor of single default configuration.
- Configure default HttpClient with BaseAddress and 30-second timeout for internal API calls.
- Allow external API consumers to modify HttpClient properties after creation for custom configurations.
- Maintain partial class organization: main files contain business logic, .Logging.cs files contain LoggerMessage delegates.
- Test files follow partial class pattern: main file contains only `[Test]` methods, .Helpers.cs contains TestScope, mocks, and utilities.

## Tasks

- [x] 1.0 Configure Default HttpClientFactory in Program.cs Files
  - [x] 1.1 Update Blazor WebAssembly Program.cs to configure single default HttpClient with BaseAddress and 30-second timeout
  - [x] 1.2 Remove all named HttpClient registrations ("DefaultClient", etc.) from Blazor Program.cs
  - [x] 1.3 Update Azure Functions Program.cs to configure IHttpClientFactory with default settings
  - [x] 1.4 Remove any named HttpClient configurations from Azure Functions Program.cs
  - [x] 1.5 Verify both Program.cs files compile without warnings using `dotnet clean && dotnet build --no-restore --verbosity quiet`

- [x] 2.0 Migrate Blazor Services to Use IHttpClientFactory
  - [x] 2.1 Update RaindropAPI.cs constructor to inject IHttpClientFactory instead of HttpClient
  - [x] 2.2 Modify RaindropAPI.cs methods to use `using var httpClient = _httpClientFactory.CreateClient();` pattern
  - [x] 2.3 Update DummyRaindropAPI.cs constructor to inject IHttpClientFactory (if applicable)
  - [x] 2.4 Apply C# 13 primary constructor pattern where appropriate in service classes
  - [x] 2.5 Ensure all async methods use `ConfigureAwait(false)` and proper error handling
  - [x] 2.6 Add ArgumentNullException.ThrowIfNull() validation for IHttpClientFactory parameters
  - [x] 2.7 Create or update .Logging.cs partial files with LoggerMessage delegates for any new logging
  - [x] 2.8 Verify services compile without warnings and maintain existing functionality

- [x] 3.0 Update Blazor Components to Inject IHttpClientFactory
  - [x] 3.1 Update MarkdownExamples.razor.cs to inject IHttpClientFactory instead of HttpClient
  - [x] 3.2 Update Weather.razor.cs to inject IHttpClientFactory and modify HTTP calls accordingly
  - [x] 3.3 Search for and update any other components currently injecting HttpClient directly
  - [x] 3.4 Ensure all component OnInitializedAsync methods include ArgumentNullException.ThrowIfNull() validation
  - [x] 3.5 Apply `using var httpClient = HttpClientFactory.CreateClient();` pattern in component methods
  - [x] 3.6 Maintain existing component functionality while simplifying HttpClient usage
  - [x] 3.7 Verify all components compile without warnings and render correctly

- [x] 4.0 Migrate Azure Functions to Use IHttpClientFactory
  - [x] 4.1 Update RaindropListArticles.cs function to use IHttpClientFactory pattern
  - [x] 4.2 Update RaindropListVideos.cs function to use IHttpClientFactory pattern
  - [x] 4.3 Search for and update any other Azure Functions using HttpClient
  - [x] 4.4 Ensure proper dependency injection registration for IHttpClientFactory in functions
  - [x] 4.5 Apply modern C# patterns and maintain isolated worker model compatibility
  - [x] 4.6 Verify functions compile without warnings and maintain API behavior

- [x] 5.0 Create and Update Test Infrastructure for IHttpClientFactory
  - [x] 5.1 Create NewTests folder structure in Api.Tests project if it doesn't exist
  - [x] 5.2 Create HttpClientFactoryTests.cs with [Test] methods for IHttpClientFactory integration
  - [x] 5.3 Create HttpClientFactoryTests.Helpers.cs with TestScope class and IHttpClientFactory mocking using LightMock.Generator
  - [x] 5.4 Update RaindropAPITests.Helpers.cs to mock IHttpClientFactory instead of HttpClient
  - [x] 5.5 Create WeatherTests.cs and WeatherTests.Helpers.cs for Weather component testing with IHttpClientFactory
  - [x] 5.6 Update existing test files in NewTests folders to use IHttpClientFactory mocking patterns
  - [x] 5.7 Ensure all test classes follow partial class pattern: main file with [Test] methods, .Helpers.cs with TestScope
  - [x] 5.8 Apply TUnit fluent chaining for related assertions and Assert.Multiple for unrelated concerns
  - [x] 5.9 Add tests to verify proper null validation in component initialization (ArgumentNullException.ThrowIfNull() for injected dependencies)
  - [x] 5.10 Verify all tests pass with `dotnet test` and maintain existing test coverage

- [x] 6.0 Clean Up Legacy HttpClient Configurations and Verify Migration
  - [x] 6.1 Search codebase for any remaining named HttpClient references and remove them
  - [x] 6.2 Remove any unused HttpClient-related using statements and dependencies
  - [x] 6.3 Run full build verification: `dotnet clean && dotnet build --no-restore --verbosity quiet`
  - [x] 6.4 Run complete test suite: `dotnet test` to ensure all tests pass
  - [x] 6.5 Verify zero build warnings (except IL2111) across all projects
  - [x] 6.6 Test application functionality manually to ensure no regressions
  - [x] 6.7 Update any remaining documentation references to reflect new IHttpClientFactory patterns
  - [x] 6.8 Generate and review code coverage report to ensure no functionality lost during migration
