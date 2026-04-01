	# PRD-020: WebAssembly Performance Metrics - Task List

## Relevant Files

### JavaScript

- `src/redmuffin.Blazor.StaticWeb/wwwroot/js/page-load-timing.js` - Add WASM metric collection functions and timing marks

### C# Models (New and Modified)

- `src/redmuffin.Blazor.StaticWeb/Features/Common/PageLoadSpeed/Core/WasmMetrics.cs` - **NEW FILE** - Record struct for WASM metrics data
- `src/redmuffin.Blazor.StaticWeb/Features/Common/PageLoadSpeed/Core/PerformanceMetrics.cs` - **MODIFY** - Add WasmMetrics property

### C# Services (Modified)

- `src/redmuffin.Blazor.StaticWeb/Services/IPerformanceMetricsService.cs` - **MODIFY** - Add GetWasmMetricsAsync method signature
- `src/redmuffin.Blazor.StaticWeb/Services/PerformanceMetricsService.cs` - **MODIFY** - Implement WASM metric retrieval via JS interop

### Blazor Components (Modified)

- `src/redmuffin.Blazor.StaticWeb/Features/Common/PageLoadSpeed/LoadSpeed.razor` - **MODIFY** - Add WASM section markup between Timing and Data Transfer
- `src/redmuffin.Blazor.StaticWeb/Features/Common/PageLoadSpeed/LoadSpeed.razor.cs` - **MODIFY** - Add WASM data binding and helper methods

### Styling (Modified)

- `src/redmuffin.Blazor.StaticWeb/scss/abstracts/_variables.scss` - **MODIFY** - Update widget width variables for iPhone 15 (430px)
- `src/redmuffin.Blazor.StaticWeb/scss/features/shared/_page-load-speed.scss` - **MODIFY** - Add WASM-specific styles and responsive breakpoints

### JavaScript Initializer (New)

- `src/redmuffin.Blazor.StaticWeb/wwwroot/redmuffin.Blazor.StaticWeb.lib.module.js` - **NEW FILE** - JS initializer for beforeStart/afterStarted timing marks

### Tests (New)

- `tests/redmuffin.Blazor.StaticWeb.Tests/Features/Common/PageLoadSpeed/WasmMetricsTests.cs` - **NEW FILE** - Unit tests for WasmMetrics model
- `tests/redmuffin.Blazor.StaticWeb.Tests/Services/PerformanceMetricsServiceTests.cs` - **MODIFY/NEW** - Add tests for WASM metric retrieval

---

## Tasks

- [ ] 1.0 Setup JavaScript WASM Metric Collection
  - [ ] 1.1 Add performance marks in JS initializer (beforeStart/afterStarted)
  - [ ] 1.2 Create getWasmMetrics() function to collect WASM data from Performance API
  - [ ] 1.3 Add helper functions to query dotnet.wasm and .dll resources
  - [ ] 1.4 Handle browser compatibility (Chrome memory API vs N/A for Firefox/Safari)

- [ ] 2.0 Create C# Data Models for WASM Metrics
  - [ ] 2.1 Create WasmMetrics.cs record struct with 5 properties
  - [ ] 2.2 Update PerformanceMetrics.cs to include WasmMetrics property
  - [ ] 2.3 Update PageLoadMetrics class to include WASM fields

- [ ] 3.0 Implement Service Layer for WASM Metrics
  - [ ] 3.1 Add GetWasmMetricsAsync method to IPerformanceMetricsService interface
  - [ ] 3.2 Implement GetWasmMetricsAsync in PerformanceMetricsService
  - [ ] 3.3 Add JS interop call to window.getWasmMetrics()
  - [ ] 3.4 Handle null/empty responses gracefully

- [ ] 4.0 Update Blazor Component UI
  - [ ] 4.1 Add WASM metrics section markup to LoadSpeed.razor (between Timing and Data Transfer)
  - [ ] 4.2 Add data binding for WASM metrics in LoadSpeed.razor.cs
  - [ ] 4.3 Add progress bar calculation helpers for WASM metrics
  - [ ] 4.4 Add semantic color coding for WASM metric thresholds

- [ ] 5.0 Update Styling and Responsive Design
  - [ ] 5.1 Update SCSS variables for widget width (330px → 430px on iPhone 15)
  - [ ] 5.2 Add .wasm CSS class with purple color (#9d4edd)
  - [ ] 5.3 Ensure compact layout to fit without scrolling
  - [ ] 5.4 Test responsive behavior on 375px and 430px viewports

- [ ] 6.0 Testing and Validation
  - [ ] 6.1 Write unit tests for WasmMetrics model
  - [ ] 6.2 Write tests for PerformanceMetricsService WASM methods
  - [ ] 6.3 Test on Chrome/Edge (full metrics available)
  - [ ] 6.4 Test on Firefox/Safari (partial metrics, should show N/A gracefully)
  - [ ] 6.5 Verify all 12 existing metrics still display correctly
  - [ ] 6.6 Verify widget fits without scrolling on mobile viewports

---

## Success Criteria

- [ ] All 5 WASM metrics display correctly on Chrome/Edge
- [ ] Widget displays partial data gracefully on Firefox/Safari (shows N/A where unavailable)
- [ ] All 12 existing metrics remain unchanged and functional
- [ ] Widget width expands to 430px on iPhone 15 viewport
- [ ] Widget fits without vertical scrolling on mobile viewports
- [ ] No horizontal scrolling on any viewport size
- [ ] WASM metrics section positioned between Timing and Data Transfer sections
- [ ] Styling matches existing sections (purple color for WASM)
- [ ] All tests pass (`dotnet test`)

---

## Notes

- **Section Order is Critical:** Timing → WASM → Data Transfer → Breakdown → Rating
- **Height Constraint:** Widget uses `position: fixed` and must fit without internal scrolling
- **Browser Compatibility:** Memory heap metric only available in Chrome/Edge; show "N/A" in other browsers
- **Performance:** Metric collection should add < 50ms to page load time
- **No Toggles:** WASM metrics display directly without buttons or tabs
