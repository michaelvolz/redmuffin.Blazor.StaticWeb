# Product Requirements Document: Open Graph Image Fallback for Articles

## Introduction/Overview

The Articles page currently displays article thumbnails using the `Cover` property from Raindrop.io data. When this property is empty or the image fails to load, articles appear without visual content, creating an inconsistent and less engaging user experience. This feature will implement automatic fallback to Open Graph meta tags from the article's website to retrieve alternative images.

**Problem Statement:** Articles without cover images create visual inconsistency and reduce user engagement due to missing thumbnails in the masonry layout.

**Goal:** Implement a robust fallback system that automatically retrieves Open Graph images from article websites when the primary `Cover` image is missing or fails to load, ensuring every article has appropriate visual content.

## Goals

1. **Visual Consistency:** Ensure all articles display appropriate images in the masonry layout
2. **Performance:** Implement efficient caching to avoid repeated API calls for the same articles
3. **User Experience:** Provide seamless image loading with proper fallback mechanisms
4. **Reliability:** Handle edge cases gracefully with appropriate error handling
5. **Storage Efficiency:** Minimize local storage usage while maintaining functionality

## User Stories

### Primary User Stories
- **US-001:** As a user, I want to see relevant images for all articles so that I can quickly identify and engage with content
- **US-002:** As a user, I want articles to load quickly with minimal delay so that I can browse efficiently
- **US-003:** As a user, I want consistent visual presentation so that the interface feels polished and professional

### Secondary User Stories
- **US-004:** As a user, I want images to load progressively so that I can start viewing content immediately
- **US-005:** As a user, I want appropriate fallback images when no suitable image is found so that the layout remains consistent
- **US-006:** As a developer, I want cached image data to reduce API calls so that the system performs efficiently

## Functional Requirements

### Core Functionality
1. **FR-001:** The system must detect when an article's `Cover` property is empty or null
2. **FR-002:** The system must fetch Open Graph meta tags from the article's website URL
3. **FR-003:** The system must prioritize Open Graph images in the following order:
   - `og:image`
   - `twitter:image`
   - `og:image:url`
   - `twitter:image:src`
4. **FR-004:** The system must validate retrieved image URLs for accessibility and format
5. **FR-005:** The system must cache successful image URLs to avoid repeated API calls
6. **FR-006:** The system must display a default placeholder image when no suitable image is found
7. **FR-007:** The system must handle failed image loads gracefully with fallback behavior

### Performance Requirements
8. **FR-008:** The system must implement local storage caching for retrieved image URLs
9. **FR-009:** The system must implement cache eviction when storage approaches capacity limits
10. **FR-010:** The system must process images asynchronously to avoid blocking the UI
11. **FR-011:** The system must implement proper loading states during image processing
12. **FR-012:** The system must batch process multiple articles in a single API call for efficiency
13. **FR-013:** The system must implement parallel processing in Azure Functions using Task.WhenAll()
14. **FR-014:** The system must validate image URLs using HTTP HEAD requests before displaying
15. **FR-015:** The system must avoid multiple access attempts for the same image URL
16. **FR-016:** The system must implement parallel HTTP HEAD requests for image validation in the Blazor client
17. **FR-017:** The system must filter cached results before sending batch requests to avoid redundant API calls
18. **FR-018:** The system must implement URL-based caching for validation results to prevent duplicate validation across articles

### API Requirements
19. **FR-019:** The system must implement an Azure Function to fetch and parse Open Graph data
20. **FR-020:** The system must implement proper error handling for unreachable or invalid URLs
21. **FR-021:** The system must implement rate limiting and timeout mechanisms
22. **FR-022:** The system must validate HTML content and extract meta tags safely
23. **FR-023:** The system must use SemaphoreSlim to control concurrency in batch processing
24. **FR-024:** The system must handle partial batch failures gracefully

### Data Storage Requirements
25. **FR-025:** The system must store only essential data: article ID, image URL, hash, and timestamp
26. **FR-026:** The system must implement LRU (Least Recently Used) cache eviction strategy
27. **FR-027:** The system must handle local storage quota exceeded errors gracefully
28. **FR-028:** The system must provide mechanism to clear cache when needed
29. **FR-029:** The system must cache both successful and failed image validation results
30. **FR-030:** The system must support batch cache updates for efficiency
31. **FR-031:** The system must implement separate caching for image validation results using URL hash keys
32. **FR-032:** The system must cache Open Graph retrieval results separately from image validation results
33. **FR-033:** The system must provide complete cached information to eliminate redundant network requests

## Non-Goals (Out of Scope)

1. **NG-001:** Image editing or manipulation beyond basic validation
2. **NG-002:** Downloading and storing images locally (only URLs are cached)
3. **NG-003:** Advanced image processing or optimization
4. **NG-004:** Custom image selection or user preferences
5. **NG-005:** Integration with external image services beyond Open Graph
6. **NG-006:** Real-time image updates or monitoring
7. **NG-007:** Image content analysis or classification
8. **NG-008:** Support for non-HTML content types
9. **NG-009:** Complex image format conversions
10. **NG-010:** Advanced caching strategies beyond LRU

## Design Considerations

### Visual Design
- **Placeholder Image:** Use a neutral, document-style icon (e.g., FontAwesome `fa-file-text` or `fa-image`)
- **Loading States:** Maintain existing shimmer effects during image processing
- **Error States:** Graceful degradation with consistent placeholder styling
- **Responsive Design:** Ensure fallback images work across all breakpoints

### User Experience
- **Progressive Loading:** Display articles immediately, then update with retrieved images
- **Seamless Transitions:** Smooth image replacement without layout shifts
- **Error Recovery:** Clear visual feedback for failed image loads
- **Performance:** Minimal impact on initial page load times

### Accessibility
- **Alt Text:** Provide meaningful alternative text for all images
- **Screen Readers:** Ensure fallback images are properly announced
- **Keyboard Navigation:** Maintain keyboard accessibility during image loading
- **Color Contrast:** Ensure placeholder images meet accessibility standards

## Technical Considerations

### Azure Functions (.NET 8) Implementation
- **Function Name:** GetOpenGraphImages
- **File Path:** `src/redmuffin.Blazor.StaticWeb.Api/Functions/GetOpenGraphImages.cs`
- **API Endpoint:** `/api/GetOpenGraphImages`
- **Input:** BatchImageRequest with List<ArticleImageRequest>
- **Output:** BatchImageResponse with List<ArticleImageResponse>
- **Parallel Processing:** Use Task.WhenAll() for parallel HTTP requests
- **Concurrency Control:** SemaphoreSlim to limit concurrent requests
- **Error Handling:** Comprehensive error handling with meaningful responses
- **Rate Limiting:** Implement proper throttling to prevent abuse

### Blazor WebAssembly (.NET 9) Implementation
- **Service:** Create OpenGraphImagesService in `src/redmuffin.Blazor.StaticWeb/Services/`
- **Integration:** Update Articles.razor to use the new service
- **Batch Processing:** Implement GetImageUrlsAsync(List<ArticleImageRequest>)
- **Image Validation:** Use HTTP HEAD requests to pre-validate image URLs
- **Caching:** Implement BrowserStorageService for validation results and Open Graph URLs
- **State Management:** Proper state management for image loading states

### Open Graph Meta Tag Parsing
- **HTML Parsing:** Use AngleSharp or HtmlAgilityPack for safe HTML parsing
- **Meta Tag Priority:** Implement priority system for different meta tag types
- **Validation:** Validate image URLs and basic image properties
- **Security:** Sanitize and validate all input data

### Local Storage Strategy
- **Key Format:** `og_image_{articleId}_{urlHash}`
- **Data Structure:** `{ imageUrl: string, timestamp: number, hash: string, isValid: boolean }`
- **Size Limits:** Monitor storage usage and implement cleanup
- **Expiration:** Implement time-based expiration (e.g., 7 days)
- **Validation Cache:** Store image validation results to avoid repeated checks
- **Batch Updates:** Update multiple cache entries efficiently

### Error Handling Strategy
- **Network Errors:** Handle unreachable URLs gracefully
- **Parsing Errors:** Handle malformed HTML or missing meta tags
- **Storage Errors:** Handle quota exceeded and storage failures
- **Image Errors:** Handle broken or inaccessible image URLs

## Success Metrics

### Technical Success Criteria
1. **Cache Hit Rate:** Achieve >80% cache hit rate for repeated article views
2. **Performance:** Open Graph image retrieval completes within 3 seconds
3. **Reliability:** Handle 95% of edge cases gracefully without errors
4. **Storage Efficiency:** Use <1MB of local storage for typical usage
5. **API Performance:** Azure Function responds within 2 seconds for 90% of requests

### User Experience Success Criteria
1. **Visual Consistency:** 95% of articles display appropriate images
2. **Loading Performance:** Articles display within 1 second, images load progressively
3. **Error Recovery:** Graceful fallback behavior for all failure scenarios
4. **Mobile Performance:** Consistent performance across all device types
5. **Accessibility:** Meet WCAG 2.1 AA standards for all image content

### Business Success Criteria
1. **User Engagement:** Maintain or improve article click-through rates
2. **Performance:** No degradation in overall page load times
3. **Reliability:** System handles high traffic without failures
4. **Maintainability:** Code follows established patterns for future enhancements

## Implementation Notes

### Development Approach
1. **Phase 1:** Implement Azure Function for Open Graph parsing
2. **Phase 2:** Create Blazor service for image retrieval and caching
3. **Phase 3:** Integrate with Articles component
4. **Phase 4:** Implement advanced caching and optimization

### Testing Strategy
- **Unit Tests:** TUnit tests for all business logic and parsing functions
- **Integration Tests:** End-to-end testing of image retrieval and caching
- **Performance Tests:** Load testing for API endpoints and caching
- **Error Scenario Tests:** Comprehensive testing of failure modes

### Quality Assurance
- **Code Review:** Follow established code review processes
- **Performance Testing:** Verify caching efficiency and API performance
- **Security Testing:** Validate input sanitization and safe HTML parsing
- **Cross-Browser Testing:** Ensure compatibility across target browsers

## Open Questions

1. **Cache Duration:** How long should cached image URLs remain valid?
2. **Storage Limits:** What is the maximum acceptable local storage usage?
3. **Fallback Strategy:** Should we implement multiple fallback levels?
4. **Performance Monitoring:** How should we monitor and alert on performance issues?
5. **User Control:** Should users have any control over image fallback behavior?

### Security Considerations

1. **Input Validation:** Validate all URLs and prevent malicious input
2. **Content Security Policy:** Ensure compliance with CSP headers
3. **Rate Limiting:** Prevent abuse of the Open Graph API
4. **Data Sanitization:** Sanitize all retrieved HTML content
5. **HTTPS Enforcement:** Ensure all image URLs use HTTPS when possible

### Articles Component Implementation Strategy

#### Phase 1: Image Availability Detection
1. **Initial Assessment:** After fetching articles, identify which need image processing:
   - Articles with null/empty `Cover` property
   - Articles with `Cover` URLs marked as invalid in cache
   - Articles with `Cover` URLs not yet validated

2. **Cache Lookup:** Check local storage for:
   - Previously retrieved Open Graph images
   - Image validation results to avoid re-checking known broken URLs
   - Timestamp-based cache expiration

#### Phase 2: Batch Validation and Processing
1. **Image Validation:** Use HTTP HEAD requests to validate existing `Cover` URLs
2. **Batch Collection:** Collect all articles requiring Open Graph processing
3. **API Call:** Send batch request to GetOpenGraphImages Azure Function
4. **Result Processing:** Handle partial successes and failures gracefully

#### Phase 3: UI Updates
1. **Progressive Enhancement:** Display articles immediately with available images
2. **Async Updates:** Update images as batch results are processed
3. **Fallback Handling:** Use placeholder images for ultimate failures

### Articles Component Changes
- **Image Collection:** Identify missing images without accessing them multiple times
- **Batch Request:** Collect articles needing image processing and batch process after initial render
- **UI Updates:** Update UI progressively as results come in
- **State Management:** Track processing state for each article individually
- **Error Handling:** Graceful degradation for various failure scenarios

## Dependencies

### External Dependencies
- **AngleSharp or HtmlAgilityPack:** For HTML parsing in Azure Functions
- **HttpClient:** For fetching external website content
- **Browser Storage APIs:** For local caching implementation

### Internal Dependencies
- **Existing Articles Component:** Integration point for new functionality
- **Raindrop Data Models:** Understanding of existing data structure
- **Azure Functions Infrastructure:** Hosting and deployment environment

## Detailed Technical Implementation

### Azure Function Structure
```csharp
public class GetOpenGraphImages
{
    [Function("GetOpenGraphImages")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
        FunctionContext executionContext)
    {
        // Implementation details
    }
}
```

### Batch Processing Data Models
```csharp
public class ArticleImageRequest
{
    public long ArticleId { get; set; }
    public string ArticleUrl { get; set; } = string.Empty;
    public string? CurrentCoverUrl { get; set; }
}

public class ArticleImageResponse
{
    public long ArticleId { get; set; }
    public string? ImageUrl { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class BatchImageRequest
{
    public List<ArticleImageRequest> Requests { get; set; } = new();
}

public class BatchImageResponse
{
    public List<ArticleImageResponse> Results { get; set; } = new();
    public int TotalProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
}
```

### Blazor Service Structure
```csharp
public interface IOpenGraphImagesService
{
    Task<BatchImageResponse> GetImageUrlsAsync(List<ArticleImageRequest> requests);
    Task<bool> IsImageValidAsync(string imageUrl);
    Task ClearCacheAsync();
    Task<Dictionary<long, string>> GetCachedImagesAsync(List<long> articleIds);
}

public class OpenGraphImagesService : IOpenGraphImagesService
{
    // Implementation details
}
```

### Image Validation Service
```csharp
public interface IImageValidationService
{
    Task<bool> ValidateImageUrlAsync(string imageUrl);
    Task<Dictionary<string, bool>> ValidateImageUrlsAsync(List<string> imageUrls);
}

public class ImageValidationService : IImageValidationService
{
    // Uses HTTP HEAD requests to validate image accessibility
}
```

### Cache Data Structure
```csharp
public class CachedImageData
{
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string UrlHash { get; set; } = string.Empty;
    public bool IsValid { get; set; } = true;
    public ImageSource Source { get; set; } = ImageSource.Original;
}

public enum ImageSource
{
    Original,
    OpenGraph,
    Placeholder
}

public class ImageValidationResult
{
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public DateTime ValidatedAt { get; set; }
    public string? ErrorMessage { get; set; }
}
```

### Local Storage Key Strategy
- **Format:** `og_image_{articleId}_{urlHash}`
- **Cleanup:** Remove entries older than 7 days
- **Size Management:** Implement LRU eviction when approaching 80% capacity

This comprehensive PRD provides a complete roadmap for implementing Open Graph image fallback functionality while addressing all technical challenges, security concerns, and user experience requirements.
