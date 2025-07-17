# 🤖 AI Code Assistant Instructions

**FOR AI ASSISTANTS ONLY** - Tech guidelines for AI. Humans use README.md.

## 🚀 Project
**Frontend**: Blazor WebAssembly (.NET 9), feature-based  
**Backend**: Azure Functions (.NET 8), isolated worker  
**Testing**: TUnit (`[Test]`, `[Arguments]`) - NOT xUnit/NUnit/MSTest  
**Mocking**: LightMock.Generator ONLY - NSubstitute deprecated
**Language**: C# 13 preview, WebAssembly optimizations  
**Build**: `WasmStripILAfterAOT=true`, `InvariantGlobalization=true`, `PublishTrimmed=true`  
**Deployment**: Azure Static Web Apps with CSP, caching configs

## 📦 Dependencies
**Blazored.LocalStorage**, **Markdig**, **Microsoft.Azure.Functions.Worker**, **TUnit**, **Zurb Foundation** (CDN), **Analyzers** (Roslynator, StyleCop, Meziantou, VSThreading), **FontAwesome** (CDN), **BuildWebCompiler2022** (SCSS), **Coverlet**

## 🚨 CRITICAL: ZERO BUILD WARNINGS POLICY

**MANDATORY WORKFLOW**: EVERY C# edit → `dotnet clean && dotnet build --no-restore --verbosity quiet` → fix ALL warnings → continue

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

## 🧪 TDD Workflow
**Red-Green-Refactor**: Write failing test → implement → refactor
**Before features**: Write failing TUnit test → `dotnet test` → implement → refactor
**Test naming**: `Should_Return_Valid_User_When_Id_Exists` (underscores only in tests)  
**Test behavior, not implementation**: Public interfaces/contracts only  
**Mock dependencies**: Constructor injection for isolation  
**Test structure**: Arrange-Act-Assert pattern

## 🎭 LightMock.Generator
**PRIMARY MOCKING FRAMEWORK** - NSubstitute deprecated, will be removed

**Mock naming**: Use `Mock` suffix: `var userServiceMock = new Mock<IUserService>();`
**Usage**: `new Mock<IInterface>()` → setup → pass `.Object` to constructor
**Benefits**: Compile-time generation, zero runtime overhead, AOT compatible

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
**JavaScript**: Minimal - prefer C#/Blazor, use `IJSRuntime.InvokeAsync<T>()`, NO JS for CSS  
**Accessibility**: WCAG 2.1 AA compliance, semantic HTML, ARIA roles  
**Performance**: Lazy loading, virtualization for large lists, optimize assets  
**Images**: WebP/AVIF, `loading="lazy"`, `srcset`

## 🔒 Security & API
**Input validation**: Always validate/sanitize | **XSS/CSRF**: Use Blazor built-ins | **Secrets**: Never expose in client | **API**: `IHttpClientFactory` only for actual HTTP needs | **CSP**: In `staticwebapp.config.json` - allows 'unsafe-inline' for styles, restricts scripts | **Azure Functions**: Isolated worker with DI | **Authentication**: ASP.NET Core Identity, RBAC

## ⚡ Modern C#
Primary Constructors: `public class Person(string name, int age)` | Collection Expressions: `int[] nums = [1,2,3];` | Default Lambda: `(x, y=5) => x+y` | Alias Types: `using IntPair = (int, int);` | params Collections: `params ReadOnlySpan<T>` | ref readonly: `void M(ref readonly int x)` | Inline Arrays: `[InlineArray(10)]` | New Lock: `var l = new Lock(); using(l.EnterScope())`

## 📁 File Organization
**Features**: `src/redmuffin.Blazor.StaticWeb/Features/` - Feature-based with pages/components  
**Static**: `src/redmuffin.Blazor.StaticWeb/wwwroot/` - CSS, SCSS, JS, sample data, libraries  
**Tests**: `tests/` - Mirror structure with TUnit projects  
**Scripts**: `scripts/` - PowerShell automation  
**Config**: `.github/` - Workflows, instructions, prompts, chatmodes

**Key dirs**: `.github/instructions/` (tech standards), `.github/prompts/` (AI prompts), `.github/chatmodes/` (AI modes), `src/.../Features/` (components), `src/.../Api/` (Azure Functions), `src/.../Common/` (shared)

## 🤖 AI Guidelines

### 🔄 Development Workflow
**MANDATORY**: After EVERY C# file change: `dotnet clean && dotnet build --no-restore --verbosity quiet` → Fix ALL warnings → Continue
**Pre-commit**: `dotnet test` must pass without errors (warnings OK) - stop commit if test errors exist
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
**Brave Search**: `brave_web_search`/`brave_local_search` (2,000 queries/month)  
**Sequential Thinking**: Complex problem-solving with dynamic adaptation

### 📍 Repository Info
**Owner**: `michaelvolz` | **Name**: `redmuffin.Blazor.StaticWeb`
**Encoding**: UTF8 with BOM for Markdown  
**Exclude samples**: `wwwroot/sample-data/markdown-cheat-sheet.md`, `wwwroot/Example.md`

### 📚 Tech Instructions (`.github/instructions/`)
**Core**: Blazor (`.razor`), C# (`.cs`), PowerShell (`.ps1`), SCSS (`.scss`), Markdown (`.md`)
**Architecture**: REST APIs, Azure Functions, GitHub Actions, Performance, Commit Standards

### 💡 Essential Examples
```csharp
// Blazor Component with full DI pattern
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

// TUnit Test with LightMock.Generator
[Test]
public async Task Should_Return_User_When_Valid_Id_Provided()
{
    // Arrange
    var httpClientMock = new Mock<HttpClient>();
    var loggerMock = new Mock<ILogger<UserService>>();
    var userService = new UserService(httpClientMock.Object, loggerMock.Object);
    
    // Act
    var result = await userService.GetUserAsync("valid-id");
    
    // Assert
    await Assert.That(result).IsNotNull();
}

// Component Testing with DI
[Test]
public void Should_Render_UserProfile_When_User_Loaded()
{
    using var ctx = new TestContext();
    var userServiceMock = new Mock<IUserService>();
    ctx.Services.AddSingleton(userServiceMock.Object);
    
    var component = ctx.RenderComponent<UserProfile>();
    await Assert.That(component.Find("h1").TextContent).Contains("Expected");
}
```

### ⭐ Best Practices
**TDD**: Test first, small steps, continuous refactoring, fast feedback, behavior-focused
**DI**: Dependency inversion, constructor injection, single responsibility, interface segregation  
**General**: Modular/reusable/testable components, strongly-typed parameters, handle exceptions with try/catch or `<ErrorBoundary>`, prefer C#/Blazor over JS, use `StateHasChanged()` sparingly, implement `IDisposable` for subscriptions/timers