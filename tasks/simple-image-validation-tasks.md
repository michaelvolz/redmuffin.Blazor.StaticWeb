# Simple Image Validation System - Task List

## Overview
This task list implements the simplified image validation system as defined in the PRD, replacing the complex existing system with a lean, maintainable solution.

## Prerequisites
- [x] **PREP-001**: Purge existing cache data before implementation
- [x] **PREP-002**: Backup current image validation implementation for reference

## Folder Structure

The new image validation system will be organized within the ArticlesPage feature folder, avoiding the general Services folder and keeping everything well-organized within the feature itself:

```
Features/Pages/ArticlesPage/
├── Core/
│   ├── Models/
│   │   └── ImageValidationResult.cs
│   ├── Services/
│   │   ├── ISimpleImageValidationService.cs
│   │   ├── SimpleImageValidationService.cs
│   │   └── PlaceholderGenerationService.cs
│   └── Templates/
│       └── PlaceholderTemplate.cs
├── Models/
│   └── [existing models]
├── Articles.razor
└── Articles.razor.cs

tests/redmuffin.Blazor.StaticWeb.Tests/Features/Pages/ArticlesPage/
├── Core/
│   └── Services/
│       └── SimpleImageValidationServiceTests.cs
└── ArticlesComponentTests.cs
```

## Phase 1: Core Service Implementation

### Task 1: Create Simple Image Validation Service
- [x] **TASK-001**: Create `ISimpleImageValidationService` interface
  - **Location**: `Features/Pages/ArticlesPage/Core/Services/ISimpleImageValidationService.cs`
  - Define `ValidateImageAsync(string imageUrl)` method
  - Define `GetCachedResultAsync(string imageUrl)` method  
  - Define `GetImageUrlOrPlaceholderAsync(string imageUrl)` method
  - Add to DI container registration

- [x] **TASK-002**: Create `ImageValidationResult` model
  - **Location**: `Features/Pages/ArticlesPage/Core/Models/ImageValidationResult.cs`
  - `bool IsValid` property
  - `string FailureReason` property
  - `DateTime ValidatedAt` property

- [x] **TASK-003**: Implement `SimpleImageValidationService` class
  - **Location**: `Features/Pages/ArticlesPage/Core/Services/SimpleImageValidationService.cs`
  - Constructor with HttpClient, IJSRuntime, ILogger dependencies
  - HTTP HEAD request validation logic
  - CORS, status code, and content-type checking
  - URL hashing using SHA256
  - localStorage cache integration

### Task 2: Cache Management
- [x] **TASK-004**: Implement localStorage cache operations
  - **Location**: Part of `SimpleImageValidationService` class
  - Cache key format: `img_validation_{url_hash}`
  - Cache value format: `{isValid, reason, timestamp}`
  - Integration with existing `BrowserStorageService`
  - Automatic cleanup at 75% quota threshold

- [x] **TASK-005**: Add cache retrieval methods
  - **Location**: Part of `SimpleImageValidationService` class
  - `GetCachedResultAsync` implementation
  - Proper error handling for localStorage access
  - Fallback when localStorage unavailable

### Task 3: Placeholder System
- [x] **TASK-006**: Create single SVG placeholder template
  - **Location**: `Features/Pages/ArticlesPage/Core/Templates/PlaceholderTemplate.cs`
  - Base SVG with dynamic text replacement
  - Consistent styling matching existing design
  - Support for failure reason text overlay
  - **Note**: Integrated into SimpleImageValidationService.GenerateSimplePlaceholder method

- [x] **TASK-007**: Implement placeholder generation
  - **Location**: `Features/Pages/ArticlesPage/Core/Services/PlaceholderGenerationService.cs`
  - `GeneratePlaceholder(string reason)` method
  - Base64 encoding of SVG
  - Standard failure reasons: "Image not found", "CORS blocked", "Invalid format", "Network error"
  - **Note**: Integrated into SimpleImageValidationService.GenerateSimplePlaceholder method

## Phase 2: Articles Component Integration

### Task 4: Simplify Articles Component
- [x] **TASK-008**: Remove complex state management
  - Remove existing image validation dictionaries
  - Remove progressive enhancement logic
  - Remove background validation orchestration
  - Keep only simple `Dictionary<string, string> _imageUrlCache`

- [x] **TASK-009**: Implement simplified image loading
  - `PopulateImageCacheAsync()` method
  - `GetImageUrl(Article article)` method
  - Integration with `ISimpleImageValidationService`
  - Maintain shimmer effect for initial load

- [x] **TASK-010**: Update Articles.razor template
  - Remove complex state-based rendering
  - Simplify image src binding
  - Remove progressive enhancement UI elements

## Phase 3: Background Validation

### Task 5: Implement Background Processing
- [x] **TASK-011**: Add fire-and-forget validation
  - `Task.Run()` for uncached images
  - No complex concurrency management
  - Simple parallel validation for multiple images

- [x] **TASK-012**: Add StateHasChanged triggering
  - Update UI when validation completes
  - Minimal state change notifications
  - No complex progress tracking

## Phase 4: Error Handling & Testing

### Task 6: Error Handling
- [x] **TASK-013**: Implement basic error handling
  - Try-catch blocks in validation methods
  - Graceful fallback to placeholder on errors
  - Basic error logging for debugging

- [x] **TASK-014**: Add validation timeout handling
  - Reasonable timeout for HTTP requests
  - Fallback to placeholder on timeout
  - Cache timeout failures to avoid retries

### Task 7: Unit Tests
- [ ] **TASK-015**: Create service unit tests
  - **Location**: `tests/redmuffin.Blazor.StaticWeb.Tests/Features/Pages/ArticlesPage/Core/Services/SimpleImageValidationServiceTests.cs`
  - Mock HttpClient for validation tests
  - Mock IJSRuntime for localStorage tests
  - Test validation logic independently
  - Test cache hit/miss scenarios

- [ ] **TASK-016**: Create component unit tests
  - **Location**: `tests/redmuffin.Blazor.StaticWeb.Tests/Features/Pages/ArticlesPage/ArticlesComponentTests.cs`
  - Test Articles component with mocked service
  - Test image URL resolution
  - Test placeholder fallback behavior

## Phase 5: Cleanup & Integration

### Task 8: Remove Legacy Code
- [ ] **TASK-017**: Remove old image validation services
  - Remove complex validation orchestration
  - Remove progressive enhancement logic
  - Remove elaborate caching strategies
  - Clean up unused dependencies

- [ ] **TASK-018**: Update dependency injection
  - Remove old service registrations
  - Add new `ISimpleImageValidationService` registration
  - Update any other components using old services

### Task 9: Integration Testing
- [ ] **TASK-019**: End-to-end testing
  - Test complete article loading flow
  - Test cache persistence across page loads
  - Test placeholder display for various failure types
  - Test automatic cache cleanup

- [ ] **TASK-020**: Performance testing
  - Verify initial page load under 500ms
  - Test with large numbers of articles
  - Verify memory usage improvements
  - Test localStorage quota management

## Phase 6: Documentation & Deployment

### Task 10: Documentation
- [ ] **TASK-021**: Update code documentation
  - Add XML comments to service interface
  - Document cache key format
  - Document placeholder generation logic

- [ ] **TASK-022**: Create deployment notes
  - Cache reset procedure
  - Performance benchmarks
  - Rollback procedures if needed

### Task 11: Monitoring & Validation
- [ ] **TASK-023**: Add basic monitoring
  - Log validation failures for debugging
  - Track cache hit/miss ratios (optional)
  - Monitor localStorage usage

- [ ] **TASK-024**: Success metrics validation
  - Verify 60%+ code reduction achieved
  - Confirm performance targets met
  - Validate functionality preservation
  - Confirm maintainability improvements

## Implementation Order

### Sprint 1 (Core Implementation)
- PREP-001, PREP-002
- TASK-001, TASK-002, TASK-003
- TASK-004, TASK-005

### Sprint 2 (UI Integration)
- TASK-006, TASK-007
- TASK-008, TASK-009, TASK-010
- TASK-011, TASK-012

### Sprint 3 (Testing & Cleanup)
- TASK-013, TASK-014
- TASK-015, TASK-016
- TASK-017, TASK-018

### Sprint 4 (Validation & Deployment)
- TASK-019, TASK-020
- TASK-021, TASK-022
- TASK-023, TASK-024

## Success Criteria

- [ ] **All tasks completed successfully**
- [ ] **60%+ reduction in image validation code**
- [ ] **Initial page load remains under 500ms**
- [ ] **100% consistent behavior across page visits**
- [ ] **Zero regression in core functionality**
- [ ] **System understandable by new developers in 30 minutes**

## Notes

- Test thoroughly with cache reset before each major test phase
- Leverage existing `BrowserStorageService` for localStorage operations
- Maintain existing Zurb Foundation styling consistency
- Focus on simplicity over optimization
- Document any deviations from the PRD during implementation
