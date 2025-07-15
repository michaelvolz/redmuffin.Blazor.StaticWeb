# AI Code Assistant Instructions

**FOR AI CODE ASSISTANTS ONLY** - This file contains technical guidelines and tool information specifically for AI assistants. Human developers should refer to README.md for project documentation.

## Project Architecture Overview
- **Frontend**: Blazor WebAssembly (.NET 9) with feature-based organization
- **Backend**: Azure Functions (.NET 8) using isolated worker model
- **Shared**: Common library (.NET 9) for models and utilities
- **IDE**: Visual Studio 2022 with .NET 9 SDK
- **Language**: C# 13 (preview features enabled)
- **Testing**: TUnit framework (NOT xUnit/NUnit/MSTest)
- **Build**: WebAssembly optimizations enabled via Directory.Build.props
- **Deployment**: Azure Static Web Apps with CSP and caching configurations

## Key Dependencies 

- **Blazored.LocalStorage**: Client-side storage for Blazor WebAssembly
- **Markdig**: Markdown parsing and rendering
- **Microsoft.Azure.Functions.Worker**: Azure Functions isolated worker model
- **TUnit**: Modern testing framework with `[Test]` and `[Arguments]` attributes
- **Zurb Foundation**: UI framework via CDN (libman.json)
- **Roslynator Analyzers**: Provides refactorings, analyzers, and code fixes.
- **StyleCop Analyzers**: Focuses on style and consistency rules for C# development.
- **Meziantou Analyzers**: Adds diagnostics for performance, security, and best practices.
- **VSThreading Analyzers**: Encourages best practices for multithreading and async operations.
- **FontAwesome**: Icons via CDN (libman.json)
- **BuildWebCompiler2022**: SCSS compilation (debug mode only)
- **Coverlet**: Code coverage collection with MSBuild integration

**Build Optimizations (Directory.Build.props):**

- **WebAssembly**: `WasmStripILAfterAOT=true`, `InvariantGlobalization=true`, `PublishTrimmed=true`
- **Security**: `CheckForOverflowUnderflow=true`, nullable reference types enabled
- **Analyzers**: Comprehensive integration of Roslynator, StyleCop, Meziantou, and VSThreading analyzers
- **C# Language**: Preview features enabled (`LangVersion=preview`)
- **Coverage**: Centralized exclusions for generated files and dependencies

## 1. Test-Driven Development (TDD)

### TDD Cycle (Red-Green-Refactor)
- **Red:** Write a failing test first that defines desired behavior
- **Green:** Write minimal code to make the test pass
- **Refactor:** Improve code quality while keeping tests green

### TDD Workflow for AI Assistants
1. **Before any new feature/method:**
   - Write failing TUnit test(s) first
   - Run `dotnet test` to confirm failure (Red)
   - Implement minimal code to pass test (Green)
   - Refactor for quality/performance while tests remain green

2. **TDD Best Practices:**
   - One test per behavior/requirement
   - Tests should be independent and isolated
   - Use descriptive test names with underscores (e.g., `Should_Return_Valid_User_When_Id_Exists`)
   - Test edge cases and error conditions
   - Mock external dependencies using constructor injection

3. **Test Behavior, Not Implementation:**
   - **Test the Contract**: Focus on testing public interfaces, parameters, and return values
   - **Avoid Internal Logic**: Do not test private methods or internal implementation details
   - **Stable Over Time**: Test what is stable (public API) rather than what changes frequently (internal logic)
   - **Refactor-Safe Tests**: Tests should survive refactoring when behavior remains unchanged
   - **Design for Testability**: If code cannot be validated through public interface, refactor to make it more testable

### TDD with Blazor Components
- Test component parameters, events, and rendering
- Use `TestContext` for component testing
- Mock services injected into components
- Test user interactions and state changes

## 2. Dependency Injection Best Practices

### Constructor Injection (Preferred)
```csharp
public class UserService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UserService> _logger;

    public UserService(IHttpClientFactory httpClientFactory, ILogger<UserService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}
```

**Important:** Only inject HttpClient/IHttpClientFactory when the service actually needs to make HTTP requests. Do not include these dependencies unless there is a genuine requirement for external API calls or HTTP communication.

### Blazor Component DI Pattern
```csharp
public partial class UserProfile : ComponentBase
{
    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private ILogger<UserProfile> Logger { get; set; } = default!;
    [Inject] private ILocalStorageService LocalStorage { get; set; } = default!;
    
    // Always validate injected services in OnInitialized if critical
    protected override async Task OnInitializedAsync()
    {
        ArgumentNullException.ThrowIfNull(UserService);
        // Component logic
    }
}
```

### Service Registration Patterns
- **Singleton:** `services.AddSingleton<IService, Service>()` - Shared instance
- **Scoped:** `services.AddScoped<IService, Service>()` - Per request/circuit
- **Transient:** `services.AddTransient<IService, Service>()` - New instance each time

### DI Guidelines for AI Assistants
1. **Always prefer constructor injection** over property/method injection
2. **Validate constructor parameters** with null checks or ArgumentNullException.ThrowIfNull
3. **Use interfaces** for all injected dependencies to enable testing
4. **Avoid service locator pattern** - inject what you need directly
5. **Design for testability** - constructor injection enables easy mocking

## 3. Coding Standards
- **Private Fields:** Use `_` prefix for private fields
- **var Usage:** Only when type is clearly apparent (e.g., `var items = new List<string>()`)
- **Line Length:** 160 characters maximum
- **Braces:** Always use braces, even for single-line statements

## 4. UI & Styling
- **Framework:** Zurb Foundation for all UI/layout
- **SCSS Only:** All styles must be implemented in SCSS files in `wwwroot/scss/` folder
- **CSS Files:** Never modify CSS files directly - they are auto-generated from SCSS
- **Component Styles:** Use `.razor.css` files for component-scoped styles (these are NOT auto-generated)
- **SCSS Partials:** All SCSS partial files must start with an underscore (_) and be included in `app.scss` for automatic compilation
- **SCSS Compilation:** Files are automatically compiled when included in `app.scss` - no manual compilation needed
- **Responsive:** Foundation grid/utilities or custom SCSS
- **Accessibility:** Semantic HTML, ARIA roles, keyboard navigation, WCAG 2.1 AA compliance, proper color contrast
- **Performance:** Optimize assets (bundling, minification), lazy loading, virtualization for large lists
- **Modern CSS:** Grid, Flexbox, variables, nesting, dark mode support
- **Images:** WebP/AVIF, `loading="lazy"`, `srcset`
- **JavaScript:** Minimal - prefer C#/Blazor, JS interop only when necessary
- **JS Interop:** Use `IJSRuntime.InvokeAsync<T>()`, dispose JS object references
- **Security:** Sanitize inputs, enforce CSP, secure cookies, RBAC

## 5. Security & API
- **Input Validation:** Always validate/sanitize user input
- **XSS/CSRF:** Use Blazor built-ins and best practices
- **Secrets:** Never expose in client code
- **API:** Use `IHttpClientFactory` for HTTP calls only when actually needed for external API communication, prefer minimal APIs
- **CSP:** Configured in `staticwebapp.config.json` - allows 'unsafe-inline' for styles, restricts scripts
- **Azure Functions:** Use isolated worker model with dependency injection
- **Authentication:** ASP.NET Core Identity, role-based access control

## 6. Testing & Documentation (Enhanced with TDD)

### TDD-First Development
- **Write tests before implementation** using Red-Green-Refactor cycle
- **Test structure:** Arrange-Act-Assert pattern
- **Mock dependencies** using constructor injection for isolation
- **Test naming:** Use underscores in test method names for readability: `Should_Return_True_If_UserId_Exists` format (underscores only in test methods, not in production code)
- **Test Behavior, Not Implementation:** Focus on testing public contracts and interfaces, not internal logic or private methods

### TUnit Testing Patterns with DI
```csharp
[Test]
public async Task Should_Return_User_When_Valid_Id_Provided()
{
    // Arrange
    var mockHttpClient = new Mock<HttpClient>();
    var mockLogger = new Mock<ILogger<UserService>>();
    var userService = new UserService(mockHttpClient.Object, mockLogger.Object);
    var userId = "valid-id";

    // Act
    var result = await userService.GetUserAsync(userId);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be(userId);
}

[Test]
[Arguments(null)]
[Arguments("")]
[Arguments("   ")]
public async Task Should_Throw_Argument_Exception_When_Invalid_Id_Provided(string invalidId)
{
    // Arrange
    var mockHttpClient = new Mock<HttpClient>();
    var mockLogger = new Mock<ILogger<UserService>>();
    var userService = new UserService(mockHttpClient.Object, mockLogger.Object);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentException>(() => userService.GetUserAsync(invalidId));
}
```

### Component Testing with DI
```csharp
[Test]
public void Should_Render_UserProfile_When_User_Loaded()
{
    // Arrange
    using var ctx = new TestContext();
    var mockUserService = new Mock<IUserService>();
    mockUserService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(new User { Name = "John" });
    ctx.Services.AddSingleton(mockUserService.Object);

    // Act
    var component = ctx.RenderComponent<UserProfile>();

    // Assert
    component.Find("h1").TextContent.Should().Contain("John");
}
```

- **Code Coverage:** Coverlet + ReportGenerator with PowerShell automation (see AI Operational Guidelines section)
- **Documentation:** XML docs for public APIs, update README/Wiki/OpenAPI

## Code Examples

**TDD Example with Dependency Injection:**
```csharp
// 1. RED: Write failing test first
[Test]
public async Task Should_Validate_User_Credentials()
{
    // Arrange
    var mockAuthService = new Mock<IAuthService>();
    var mockLogger = new Mock<ILogger<LoginComponent>>();
    var loginComponent = new LoginComponent(mockAuthService.Object, mockLogger.Object);
    
    // Act
    var result = await loginComponent.ValidateLoginAsync("user", "pass");
    
    // Assert
    result.Should().BeTrue();
}

// 2. GREEN: Implement minimal code to pass
public partial class LoginComponent : ComponentBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<LoginComponent> _logger;
    
    public LoginComponent(IAuthService authService, ILogger<LoginComponent> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<bool> ValidateLoginAsync(string username, string password)
    {
        return await _authService.ValidateAsync(username, password);
    }
}

// 3. REFACTOR: Improve while keeping tests green
```

**Blazor Component Structure:**
```csharp
// Features/Pages/ExamplePage/Example.razor.cs
public partial class Example : ComponentBase
{
    [Inject] private ILocalStorageService LocalStorage { get; set; } = default!;
    [Parameter] public string Title { get; set; } = string.Empty;
    
    protected override async Task OnInitializedAsync()
    {
        // Component initialization
    }
}
```

**TUnit Test Pattern:**
```csharp
[Test]
public async Task Should_Return_Expected_Result()
{
    // Test
}

[Test]
[Arguments("input1", "expected1")]
[Arguments("input2", "expected2")]
public async Task Should_Handle_Multiple_Inputs(string input, string expected)
{
    // Data-driven test
}
```

**Azure Function Pattern:**
```csharp
[Function("FunctionName")]
public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
{
    // Function implementation
}
```

## 7. Modern C# Features (12/13)
| Feature | Example |
|---------|---------|
| Primary Constructors | `public class Person(string name, int age) { ... }` |
| Collection Expressions | `int[] nums = [1,2,3];` |
| Default Lambda Params | `Func<int,int,int> add = (x, y=5) => x+y;` |
| ref readonly Parameters | `void M(ref readonly int x) { ... }` |
| Alias Any Type | `using IntPair = (int, int);` |
| Inline Arrays | `[InlineArray(10)] struct Buffer { ... }` |
| params Collections | `void M(params ReadOnlySpan<T> items) { ... }` |
| New Lock Object | `var l = new Lock(); using(l.EnterScope()) { ... }` |
| New Escape Sequence | `char esc = '\e';` |
| Method Group Natural Type | `var act = (string s) => ...;` |
| Implicit Index Access | `buffer = { [^1]=0 }` |
| ref/unsafe in Iterators | `async Task M() { ref int x = ...; }` |
| Partial Properties | `public partial string Name { get; set; }` |
| Overload Priority | `[OverloadResolutionPriority(1)] void M(int a) {}` |

## 8. File & Directory Organization
- **Features:** `src/redmuffin.Blazor.StaticWeb/Features/` - Feature-based organization with pages and components
- **Static Assets:** `src/redmuffin.Blazor.StaticWeb/wwwroot/` - CSS, SCSS, JS, sample data, and libraries
- **Tests:** `tests/` - Mirror main project structure with TUnit test projects
- **Scripts:** `scripts/` - PowerShell scripts for build, coverage, and utilities
- **Configuration:** `.github/` - GitHub workflows, instructions, prompts, and chatmodes

**Key Directories:**
- `.github/instructions/` - Technology-specific coding standards and best practices
- `.github/prompts/` - Reusable AI prompts for development workflows
- `.github/chatmodes/` - Specialized AI assistant modes for different roles
- `src/redmuffin.Blazor.StaticWeb/Features/` - Feature-based Blazor components and pages
- `src/redmuffin.Blazor.StaticWeb.Api/` - Azure Functions backend API
- `src/redmuffin.Blazor.StaticWeb.Common/` - Shared models and utilities
- `tests/` - TUnit test projects mirroring source structure

## 9. Best Practices (Enhanced)

### TDD Principles
- **Test First:** Write tests before implementation
- **Small Steps:** Make minimal changes to pass tests
- **Continuous Refactoring:** Improve code while tests remain green
- **Fast Feedback:** Keep test execution time minimal
- **Behavior-Focused:** Test public contracts and interfaces, not internal implementation details

### Dependency Injection Principles
- **Dependency Inversion:** Depend on abstractions, not concretions
- **Constructor Injection:** Preferred method for required dependencies
- **Single Responsibility:** Each service should have one reason to change
- **Interface Segregation:** Create small, focused interfaces

### Integration Guidelines
- Combine TDD with DI for highly testable code
- Mock external dependencies in unit tests
- Use integration tests to verify DI container configuration
- Design services with constructor injection for easy mocking

### General Best Practices
- Develop modular, reusable, testable components
- Favor strongly-typed parameters over dynamic
- Handle exceptions with try/catch or error boundaries (`<ErrorBoundary>`)
- Reference code with filename and line numbers
- Prefer C#/Blazor over JavaScript/HTML unless required
- Keep builds and tests passing before merging
- Use `StateHasChanged()` sparingly, prefer parameter binding
- Implement `IDisposable` for event subscriptions and timers

## 10. AI Operational Guidelines

### Known Build Warnings

**IL2111 Warnings (Expected and Safe to Ignore):**
The following IL2111 warnings are expected during Blazor WebAssembly compilation and do not indicate issues with the code:

```
warning IL2111: Method 'Microsoft.AspNetCore.Components.LayoutView.Layout.set' with parameters or return value with `DynamicallyAccessedMembersAttribute` is accessed via reflection. Trimmer can't guarantee availability of the requirements of the method.
```

**Context:** These warnings occur in generated Razor files (`App_razor.g.cs`) and are related to Blazor's internal layout handling mechanism. They are:
- **Safe to ignore** - Do not affect application functionality
- **Expected behavior** - Part of Blazor's compilation process
- **Generated code** - Not under developer control
- **Framework-level** - Related to ASP.NET Core Components trimming optimization

**Action:** No action required. These warnings can be safely ignored during development and deployment.

### Technology-Specific Instructions
Consult these instruction files based on the file types you're working with:

**Core Technologies:**
- **Blazor** (`*.razor`, `*.razor.cs`, `*.razor.css`) → [Blazor.instructions.md](.github/instructions/Blazor.instructions.md)
- **C#** (`*.cs`) → [CSharp.instructions.md](.github/instructions/CSharp.instructions.md)
- **PowerShell** (`*.ps1`, `*.psm1`, `*.psd1`) → [Powershell.instructions.md](.github/instructions/Powershell.instructions.md)
- **SCSS** (`*.scss`) → [_SCSS.instructions.md](.github/instructions/_SCSS.instructions.md)
- **Markdown** (`*.md`) → [markdown.instructions.md](.github/instructions/markdown.instructions.md)

**Architecture & APIs:**
- **REST APIs** (`*.cs`, `*.json`) → [aspnet-rest-apis.instructions.md](.github/instructions/aspnet-rest-apis.instructions.md)
- **Azure Functions** → [_AzureFunctionsProgrammingBestPractices.instructions.md](.github/instructions/_AzureFunctionsProgrammingBestPractices.instructions.md)

**Development Workflow:**
- **GitHub Actions** (all files) → [github-actions-ci-cd-best-practices.instructions.md](.github/instructions/github-actions-ci-cd-best-practices.instructions.md)
- **Performance** (all files) → [performance-optimization.instructions.md](.github/instructions/performance-optimization.instructions.md)
- **Commit Standards** → [_CommitStandars.instructions.md](.github/instructions/_CommitStandars.instructions.md)
- **General Documentation Resources** → [_Documentation.instructions.md](.github/instructions/_Documentation.instructions.md)

### TDD Workflow for AI Assistants
1. **Before implementing any new feature:**
   - Ask: "What should this feature do?" (requirements clarification)
   - Write failing test(s) that define expected behavior
   - Run `dotnet test` to confirm red state
   - Implement minimal code to achieve green state
   - Refactor while maintaining green state

2. **When modifying existing code:**
   - Ensure existing tests pass before changes
   - Add new tests for new behaviors
   - Refactor with confidence knowing tests will catch regressions

3. **Testing Strategy:**
   - Unit tests for business logic with mocked dependencies
   - Integration tests for component interactions
   - End-to-end tests for critical user flows
   - Always test edge cases and error conditions
   - **Focus on Behavior:** Test public interfaces, parameters, and return values - not internal implementation
   - **Refactor-Safe Tests:** Write tests that survive refactoring when public behavior remains unchanged

### Dependency Injection Guidelines
1. **Service Design:** Design services with single responsibility
2. **Interface Segregation:** Create focused interfaces for better testability
3. **Lifecycle Management:** Choose appropriate service lifetimes
4. **Testing:** Always design services with constructor injection for easy mocking

### Development Workflow
- **Pre-commit Testing:** Before any git commit, run `dotnet test` and ensure it passes without errors (warnings are acceptable)
  - If test errors exist, the commit must be stopped until errors are resolved
  - This ensures code quality and prevents breaking changes in the repository
- When asked to git commit something do it in batches for SRP to produce the best possible commit messages
- When anything is unclear ask questions and stop and wait for all the answers before continuing
- Offer PowerShell scripts for complex/data-intensive tasks
- **Code Coverage Automation:** PowerShell scripts for test coverage
  - Generate: `scripts/Generate-CoverageReport.ps1` | View: `scripts/View-CoverageReport.ps1`
  - Config: Coverlet MSBuild integration, exclusions in .coverletrc/Directory.Build.props
  - Output: HTML (unified/branded), XML, JSON, Cobertura formats to `coverage/`
- **Context7 MCP:** Up-to-date documentation retrieval
  - Tools: `resolve-library-id` → `get-library-docs` (always call resolve first)
  - Prioritizes current docs over training data for .NET/Blazor/Azure/TUnit
  - Auto-detects libraries in conversation for relevant documentation
- **Fetch MCP:** Web content retrieval
  - Converts URLs to markdown (HTML/JSON/MD/text formats)
  - Use for: API docs, GitHub repos, tutorials, specifications
  - Handles content truncation and pagination
- **Brave Search MCP:** Web and local search
  - Tools: `brave_web_search` (general), `brave_local_search` (businesses)
  - Params: query (required), count (max 20), offset (max 9), auto-fallback
  - Use for: Recent info, news, API changes | Free tier: 2,000 queries/month
- **Sequential Thinking MCP:** Structured problem-solving
  - Tool: `sequentialthinking` with dynamic adaptation and revision capability
  - Use for: Complex coding, architecture planning, debugging, feature design
  - Can adjust total thoughts and revise previous steps as understanding evolves
- **Workflow:**
  - Edit one file at a time to avoid conflicts
  - For large changes: outline plan, get approval, make incremental edits
  - Track progress (e.g., "Edit 2 of 5"), pause for clarification when blocked
  - Keep code buildable at each stage
- **Standards:** Follow all coding/testing practices, prefer Blazor over JavaScript
- **Repository:** Owner: `michaelvolz`, Name: `redmuffin.Blazor.StaticWeb`
- **File Encoding**: Always use UTF8 with BOM for Markdown files
- **Exclude sample files**: Don't use these files when working, they are only dummy files:
    - `src\redmuffin.Blazor.StaticWeb\wwwroot\sample-data\markdown-cheat-sheet.md`
    - `src\redmuffin.Blazor.StaticWeb\wwwroot\Example.md `
