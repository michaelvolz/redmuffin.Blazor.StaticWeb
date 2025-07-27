# Product Requirements Document: HttpClientFactory Migration

## Overview

Migrate the entire codebase from direct HttpClient usage to the recommended IHttpClientFactory pattern for better resource management, configuration, and testability.

## Current Issues

### Direct HttpClient Usage Found In:

1. **Service Constructors:**
   - `DummyRaindropAPI(HttpClient httpClient, ...)`
   - `RaindropAPI(HttpClient httpClient, ...)`

2. **Component Injection:**
   - `Weather.razor` - `@inject HttpClient Http`

3. **Program.cs Registration:**
   - `builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });`

4. **Test Helpers:**
   - Various test classes creating HttpClient instances directly

## Benefits of IHttpClientFactory

- **Resource Management:** Automatic disposal and connection pooling
- **Configuration:** Named clients with specific configurations
- **Testability:** Easier mocking and testing
- **Performance:** Better connection reuse and DNS handling
- **Best Practice:** Microsoft recommended pattern for .NET applications

## Migration Tasks

### High Priority Tasks

#### Task 1: Update Service Constructors
**Priority:** Critical  
**Effort:** Medium  
**Files:**
- `src/redmuffin.Blazor.StaticWeb/Features/Raindrop/Services/DummyRaindropAPI.cs`
- `src/redmuffin.Blazor.StaticWeb/Features/Raindrop/Services/RaindropAPI.cs`

**Changes:**
```csharp
// Before
public DummyRaindropAPI(HttpClient httpClient, ILogger<DummyRaindropAPI> logger)

// After
public DummyRaindropAPI(IHttpClientFactory httpClientFactory, ILogger<DummyRaindropAPI> logger)
```

#### Task 2: Update Component Injection
**Priority:** Critical  
**Effort:** Low  
**Files:**
- `src/redmuffin.Blazor.StaticWeb/Features/Pages/WeatherPage/Weather.razor`
- `src/redmuffin.Blazor.StaticWeb/Features/Pages/WeatherPage/Weather.razor.cs`

**Changes:**
```csharp
// Before
@inject HttpClient Http

// After
@inject IHttpClientFactory HttpClientFactory
[Inject] private IHttpClientFactory HttpClientFactory { get; set; } = default!;
```

#### Task 3: Update Program.cs Registration
**Priority:** Critical  
**Effort:** Low  
**Files:**
- `src/redmuffin.Blazor.StaticWeb/Program.cs`

**Changes:**
```csharp
// Remove this line:
// builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Keep existing HttpClient registrations:
// builder.Services.AddHttpClient("DefaultHttpClient", ...);
// builder.Services.AddHttpClient("ExternalHttpClient", ...);
// builder.Services.AddHttpClient();
```

#### Task 4: Update Service Implementations
**Priority:** Critical  
**Effort:** Medium  
**Files:**
- `src/redmuffin.Blazor.StaticWeb/Features/Raindrop/Services/DummyRaindropAPI.cs`
- `src/redmuffin.Blazor.StaticWeb/Features/Raindrop/Services/RaindropAPI.cs`

**Changes:**
```csharp
// Update field and usage
private readonly IHttpClientFactory _httpClientFactory;

// In methods, create HttpClient as needed:
var httpClient = _httpClientFactory.CreateClient("DefaultHttpClient");
```

### Medium Priority Tasks

#### Task 5: Update Test Helpers
**Priority:** Medium  
**Effort:** Medium  
**Files:**
- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Raindrop/Services/IRaindropAPITests.Helpers.cs`
- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Home/HomeTests.Helpers.cs`

**Changes:**
- Update TestScope to use IHttpClientFactory pattern
- Update service instantiation in tests

#### Task 6: Update PRD Documentation
**Priority:** Medium  
**Effort:** Low  
**Files:**
- `tasks/PRD-002-DummyRaindropData.md`
- `tasks/PRD-002-DummyRaindropData-ToDo.md`
- Other relevant PRD files

**Changes:**
- Update all code examples to use IHttpClientFactory
- Update service registration examples
- Update constructor signatures in documentation

### Low Priority Tasks

#### Task 7: Update Integration Tests
**Priority:** Low  
**Effort:** Low  
**Files:**
- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Integration/HomePageIntegrationTests.cs`

#### Task 8: Update Task Documentation
**Priority:** Low  
**Effort:** Low  
**Files:**
- `tasks/simple-integration-test-prd-tasks-2.md`
- Other task files with HttpClient examples

## Implementation Plan

### Phase 1: Core Services (Week 1)
1. Update DummyRaindropAPI and RaindropAPI constructors
2. Update service implementations to use IHttpClientFactory
3. Update Program.cs registration
4. Run tests to ensure functionality

### Phase 2: Components (Week 1)
1. Update Weather component injection
2. Update component code-behind to use IHttpClientFactory
3. Test component functionality

### Phase 3: Tests and Documentation (Week 2)
1. Update test helpers and test scopes
2. Update PRD documentation
3. Update task documentation
4. Run full test suite

## Validation Criteria

### Functional Requirements
- [ ] All services use IHttpClientFactory instead of direct HttpClient injection
- [ ] All components use IHttpClientFactory instead of direct HttpClient injection
- [ ] No direct HttpClient registration in Program.cs (except for named clients)
- [ ] All tests pass with updated pattern
- [ ] Application functionality remains unchanged

### Code Quality Requirements
- [ ] Zero build warnings
- [ ] All analyzer rules pass
- [ ] Proper null validation for IHttpClientFactory parameters
- [ ] Consistent usage pattern across all services

### Documentation Requirements
- [ ] All PRD files updated with correct patterns
- [ ] All task files updated with correct examples
- [ ] Code comments updated where necessary

## Risk Assessment

### Low Risk
- **Service Constructor Changes:** Well-defined pattern with clear migration path
- **Component Updates:** Simple injection change
- **Documentation Updates:** No functional impact

### Medium Risk
- **Test Updates:** May require significant changes to test helpers
- **Integration Testing:** Need to verify all HTTP functionality works correctly

### Mitigation Strategies
- Implement changes incrementally
- Run tests after each change
- Keep backup of working state
- Test both dummy and real API scenarios

## Success Metrics

1. **Zero Build Warnings:** Maintain project's zero-warning policy
2. **All Tests Pass:** 100% test success rate after migration
3. **Functionality Preserved:** All HTTP operations work as before
4. **Code Consistency:** Uniform IHttpClientFactory usage across codebase
5. **Documentation Accuracy:** All examples and documentation reflect new pattern

## Notes

- This migration aligns with Microsoft's recommended practices for HttpClient usage in .NET applications
- The change improves testability by making HTTP dependencies more explicit
- Named HttpClient configurations ("DefaultHttpClient", "ExternalHttpClient") should be preserved
- Existing HttpClient factory registrations in Program.cs are correct and should be kept