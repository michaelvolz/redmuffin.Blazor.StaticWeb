# PRD-006: Blazor WASM Bundle Size Optimization - Task Breakdown

## Overview

Detailed task breakdown for implementing Blazor WebAssembly bundle size optimization to reduce from 7.6MB to 2-4MB.

## Phase 1: Safe Optimizations (Week 1)

### Task 1.1: Baseline Measurement and Documentation

**Priority**: High  
**Effort**: 2 hours  
**Dependencies**: None

- [ ] Create baseline measurement script
  ```powershell
  # Create scripts/Measure-BundleSize.ps1
  # Measure current _framework folder size
  # Document PageLoadSpeed on iPhone Safari
  # Create size tracking spreadsheet/log
  ```
- [ ] Document current bundle composition
  - [ ] List all .wasm files with sizes
  - [ ] Identify largest contributors
  - [ ] Screenshot browser network tab
- [ ] Establish measurement methodology
  - [ ] iPhone Safari private session testing
  - [ ] Browser dev tools network measurement
  - [ ] Consistent testing environment

### Task 1.2: Disable SIMD Optimizations

**Priority**: High  
**Effort**: 1 hour  
**Dependencies**: Task 1.1

- [ ] Update `Directory.Build.props`
  ```xml
  <!-- In RELEASE MODE section -->
  <WasmEnableSIMD>false</WasmEnableSIMD>
  ```
- [ ] Build and measure size impact
  ```powershell
  dotnet clean
  dotnet publish -c Release
  scripts/Measure-BundleSize.ps1
  ```
- [ ] Test performance impact
  - [ ] Run existing performance benchmarks
  - [ ] Verify no significant degradation
- [ ] Document results
  - [ ] Size reduction achieved
  - [ ] Performance impact assessment

### Task 1.3: Remove Timezone Support

**Priority**: Medium  
**Effort**: 2 hours  
**Dependencies**: Task 1.2

- [ ] Audit codebase for timezone usage
  ```powershell
  # Search for DateTime timezone operations
  Get-ChildItem -Recurse -Include "*.cs","*.razor" | Select-String -Pattern "TimeZone|DateTimeOffset|ToLocalTime|ToUniversalTime"
  ```
- [ ] Verify no timezone dependencies
  - [ ] Check all DateTime operations
  - [ ] Verify no third-party timezone usage
- [ ] Add configuration flag
  ```xml
  <BlazorEnableTimeZoneSupport>false</BlazorEnableTimeZoneSupport>
  ```
- [ ] Build and test
  - [ ] Verify no runtime errors
  - [ ] Measure size reduction
  - [ ] Run full test suite

### Task 1.4: Advanced Runtime Feature Trimming

**Priority**: Medium  
**Effort**: 3 hours  
**Dependencies**: Task 1.3

- [ ] Add advanced trimming flags
  ```xml
  <EventSourceSupport>false</EventSourceSupport>
  <IlcOptimizationPreference>Size</IlcOptimizationPreference>
  <IlcFoldIdenticalMethodBodies>true</IlcFoldIdenticalMethodBodies>
  ```
- [ ] Audit EventSource usage
  ```powershell
  # Search for EventSource usage
  Get-ChildItem -Recurse -Include "*.cs" | Select-String -Pattern "EventSource|WriteEvent"
  ```
- [ ] Test and validate
  - [ ] Build and measure impact
  - [ ] Run comprehensive tests
  - [ ] Verify logging still works
- [ ] Document Phase 1 results
  - [ ] Total size reduction achieved
  - [ ] Cumulative impact summary
  - [ ] Performance assessment

## Phase 2: Aggressive Trimming (Week 2)

### Task 2.1: Enable Full Trimming Mode

**Priority**: High  
**Effort**: 4 hours  
**Dependencies**: Phase 1 complete

- [ ] Create backup branch
  ```bash
  git checkout -b feature/full-trimming-mode
  ```
- [ ] Update trimming configuration
  ```xml
  <TrimMode>full</TrimMode>
  ```
- [ ] Build and identify issues
  ```powershell
  dotnet clean
  dotnet publish -c Release 2>&1 | Tee-Object -FilePath "trimming-warnings.log"
  ```
- [ ] Address trimming warnings
  - [ ] Review all IL2XXX warnings
  - [ ] Add necessary `[DynamicallyAccessedMembers]` attributes
  - [ ] Create trimming descriptors for problematic areas
- [ ] Comprehensive testing
  - [ ] All TUnit tests pass
  - [ ] Manual feature testing
  - [ ] Cross-browser validation

### Task 2.2: Create Custom Trimming Descriptors

**Priority**: Medium  
**Effort**: 6 hours  
**Dependencies**: Task 2.1

- [ ] Analyze trimming requirements
  - [ ] Identify reflection usage patterns
  - [ ] Map JSON serialization requirements
  - [ ] Document dynamic code access
- [ ] Create `TrimmerRoots.xml`
  ```xml
  <linker>
    <assembly fullname="redmuffin.Blazor.StaticWeb">
      <!-- Preserve essential types -->
      <type fullname="redmuffin.Blazor.StaticWeb.Features.*" />
      <!-- JSON serialization contexts -->
      <type fullname="*JsonSerializerContext" />
    </assembly>
  </linker>
  ```
- [ ] Add to project file
  ```xml
  <ItemGroup>
    <TrimmerRootDescriptor Include="TrimmerRoots.xml" />
  </ItemGroup>
  ```
- [ ] Iterative refinement
  - [ ] Build and test
  - [ ] Refine descriptor based on errors
  - [ ] Minimize preserved surface area
- [ ] Validate and measure
  - [ ] Ensure functionality preserved
  - [ ] Measure size impact
  - [ ] Document final configuration

### Task 2.3: Comprehensive Testing and Validation

**Priority**: High  
**Effort**: 8 hours  
**Dependencies**: Task 2.2

- [ ] Automated testing
  - [ ] All TUnit tests pass
  - [ ] No new test failures
  - [ ] Performance benchmarks
- [ ] Manual testing checklist
  - [ ] All major features functional
  - [ ] Navigation works correctly
  - [ ] Data loading and display
  - [ ] Local storage operations
  - [ ] API communication
- [ ] Cross-browser testing
  - [ ] Chrome (desktop/mobile)
  - [ ] Firefox (desktop/mobile)
  - [ ] Safari (desktop/mobile)
  - [ ] Edge (desktop)
- [ ] Performance validation
  - [ ] Loading time measurements
  - [ ] Runtime performance checks
  - [ ] Memory usage assessment
- [ ] Document Phase 2 results
  - [ ] Size reduction achieved
  - [ ] Functionality verification
  - [ ] Performance impact

## Phase 3: Dependency Optimization (Week 3)

### Task 3.1: Dependency Analysis and Audit

**Priority**: Medium  
**Effort**: 4 hours  
**Dependencies**: Phase 2 complete

- [ ] Generate dependency report
  ```powershell
  dotnet list package --include-transitive > dependency-report.txt
  ```
- [ ] Analyze each NuGet package
  - [ ] `Blazored.LocalStorage` (4.5.0) - Essential
  - [ ] `Markdig` (0.41.3) - **Optimization candidate**
  - [ ] `Microsoft.AspNetCore.Components.Authorization` (9.0.6)
  - [ ] `Microsoft.AspNetCore.WebUtilities` (9.0.6)
  - [ ] `Microsoft.Extensions.Http` (9.0.6)
- [ ] Measure individual package impact
  ```powershell
  # Temporarily remove each package and measure size
  # Document size contribution of each dependency
  ```
- [ ] Identify optimization opportunities
  - [ ] Server-side markdown processing
  - [ ] Custom utility implementations
  - [ ] Alternative lightweight libraries

### Task 3.2: Markdig Optimization

**Priority**: High  
**Effort**: 6 hours  
**Dependencies**: Task 3.1

- [ ] Audit Markdig usage
  ```powershell
  Get-ChildItem -Recurse -Include "*.cs","*.razor" | Select-String -Pattern "Markdig|Markdown"
  ```
- [ ] Evaluate alternatives
  - [ ] Server-side markdown processing
  - [ ] Pre-compiled HTML content
  - [ ] Lightweight markdown parser
- [ ] Implement chosen solution
  - [ ] Move markdown processing to Azure Functions
  - [ ] Update client to consume HTML
  - [ ] Remove Markdig dependency
- [ ] Test and validate
  - [ ] Verify markdown content displays correctly
  - [ ] Measure size reduction
  - [ ] Performance impact assessment

### Task 3.3: JSON Serialization Optimization

**Priority**: Low  
**Effort**: 4 hours  
**Dependencies**: Task 3.2

- [ ] Evaluate current JSON usage
  - [ ] Audit `System.Text.Json` usage patterns
  - [ ] Identify serialization hotspots
  - [ ] Measure current performance
- [ ] Research alternatives
  - [ ] `MessagePack` for binary serialization
  - [ ] `MemoryPack` for high-performance scenarios
  - [ ] Custom serialization for simple cases
- [ ] Implement proof of concept
  - [ ] Replace one serialization scenario
  - [ ] Measure size and performance impact
  - [ ] Assess complexity vs. benefit
- [ ] Decision and implementation
  - [ ] Choose optimal approach
  - [ ] Implement if beneficial
  - [ ] Document trade-offs

## Final Validation and Deployment

### Task 4.1: Comprehensive Validation

**Priority**: High  
**Effort**: 6 hours  
**Dependencies**: All phases complete

- [ ] Final size measurement
  - [ ] Measure total bundle size
  - [ ] Compare to baseline (7.6MB target: 2-4MB)
  - [ ] Document size reduction percentage
- [ ] Complete functionality testing
  - [ ] All features working
  - [ ] No runtime errors
  - [ ] Performance acceptable
- [ ] Mobile testing validation
  - [ ] iPhone Safari PageLoadSpeed measurement
  - [ ] Android Chrome testing
  - [ ] Loading experience assessment
- [ ] Performance benchmarking
  - [ ] Loading time comparisons
  - [ ] Runtime performance metrics
  - [ ] Memory usage analysis

### Task 4.2: Documentation and Monitoring

**Priority**: Medium  
**Effort**: 3 hours  
**Dependencies**: Task 4.1

- [ ] Update project documentation
  - [ ] Document new build configuration
  - [ ] Update deployment procedures
  - [ ] Create troubleshooting guide
- [ ] Implement size monitoring
  - [ ] Add size checks to CI/CD pipeline
  - [ ] Create size regression alerts
  - [ ] Automated reporting
- [ ] Create rollback procedures
  - [ ] Document rollback steps
  - [ ] Test rollback process
  - [ ] Emergency procedures

### Task 4.3: Production Deployment

**Priority**: High  
**Effort**: 2 hours  
**Dependencies**: Task 4.2

- [ ] Staging deployment
  - [ ] Deploy to staging environment
  - [ ] Final validation testing
  - [ ] Performance monitoring
- [ ] Production deployment
  - [ ] Deploy optimized build
  - [ ] Monitor for issues
  - [ ] Validate user experience
- [ ] Post-deployment monitoring
  - [ ] Track loading performance metrics
  - [ ] Monitor error rates
  - [ ] User feedback collection

## Success Criteria Checklist

- [ ] Bundle size reduced from 7.6MB to ≤4MB (≥47% reduction)
- [ ] All existing functionality preserved
- [ ] No new runtime errors introduced
- [ ] Performance impact within acceptable limits (<10% degradation)
- [ ] Cross-browser compatibility maintained
- [ ] Mobile loading experience significantly improved
- [ ] Automated size monitoring implemented
- [ ] Documentation updated with new configuration

## Risk Mitigation

### High-Risk Tasks

- **Task 2.1** (Full Trimming): Create comprehensive backup, incremental testing
- **Task 3.2** (Markdig Removal): Validate all markdown content, fallback plan

### Rollback Triggers

- Bundle size reduction <30%
- Any functionality broken
- Performance degradation >15%
- Critical runtime errors

## Estimated Timeline

**Total Effort**: ~45 hours (3 weeks)  
**Week 1**: Phase 1 (8 hours)  
**Week 2**: Phase 2 (18 hours)  
**Week 3**: Phase 3 + Final (19 hours)

## Dependencies and Blockers

- Access to iPhone for mobile testing
- Staging environment for validation
- QA resources for comprehensive testing
- DevOps support for CI/CD updates

---

**Status**: Not Started  
**Assigned**: Development Team  
**Created**: January 2025  
**Last Updated**: January 2025
