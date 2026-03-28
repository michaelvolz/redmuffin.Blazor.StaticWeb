# HttpClientFactory Migration - Implementation Tasks

## Task Priority Matrix

### 🔴 Critical Priority (Must Complete First)

#### TASK-001: Update DummyRaindropAPI Constructor

**Status:** Not Started  
**Effort:** 2 hours  
**Dependencies:** None  
**Files:**

- `src/redmuffin.Blazor.StaticWeb/Features/Raindrop/Services/DummyRaindropAPI.cs`

**Implementation:**

```csharp
// Change constructor from:
public DummyRaindropAPI(HttpClient httpClient, ILogger<DummyRaindropAPI> logger)

// To:
public DummyRaindropAPI(IHttpClientFactory httpClientFactory, ILogger<DummyRaindropAPI> logger)

// Update field:
private readonly IHttpClientFactory _httpClientFactory;

// Update usage in methods:
var httpClient = _httpClientFactory.CreateClient("DefaultHttpClient");
```

#### TASK-002: Update RaindropAPI Constructor

**Status:** Not Started  
**Effort:** 2 hours  
**Dependencies:** None  
**Files:**

- `src/redmuffin.Blazor.StaticWeb/Features/Raindrop/Services/RaindropAPI.cs`

**Implementation:**

```csharp
// Same pattern as TASK-001
// Change constructor signature and update internal usage
```

#### TASK-003: Update Weather Component

**Status:** Not Started  
**Effort:** 1 hour  
**Dependencies:** None  
**Files:**

- `src/redmuffin.Blazor.StaticWeb/Features/Pages/WeatherPage/Weather.razor`
- `src/redmuffin.Blazor.StaticWeb/Features/Pages/WeatherPage/Weather.razor.cs`

**Implementation:**

```csharp
// In Weather.razor, change:
@inject HttpClient Http

// To:
@inject IHttpClientFactory HttpClientFactory

// In Weather.razor.cs, add property:
[Inject] private IHttpClientFactory HttpClientFactory { get; set; } = default!;

// Update OnInitializedAsync method:
var httpClient = HttpClientFactory.CreateClient();
_forecasts = await httpClient.GetFromJsonAsync<WeatherForecast[]>("sample-data/weather.json", JsonOptions).ConfigureAwait(false);
```

#### TASK-004: Update Program.cs Registration

**Status:** Not Started  
**Effort:** 30 minutes  
**Dependencies:** TASK-001, TASK-002  
**Files:**

- `src/redmuffin.Blazor.StaticWeb/Program.cs`

**Implementation:**

```csharp
// Remove this line:
// builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Keep existing named HttpClient registrations:
// builder.Services.AddHttpClient("DefaultHttpClient", ...);
// builder.Services.AddHttpClient("ExternalHttpClient", ...);
// builder.Services.AddHttpClient();
```

### 🟡 High Priority (Complete After Critical)

#### TASK-005: Update Test Helpers - IRaindropAPITests

**Status:** Not Started  
**Effort:** 3 hours  
**Dependencies:** TASK-001, TASK-002  
**Files:**

- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Raindrop/Services/IRaindropAPITests.Helpers.cs`

**Implementation:**

- Update TestScope to register IHttpClientFactory instead of HttpClient
- Update DummyAPI and RealAPI instantiation to use IHttpClientFactory
- Update TestHttpClientFactory to work with IHttpClientFactory pattern

#### TASK-006: Update Test Helpers - HomeTests

**Status:** Not Started  
**Effort:** 2 hours  
**Dependencies:** TASK-003  
**Files:**

- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Home/HomeTests.Helpers.cs`

**Implementation:**

- Update TestScope to use IHttpClientFactory pattern
- Ensure TestHttpClientFactory works with new pattern

#### TASK-007: Fix Current Build Issues

**Status:** Not Started  
**Effort:** 2 hours  
**Dependencies:** TASK-005  
**Files:**

- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Raindrop/Services/IRaindropAPITests.Helpers.cs`

**Implementation:**

- Fix CS1503 errors related to TestHttpClientFactory conversion
- Ensure proper IHttpClientFactory usage in test instantiation
- Resolve CA2000 warnings for TestScope disposal

### 🟢 Medium Priority (Complete After High Priority)

#### TASK-008: Update PRD Documentation

**Status:** Not Started  
**Effort:** 1 hour  
**Dependencies:** TASK-001, TASK-002, TASK-003  
**Files:**

- `tasks/PRD-002-DummyRaindropData.md`
- `tasks/PRD-002-DummyRaindropData-ToDo.md`

**Implementation:**

- Update all code examples to show IHttpClientFactory usage
- Update service registration examples
- Update constructor signatures in documentation

#### TASK-009: Update Task Documentation

**Status:** Not Started  
**Effort:** 1 hour  
**Dependencies:** TASK-008  
**Files:**

- `tasks/simple-integration-test-prd-tasks-2.md`
- Other task files with HttpClient examples

**Implementation:**

- Replace HttpClient examples with IHttpClientFactory patterns
- Update test setup examples

### 🔵 Low Priority (Complete When Time Permits)

#### TASK-010: Update Integration Tests

**Status:** Not Started  
**Effort:** 1 hour  
**Dependencies:** TASK-005, TASK-006  
**Files:**

- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Integration/HomePageIntegrationTests.cs`

#### TASK-011: Update copilot-instructions.md

**Status:** Not Started  
**Effort:** 30 minutes  
**Dependencies:** All previous tasks  
**Files:**

- `copilot-instructions.md`

**Implementation:**

- Add IHttpClientFactory usage guidelines
- Update examples to show correct pattern
- Add to mandatory standards section

## Implementation Sequence

### Week 1 - Core Migration

1. **Day 1:** Complete TASK-001 and TASK-002 (Service constructors)
2. **Day 2:** Complete TASK-004 (Program.cs) and TASK-003 (Weather component)
3. **Day 3:** Complete TASK-007 (Fix build issues)
4. **Day 4:** Complete TASK-005 (IRaindropAPITests helpers)
5. **Day 5:** Complete TASK-006 (HomeTests helpers)

### Week 2 - Documentation and Polish

1. **Day 1:** Complete TASK-008 (PRD documentation)
2. **Day 2:** Complete TASK-009 (Task documentation)
3. **Day 3:** Complete TASK-010 (Integration tests)
4. **Day 4:** Complete TASK-011 (copilot-instructions.md)
5. **Day 5:** Final testing and validation

## Validation Checklist

### After Each Critical Task

- [ ] Code compiles without errors
- [ ] No new build warnings introduced
- [ ] Relevant tests pass
- [ ] Service registration works correctly

### After All Critical Tasks

- [ ] Full test suite passes
- [ ] Application runs correctly
- [ ] All HTTP functionality works
- [ ] Zero build warnings maintained

### After All Tasks

- [ ] Documentation is accurate and up-to-date
- [ ] Code follows consistent patterns
- [ ] All examples use IHttpClientFactory
- [ ] Integration tests pass

## Risk Mitigation

### Before Starting

- [ ] Create backup branch
- [ ] Document current working state
- [ ] Run full test suite to establish baseline

### During Implementation

- [ ] Test after each task completion
- [ ] Commit working changes frequently
- [ ] Validate functionality at each step

### Rollback Plan

- If critical issues arise, revert to backup branch
- Complete tasks can be cherry-picked individually
- Focus on maintaining zero-warning policy

## Success Criteria

1. **Zero Build Warnings:** Maintain project's strict warning policy
2. **All Tests Pass:** 100% test success rate
3. **Consistent Pattern:** All services use IHttpClientFactory
4. **Documentation Updated:** All examples reflect new pattern
5. **Functionality Preserved:** No regression in HTTP operations

## Notes

- **ImageValidationService** already uses IHttpClientFactory correctly - no changes needed
- **OpenGraphImagesService** already uses IHttpClientFactory correctly - no changes needed
- Focus on services that directly inject HttpClient in constructors
- Maintain existing named HttpClient configurations
- Preserve all existing functionality while improving architecture
