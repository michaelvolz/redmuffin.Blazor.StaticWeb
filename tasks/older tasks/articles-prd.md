# Product Requirements Document: Articles Page

## Introduction/Overview

The Articles page is a feature-complete implementation mirroring the existing Videos page functionality, designed to display and manage articles from Raindrop.io. This feature will provide users with a curated collection of articles on programming, AI, .NET, C#, Blazor, and related technologies, presented in an intuitive masonry layout optimized for article consumption.

**Problem Statement:** Users need a dedicated interface to access curated articles similar to the existing Videos page, but specifically tailored for reading materials and articles.

**Goal:** Create a comprehensive Articles page that reuses the existing Raindrop.io infrastructure while providing a distinct user experience optimized for article browsing and consumption.

**Page Title:** Articles
**API Endpoint:** `/api/RaindropListArticles`
**Loading Behavior:** Manual fetch via "Fetch Articles" button (similar to Videos page)

## Goals

1. **Feature Parity:** Mirror all Videos page functionality except Raindrop.io login integration
2. **Data Consistency:** Utilize existing RaindropItem data structures to maintain consistency
3. **User Experience:** Provide an intuitive, responsive article browsing experience
4. **Performance:** Implement efficient data fetching and rendering for article content
5. **Maintainability:** Create reusable components and patterns for future content types

## User Stories

### Primary User Stories

- **US-001:** As a visitor, I want to browse articles without needing to authenticate with Raindrop.io so that I can quickly access content
- **US-002:** As a user, I want to view articles in a masonry layout so that I can efficiently scan through available content
- **US-003:** As a user, I want to click on an article to open it in a new tab so that I can read the full content without losing my place
- **US-004:** As a user, I want to see article metadata (title, excerpt, creation date) so that I can decide which articles to read
- **US-005:** As a user, I want the articles page to be responsive so that I can browse on mobile, tablet, and desktop devices

### Secondary User Stories

- **US-006:** As a developer, I want to see clear loading states so that I understand when articles are being fetched
- **US-007:** As a user, I want to see meaningful error messages if articles fail to load so that I understand what went wrong
- **US-008:** As a user, I want articles to be visually distinct from videos so that I can immediately identify the content type

## Functional Requirements

### Core Functionality

1. **FR-001:** The system must display a page accessible via `/articles` route
2. **FR-002:** The system must fetch articles from Raindrop.io using category ID `56658122`
3. **FR-003:** The system must display articles in a responsive masonry layout
4. **FR-004:** The system must NOT include Raindrop.io login functionality or button
5. **FR-005:** The system must provide a "Fetch Articles" button to manually load content (no automatic loading)
6. **FR-006:** The system must display article title, excerpt, creation date, and thumbnail
7. **FR-007:** The system must provide direct links to original articles in new tabs
8. **FR-008:** The system must implement proper error handling for failed API calls
9. **FR-009:** The system must provide loading states during data fetching
10. **FR-010:** The system must add "Articles" navigation menu item after "Videos" with appropriate FontAwesome icon

### Data Requirements

11. **FR-011:** The system must use existing RaindropItem data structure if compatible
12. **FR-012:** The system must verify data structure compatibility before implementation
13. **FR-013:** The system must handle missing or incomplete article data gracefully
14. **FR-014:** The system must implement proper JSON deserialization with trimming support

### API Requirements

15. **FR-015:** The system must implement RaindropListArticles Azure Function
16. **FR-016:** The system must follow TDD principles for Azure Function development
17. **FR-017:** The system must implement proper logging and error handling in API
18. **FR-018:** The system must use consistent patterns with existing RaindropListVideos function

### UI/UX Requirements

19. **FR-019:** The system must use subtle masonry layout optimizations to make cards look more article-focused (no color changes)
20. **FR-020:** The system must implement shimmer loading effects for article thumbnails
21. **FR-021:** The system must use appropriate FontAwesome icon for articles navigation
22. **FR-022:** The system must provide clear visual feedback for user interactions
23. **FR-023:** The system must implement responsive design using Foundation breakpoints

## Non-Goals (Out of Scope)

1. **NG-001:** User authentication or login functionality
2. **NG-002:** Article search and filtering capabilities
3. **NG-003:** Article bookmarking or favorites functionality
4. **NG-004:** Article commenting or social features
5. **NG-005:** Article content extraction or display within the app
6. **NG-006:** Advanced article categorization or tagging
7. **NG-007:** Article recommendation algorithms
8. **NG-008:** Performance optimizations beyond basic responsive design
9. **NG-009:** Integration with external reading applications
10. **NG-010:** Article content modification or editing capabilities

## Design Considerations

### Visual Design

- **Typography:** Use Foundation typography system with clear hierarchy
- **Color Scheme:** Maintain existing color scheme (no color changes from videos)
- **Icons:** Use appropriate FontAwesome icon for navigation (e.g., `fa-file-text`, `fa-newspaper`)
- **Cards:** Optimize article cards for text content and readability while maintaining visual consistency
- **Spacing:** Maintain consistent spacing patterns with existing site design

### User Experience

- **Navigation:** Add "Articles" menu item with appropriate icon after "Videos"
- **Loading States:** Implement consistent loading patterns with shimmer effects
- **Error Handling:** Provide clear, actionable error messages
- **Accessibility:** Follow WCAG 2.1 AA guidelines for screen readers and keyboard navigation
- **Mobile First:** Design for mobile devices first, then enhance for larger screens

### Responsive Design

- **Breakpoints:** Use Foundation SCSS breakpoints (small, medium, large)
- **Masonry Layout:** Implement responsive column counts (1-4 columns based on screen size)
- **Touch Interactions:** Optimize for touch devices with appropriate touch targets
- **Performance:** Ensure fast loading and smooth scrolling on all devices

## Technical Considerations

### Blazor WebAssembly (.NET 9) Implementation

- **Component Structure:** Create Articles.razor page in `src/redmuffin.Blazor.StaticWeb/Features/Pages/ArticlesPage/`
- **Code Organization:** Use code-behind pattern (Articles.razor.cs) for complex logic
- **State Management:** Implement proper state management for article data
- **Dependency Injection:** Use HttpClient injection for API calls
- **Error Boundaries:** Implement proper error handling with user-friendly messages

### Azure Functions (.NET 8) API

- **Function Name:** RaindropListArticles
- **File Path:** `src/redmuffin.Blazor.StaticWeb.Api/Functions/RaindropListArticles.cs`
- **API Endpoint:** `/api/RaindropListArticles`
- **Category ID:** Use `56658122` for articles category
- **Pattern Consistency:** Follow existing RaindropListVideos patterns
- **Error Handling:** Implement comprehensive error handling and logging
- **Performance:** Use proper async/await patterns and ConfigureAwait(false)

### Data Verification Strategy

1. **Data Structure Analysis:** Compare Videos vs Articles API responses
2. **Compatibility Check:** Verify RaindropItem model works for articles
3. **Model Adaptation:** Create article-specific models if needed
4. **Testing:** Implement comprehensive tests for data handling

### Styling and Assets

- **SCSS File:** Create `src/redmuffin.Blazor.StaticWeb/wwwroot/scss/_articles.scss` with article-specific styling
- **Import:** Add `@import '_articles';` to `app.scss`
- **Masonry Layout:** Optimize for article content display while maintaining visual consistency
- **Foundation Classes:** Use existing Foundation classes for consistency

## Success Metrics

### Technical Success Criteria

1. **Build Success:** All code compiles without errors
2. **Test Coverage:** 100% pass rate for all TUnit tests
3. **Performance:** Page loads within 3 seconds on typical connections
4. **Compatibility:** Works correctly across Chrome, Firefox, Safari, Edge
5. **Responsive:** Functions properly on mobile, tablet, and desktop

### User Experience Success Criteria

1. **Usability:** Users can successfully browse and access articles
2. **Accessibility:** Meets WCAG 2.1 AA compliance standards
3. **Visual Distinction:** Users can immediately identify articles vs videos
4. **Error Recovery:** Users receive clear guidance when errors occur
5. **Mobile Experience:** Seamless experience on mobile devices

### Business Success Criteria

1. **Content Access:** Users can access all available articles from Raindrop.io
2. **Engagement:** Users successfully navigate to external article links
3. **Reliability:** System handles API failures gracefully
4. **Maintainability:** Code follows established patterns for future enhancements

## Implementation Notes

### Development Approach

1. **TDD Implementation:** Write tests first for Azure Function
2. **Data Verification:** Verify API response structure before development
3. **Incremental Development:** Build page incrementally with testing at each step
4. **Code Reuse:** Maximize reuse of existing patterns and components

### Testing Strategy

- **Unit Tests:** TUnit tests for all business logic and API functions
- **Integration Tests:** End-to-end testing of article fetching and display
- **UI Tests:** Component testing for proper rendering and interactions
- **Data Tests:** Verification of JSON deserialization and data handling

### Quality Assurance

- **Code Review:** Follow established code review processes
- **Performance Testing:** Verify loading times and responsiveness
- **Cross-Browser Testing:** Ensure compatibility across target browsers
- **Accessibility Testing:** Verify screen reader and keyboard navigation

## Open Questions

1. **Data Structure:** Are Articles and Videos API responses identical in structure?
2. **Content Preview:** Should article cards show more text content than video cards?
3. **Sorting:** Should articles be sorted by creation date or another criterion?
4. **Caching:** Should article data be cached client-side for performance?
5. **Future Integration:** Are there plans for additional content types that would benefit from this pattern?

### Resolved Questions

- **Icon Selection:** Will use appropriate FontAwesome icon for articles navigation (e.g., `fa-file-text`, `fa-newspaper`)
- **Loading Behavior:** Manual fetch via "Fetch Articles" button (no automatic loading)
- **Page Title:** "Articles" (SEO optimization not required)
- **Visual Design:** Maintain existing color scheme, optimize masonry layout for article readability
- **Error Messages:** Implement standard error handling patterns following best practices

## Dependencies

### External Dependencies

- **Raindrop.io API:** Articles category must be accessible via API
- **FontAwesome:** Icons for navigation and UI elements
- **Foundation Framework:** UI components and responsive grid system

### Internal Dependencies

- **Existing Infrastructure:** RaindropItem models and serialization context
- **Common Libraries:** HTTP client factory and logging infrastructure
- **Azure Functions:** API hosting and configuration
- **SCSS Pipeline:** Styling compilation and integration

### Development Dependencies

- **TUnit Framework:** For comprehensive testing
- **Development Tools:** Visual Studio, .NET 9 SDK
- **Build Pipeline:** SCSS compilation and asset management
