## Relevant Files

### Blazor Components

- `src/redmuffin.Blazor.StaticWeb/Features/Pages/ArticlesPage/Articles.razor` - Main Articles component with integrated image fallback functionality.
- `src/redmuffin.Blazor.StaticWeb/Features/Pages/ArticlesPage/Articles.razor.cs` - Code-behind for Articles component with image processing logic.

### Services

- `src/redmuffin.Blazor.StaticWeb/Services/IOpenGraphImagesService.cs` - Interface for Open Graph image retrieval service.
- `src/redmuffin.Blazor.StaticWeb/Services/OpenGraphImagesService.cs` - Service for retrieving Open Graph images and managing cache.
- `src/redmuffin.Blazor.StaticWeb/Services/IImageValidationService.cs` - Interface for image validation service.
- `src/redmuffin.Blazor.StaticWeb/Services/ImageValidationService.cs` - Service for validating image URLs using HTTP HEAD requests.
- `src/redmuffin.Blazor.StaticWeb/Services/IBrowserStorageService.cs` - Interface for browser storage service with LRU eviction and quota management.
- `src/redmuffin.Blazor.StaticWeb/Services/BrowserStorageService.cs` - Enhanced service for local storage management with LRU eviction and quota management.
- `src/redmuffin.Blazor.StaticWeb/Services/ICacheService.cs` - Interface for cache service with namespace separation.
- `src/redmuffin.Blazor.StaticWeb/Services/CacheService.cs` - Service for namespace-separated cache management.
- `src/redmuffin.Blazor.StaticWeb/Services/ICacheMonitoringService.cs` - Interface for comprehensive cache monitoring and optimization.
- `src/redmuffin.Blazor.StaticWeb/Services/CacheMonitoringService.cs` - Service for cache health monitoring, statistics, and performance optimization.

### Azure Functions (API)

- `src/redmuffin.Blazor.StaticWeb.Api/Functions/GetOpenGraphImages.cs` - Azure Function for batch Open Graph image retrieval.

### Shared/Common

- `src/redmuffin.Blazor.StaticWeb.Common/Models/ArticleImageRequest.cs` - Request model for article image processing.
- `src/redmuffin.Blazor.StaticWeb.Common/Models/ArticleImageResponse.cs` - Response model for article image processing.
- `src/redmuffin.Blazor.StaticWeb.Common/Models/BatchImageRequest.cs` - Batch request model for multiple articles.
- `src/redmuffin.Blazor.StaticWeb.Common/Models/BatchImageResponse.cs` - Batch response model for multiple articles.
- `src/redmuffin.Blazor.StaticWeb.Common/Models/CachedImageData.cs` - Model for cached image data structure.
- `src/redmuffin.Blazor.StaticWeb.Common/Models/ImageValidationResult.cs` - Model for image validation results.
- `src/redmuffin.Blazor.StaticWeb.Common/Enums/ImageSource.cs` - Enum for image source types.

### Styles

- `src/redmuffin.Blazor.StaticWeb/wwwroot/scss/_articles.scss` - SCSS partial for Articles component with image loading states and placeholders.
- `src/redmuffin.Blazor.StaticWeb/wwwroot/scss/_shimmerLoadingEffect.scss` - SCSS partial for shimmer loading effects.
- `src/redmuffin.Blazor.StaticWeb/wwwroot/scss/_ArticleImageDisplay.scss` - SCSS partial for article image display enhancements.
- `src/redmuffin.Blazor.StaticWeb/wwwroot/scss/app.scss` - Main SCSS file that imports all partials and compiles to CSS.

### Tests

- `tests/redmuffin.Blazor.StaticWeb.Tests/Services/OpenGraphImagesServiceTests.cs` - TUnit tests for Open Graph image service.
- `tests/redmuffin.Blazor.StaticWeb.Tests/Services/ImageValidationServiceTests.cs` - TUnit tests for image validation service.
- `tests/redmuffin.Blazor.StaticWeb.Tests/Services/BrowserStorageServiceTests.cs` - TUnit tests for browser storage service.
- `tests/redmuffin.Blazor.StaticWeb.Tests/Features/Pages/ArticlesPage/ArticlesTests.cs` - TUnit tests for Articles component integration.
- `tests/redmuffin.Blazor.StaticWeb.Api.Tests/Functions/GetOpenGraphImages_Tests.cs` - TUnit tests for Azure Function.
- `tests/redmuffin.Blazor.StaticWeb.Tests/Integration/OpenGraphIntegrationTests.cs` - TUnit integration tests for end-to-end image retrieval and caching.
- `tests/redmuffin.Blazor.StaticWeb.Tests/Integration/MockCacheService.cs` - Mock cache service for integration tests.
- `tests/redmuffin.Blazor.StaticWeb.Tests/Integration/TestHttpMessageHandler.cs` - Mock HTTP message handler for integration tests.
- `tests/redmuffin.Blazor.StaticWeb.Tests/Integration/TestBase.cs` - Base test class for integration tests.
- `tests/redmuffin.Blazor.StaticWeb.Tests/Performance/OpenGraphPerformanceTests.cs` - TUnit performance tests for batch processing and caching efficiency.
- `tests/redmuffin.Blazor.StaticWeb.Tests/Performance/TestBase.cs` - Base test class for performance tests.

### Notes

- Tests use TUnit framework with `[Test]` attribute for test methods and `[Arguments]` for data-driven tests.
- Use `dotnet test` to run all tests or `dotnet test --filter "FullyQualifiedName~[TestClassName]"` for specific test classes.
- Blazor components follow feature-based organization under `src/redmuffin.Blazor.StaticWeb/Features/`.
- Azure Functions use isolated worker model with dependency injection.
- Use Zurb Foundation classes for consistent UI styling.
- Component styling uses SCSS partials imported into app.scss and compiled to CSS.
- SCSS partials are located in `src/redmuffin.Blazor.StaticWeb/wwwroot/scss/` directory.
- No \*.razor.css files are used; all styling is handled through SCSS compilation.

## Tasks

- [x] 1.0 Azure Function for Open Graph Parsing
  - [x] 1.1 Create shared data models for batch image processing requests and responses
  - [x] 1.2 Implement GetOpenGraphImages Azure Function with HTML parsing using AngleSharp
  - [x] 1.3 Add parallel processing with Task.WhenAll() and SemaphoreSlim for concurrency control
  - [x] 1.4 Implement comprehensive error handling and rate limiting mechanisms
  - [x] 1.5 Add Open Graph meta tag parsing with priority system (og:image, twitter:image, etc.)
  - [x] 1.6 Implement proper timeout handling and request validation
- [x] 2.0 Blazor Service for Image Retrieval and Caching
  - [x] 2.1 Create OpenGraphImagesService interface and implementation
  - [x] 2.2 Implement ImageValidationService for HTTP HEAD request validation
  - [x] 2.3 Enhance BrowserStorageService with LRU eviction and quota management
  - [x] 2.4 Add batch processing capabilities for multiple article requests
  - [x] 2.5 Implement cache lookup and result filtering to avoid redundant API calls
  - [x] 2.6 Add parallel image validation processing with proper error handling
- [x] 3.0 Integrate with Articles Component
  - [x] 3.1 Update Articles.razor to identify articles requiring image processing
  - [x] 3.2 Implement batch collection and API communication logic
  - [x] 3.3 Add progressive UI updates for image loading states
  - [x] 3.4 Implement fallback placeholder handling for failed image loads
  - [x] 3.5 Add proper state management for individual article processing states
  - [x] 3.6 Integrate image validation and cache updates with UI rendering
- [x] 4.0 Advanced Caching and Optimization
  - [x] 4.1 Implement local storage key strategy with URL hashing
  - [x] 4.2 Add time-based cache expiration (7 days) and cleanup mechanisms
  - [x] 4.3 Implement LRU cache eviction when approaching storage capacity limits
  - [x] 4.4 Add separate caching for Open Graph results and image validation results
  - [x] 4.5 Implement batch cache updates for improved performance
  - [x] 4.6 Add cache statistics and monitoring for performance optimization
- [ ] 5.0 Testing and Quality Assurance
  - [x] 5.1 **HIGH PRIORITY** Write specialized unit test for null value handling in OpenGraphImagesService.GetCacheStatsAsync()
  - [x] 5.2 Write TUnit tests for Azure Function Open Graph parsing logic
  - [x] 5.3 Create TUnit tests for Blazor services (OpenGraphImagesService, ImageValidationService)
- [x] 5.4 Implement integration tests for end-to-end image retrieval and caching
  - [x] 5.5 Add performance tests for batch processing and caching efficiency
  - [ ] 5.6 Write error scenario tests for various failure modes and edge cases
