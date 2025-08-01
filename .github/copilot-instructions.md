# 🤖 AI Code Assistant Instructions

**FOR AI ASSISTANTS ONLY** - Tech guidelines for AI. Humans use README.md.

## 🚀 Project
**Frontend**: Blazor WebAssembly (.NET 9), feature-based
**Backend**: Azure Functions (.NET 8), isolated worker
**Testing**: TUnit (`[Test]`, `[Arguments]`) - NOT xUnit/NUnit/MSTest
**Mocking**: Strategic approach - LightMock.Generator for external deps, custom mocks for internal, Never NSubstitute
**Language**: C# 13 preview, WebAssembly optimizations
**Build**: `WasmStripILAfterAOT=true`, `InvariantGlobalization=true`, `PublishTrimmed=true`
**Deployment**: Azure Static Web Apps with CSP, caching configs

## 📦 Dependencies
**Blazored.LocalStorage**, **Markdig**, **Microsoft.Azure.Functions.Worker**, **TUnit**, **Zurb Foundation** (CDN), **Analyzers** (Roslynator, StyleCop, Meziantou, VSThreading), **FontAwesome** (CDN), **BuildWebCompiler2022** (SCSS), **Coverlet**

## 🚨 CRITICAL: ZERO BUILD WARNINGS POLICY

**MANDATORY WORKFLOW**: EVERY completed C# edit  → `dotnet clean && dotnet build --no-restore --verbosity quiet` → fix ALL warnings → continue

**PREVENTION CHECKLIST** (Apply to ALL C# code):
- `ConfigureAwait(false)` on ALL awaits
- `ArgumentNullException.ThrowIfNull()` for ALL parameters
- `using` statements: System first, then alphabetical
- Member order: fields→properties→constructors→methods
- `IDisposable` for ANY disposable fields
- `LoggerMessage` delegates (NOT `Logger.LogError()`)
- Remove ALL trailing whitespace
- ONE blank line maximum between members

**ENFORCEMENT**: Run `dotnet clean && dotnet build --no-restore --verbosity quiet` after EVERY C# file change. Zero warnings required (except IL2111).

### ANALYZER RULES (ZERO TOLERANCE)

| Rule | **MANDATORY COMPLIANCE** |
|------|--------------------------|
| **StyleCop** | **🚨 CRITICAL - ENFORCE IMMEDIATELY** |
| SA1402 | ONE type per file - NO exceptions |
| SA1208/1210 | Order usings: System first, then alphabetical - ALWAYS |
| SA1201-1214 | Member order: fields→properties→constructors→methods, public→internal→protected→private - STRICT |
| SA1413 | Trailing commas in multi-line initializers - REQUIRED |
| SA1028 | Remove ALL trailing whitespace - ZERO tolerance |
| SA1500 | Opening brace on new line - MANDATORY |
| SA1507 | NO multiple blank lines - ENFORCE |
| SA1508 | NO blank line before closing brace - STRICT |
| **Meziantou** | **🚨 CRITICAL - ENFORCE IMMEDIATELY** |
| MA0016 | Use `IEnumerable<T>`, `IList<T>` abstractions - REQUIRED |
| MA0002/0006/0074 | Specify `StringComparison.OrdinalIgnoreCase` - MANDATORY |
| MA0004 | Use `ConfigureAwait(false)` in library code - REQUIRED |
| MA0048 | File name MUST match type name - STRICT |
| MA0051 | Methods <60 lines - ENFORCE |
| MA0053 | Make class sealed when possible - REQUIRED |
| **Microsoft** | **🚨 CRITICAL - ENFORCE IMMEDIATELY** |
| CA1845 | Use `AsSpan()` instead of `Substring()` - MANDATORY |
| CA1854 | Use `TryGetValue` for Dictionary - REQUIRED |
| CA1869 | Cache `JsonSerializerOptions` instances - STRICT |
| CA1848 | Use LoggerMessage delegates - REQUIRED |
| CA2007 | Use `ConfigureAwait(false)` - MANDATORY |
| CA2016 | Forward `CancellationToken` parameters - REQUIRED |
| CA1805 | Remove explicit default initialization - STRICT |
| CA1822 | Mark members static when possible - ENFORCE |

### ONLY PERMITTED WARNING
**IL2111**: Safe to ignore (Blazor WebAssembly auto-generated `App_razor.g.cs` trimming optimization) - ALL others FORBIDDEN

### DOCUMENTATION WARNINGS: Visual Studio Only
**SA1623/SA1615**: Fix if existing documentation, skip if undocumented (Visual Studio may show these when terminal doesn't)

## 🧩 MANDATORY: Partial Class Organization Standards

### BLAZOR COMPONENTS: Split by Concern
**Pattern**: `ComponentName.razor.cs` (main) + `ComponentName.Logging.cs` (LoggerMessage ONLY)
```csharp
// Home.razor.cs - Business logic, lifecycle, properties, events
public partial class Home : ComponentBase
{
    [Inject] public required ILogger<Home> Logger { get; set; }
    private async Task HandleClickAsync()
    {
        LogButtonClicked(Logger, null); // Reference logging partial
        // Business logic...
    }
}

// Home.Logging.cs - LoggerMessage delegates ONLY
public partial class Home
{
    private static readonly Action<ILogger, Exception?> LogButtonClicked =
        LoggerMessage.Define(LogLevel.Information, new EventId(5, nameof(LogButtonClicked)),
            "Button clicked");
}
```

### SERVICES/CLASSES: Split by Concern
**Pattern**: `ServiceName.cs` (main) + `ServiceName.Logging.cs` (LoggerMessage ONLY)
```csharp
// UserService.cs - Business logic, methods, properties
public partial class UserService : IUserService
{
    private readonly ILogger<UserService> _logger;
    public async Task<User> GetUserAsync(string id)
    {
        LogUserRequested(_logger, id, null); // Reference logging partial
        // Service logic...
    }
}

// UserService.Logging.cs - LoggerMessage delegates ONLY
public partial class UserService
{
    private static readonly Action<ILogger, string, Exception?> LogUserRequested =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(1, nameof(LogUserRequested)),
            "User requested: {UserId}");
}
```

### TEST CLASSES: Split by Concern
**Pattern**: `TestClassName.cs` ([Test] methods ONLY) + `TestClassName.Helpers.cs` (TestScope, mocks, utilities)
```csharp
// HomeTests.cs - [Test] methods ONLY
public partial class HomeTests
{
    [Test]
    public async Task Home_ButtonClick_LogsExpectedEvent()
    {
        // Arrange
        using var scope = CreateTestScope();
        var component = scope.Context.RenderComponent<HomePage>();

        // Act & Assert
        await button.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);
        await Assert.That(scope.Logger.LogEntries.Any(entry =>
            entry.Message.Contains("Button clicked"))).IsTrue();
    }
}

// HomeTests.Helpers.cs - Infrastructure ONLY
public partial class HomeTests
{
    public sealed class TestScope(string baseUri = "http://localhost:5000/") : IDisposable
    {
        public TestContext Context { get; } = new();
        public NavigationManagerMock NavigationManager { get; } = new(baseUri);
        public TestLogger<HomePage> Logger { get; } = new();
        // TestScope infrastructure...
    }

    private static TestScope CreateTestScope() => new TestScope().WithStandardServices();
    // Helper methods, mocks, utilities...
}
```

### FILE NAMING CONVENTION
```
Components:
├── Home.razor.cs                 # Main component logic
└── Home.Logging.cs              # LoggerMessage delegates

Services:
├── UserService.cs               # Main service logic
└── UserService.Logging.cs       # LoggerMessage delegates

Tests:
├── HomeTests.cs                 # [Test] methods only
└── HomeTests.Helpers.cs         # TestScope, mocks, utilities
```

### MIGRATION PRIORITIES
1. **✅ Components**: Already established with Home.Logging.cs pattern
2. **🔄 Services**: Migrate existing services with LoggerMessage (like LogHelpers.cs)
3. **🎯 Tests**: Split large test files (HomeTests.cs) into main + helpers
4. **📝 New Code**: ALL new classes must follow partial class standards

### BENEFITS
- **✅ Cleaner Main Files**: Business logic without logging clutter
- **✅ Focused Test Files**: Only actual tests in main file
- **✅ Better Organization**: Clear separation of concerns
- **✅ Easier Maintenance**: Infrastructure changes isolated
- **✅ Consistent Standards**: Uniform pattern across solution

## 🧪 TDD Workflow + MANDATORY Testing Patterns

**Red-Green-Refactor**: Write failing test → implement → refactor
**Before features**: Write failing TUnit test → `dotnet test` → implement → refactor
**Test naming**: `Component_Behavior_ExpectedOutcome` (underscores only in tests)
**Test behavior, not implementation**: Public interfaces/contracts only
**Mock dependencies**: Constructor injection for isolation
**Test structure**: Arrange-Act-Assert pattern

### MANDATORY: TestScope Architecture (ALL Test Classes)
```csharp
/// <summary>
///     Modern test scope that encapsulates all test resources with automatic disposal.
///     Uses C# 13 primary constructor pattern for clean, professional resource management.
/// </summary>
public sealed class TestScope(string baseUri = "http://localhost:5000/") : IDisposable
{
    public TestContext Context { get; } = new();
    public NavigationManagerMock NavigationManager { get; } = new(baseUri);
    public TestLogger<T> Logger { get; } = new();

    // Fluent builder methods for service configuration
    public TestScope WithStandardServices() { /* setup */ return this; }
    public TestScope WithFailingHttpClient() { /* setup */ return this; }
    public TestScope WithJSInterop(JSRuntimeMode mode = JSRuntimeMode.Strict) { /* setup */ return this; }

    public void Dispose() => Context?.Dispose();
}

// Factory methods for common scenarios
private static TestScope CreateTestScope() => new TestScope().WithStandardServices();
```

### MANDATORY: TUnit Fluent Chaining
**✅ USE CHAINING FOR**: Same object/property assertions, logically sequential validations
**⚠️ USE Assert.Multiple FOR**: Different objects, unrelated concerns

```csharp
// ✅ OPTIMAL: Chain related assertions on same object
await Assert.That(component.Markup).IsNotNull().And.Contains("expected").And.Contains("more");

// ✅ OPTIMAL: Use Assert.Multiple for unrelated concerns
using (Assert.Multiple())
{
    await Assert.That(component.Find("h1")).IsNotNull();  // DOM structure
    await Assert.That(scope.Logger.LogEntries.Any(entry => entry.Message.Contains("logged"))).IsTrue();  // Logging
}

// ❌ NEVER: Separate assertions on same object
using (Assert.Multiple())
{
    await Assert.That(component.Markup).IsNotNull();
    await Assert.That(component.Markup).Contains("text");  // WRONG
}
```

### MANDATORY: Test Quality Checklist
**Before committing ANY test:**
- [ ] ConfigureAwait(false) on all async calls
- [ ] **NEVER put ConfigureAwait(false) at the end of assert statements**
- [ ] TestScope pattern with fluent configuration
- [ ] TUnit chaining for related assertions
- [ ] Clear AAA structure with comments
- [ ] Single responsibility principle
- [ ] Zero build warnings compliance
- [ ] Resource disposal via using statements
- [ ] Comprehensive error scenario testing
- [ ] Partial class structure: Tests in main, helpers in .Helpers.cs

## 🎭 Mocking Strategy
**STRATEGIC APPROACH**: Use appropriate mocking based on dependency type

### LightMock.Generator - For 3rd Party/External Dependencies ONLY
**USE FOR**: `IHttpClientFactory`, `ILocalStorageService`, `ILogger<T>`, external APIs, Azure services
**Benefits**: Compile-time generation, zero runtime overhead, AOT compatible
**Mock naming**: Use `Mock` suffix: `var httpClientMock = new Mock<IHttpClientFactory>();`
**Usage**: `new Mock<IInterface>()` → setup → pass `.Object` to constructor

**🔧 CRITICAL: Optional Parameters Solution**
**CS0854 Fix**: ALWAYS specify ALL parameters explicitly in `Arrange()`/`Assert()` calls:
```csharp
// ❌ FAILS: _mock.Arrange(f => f.GetAsync("key"))
// ✅ WORKS: _mock.Arrange(f => f.GetAsync("key", CancellationToken.None))
// ✅ WORKS: _mock.Arrange(f => f.SetAsync("key", value, null, CancellationToken.None))
```
**Pattern**: `CancellationToken.None`, `null`, `The<T>.IsAnyValue` for optional params

### Custom Mocks - For Internal Components/Services
**USE FOR**: `NavigationManager`, internal services, Blazor components, project-specific abstractions
**Benefits**: Full control, tailored behavior, easier debugging, no external dependencies
**Pattern**: Follow HomeTests.Helpers.cs examples with sealed classes and primary constructors

```csharp
// ✅ CUSTOM MOCK: Internal NavigationManager
public sealed class NavigationManagerMock(string baseUri) : NavigationManager
{
    public string? NavigatedTo { get; private set; }
    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
        NavigatedTo = uri;
    }
}

// ✅ LIGHTMOCK: External dependency
var httpClientMock = new Mock<IHttpClientFactory>();
httpClientMock.Arrange(f => f.CreateClient(The<string>.IsAnyValue))
    .Returns(new HttpClient());
```

```csharp
[Test, Arguments(null), Arguments("")]
public async Task Should_Throw_When_Invalid_Id(string invalidId) { /*...*/ }
```

## 💉 Dependency Injection
**Constructor injection required** with null validation:
```csharp
public UserService(IHttpClientFactory httpClientFactory, ILogger<UserService> logger)
{
    _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
}
```

**Important**: Only inject `HttpClient`/`IHttpClientFactory` when service actually needs HTTP requests

**Blazor components**:
```csharp
public partial class UserProfile : ComponentBase
{
    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private ILocalStorageService LocalStorage { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        ArgumentNullException.ThrowIfNull(UserService);
    }
}
```

**Service lifetimes**: Singleton (shared), Scoped (per request/circuit), Transient (new each time)

## 📏 Standards
**Private fields**: `_` prefix | **var**: Only when type apparent | **Line length**: 160 chars | **Braces**: Always use | **Null checks**: `ArgumentNullException.ThrowIfNull()`

## 🎨 UI & Styling
**Framework**: Zurb Foundation only
**SCSS ONLY**: All styles in `wwwroot/scss/` - NEVER modify CSS directly
**Component styles**: NEVER use `.razor.css` - use SCSS partials with `_` prefix
**SCSS partials**: Must start with `_`, included in `app.scss` for auto-compilation
**SCSS Build**: Use `Debug-Sass` configuration for SCSS compilation: `dotnet build --configuration Debug-Sass` | `BuildWebCompiler2022` package auto-included
**JavaScript**: Minimal - prefer C#/Blazor, use `IJSRuntime.InvokeAsync<T>()`, NO JS for CSS
**Accessibility**: WCAG 2.1 AA compliance, semantic HTML, ARIA roles
**Performance**: Lazy loading, virtualization for large lists, optimize assets
**Images**: WebP/AVIF, `loading="lazy"`, `srcset`

## 🔒 Security & API
**Input validation**: Always validate/sanitize | **XSS/CSRF**: Use Blazor built-ins | **Secrets**: Never expose in client | **API**: `IHttpClientFactory` only for actual HTTP needs | **CSP**: In `staticwebapp.config.json` - allows 'unsafe-inline' for styles, restricts scripts | **Azure Functions**: Isolated worker with DI | **Authentication**: ASP.NET Core Identity, RBAC

## ⚡ Modern C#
**ALWAYS remember to use modern C# 13 and .NET 9 patterns and techniques**
Primary Constructors: `public class Person(string name, int age)` | Collection Expressions: `int[] nums = [1,2,3];` | Default Lambda: `(x, y=5) => x+y` | Alias Types: `using IntPair = (int, int);` | params Collections: `params ReadOnlySpan<T>` | ref readonly: `void M(ref readonly int x)` | Inline Arrays: `[InlineArray(10)]` | New Lock: `var l = new Lock(); using(l.EnterScope())`

## 📁 File Organization
**Features**: `src/redmuffin.Blazor.StaticWeb/Features/` - Feature-based with pages/components
**Static**: `src/redmuffin.Blazor.StaticWeb/wwwroot/` - CSS, SCSS, JS, sample data, libraries
**Tests**: `tests/` - Mirror structure with TUnit projects
**Scripts**: `scripts/` - PowerShell automation
**Config**: `.github/` - Workflows, instructions, prompts, chatmodes

**Key dirs**: `.github/instructions/` (tech standards), `.github/prompts/` (AI prompts), `.github/chatmodes/` (AI modes), `src/.../Features/` (components), `src/.../Api/` (Azure Functions), `src/.../Common/` (shared)

## 📝 Markdown Standards
**MANDATORY**: For ALL Markdown file creation/modification → Follow `.github/instructions/markdown.instructions.md`
**Compliance**: MarkdownLint rules (MD001-MD059), auto-fix enabled, configuration via `.markdownlint.jsonc`
**VS Code**: Auto-format on save, lint workspace, fix violations commands available
**Structure**: Proper headings hierarchy, consistent formatting, table alignment, link validation

## 🤖 AI Guidelines

### 🔄 Development Workflow
**MANDATORY**: After EVERY major C# file change: `dotnet clean && dotnet build --no-restore --verbosity quiet` → Fix ALL warnings → Continue
**Pre-commit**: `dotnet test` must pass without errors (warnings OK) - stop commit if test errors exist → does not need build or clean before
**Local Testing**: Start web project → navigate to `localhost:5233` → use Puppeteer for page verification → NEVER use SWA emulator
**Git commits**: Batch by SRP for quality messages
**File editing**: One file at a time, track progress ("Edit 2 of 5")
**Unclear items**: Ask questions, wait for answers before continuing
**Large changes**: Outline plan, get approval, incremental edits, keep buildable

### 🛠️ Tools & Coverage
**Coverage**: `scripts/Generate-CoverageReport.ps1` | `scripts/View-CoverageReport.ps1`
**Config**: Coverlet MSBuild integration, exclusions in `.coverletrc`/`Directory.Build.props`
**Output**: HTML (unified/branded), XML, JSON, Cobertura to `coverage/`
**Build**: Use `run_build` tool to verify changes

### 🌐 External Tools
**Context7 MCP**: `resolve-library-id` → `get-library-docs` (current docs over training data)
**Fetch MCP**: URL→markdown conversion for docs/repos/tutorials
**Brave Search**: `brave_web_search` (2,000 queries/month)
**Sequential Thinking**: Complex problem-solving with dynamic adaptation

### 📍 Repository Info
**Owner**: `michaelvolz` | **Name**: `redmuffin.Blazor.StaticWeb`
**Encoding**: UTF8 without BOM for Markdown
**Exclude samples**: `wwwroot/sample-data/markdown-cheat-sheet.md`, `wwwroot/Example.md`

### 📚 Tech Instructions (`.github/instructions/`)
**Core**: Blazor (`.razor`), C# (`.cs`), PowerShell (`.ps1`), SCSS (`.scss`), Markdown (`.md`)
**Architecture**: REST APIs, Azure Functions, GitHub Actions, Performance, Commit Standards

### 💡 Essential Examples
```csharp
// Blazor Component with full DI pattern + partial class structure
public partial class Example : ComponentBase
{
    [Inject] private ILocalStorageService LocalStorage { get; set; } = default!;
    [Parameter] public string Title { get; set; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        ArgumentNullException.ThrowIfNull(LocalStorage);
        // Component initialization
    }
}

// Azure Function (.NET 8, isolated worker)
[Function("FunctionName")]
public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
{
    // Function implementation
}

// TUnit Test with TestScope Architecture + Custom Mocks (Internal)
[Test]
public async Task Component_Behavior_ExpectedOutcome()
{
    // Arrange
    using var scope = CreateTestScope(); // From .Helpers.cs partial - uses custom mocks

    // Act
    var component = scope.BUnitContext.Render<MyComponent>();
    var button = component.Find("button");
    await button.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

    // Assert - Use chaining for related assertions
    await Assert.That(component.Markup).IsNotNull().And.Contains("expected");
}

// Service Testing with LightMock.Generator (External Dependencies)
[Test]
public async Task Should_Return_User_When_Valid_Id_Provided()
{
    // Arrange - LightMock for external dependencies
    var httpClientMock = new Mock<IHttpClientFactory>();
    var loggerMock = new Mock<ILogger<UserService>>();
    httpClientMock.Arrange(f => f.CreateClient(The<string>.IsAnyValue))
        .Returns(new HttpClient());
    
    var userService = new UserService(httpClientMock.Object, loggerMock.Object);
    
    // Act
    var result = await userService.GetUserAsync("valid-id").ConfigureAwait(false);
    
    // Assert
    await Assert.That(result).IsNotNull();
}
```

### ⭐ Best Practices
**TDD**: Test first, small steps, continuous refactoring, fast feedback, behavior-focused
**DI**: Dependency inversion, constructor injection, single responsibility, interface segregation
**Testing**: TestScope architecture, TUnit fluent chaining, comprehensive error scenarios, zero warnings compliance, partial class organization
**Partial Classes**: Components, services, and tests MUST follow established partial class patterns for clean separation of concerns
**General**: Modular/reusable/testable components, strongly-typed parameters, handle exceptions with try/catch or `<ErrorBoundary>`, prefer C#/Blazor over JS, use `StateHasChanged()` sparingly, implement `IDisposable` for subscriptions/timers