---
mode: 'agent'
description: 'Product Requirements Document for Video Image Placeholders with Clean Architecture'
---

# Product Requirements Document: Video Image Placeholders with Clean Architecture

## Introduction/Overview

The Videos page currently lacks image placeholder functionality when video cover images fail to load or are missing, creating visual inconsistency compared to the Articles page. This feature will implement identical image placeholder behavior for videos while extracting shared image placeholder logic into reusable services to eliminate code duplication and improve maintainability.

**Problem Statement:** Videos without cover images create visual inconsistency and the current Articles page image placeholder logic is duplicated code that should be extracted into shared services.

**Goal:** Implement identical image placeholder functionality for Videos page while refactoring shared image placeholder logic into clean, reusable services following proper feature-based architecture.

## Goals

1. **Feature Parity:** Videos page has identical image placeholder behavior as Articles page
2. **Clean Architecture:** Extract shared image placeholder logic into reusable services
3. **Zero Code Duplication:** Eliminate duplicate image placeholder code between Articles and Videos
4. **Maintainability:** Create well-structured, testable services with proper naming conventions
5. **Performance:** Maintain existing performance characteristics of Articles page
6. **Consistency:** Ensure identical visual behavior and error handling across both pages

## User Stories

### Primary User Stories

- **US-001:** As a user, I want to see appropriate placeholders for videos with missing cover images so that the Videos page has consistent visual presentation
- **US-002:** As a user, I want to see failure reason text when video images fail to load so that I understand why the placeholder is shown
- **US-003:** As a user, I want the Videos page to load as quickly as the Articles page with proper shimmer effects during image loading

### Developer Stories

- **DS-001:** As a developer, I want shared image placeholder logic in reusable services so that I don't duplicate code between Articles and Videos
- **DS-002:** As a developer, I want clean service interfaces so that image placeholder functionality is easily testable and maintainable
- **DS-003:** As a developer, I want proper feature-based organization so that image placeholder services are logically grouped

## Functional Requirements

### Core Image Placeholder Requirements

1. **FR-001:** Videos page MUST display SVG placeholders when cover images are missing or empty
2. **FR-002:** Videos page MUST display SVG placeholders when cover images fail to load (404, CORS, timeout, etc.)
3. **FR-003:** Videos page MUST show appropriate failure reason text in placeholders ("CORS blocked", "Image not found", "Network error", "Invalid format", "Image not available")
4. **FR-004:** Videos page MUST maintain shimmer loading effects identical to Articles page
5. **FR-005:** Videos page MUST handle image load success/failure events with @onload and @onerror handlers
6. **FR-006:** Videos page MUST display fallback placeholder overlay with icon and reason text when using placeholders

### Architecture Requirements

7. **FR-007:** Shared image placeholder logic MUST be extracted into reusable services
8. **FR-008:** Services MUST be placed in appropriate feature-based folder structure
9. **FR-009:** Service interfaces MUST follow clean code principles with descriptive naming
10. **FR-010:** Both Articles and Videos pages MUST use the same shared services
11. **FR-011:** Existing Articles page functionality MUST remain unchanged (no regressions)
12. **FR-012:** All shared services MUST be fully unit tested with TUnit framework

### Performance Requirements

13. **FR-013:** Videos page MUST load initial placeholders within 500ms (same as Articles)
14. **FR-014:** Background image validation MUST work identically to Articles page
15. **FR-015:** Image URL caching MUST work identically to Articles page

## Non-Goals (Out of Scope)

1. **NG-001:** Changing existing Articles page visual design or behavior
2. **NG-002:** Adding new image validation features not present in Articles page
3. **NG-003:** Modifying video data structure or API endpoints
4. **NG-004:** Creating video-specific placeholder designs (must match Articles)
5. **NG-005:** Performance improvements beyond maintaining current Articles page performance
6. **NG-006:** Adding new logging or monitoring features

## Design Considerations

### Visual Design

- Videos page placeholders MUST be visually identical to Articles page placeholders
- Same SVG placeholder template with dynamic failure reason text
- Same shimmer loading animation and timing
- Same fallback placeholder overlay with FontAwesome icon and reason text
- Same card layout and styling integration

### Service Architecture

Shared services will be organized in a new `Core/ImagePlaceholder` folder structure:

```
src/redmuffin.Blazor.StaticWeb/Core/ImagePlaceholder/
├── Abstractions/
│   ├── IImagePlaceholderService.cs
│   └── IImageValidationCacheService.cs
├── Models/
│   ├── ImageValidationResult.cs
│   └── PlaceholderConfiguration.cs
├── Services/
│   ├── ImagePlaceholderService.cs
│   ├── ImageValidationCacheService.cs
│   └── PlaceholderGenerationService.cs
└── Templates/
    └── SvgPlaceholderTemplate.cs
```

## Technical Considerations

### Blazor WebAssembly .NET 9 Integration

- Services MUST be registered in DI container with appropriate lifetimes
- Services MUST use constructor injection with proper null validation
- Services MUST follow async/await patterns with `ConfigureAwait(false)`
- Services MUST use `LoggerMessage` delegates for performance
- Services MUST handle `CancellationToken` parameters appropriately

### Existing Integration Points

- Videos page MUST integrate with existing `ISimpleImageValidationService`
- Videos page MUST use existing shimmer CSS classes and JavaScript functions
- Videos page MUST maintain existing `RaindropItem` model usage
- Videos page MUST use existing Foundation CSS framework styling

### Code Quality Standards

- All services MUST pass StyleCop, Meziantou, and Microsoft analyzers
- All services MUST have zero build warnings (except IL2111)
- All services MUST use proper member ordering and naming conventions
- All services MUST include comprehensive XML documentation

## Success Metrics

1. **Visual Consistency:** Videos page placeholders are visually identical to Articles page
2. **Performance Parity:** Videos page loads within 500ms with proper shimmer effects
3. **Code Quality:** Zero code duplication between Articles and Videos image placeholder logic
4. **Test Coverage:** 100% unit test coverage for all new shared services
5. **Build Quality:** Zero build warnings in all modified and new files
6. **Functionality:** All Articles page image placeholder features work identically on Videos page

## Implementation Notes

### Service Extraction Strategy

1. **Extract Common Logic:** Move shared image placeholder logic from Articles.razor.cs to new services
2. **Create Abstractions:** Define clean interfaces for image placeholder and validation caching
3. **Implement Services:** Create concrete implementations following clean code principles
4. **Update Articles Page:** Refactor Articles page to use new shared services
5. **Implement Videos Page:** Add image placeholder functionality using shared services
6. **Add Tests:** Create comprehensive unit tests for all services

### Key Methods to Extract

- `GetDefaultPlaceholder()` → `IImagePlaceholderService.GetDefaultPlaceholder()`
- `GenerateSimplePlaceholder(string reason)` → `IImagePlaceholderService.GenerateSimplePlaceholder(string reason)`
- `GetImageUrl(RaindropItem item)` → `IImagePlaceholderService.GetImageUrl(RaindropItem item, Dictionary<string, string> cache)`
- `HandleImageLoadAsync()` → `IImagePlaceholderService.HandleImageLoadAsync()`
- `HasFallbackPlaceholder()` → `IImagePlaceholderService.HasFallbackPlaceholder()`
- `GetFallbackReason()` → `IImagePlaceholderService.GetFallbackReason()`

### Videos Page Integration

Videos.razor template changes:

```razor
<div class="shimmer-placeholder" id="shimmer-@video.Id">
    <img src="@GetImageUrl(video)" alt="Video Cover"
         @onload="@(() => HandleImageLoadAsync($\"shimmer-{video.Id}\", video.Link, true))"
         @onerror="@(() => HandleImageLoadAsync($\"shimmer-{video.Id}\", video.Link, false))"/>
    @if (HasFallbackPlaceholder(video))
    {
        <div class="fallback-placeholder-overlay">
            <i class="fas fa-image"></i>
            <span>@GetFallbackReason(video)</span>
        </div>
    }
</div>
```

Videos.razor.cs changes:

- Add `Dictionary<string, string> _imageUrlCache` field
- Inject `IImagePlaceholderService` and `ISimpleImageValidationService`
- Add image cache population in `FetchVideosAsync()`
- Add wrapper methods that delegate to injected services

### Testing Strategy

- **Unit Tests:** Test all service methods with various input scenarios
- **Integration Tests:** Test Articles and Videos pages with shared services
- **Regression Tests:** Ensure Articles page behavior remains unchanged
- **Performance Tests:** Verify Videos page meets 500ms load time requirement

## Open Questions

1. **Service Lifetime:** Should image placeholder services be Singleton, Scoped, or Transient?
2. **Cache Sharing:** Should Articles and Videos pages share the same image validation cache instance?
3. **Error Handling:** Should service exceptions be logged at service level or component level?
4. **Configuration:** Should placeholder templates be configurable or hardcoded?

## Dependencies

- Existing `ISimpleImageValidationService` interface and implementation
- Existing shimmer CSS classes and JavaScript functions
- Existing Foundation CSS framework styling
- TUnit testing framework for unit tests
- LightMock.Generator for mocking in tests

## Acceptance Criteria

### Must Have

- [ ] Videos page displays placeholders for missing/failed images identical to Articles page
- [ ] Shared image placeholder services are extracted and properly tested
- [ ] Articles page functionality remains unchanged (no regressions)
- [ ] Zero code duplication between Articles and Videos image placeholder logic
- [ ] All new code passes build with zero warnings
- [ ] 100% unit test coverage for new services

### Should Have

- [ ] Videos page loads within 500ms with proper shimmer effects
- [ ] Background image validation works identically to Articles page
- [ ] Proper error handling and logging throughout

### Could Have

- [ ] Performance improvements through service optimizations
- [ ] Enhanced error messages for debugging

---

**Target Audience:** Junior developers familiar with Blazor WebAssembly and .NET 9
**Estimated Effort:** 2-3 days for implementation and testing
**Priority:** Medium (improves consistency and code quality)
**Risk Level:** Low (well-defined requirements with existing reference implementation)
