# Product Requirements Document: Dummy RaindropIO Data for Local Design Testing

## Introduction/Overview

The current development workflow for the `redmuffin.Blazor.StaticWeb` project requires running multiple console instances (Blazor WebAssembly app + Azure Functions API) to test design and functionality changes. This creates friction in the development process and slows down iteration cycles. This feature will implement a localhost detection system that automatically switches between real Azure Function API calls and pre-extracted dummy data based on the development environment.

**Problem Statement:** Developers need to run both the Blazor WebAssembly app and Azure Functions API simultaneously to test design changes, creating unnecessary complexity and slower iteration cycles during local development.

**Goal:** Simplify local development by enabling single-command startup with automatic environment detection that uses dummy RaindropIO data on `localhost:5233` while preserving real API functionality on `localhost:4280` (SWA proxy).

## Goals

1. **Development Simplification:** Enable single-command startup for local development without Azure Functions dependency
2. **Environment Detection:** Automatically detect localhost environment and switch data sources accordingly
3. **Functionality Preservation:** Maintain all existing functionality while using dummy data
4. **Clean Architecture:** Implement testable, maintainable service abstraction for API calls
5. **Data Realism:** Provide realistic dummy data that mirrors actual RaindropIO API responses

## User Stories

### Primary User Stories

- **US-001:** As a developer, I want to start only the Blazor WebAssembly app on `localhost:5233` and see realistic video/article data so that I can quickly test UI changes without running Azure Functions
- **US-002:** As a developer, I want the app to automatically use real API calls when running on `localhost:4280` (SWA proxy) so that I can test full integration scenarios
- **US-003:** As a developer, I want the dummy data to be realistic and comprehensive so that I can test edge cases and various content scenarios
- **US-004:** As a developer, I want the service abstraction to be testable so that I can write unit tests for both dummy and real API implementations

### Secondary User Stories

- **US-005:** As a developer, I want the dummy data to be stored in JSON files so that I can easily modify test scenarios
- **US-006:** As a developer, I want the environment detection to be reliable so that I never accidentally use dummy data in production scenarios

## Functional Requirements

1. **Environment Detection System**
   - The system must detect when running on `localhost:5233` and automatically use dummy data
   - The system must detect when running on `localhost:4280` and use real Azure Function API calls
   - The detection must be reliable and not interfere with production deployments

2. **IRaindropAPI Interface**
   - Create a new `IRaindropAPI` interface that abstracts all RaindropIO API operations
   - Interface must support `GetVideosAsync()` and `GetArticlesAsync()` methods
   - Interface must return `List<RaindropItem>` to match existing implementation
   - Interface must support cancellation tokens and proper async patterns
   - Interface must include comprehensive exception documentation for different implementation scenarios

3. **DummyRaindropAPI Implementation**
   - Implement `DummyRaindropAPI` class that inherits from `IRaindropAPI`
   - Load dummy data from JSON files in the `mockdata` folder
   - Return realistic data that matches the structure of actual RaindropIO responses
   - Handle errors gracefully with appropriate logging

4. **RaindropAPI Implementation**
   - Implement `RaindropAPI` class that inherits from `IRaindropAPI`
   - Route all existing Azure Function calls through this service
   - Maintain existing error handling and logging patterns
   - Preserve all current functionality without breaking changes

5. **Service Registration**
   - Register appropriate service implementation based on environment detection
   - Use dependency injection to provide the correct implementation to components
   - Ensure proper service lifetime management (Scoped for Blazor WebAssembly)

6. **Dummy Data Management**
   - Store dummy data in JSON files under `wwwroot/mockdata/videos.json` and `wwwroot/mockdata/articles.json`
   - Use realistic data extracted from actual RaindropIO responses
   - Include edge cases like missing covers, long titles, various content types
   - Ensure data structure matches `RaindropItem` model exactly
   - Access files via HTTP requests using default HttpClient (no named clients)

## Non-Goals (Out of Scope)

1. **Authentication Simulation:** Will not simulate RaindropIO authentication flows
2. **Real-time Data Sync:** Will not sync dummy data with live RaindropIO data
3. **Data Modification:** Will not support modifying dummy data through the UI
4. **Performance Optimization:** Will not optimize for large datasets (dummy data will be limited)
5. **Configuration UI:** Will not provide UI for switching between dummy and real data

## Design Considerations

### Service Architecture

```csharp
/// <summary>
/// Provides abstraction for RaindropIO API operations.
/// Supports both real API calls and dummy data for local development.
/// Implementations should handle their own HttpClient management and error handling strategies.
/// </summary>
public interface IRaindropAPI
{
    /// <summary>
    /// Retrieves a list of video items from the RaindropIO service.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A list of video items, or empty list if none found.</returns>
    /// <exception cref="HttpRequestException">Thrown when API request fails.</exception>
    /// <exception cref="JsonException">Thrown when response cannot be deserialized.</exception>
    Task<List<RaindropItem>> GetVideosAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a list of article items from the RaindropIO service.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A list of article items, or empty list if none found.</returns>
    /// <exception cref="HttpRequestException">Thrown when API request fails.</exception>
    /// <exception cref="JsonException">Thrown when response cannot be deserialized.</exception>
    Task<List<RaindropItem>> GetArticlesAsync(CancellationToken cancellationToken = default);
}
```

### Environment Detection

- Use `NavigationManager.BaseUri` to detect current environment
- `localhost:5233` → Use `DummyRaindropAPI`
- `localhost:4280` → Use `RaindropAPI`
- All other environments → Use `RaindropAPI`

### Component Integration

- **Service Injection:** Update `Videos.razor.cs` and `Articles.razor.cs` to inject `IRaindropAPI` instead of `HttpClient`
- **API Abstraction:** Replace direct API calls with service method calls through `IRaindropAPI` interface
- **Error Handling:** Maintain existing error handling patterns while leveraging service-level error management
- **UI Consistency:** Preserve existing shimmer effects, masonry layout, and loading states
- **Dependency Validation:** Add proper null validation for injected `IRaindropAPI` service

### Styling Integration

- Use existing Zurb Foundation classes and SCSS structure
- No changes required to existing styling or layout
- Maintain masonry layout and shimmer effects

## Technical Considerations

### Blazor WebAssembly .NET 9 Specific

- **Service Registration:** Register services in `Program.cs` with factory pattern for environment-based logic
- **Dependency Injection:** Use constructor injection pattern with comprehensive null validation
- **Async Patterns:** Implement proper async/await with `ConfigureAwait(false)` and `CancellationToken` support
- **JSON Serialization:** Use enhanced `System.Text.Json` with `RaindropJsonSerializerContext` for AOT compatibility
- **HttpClient Optimization:** Only inject `HttpClient` in services that actually make HTTP requests (`RaindropAPI`)
- **Environment Detection:** Use `NavigationManager.BaseUri` for reliable localhost:5233 vs localhost:4280 detection

### File Structure (Feature-Oriented)

```
src/redmuffin.Blazor.StaticWeb/
├── Features/
│   ├── Raindrop/
│   │   └── Services/
│   │       ├── IRaindropAPI.cs
│   │       ├── IRaindropAPIFactory.cs
│   │       ├── RaindropAPI.cs
│   │       ├── RaindropAPIFactory.cs
│   │       └── DummyRaindropAPI.cs
│   └── Pages/
│       ├── VideosPage/
│       │   └── Videos.razor.cs (updated)
│       └── ArticlesPage/
│           └── Articles.razor.cs (updated)
└── wwwroot/
    └── mockdata/
        ├── videos.json
        └── articles.json

tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/
└── Features/
    └── Raindrop/
        └── Services/
            ├── IRaindropAPITests.cs
            ├── RaindropAPIFactoryTests.cs
            ├── DummyRaindropAPITests.cs
            └── RaindropAPITests.cs
```

### Service Architecture Optimization

- **Factory Pattern:** Use `IRaindropAPIFactory` for environment-based service resolution
- **Dependency Injection:** Optimize service registration with proper scoping and validation
- **HttpClient Management:** Use default HttpClient without named clients for both implementations
- **Environment Detection:** Use `NavigationManager.BaseUri` for reliable localhost detection
- **Error Handling:** Implement consistent error patterns across all service implementations

### Azure Functions Integration

- **Existing Functions:** No changes required to `RaindropListVideos.cs` and `RaindropListArticles.cs`
- **API Endpoints:** Maintain existing `/api/RaindropListVideos` and `/api/RaindropListArticles` endpoints
- **Error Handling:** Preserve existing error handling and logging patterns

### JSON Serialization Enhancement

- **Enhanced Context:** Update `RaindropJsonSerializerContext` with improved configuration for robust parsing
- **Better Error Handling:** Add support for edge cases and malformed JSON responses
- **Performance Optimization:** Leverage .NET 9 AOT improvements with enhanced source generation
- **Compatibility:** Maintain backward compatibility with existing deserialization calls

### Testing Considerations

- **TUnit Framework:** Write tests using `[Test]` attribute
- **LightMock.Generator:** Mock `IRaindropAPI` interface for component testing
- **Test Coverage:** Test both dummy and real API implementations
- **Integration Tests:** Verify environment detection logic
- **Test Examples:** Use the tests in the tests\redmuffin.Blazor.StaticWeb.Tests\NewTests folder as examples how to code new tests clean and effectively.
- **Test locations:** Place all the new tests only into the tests\redmuffin.Blazor.StaticWeb.Tests\NewTests folder but with the correct subfolder structure. We need to separate them from some old and outdated tests.

### Code Quality Standards

- **StyleCop Compliance:** Follow SA1402, SA1208, SA1201-1214 rules
- **Meziantou Compliance:** Use `ConfigureAwait(false)`, proper abstractions
- **Microsoft Analyzers:** Implement LoggerMessage delegates, proper async patterns
- **Null Safety:** Use `ArgumentNullException.ThrowIfNull()` for all parameters
- **Enhanced JSON Serialization:** Use enhanced `RaindropJsonSerializerContext` for all JSON operations
- **Robust JSON Parsing:** Implement proper error handling for malformed responses

## Success Metrics

1. **Development Speed:** Reduce local development startup time from ~30 seconds (two processes) to ~10 seconds (single process)
2. **Developer Experience:** 100% of developers can run design tests without Azure Functions setup
3. **Functionality Preservation:** 0% regression in existing functionality when using real API
4. **Test Coverage:** Achieve >90% test coverage for new service implementations
5. **Build Quality:** Maintain zero build warnings policy

## Implementation Notes

### Enhanced JSON Serialization Context

```csharp
// Enhanced RaindropJsonSerializerContext.cs
[JsonSerializable(typeof(List<RaindropItem>), TypeInfoPropertyName = "RaindropItemList")]
[JsonSerializable(typeof(RaindropItem))]
[JsonSerializable(typeof(UserReference))]
[JsonSerializable(typeof(MediaItem))]
[JsonSerializable(typeof(Reminder))]
[JsonSerializable(typeof(CollectionReference))]
[JsonSerializable(typeof(Highlight))]
[JsonSerializable(typeof(CreatorReference))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(List<MediaItem>))]
[JsonSerializable(typeof(List<Highlight>))]
[JsonSerializable(typeof(List<CreatorReference>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(JsonDocument))]
[JsonSerializable(typeof(JsonElement))]
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    GenerationMode = JsonSourceGenerationMode.Default,
    WriteIndented = false
)]
public partial class RaindropJsonSerializerContext : JsonSerializerContext
{
    public static JsonSerializerOptions DefaultOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };
}
```

### Optimized Service Registration Pattern

```csharp
// In Program.cs
// Register concrete implementations with appropriate dependencies
// DummyRaindropAPI - Uses HttpClient to load JSON files via HTTP requests
builder.Services.AddScoped<DummyRaindropAPI>();

// RaindropAPI - Requires HttpClient for real API calls
builder.Services.AddScoped<RaindropAPI>();

// Register factory for environment-based service resolution
builder.Services.AddScoped<IRaindropAPIFactory, RaindropAPIFactory>();

// Register IRaindropAPI using factory pattern with error handling
builder.Services.AddScoped<IRaindropAPI>(serviceProvider =>
{
    try
    {
        var factory = serviceProvider.GetRequiredService<IRaindropAPIFactory>();
        return factory.CreateRaindropAPI();
    }
    catch (Exception ex)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to create IRaindropAPI instance");
        throw;
    }
});
```

### Enhanced Factory Implementation Pattern

```csharp
// IRaindropAPIFactory.cs
/// <summary>
/// Factory interface for creating IRaindropAPI instances based on the current environment.
/// Provides environment-aware service resolution for optimal development and production workflows.
/// </summary>
public interface IRaindropAPIFactory
{
    /// <summary>
    /// Creates an appropriate IRaindropAPI implementation based on the current environment.
    /// </summary>
    /// <returns>An IRaindropAPI implementation suitable for the current environment.</returns>
    IRaindropAPI CreateRaindropAPI();
}

// RaindropAPIFactory.cs
/// <summary>
/// Factory implementation that creates IRaindropAPI instances based on environment detection.
/// Uses NavigationManager.BaseUri to determine whether to use dummy data or real API calls.
/// Supports localhost:5233 (dummy data) and localhost:4280 (real API) environments.
/// </summary>
public sealed class RaindropAPIFactory : IRaindropAPIFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly NavigationManager _navigationManager;
    private readonly ILogger<RaindropAPIFactory> _logger;

    public RaindropAPIFactory(
        IServiceProvider serviceProvider,
        NavigationManager navigationManager,
        ILogger<RaindropAPIFactory> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _navigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IRaindropAPI CreateRaindropAPI()
    {
        var baseUri = _navigationManager.BaseUri;
        var isDevelopmentLocalhost = baseUri.Contains("localhost:5233", StringComparison.OrdinalIgnoreCase);

        if (isDevelopmentLocalhost)
        {
            _logger.LogInformation("Environment detected: localhost:5233 - Using DummyRaindropAPI for local development");
            return _serviceProvider.GetRequiredService<DummyRaindropAPI>();
        }

        _logger.LogInformation("Environment detected: {BaseUri} - Using RaindropAPI for real API calls", baseUri);
        return _serviceProvider.GetRequiredService<RaindropAPI>();
    }
 }
 ```

### Component Update Pattern

```csharp
// In Videos.razor.cs
public partial class Videos
{
    // LoggerMessage delegates for better performance
    private static readonly Action<ILogger, Exception?> LogStartingFetchVideos =
        LoggerMessage.Define(LogLevel.Information, new EventId(1, nameof(LogStartingFetchVideos)),
            "Starting to fetch videos from IRaindropAPI service");

    private static readonly Action<ILogger, int, Exception?> LogVideosRetrieved =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(2, nameof(LogVideosRetrieved)),
            "Successfully retrieved {VideoCount} videos from service");

    private static readonly Action<ILogger, Exception> LogExceptionFetchingVideos =
        LoggerMessage.Define(LogLevel.Error, new EventId(3, nameof(LogExceptionFetchingVideos)),
            "Exception occurred while fetching videos from service");

    [Inject]
    private IRaindropAPI RaindropAPI { get; set; } = default!;

    [Inject]
    private ILogger<Videos> Logger { get; set; } = default!;

    protected override Task OnInitializedAsync()
    {
        // Validate injected dependencies
        ArgumentNullException.ThrowIfNull(RaindropAPI);
        ArgumentNullException.ThrowIfNull(Logger);

        // Load videos automatically when the page starts
        return FetchVideosAsync();
    }

    private async Task FetchVideosAsync()
    {
        _errorMessage = null;
        _videoItems = null;

        try
        {
            LogStartingFetchVideos(Logger, null);
            _videoItems = await RaindropAPI.GetVideosAsync().ConfigureAwait(false);

            if (_videoItems != null)
            {
                LogVideosRetrieved(Logger, _videoItems.Count, null);
            }
        }
        catch (Exception ex)
        {
            LogExceptionFetchingVideos(Logger, ex);
            _errorMessage = $"Exception fetching videos: {ex.Message}";
        }

        StateHasChanged();
    }
}
```

### Enhanced Service Implementations

#### DummyRaindropAPI (HTTP-based, Uses HttpClient)

```csharp
public sealed class DummyRaindropAPI : IRaindropAPI
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DummyRaindropAPI> _logger;

    public DummyRaindropAPI(IHttpClientFactory httpClientFactory, ILogger<DummyRaindropAPI> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<RaindropItem>> GetVideosAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync("mockdata/videos.json", cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            
            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var result = JsonSerializer.Deserialize(jsonContent, RaindropJsonSerializerContext.Default.RaindropItemList) 
                        ?? new List<RaindropItem>();
            _logger.LogInformation("Successfully loaded {Count} videos from dummy data", result.Count);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed while fetching videos from mockdata/videos.json");
            return new List<RaindropItem>();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize videos JSON from mockdata/videos.json");
            return new List<RaindropItem>();
        }
    }

    public async Task<List<RaindropItem>> GetArticlesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync("mockdata/articles.json", cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            
            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var result = JsonSerializer.Deserialize(jsonContent, RaindropJsonSerializerContext.Default.RaindropItemList) 
                        ?? new List<RaindropItem>();
            _logger.LogInformation("Successfully loaded {Count} articles from dummy data", result.Count);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed while fetching articles from mockdata/articles.json");
            return new List<RaindropItem>();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize articles JSON from mockdata/articles.json");
            return new List<RaindropItem>();
        }
    }
}
```

#### RaindropAPI (HTTP-based, Requires HttpClient)

```csharp
public sealed class RaindropAPI : IRaindropAPI
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RaindropAPI> _logger;

    public RaindropAPI(HttpClient httpClient, ILogger<RaindropAPI> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<RaindropItem>> GetVideosAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/RaindropListVideos", cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var result = JsonSerializer.Deserialize(json, RaindropJsonSerializerContext.Default.RaindropItemList) 
                        ?? new List<RaindropItem>();
            _logger.LogInformation("Successfully retrieved {Count} videos from API", result.Count);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed while fetching videos");
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize videos JSON response");
            throw;
        }
    }

    public async Task<List<RaindropItem>> GetArticlesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/RaindropListArticles", cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var result = JsonSerializer.Deserialize(json, RaindropJsonSerializerContext.Default.RaindropItemList) 
                        ?? new List<RaindropItem>();
            _logger.LogInformation("Successfully retrieved {Count} articles from API", result.Count);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed while fetching articles");
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize articles JSON response");
            throw;
        }
    }
}
```

### Dummy Data Structure

```json
// mockdata/videos.json
{
  "items": [
    {
      "_id": "12345678",
      "link": "https://www.youtube.com/watch?v=example1",
      "title": "Sample Video Title 1",
      "excerpt": "This is a sample video description for testing purposes.",
      "note": "",
      "type": "video",
      "user": {
        "$ref": "users",
        "$id": 123456
      },
      "cover": "https://img.youtube.com/vi/example1/maxresdefault.jpg",
      "media": [
        {
          "type": "video",
          "link": "https://www.youtube.com/embed/example1"
        }
      ],
      "tags": ["development", "tutorial", "blazor"],
      "important": false,
      "reminder": null,
      "removed": false,
      "created": "2024-01-15T10:30:00.000Z",
      "collection": {
        "$ref": "collections",
        "$id": 12345678
      },
      "highlights": [],
      "domain": "youtube.com",
      "creatorRef": 123456,
      "sort": 12345678
    }
    // ... 10-15 more video items with variations
  ]
}
```

**Data Quality Guidelines:**

- Use realistic URLs and domains (youtube.com, vimeo.com, etc.)
- Include variety in title lengths (short, medium, long)
- Mix of items with and without covers/excerpts
- Include edge cases: special characters, empty fields, long descriptions
- Maintain exact JSON property names and structure from actual API
- Use realistic timestamps and IDs
- Include diverse tag combinations for testing filtering scenarios

### Code Quality Standards

- **StyleCop Compliance:** Follow SA1402, SA1208, SA1201-1214 rules
- **Meziantou Compliance:** Use `ConfigureAwait(false)`, proper abstractions
- **Microsoft Analyzers:** Implement LoggerMessage delegates, proper async patterns
- **Null Safety:** Use `ArgumentNullException.ThrowIfNull()` for all parameters

## Open Questions

1. **Data Refresh Strategy:** Should dummy data be refreshed periodically from live API, or maintained manually?
2. **Error Simulation:** Should dummy API simulate various error conditions for testing?
3. **Performance Testing:** Should dummy data include performance testing scenarios with large datasets?
4. **Configuration Override:** Should there be a way to force real API usage even on localhost:5233 for debugging?

---

**Target Audience:** Junior developers familiar with Blazor WebAssembly and .NET 9
**Technology Stack:** Blazor WebAssembly .NET 9, Azure Functions .NET 8, TUnit testing, LightMock.Generator
**Architecture:** Feature-based organization with dependency injection and service abstraction patterns
