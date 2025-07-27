# Simple Image Validation System PRD

## Introduction/Overview

The current Articles component image checking system has become overly complex with multiple layers of validation, state management, progressive enhancement, and fallback mechanisms. This PRD outlines a simplified approach that maintains core functionality while removing unnecessary complexity.

**Problem**: The current system has:
- Complex state management with multiple dictionaries and tracking mechanisms
- Elaborate progressive enhancement logic  
- Extensive background validation processes
- Intricate fallback placeholder management
- Multiple validation phases and caching layers
- Overengineered error handling and recovery

**Goal**: Create a simple, robust image validation system that:
1. Checks image validity once per image URL
2. Caches results in localStorage indefinitely  
3. Shows images or placeholders based on cache
4. Removes all superfluous complexity

## Goals

1. **Simplicity**: Reduce the image checking system to its essential components
2. **Performance**: Maintain fast initial page loads without complex background processing
3. **Reliability**: Ensure consistent behavior across page visits using localStorage cache
4. **Maintainability**: Create code that is easy to understand and modify
5. **Functionality**: Preserve core image display and fallback capabilities

## User Stories

1. **As a user**, I want articles to load quickly without waiting for complex image processing
2. **As a user**, I want to see either a working image or a clear placeholder with failure reason
3. **As a user**, I want consistent image display behavior when I revisit the page
4. **As a developer**, I want simple, maintainable code for image validation
5. **As a developer**, I want clear separation between image validation and display logic

## Functional Requirements

1. **Simple Image Validation**
   - Check each image URL exactly once using HTTP HEAD request
   - Validate response status, content-type, and CORS accessibility
   - Store validation result (success/failure + reason) in localStorage
   - Never re-validate images that have cached results

2. **LocalStorage Cache Management**
   - Use simple key-value pairs: `img_validation_{url_hash}` → `{isValid: boolean, reason: string, timestamp: number}`
   - Cache results indefinitely (no automatic expiration)
   - Leverage existing cache purge functionality for manual cleanup
   - Store minimal data: validation status and failure reason only
   - **Automatic Cache Cleanup**: When cache reaches 75% of quota, remove oldest entries until it's 50% full

3. **Image Display Logic**
   - On article load: Check cache first, display image if valid or placeholder if invalid
   - If no cache exists: Show shimmereing effect immediately, validate in background, update display
   - Use single placeholder image with dynamic reason text overlay
   - No progressive enhancement or complex state transitions

4. **Fallback Placeholder System**
   - Single SVG placeholder with configurable reason text
   - Reasons: "Image not found", "CORS blocked", "Invalid format", "Network error"
   - No complex state management or processing indicators
   - Clear, informative messaging for users

5. **Background Validation**
   - Simple parallel validation for uncached images only
   - No complex concurrency management or progress tracking
   - Fire-and-forget: validate, cache, update display
   - No elaborate error recovery or retry logic

## Non-Goals (Out of Scope)

1. **Complex Progressive Enhancement**: No elaborate loading states or transitions
2. **OpenGraph Image Enhancement**: Remove complex OpenGraph processing (separate concern)
3. **Multi-Phase Validation**: No validation state tracking or progressive updates
4. **Advanced Error Recovery**: No retry mechanisms or sophisticated error handling
5. **Performance Optimization**: No complex caching strategies or optimization
6. **Image Processing**: No image manipulation, resizing, or format conversion
7. **Analytics**: No detailed tracking of image load success/failure rates
8. **User Controls**: No user preferences for image loading behavior

## Design Considerations

### UI/UX Requirements
- Use existing Zurb Foundation styling for consistency
- Single placeholder design with clear failure messaging
- Maintain current shimmer loading effect for initial page load
- No additional loading indicators or progress bars

### Technical Architecture
- **Single Service**: `SimpleImageValidationService` replacing current complex system
- **Minimal State**: Remove complex state dictionaries and tracking
- **Cache-First**: Always check localStorage before any network requests
- **Simple Fallback**: Single placeholder generation method

## Technical Considerations

### Blazor WebAssembly .NET 9 Implementation
- **Component Structure**: Simplify Articles.razor.cs, remove complex state management
- **Dependency Injection**: Single `ISimpleImageValidationService` interface
- **localStorage Integration**: Direct IJSRuntime calls for cache operations
- **Background Tasks**: Simple Task.Run() for validation, no complex orchestration

### Performance Characteristics
- **Initial Load**: Fast rendering with cached results or immediate placeholders
- **Memory Usage**: Minimal in-memory state, rely on localStorage
- **Network Requests**: One HTTP HEAD request per unique image URL ever
- **Browser Storage**: Efficient key-value storage with minimal data

### Error Handling
- **Simple Try-Catch**: Basic error handling without complex recovery
- **Graceful Degradation**: Show placeholder on any error condition
- **Logging**: Basic error logging for debugging purposes
- **No Retry Logic**: Single validation attempt per image URL

## Implementation Notes

### Core Components
1. **SimpleImageValidationService**: Single service for all image validation
2. **ImageValidationCache**: Simple localStorage wrapper
3. **PlaceholderGenerator**: Single method for generating failure placeholders
4. **Articles Component**: Simplified component with minimal state

### Key Methods
```csharp
// Simple validation service interface
public interface ISimpleImageValidationService
{
    Task<ImageValidationResult> ValidateImageAsync(string imageUrl);
    Task<ImageValidationResult?> GetCachedResultAsync(string imageUrl);
    Task<string> GetImageUrlOrPlaceholderAsync(string imageUrl);
}

// Minimal validation result
public class ImageValidationResult
{
    public bool IsValid { get; set; }
    public string FailureReason { get; set; } = string.Empty;
    public DateTime ValidatedAt { get; set; }
}
```

### Cache Structure
```javascript
// localStorage key format
"img_validation_[SHA256_hash_of_url]": {
    "isValid": true/false,
    "reason": "failure reason or empty",
    "timestamp": 1640995200000
}
```

### Placeholder Generation
- Single SVG template with dynamic text replacement
- Consistent styling and dimensions
- Clear, user-friendly messaging
- No complex state-based placeholder variations

## Success Metrics

1. **Code Reduction**: Remove at least 60% of current image validation code
2. **Performance**: Initial page load under 500ms (maintained)
3. **Reliability**: 100% consistent behavior across page visits
4. **Maintainability**: New developers can understand system in under 30 minutes
5. **Functionality**: Zero regression in core image display capabilities

## Implementation Decisions

1. **Migration Strategy**: No migration needed - existing cache data will be purged before first test
2. **Error Boundaries**: Basic try-catch in validation service with graceful fallback to placeholder
3. **Testing Strategy**: Mock IJSRuntime for localStorage operations, test validation logic independently
4. **Monitoring**: Basic error logging for failed validations, no complex metrics needed

## Dependencies

### External Dependencies
- **IJSRuntime**: For localStorage operations
- **HttpClient**: For HTTP HEAD requests
- **System.Security.Cryptography**: For URL hashing

### Internal Dependencies
- **Articles Component**: Primary integration point
- **Cache Management**: Integration with existing cache purge functionality
- **Placeholder System**: Integration with existing shimmer effects

## Technical Implementation

### Service Implementation
```csharp
public class SimpleImageValidationService : ISimpleImageValidationService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<SimpleImageValidationService> _logger;

    public async Task<string> GetImageUrlOrPlaceholderAsync(string imageUrl)
    {
        // Check cache first
        var cached = await GetCachedResultAsync(imageUrl);
        if (cached != null)
        {
            return cached.IsValid ? imageUrl : GeneratePlaceholder(cached.FailureReason);
        }

        // No cache - validate in background and return placeholder
        _ = Task.Run(async () => await ValidateImageAsync(imageUrl));
        return GeneratePlaceholder("Checking image...");
    }

    private async Task<ImageValidationResult> ValidateImageAsync(string imageUrl)
    {
        // Simple HTTP HEAD request
        // Cache result in localStorage
        // Return validation result
    }

    private string GeneratePlaceholder(string reason)
    {
        // Return base64 encoded SVG with reason text
    }
}
```

### Articles Component Changes
```csharp
public partial class Articles
{
    private readonly Dictionary<string, string> _imageUrlCache = new();
    
    [Inject] private ISimpleImageValidationService ImageValidationService { get; set; }

    private async Task PopulateImageCacheAsync()
    {
        foreach (var article in _articles)
        {
            var imageUrl = await ImageValidationService.GetImageUrlOrPlaceholderAsync(article.Cover);
            _imageUrlCache[article.Link] = imageUrl;
        }
        StateHasChanged();
    }

    private string GetImageUrl(Article article)
    {
        return _imageUrlCache.GetValueOrDefault(article.Link, GenerateDefaultPlaceholder());
    }
}
```

This simplified approach removes the complex state management, progressive enhancement, and elaborate caching strategies while maintaining core functionality and improving maintainability.

<citations>
<document>
<document_type>RULE</document_type>
<document_id>0gOJrXbpFRKiy71lLpeMAJ</document_id>
</document>
</citations>
