# PRD-006: Blazor WASM Bundle Size Optimization

## Executive Summary

**Problem**: Current Blazor WebAssembly application has a 7.6MB loading size on mobile devices, which is excessive and impacts user experience, especially on slower connections.

**Goal**: Reduce bundle size from 7.6MB to 2-4MB (60-70% reduction) through aggressive optimization techniques while maintaining functionality and performance.

**Priority**: High - Mobile user experience and loading performance

**Timeline**: 2-3 weeks

## Current State Analysis

### Bundle Size Breakdown
- **Total Size**: 7.6MB (iPhone Safari measurement)
- **Largest Contributors**:
  - `System.Private.CoreLib.s7q7c40kwm.wasm`: 4.43 MB
  - `System.Private.Xml.ceekp79nva.wasm`: 2.95 MB
  - `dotnet.native.x4stzgpoa0.wasm`: 2.89 MB
  - `System.Data.Common.q3ol77wfoq.wasm`: 0.96 MB
  - Other runtime libraries: ~1.5 MB

### Current Optimizations (Already Enabled)
- ✅ `PublishTrimmed=true` with `TrimMode=partial`
- ✅ `InvariantGlobalization=true`
- ✅ `BlazorWebAssemblyPreserveCollationData=false`
- ✅ `WasmStripILAfterAOT=true`
- ✅ `WasmEnableSIMD=true`
- ✅ Pre-compression (Brotli/Gzip)

## Optimization Strategy

### Phase 1: Safe Optimizations (Week 1)
**Target Reduction**: 20-30% (1.5-2.3MB)

#### 1.1 Disable SIMD Optimizations
**Impact**: High reduction potential
**Risk**: Low (performance trade-off)
**Implementation**:
```xml
<WasmEnableSIMD>false</WasmEnableSIMD>
```

#### 1.2 Remove Timezone Support
**Impact**: Medium (several hundred KB)
**Risk**: Low (verify no timezone usage)
**Implementation**:
```xml
<BlazorEnableTimeZoneSupport>false</BlazorEnableTimeZoneSupport>
```

#### 1.3 Advanced Runtime Feature Trimming
**Impact**: Medium
**Risk**: Low
**Implementation**:
```xml
<EventSourceSupport>false</EventSourceSupport>
<UseSystemResourceKeys>true</UseSystemResourceKeys>
<IlcOptimizationPreference>Size</IlcOptimizationPreference>
<IlcFoldIdenticalMethodBodies>true</IlcFoldIdenticalMethodBodies>
```

### Phase 2: Aggressive Trimming (Week 2)
**Target Reduction**: 30-40% (2.3-3.0MB)

#### 2.1 Switch to Full Trimming Mode
**Impact**: Very High (20-40% reduction)
**Risk**: Medium (compatibility issues)
**Implementation**:
```xml
<TrimMode>full</TrimMode>
```
**Testing Required**: Comprehensive functional testing

#### 2.2 Custom Trimming Descriptors
**Impact**: High
**Risk**: Medium
**Implementation**: Create `TrimmerRoots.xml` with minimal preserved APIs

### Phase 3: Dependency Optimization (Week 3)
**Target Reduction**: 10-20% (0.8-1.5MB)

#### 3.1 Evaluate NuGet Dependencies
**Current Dependencies**:
- `Blazored.LocalStorage` (4.5.0)
- `Markdig` (0.41.3) - **High Impact Candidate**
- `Microsoft.AspNetCore.Components.Authorization` (9.0.6)
- `Microsoft.AspNetCore.WebUtilities` (9.0.6)
- `Microsoft.Extensions.Http` (9.0.6)

**Actions**:
- Consider server-side markdown processing to remove `Markdig`
- Evaluate if `Microsoft.AspNetCore.WebUtilities` is essential
- Review `Microsoft.Extensions.Http` usage patterns

#### 3.2 JSON Serialization Optimization
**Current**: `System.Text.Json` with `JsonSerializerContext`
**Alternative**: Consider `MessagePack` or `MemoryPack` for binary serialization
**Impact**: Medium (won't remove XML dependencies but improves serialization efficiency)

## Technical Implementation Plan

### Step 1: Baseline Measurement
```powershell
# Build and measure current size
dotnet publish -c Release
# Measure _framework folder size
# Document current PageLoadSpeed on iPhone
```

### Step 2: Phase 1 Implementation
1. **Update `Directory.Build.props`**:
   ```xml
   <!-- Add to RELEASE MODE section -->
   <WasmEnableSIMD>false</WasmEnableSIMD>
   <BlazorEnableTimeZoneSupport>false</BlazorEnableTimeZoneSupport>
   <EventSourceSupport>false</EventSourceSupport>
   <IlcOptimizationPreference>Size</IlcOptimizationPreference>
   <IlcFoldIdenticalMethodBodies>true</IlcFoldIdenticalMethodBodies>
   ```

2. **Build and Test**:
   ```powershell
   dotnet clean
   dotnet publish -c Release
   # Measure size reduction
   # Test functionality
   ```

### Step 3: Phase 2 Implementation
1. **Enable Full Trimming**:
   ```xml
   <TrimMode>full</TrimMode>
   ```

2. **Create Trimming Descriptor**:
   ```xml
   <!-- TrimmerRoots.xml -->
   <linker>
     <assembly fullname="redmuffin.Blazor.StaticWeb">
       <!-- Preserve essential types only -->
     </assembly>
   </linker>
   ```

3. **Comprehensive Testing**:
   - All features functional
   - No runtime exceptions
   - Performance benchmarks

### Step 4: Phase 3 Implementation
1. **Dependency Analysis**:
   ```powershell
   dotnet list package --include-transitive
   # Analyze each dependency's contribution
   ```

2. **Selective Removal/Replacement**:
   - Move markdown processing server-side
   - Replace heavy dependencies with lighter alternatives
   - Implement custom solutions for simple utilities

## Testing Strategy

### Functional Testing
- **All Features**: Verify complete application functionality
- **Cross-Browser**: Test on Chrome, Firefox, Safari, Edge
- **Mobile Testing**: Specific focus on iOS Safari and Android Chrome
- **Performance**: Ensure no significant performance degradation

### Size Measurement
- **Build Output**: Measure `_framework` folder size
- **Network Transfer**: Use browser dev tools to measure actual transfer
- **Mobile Testing**: Use `page-load-timing.js` script on iPhone
- **Compression**: Verify Brotli/Gzip effectiveness

### Regression Testing
- **Automated Tests**: All TUnit tests must pass
- **Manual Testing**: Key user workflows
- **Error Handling**: Verify graceful degradation

## Success Metrics

### Primary Metrics
- **Bundle Size**: Reduce from 7.6MB to 2-4MB (60-70% reduction)
- **PageLoadSpeed**: Measure on iPhone Safari in private session
- **First Contentful Paint**: Improve loading performance metrics

### Secondary Metrics
- **Functionality**: 100% feature parity maintained
- **Performance**: No significant runtime performance degradation
- **Compatibility**: Works across all supported browsers

## Risk Assessment

### High Risk
- **Full Trimming Mode**: May break reflection-based code
- **Dependency Removal**: Could impact functionality

### Medium Risk
- **SIMD Disable**: Performance impact on compute-intensive operations
- **Custom Trimming**: Complex configuration management

### Low Risk
- **Feature Flags**: Well-documented .NET optimizations
- **Timezone Removal**: Easily verifiable impact

## Rollback Plan

### Immediate Rollback
1. Revert `Directory.Build.props` changes
2. Restore `TrimMode=partial`
3. Re-enable removed features
4. Rebuild and redeploy

### Incremental Rollback
- Each phase can be independently reverted
- Git commits should be atomic per optimization
- Maintain separate branches for each phase

## Monitoring and Validation

### Build Pipeline
- Add size measurement to CI/CD
- Fail builds if size exceeds threshold
- Generate size reports for each build

### Production Monitoring
- Track loading performance metrics
- Monitor error rates post-deployment
- User experience feedback collection

## Dependencies and Prerequisites

### Technical Requirements
- .NET 9 SDK
- PowerShell for measurement scripts
- Browser developer tools for testing
- Mobile devices for validation

### Team Requirements
- Development time: 2-3 weeks
- Testing resources: QA validation
- Deployment coordination: DevOps support

## Deliverables

1. **Optimized Build Configuration**: Updated project files
2. **Size Measurement Scripts**: Automated size tracking
3. **Testing Documentation**: Validation procedures
4. **Performance Report**: Before/after metrics
5. **Deployment Guide**: Production rollout plan

## Acceptance Criteria

- [ ] Bundle size reduced to 4MB or less
- [ ] All existing functionality preserved
- [ ] No new runtime errors introduced
- [ ] Performance impact within acceptable limits
- [ ] Cross-browser compatibility maintained
- [ ] Mobile loading experience significantly improved
- [ ] Automated size monitoring implemented
- [ ] Documentation updated with new configuration

## Future Considerations

### Long-term Optimizations
- **Lazy Loading**: Implement component-level code splitting
- **Progressive Loading**: Load non-critical features on-demand
- **CDN Optimization**: Leverage edge caching for static assets
- **Bundle Splitting**: Separate vendor and application code

### Monitoring and Maintenance
- Regular size audits with each release
- Dependency update impact assessment
- Performance regression testing
- User experience metrics tracking

---

**Document Version**: 1.0  
**Created**: January 2025  
**Owner**: Development Team  
**Reviewers**: Architecture Team, QA Team  
**Approval**: Product Owner