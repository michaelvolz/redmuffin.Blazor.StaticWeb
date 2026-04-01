# Product Requirements Document: Video Image Placeholders with Clean Architecture - To Do

## Relevant Files

### Blazor Components

- `src/redmuffin.Blazor.StaticWeb/Features/Pages/VideosPage/Videos.razor` - Main Videos page component with image placeholder functionality.
- `src/redmuffin.Blazor.StaticWeb/Features/Pages/VideosPage/Videos.razor.cs` - Code-behind for Videos component with image placeholder logic.
- `src/redmuffin.Blazor.StaticWeb/Features/Pages/VideosPage/Videos.Logging.cs` - LoggerMessage delegates for Videos component.
- `src/redmuffin.Blazor.StaticWeb/Features/Pages/ArticlesPage/Articles.razor.cs` - Articles component to be refactored to use shared services.
- `src/redmuffin.Blazor.StaticWeb/Features/Pages/ArticlesPage/Articles.Logging.cs` - LoggerMessage delegates for Articles component (if not exists).

### Shared Services

- `src/redmuffin.Blazor.StaticWeb/Core/ImagePlaceholder/Abstractions/IImagePlaceholderService.cs` - Interface for image placeholder operations.
- `src/redmuffin.Blazor.StaticWeb/Core/ImagePlaceholder/Abstractions/IImageValidationCacheService.cs` - Interface for image validation caching.
- `src/redmuffin.Blazor.StaticWeb/Core/ImagePlaceholder/Services/ImagePlaceholderService.cs` - Main service for image placeholder functionality.
- `src/redmuffin.Blazor.StaticWeb/Core/ImagePlaceholder/Services/ImagePlaceholderService.Logging.cs` - LoggerMessage delegates for ImagePlaceholderService.
- `src/redmuffin.Blazor.StaticWeb/Core/ImagePlaceholder/Services/ImageValidationCacheService.cs` - Service for caching image validation results.
- `src/redmuffin.Blazor.StaticWeb/Core/ImagePlaceholder/Services/ImageValidationCacheService.Logging.cs` - LoggerMessage delegates for ImageValidationCacheService.
- `src/redmuffin.Blazor.StaticWeb/Core/ImagePlaceholder/Services/PlaceholderGenerationService.cs` - Service for generating SVG placeholders.
- `src/redmuffin.Blazor.StaticWeb/Core/ImagePlaceholder/Services/PlaceholderGenerationService.Logging.cs` - LoggerMessage delegates for PlaceholderGenerationService.
- `src/redmuffin.Blazor.StaticWeb/Core/ImagePlaceholder/Models/ImageValidationResult.cs` - Model for image validation results.
- `src/redmuffin.Blazor.StaticWeb/Core/ImagePlaceholder/Models/PlaceholderConfiguration.cs` - Configuration model for placeholders.
- `src/redmuffin.Blazor.StaticWeb/Core/ImagePlaceholder/Templates/SvgPlaceholderTemplate.cs` - SVG template for placeholders.

### Dependency Injection

- `src/redmuffin.Blazor.StaticWeb/Program.cs` - Register new image placeholder services in DI container.

### Tests

- `tests/redmuffin.Blazor.StaticWeb.Tests/Core/ImagePlaceholder/Services/ImagePlaceholderServiceTests.cs` - TUnit tests for ImagePlaceholderService.
- `tests/redmuffin.Blazor.StaticWeb.Tests/Core/ImagePlaceholder/Services/ImagePlaceholderServiceTests.Helpers.cs` - TestScope and test infrastructure for ImagePlaceholderService.
- `tests/redmuffin.Blazor.StaticWeb.Tests/Core/ImagePlaceholder/Services/ImageValidationCacheServiceTests.cs` - TUnit tests for ImageValidationCacheService.
- `tests/redmuffin.Blazor.StaticWeb.Tests/Core/ImagePlaceholder/Services/ImageValidationCacheServiceTests.Helpers.cs` - TestScope and test infrastructure for ImageValidationCacheService.
- `tests/redmuffin.Blazor.StaticWeb.Tests/Core/ImagePlaceholder/Services/PlaceholderGenerationServiceTests.cs` - TUnit tests for PlaceholderGenerationService.
- `tests/redmuffin.Blazor.StaticWeb.Tests/Core/ImagePlaceholder/Services/PlaceholderGenerationServiceTests.Helpers.cs` - TestScope and test infrastructure for PlaceholderGenerationService.
- `tests/redmuffin.Blazor.StaticWeb.Tests/Features/Pages/VideosPage/VideosTests.cs` - TUnit tests for Videos component.
- `tests/redmuffin.Blazor.StaticWeb.Tests/Features/Pages/VideosPage/VideosTests.Helpers.cs` - TestScope and test infrastructure for Videos component.
- `tests/redmuffin.Blazor.StaticWeb.Tests/Features/Pages/ArticlesPage/ArticlesTests.cs` - Updated TUnit tests for Articles component.
- `tests/redmuffin.Blazor.StaticWeb.Tests/Features/Pages/ArticlesPage/ArticlesTests.Helpers.cs` - TestScope and test infrastructure for Articles component.

### Notes

#### 🚨 MANDATORY: Partial Class Organization Standards

- **ALL Services MUST follow**: `ServiceName.cs` (main logic) + `ServiceName.Logging.cs` (LoggerMessage delegates ONLY)
- **ALL Components MUST follow**: `ComponentName.razor.cs` (main) + `ComponentName.Logging.cs` (LoggerMessage delegates ONLY)
- **ALL Tests MUST follow**: `TestClassName.cs` ([Test] methods ONLY) + `TestClassName.Helpers.cs` (TestScope, mocks, utilities)

#### 🧪 MANDATORY: TestScope Architecture

- ALL test classes MUST use TestScope pattern with primary constructor: `public sealed class TestScope(string baseUri = "http://localhost:5000/") : IDisposable`
- TestScope MUST include fluent builder methods: `WithStandardServices()`, `WithFailingHttpClient()`, `WithJSInterop()`
- Factory method MUST be provided: `private static TestScope CreateTestScope() => new TestScope().WithStandardServices();`
- TUnit fluent chaining for related assertions: `await Assert.That(result).IsNotNull().And.Contains("expected");`
- Assert.Multiple for unrelated concerns: DOM structure vs logging vs different objects

#### 🎭 MANDATORY: LightMock.Generator Requirements

- Mocking uses LightMock.Generator ONLY (NSubstitute deprecated)
- ALWAYS specify ALL parameters explicitly: `_mock.Arrange(f => f.GetAsync("key", CancellationToken.None))`
- Use `CancellationToken.None`, `null`, `The<T>.IsAnyValue` for optional parameters
- Mock naming: `var userServiceMock = new Mock<IUserService>();`

#### 🚨 MANDATORY: LoggerMessage Delegates

- ALL logging MUST use LoggerMessage.Define (NOT Logger.LogError())
- LoggerMessage delegates MUST be in separate `.Logging.cs` partial files
- Performance-critical requirement for Blazor WebAssembly

#### 🔧 MANDATORY: Code Quality Standards (ZERO TOLERANCE)

- Run `dotnet clean && dotnet build --no-restore --verbosity quiet` after EVERY C# file change
- Zero build warnings required (except IL2111)
- `ConfigureAwait(false)` on ALL awaits
- `ArgumentNullException.ThrowIfNull()` for ALL parameters
- StyleCop/Meziantou/Microsoft analyzer compliance
- Member order: fields→properties→constructors→methods
- Remove ALL trailing whitespace
- ONE blank line maximum between members

#### 📋 Testing Quality Checklist

- [ ] ConfigureAwait(false) on all async calls
- [ ] TestScope pattern with fluent configuration
- [ ] TUnit chaining for related assertions
- [ ] Clear AAA structure with comments
- [ ] Single responsibility principle
- [ ] Zero build warnings compliance
- [ ] Resource disposal via using statements
- [ ] Comprehensive error scenario testing
- [ ] Partial class structure: Tests in main, helpers in .Helpers.cs

#### 🎯 General Requirements

- Tests use TUnit framework with `[Test]` attribute for test methods and `[Arguments]` for data-driven tests
- Use `dotnet test` to run all tests or `dotnet test --filter "FullyQualifiedName~[TestClassName]"` for specific test classes
- Blazor components follow feature-based organization under `src/redmuffin.Blazor.StaticWeb/Features/`
- Shared services are organized under `src/redmuffin.Blazor.StaticWeb/Core/ImagePlaceholder/`
- All async methods must use `ConfigureAwait(false)` and proper error handling
- Maintain identical visual behavior between Articles and Videos pages
- Extract shared logic to eliminate code duplication

## Tasks

## Task 1.0: Create Shared Image Placeholder Service Architecture ✅ COMPLETED

### Sub-tasks

- [x] 1.1 Create Core/ImagePlaceholder directory structure
- [x] 1.2 Create IImagePlaceholderService interface
- [x] 1.3 Create IImageValidationCacheService interface
- [x] 1.4 Create ImageValidationResult model
- [x] 1.5 Create PlaceholderConfiguration model
- [x] 1.6 Create SvgPlaceholderTemplate class
- [x] 1.7 Create ImagePlaceholderService implementation
- [x] 1.8 Create ImagePlaceholderService.Logging.cs with LoggerMessage delegates
- [x] 1.9 Create ImageValidationCacheService implementation
- [x] 1.10 Create ImageValidationCacheService.Logging.cs with LoggerMessage delegates
- [x] 1.11 Create PlaceholderGenerationService implementation
- [x] 1.12 Create PlaceholderGenerationService.Logging.cs with LoggerMessage delegates

## Task 2.0: Extract Common Logic from Articles Page ✅ COMPLETED

### Sub-tasks

- [x] 2.1 Extract GetDefaultPlaceholder() to ImagePlaceholderService
- [x] 2.2 Extract GenerateSimplePlaceholder() to PlaceholderGenerationService
- [x] 2.3 Extract GetImageUrl() logic to ImagePlaceholderService
- [x] 2.4 Extract HandleImageLoadAsync() to ImagePlaceholderService
- [x] 2.5 Extract HasFallbackPlaceholder() to ImagePlaceholderService
- [x] 2.6 Extract GetFallbackReason() to ImagePlaceholderService
- [x] 2.7 Extract image cache management logic to ImageValidationCacheService
- [x] 2.8 Create Articles.Logging.cs with extracted LoggerMessage delegates
- [x] 2.9 Refactor Articles.razor.cs to use new shared services
- [x] 2.10 Update Articles.razor template to use new service methods

## Task 3.0: Implement Videos Page Image Placeholder Functionality ✅ COMPLETED

### Sub-tasks

- [x] 3.1 Add IImagePlaceholderService injection to Videos.razor.cs
- [x] 3.2 Add ISimpleImageValidationService injection to Videos.razor.cs
- [x] 3.3 Add image URL cache field to Videos.razor.cs
- [x] 3.4 Implement PopulateImageUrlCacheAsync() in Videos.razor.cs
- [x] 3.5 Implement GetImageUrl() wrapper in Videos.razor.cs
- [x] 3.6 Implement HandleImageLoadAsync() wrapper in Videos.razor.cs
- [x] 3.7 Implement HasFallbackPlaceholder() wrapper in Videos.razor.cs
- [x] 3.8 Implement GetFallbackReason() wrapper in Videos.razor.cs
- [x] 3.9 Create Videos.Logging.cs with LoggerMessage delegates
- [x] 3.10 Update Videos.razor template with image placeholder functionality
- [x] 3.11 Update FetchVideosAsync() to populate image cache

## Task 4.0: Update Dependency Injection Configuration ✅ COMPLETED

### Sub-tasks

- [x] 4.1 Register IImagePlaceholderService in DI container
- [x] 4.2 Register IImageValidationCacheService in DI container
- [x] 4.3 Register PlaceholderGenerationService in DI container
- [x] 4.4 Verify service lifetimes are appropriate (Scoped/Singleton)
- [x] 4.5 Update Program.cs or ServiceCollectionExtensions

## Task 5.0: Create Comprehensive Unit Tests

### Sub-tasks

- [x] 5.1 Create ImagePlaceholderServiceTests.cs with TestScope architecture
- [x] 5.2 Create ImagePlaceholderServiceTests.Helpers.cs with test infrastructure
- [x] 5.3 Create ImageValidationCacheServiceTests.cs with TestScope architecture
- [x] 5.4 Create ImageValidationCacheServiceTests.Helpers.cs with test infrastructure
- [x] 5.5 Create PlaceholderGenerationServiceTests.cs with TestScope architecture
- [x] 5.6 Create PlaceholderGenerationServiceTests.Helpers.cs with test infrastructure
- [x] 5.7 Refactor VideosTests.cs to follow HomeTests pattern (remove TUnit setup methods)
- [x] 5.8 Create VideosTests.Helpers.cs with test infrastructure
- [x] 5.9 Update ArticlesTests.cs for refactored implementation
- [x] 5.10 Update ArticlesTests.Helpers.cs for refactored implementation
- [x] 5.11 Verify 100% test coverage for all new services
- [x] 5.12 Verify all tests use ConfigureAwait(false) and LightMock.Generator

## Task 6.0: Refactor VideosTests to Follow HomeTests Pattern

### Sub-tasks

- [x] 6.1 Remove `[Before(Test)]` and `[After(Test)]` setup methods from VideosTests.cs
- [x] 6.2 Remove shared `_testScope` field and related setup logic
- [x] 6.3 Create factory method `CreateTestScope()` following HomeTests pattern
- [x] 6.4 Update each test method to use `using var scope = CreateTestScope();`
- [x] 6.5 Move TestScope class and infrastructure to VideosTests.Helpers.cs
- [x] 6.6 Update TestScope to use fluent builder pattern like HomeTests
- [x] 6.7 Ensure proper test isolation without shared state
- [x] 6.8 Verify all tests pass with new pattern
- [x] 6.9 Remove manual mock implementations and use standard Mock<T> pattern
- [x] 6.10 Ensure zero build warnings compliance

**CURRENT STATUS: All tasks completed successfully. Tests moved to NewTests folder with proper testing style, build warnings resolved for zero-warning policy compliance, and VideosTests refactored to follow established HomeTests pattern for better test isolation. All ConfigureAwait(false) issues addressed and comprehensive test coverage achieved.**
