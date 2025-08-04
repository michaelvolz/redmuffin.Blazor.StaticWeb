# 🤖 AI-Optimized Technical Instructions

## 🚨 CRITICAL AI DIRECTIVES
- **ZERO BUILD WARNINGS MANDATE**: AFTER EVERY C# FILE CHANGE, RUN `dotnet clean && dotnet build --no-restore --verbosity quiet`. FIX ALL WARNINGS (EXCEPT IL2111) BEFORE CONTINUING. FAILURE CAUSES HUNDREDS OF ERRORS.
- **Local Testing**: Use `dotnet run` to start Blazor app on `localhost:5233` with mocked data. NEVER use Azure Static Web Apps emulator or PowerShell scripts.
- **Pre-Commit Testing**: Run `dotnet test` before committing. Ensure 100% test success and no build errors (except IL2111). Stop commit if errors exist.
- **File Editing**: Edit one file at a time, track progress (e.g., "Edit 2 of 5").
- **Unclear Items**: Ask questions and wait for answers before proceeding with ambiguous tasks.
- **Large Changes**: Outline plan, get approval, make incremental edits, ensure buildable state.
- **Batch Data Processing**: Process large datasets or text in smaller batches to prevent errors, file corruption, or performance issues. Avoid handling entire files at once unless explicitly requested.

## 🖥️ Project Configuration
- **Frontend**: Blazor WebAssembly (.NET 9), feature-based structure.
- **Backend**: Azure Functions (.NET 8), isolated worker.
- **Testing Framework**: TUnit with `[Test]` and `[Arguments]`. NEVER use xUnit, NUnit, or MSTest.
- **Mocking**: LightMock.Generator for external dependencies (`IHttpClientFactory`, `ILogger<T>`, etc.), custom mocks for internal components (`NavigationManager`, services).
- **Language**: C# 13 preview with WebAssembly optimizations.
- **Build Settings**: `WasmStripILAfterAOT=true`, `InvariantGlobalization=true`, `PublishTrimmed=true`.
- **Deployment**: Azure Static Web Apps with CSP and caching in `staticwebapp.config.json`.
- **Dependencies**: Blazored.LocalStorage, Markdig, Microsoft.Azure.Functions.Worker, TUnit, Zurb Foundation (CDN), FontAwesome (CDN), BuildWebCompiler2022 (SCSS), Coverlet, Analyzers (Roslynator, StyleCop, Meziantou, VSThreading).

## 📝 Coding Standards
- **Async Calls**: Use `ConfigureAwait(false)` on all async calls, except TUnit assert statements, to prevent deadlocks in WebAssembly.
  ```csharp
  // ✅ Correct
  var result = await service.GetAsync().ConfigureAwait(false);
  // ❌ Wrong in asserts
  await Assert.That(result).IsNotNull(); // No ConfigureAwait
  ```
- **Null Checks**: Use `ArgumentNullException.ThrowIfNull()` for all parameters.
  ```csharp
  public UserService(IHttpClientFactory factory) => _factory = factory ?? throw new ArgumentNullException(nameof(factory));
  ```
- **Using Statements**: Order: System first, then alphabetical.
- **Member Order**: Fields → Properties → Constructors → Methods; Public → Internal → Protected → Private.
- **IDisposable**: Implement for disposable fields, use `using` for resource disposal.
- **Logging**: Use `LoggerMessage` delegates, NEVER `Logger.LogError()`.
  ```csharp
  private static readonly Action<ILogger, Exception?> LogEvent = LoggerMessage.Define(LogLevel.Information, new EventId(1, "Event"), "Event occurred");
  ```
- **Whitespace**: Remove all trailing whitespace, maximum one blank line between members.
- **Analyzer Rules** (Zero Tolerance):
  - **StyleCop**:
    - SA1402: One type per file.
    - SA1208/1210: Order usings (System first, alphabetical).
    - SA1201-1214: Enforce member order.
    - SA1413: Trailing commas in multi-line initializers.
    - SA1028: No trailing whitespace.
    - SA1500: Opening brace on new line.
    - SA1507: No multiple blank lines.
    - SA1508: No blank line before closing brace.
  - **Meziantou**:
    - MA0016: Use `IEnumerable<T>`, `IList<T>` abstractions.
    - MA0002/0006/0074: Specify `StringComparison.OrdinalIgnoreCase`.
    - MA0048: File name must match type name.
    - MA0051: Methods <60 lines.
    - MA0053: Make class sealed when possible.
  - **Microsoft**:
    - CA1845: Use `AsSpan()` instead of `Substring()`.
    - CA1854: Use `TryGetValue` for Dictionary.
    - CA1869: Cache `JsonSerializerOptions` instances.
    - CA1848: Use `LoggerMessage` delegates.
    - CA2016: Forward `CancellationToken` parameters.
    - CA1805: Remove explicit default initialization.
    - CA1822: Mark members static when possible.
- **Permitted Warning**: IL2111 (Blazor WebAssembly `App_razor.g.cs` trimming) is safe to ignore.
- **Documentation Warnings**: Fix SA1623/SA1615 in Visual Studio if documented; skip if undocumented.
- **C# 13 Features**: Use primary constructors, collection expressions `[1,2,3]`, default lambda `(x, y=5) => x+y`, alias types `using IntPair = (int, int);`, params collections `params ReadOnlySpan<T>`, ref readonly, inline arrays `[InlineArray(10)]`, new Lock `var l = new Lock(); using(l.EnterScope())`.

## 🧩 Partial Class Organization
- **Blazor Components**: Split into `ComponentName.razor.cs` (logic, lifecycle, properties, events) and `ComponentName.Logging.cs` (`LoggerMessage` delegates).
  ```csharp
  // ComponentName.razor.cs
  public partial class Home : ComponentBase
  {
      [Inject] public required ILogger<Home> Logger { get; set; }
      private async Task HandleClickAsync()
      {
          LogButtonClicked(Logger, null);
      }
  }
  // ComponentName.Logging.cs
  public partial class Home
  {
      private static readonly Action<ILogger, Exception?> LogButtonClicked = LoggerMessage.Define(LogLevel.Information, new EventId(5, "ButtonClicked"), "Button clicked");
  }
  ```
- **Services**: Split into `ServiceName.cs` (logic, methods, properties) and `ServiceName.Logging.cs` (`LoggerMessage` delegates).
- **Tests**: Split into `TestClassName.cs` (`[Test]` methods) and `TestClassName.Helpers.cs` (TestScope, mocks, utilities in same partial class). NEVER create separate helper files.
- **File Naming**:
  - Components: `Home.razor.cs`, `Home.Logging.cs`.
  - Services: `UserService.cs`, `UserService.Logging.cs`.
  - Tests: `HomeTests.cs`, `HomeTests.Helpers.cs`.
  - Test files: Use `Component_Tests.cs` for new test files (e.g., `Home_Tests.cs`), incorporating an underscore for consistency with naming standards.
- **Migration Priorities**:
  - Components: Already follow partial class pattern.
  - Services: Migrate to `LoggerMessage` in partial classes.
  - Tests: Split large test files into main and helpers.
  - New Code: Follow partial class standards.

## 🧪 TDD and Testing Patterns
- **Workflow**: Red-Green-Refactor. Write failing TUnit test, run `dotnet test`, implement, refactor.
- **Test Naming**: `Component_Behavior_ExpectedOutcome` with underscores.
- **Test Categorization Rules**: Organize TUnit tests into partial class files for each test class (e.g., `HomeTests`) to enhance maintainability. Apply this decision matrix in order:
  - **File Structure**:
    - `[TestClass].cs`: Basic functionality tests.
    - `[TestClass].EdgeCases.cs`: Error handling and edge case tests.
    - `[TestClass].Infrastructure.cs`: Framework and system-level tests.
    - `[TestClass].Behavior.cs`: User interaction and workflow tests.
    - `[TestClass].Helpers.cs`: TestScope, mocks, utilities (per existing pattern).
  - **Decision Flow**:
    1. **[TestClass].EdgeCases.cs**:
       - Test name includes: `Error`, `Exception`, `Fail`, `Invalid`, `Null`, `Empty`, `Timeout`, `Malformed`, `Corrupt`.
       - Uses: `Assert.Throws`, `ThrowsAsync`, `SetException`, `HttpRequestException`, `InvalidOperationException`.
       - Setup includes: `CreateFailing*`, `WithFailing*`, `SetupFailure`, `SetupException`, `SetupThrows`.
       - Validates: Error messages, exception handling, fallback behavior, graceful degradation.
       - Inputs: Null values, empty collections, invalid data, extreme values.
    2. **[TestClass].Infrastructure.cs**:
       - Test name includes: `Lifecycle`, `Logging`, `Cache`, `Auth`, `DI`, `JSInterop`, `Serializ`, `Disposal`, `Memory`, `Event`.
       - Validates: `OnInitialized`, `OnParametersSet`, `OnAfterRender`, `StateHasChanged`, `Dispose`.
       - Checks: Log entries, event IDs, authentication state, dependency injection, JS calls.
       - Uses: `CascadingValue`, `AuthenticationState`, `JSInterop`, `LocalStorage`, cache services.
       - Focuses on: Framework behavior, system integration, resource management.
    3. **[TestClass].Behavior.cs**:
       - Test name includes: `Click`, `Submit`, `Change`, `Interaction`, `Workflow`, `Concurrent`, `Multiple`, `Rapid`.
       - Uses: `ClickAsync`, `ChangeAsync`, `TriggerEventAsync`, `MouseEventArgs`, `ChangeEventArgs`.
       - Performs: User interactions, form submissions, button clicks, input changes.
       - Validates: State transitions, user workflows, interactive behavior.
       - Setup: Multiple operations, concurrent tasks, user simulation.
    4. **[TestClass].cs** (Default):
       - Covers: Basic rendering, simple property validation, "happy path" scenarios, structure verification, default state validation.
  - **Code Structure**:
    - Use same namespace (e.g., `Tests.[ProjectName].Features`) and `partial class [TestClass]` declaration across all files.
    ```csharp
    namespace Tests.[ProjectName].Features;
    partial class HomeTests
    {
        // Test methods for specific category
    }
    ```
  - **Examples**:
    - `HomeTests.EdgeCases.cs`: `Should_Handle_Null_Input_Gracefully`, `Should_Throw_ArgumentException_When_Invalid`, `Should_Display_Error_When_API_Fails`.
    - `HomeTests.Infrastructure.cs`: `Should_Log_Initialization_Events`, `Should_Dispose_Resources_Properly`, `Should_Handle_Authentication_State`.
    - `HomeTests.Behavior.cs`: `Should_Submit_Form_When_Button_Clicked`, `Should_Handle_Concurrent_Operations`, `Should_Update_State_On_Input_Change`.
    - `HomeTests.cs`: `Should_Render_Successfully`, `Should_Display_Correct_Title`, `Should_Have_Required_Elements`.
  - **Override Rule**: If a test fits multiple categories, prioritize: 1. EdgeCases, 2. Infrastructure, 3. Behavior, 4. Main.
- **Minimal Component Tests**: Write only essential TUnit tests for simple components (e.g., buttons). Avoid overengineering with excessive or redundant tests.
- **Test Structure**: Arrange-Act-Assert with comments.
- **TestScope Architecture**: Use `TestScope` with primary constructor, fluent methods, and `IDisposable`.
  ```csharp
  public sealed class TestScope(string baseUri = "http://localhost:5233/") : IDisposable
  {
      public TestContext Context { get; } = new();
      public NavigationManagerMock NavigationManager { get; } = new(baseUri);
      public TestLogger<T> Logger { get; } = new();
      public TestScope WithStandardServices() { /* Setup */ return this; }
      public void Dispose() => Context?.Dispose();
  }
  private static TestScope CreateTestScope() => new TestScope().WithStandardServices();
  - Use TUnit’s `TestContext` for debug output in tests to ensure visibility in test output. Avoid other methods.
- **TUnit Assertions**:
  - Chain related assertions: `await Assert.That(markup).IsNotNull().And.Contains("expected")`.
  - Use `Assert.Multiple` for unrelated concerns:
    ```csharp
    using (Assert.Multiple())
    {
        await Assert.That(component.Find("h1")).IsNotNull();
        await Assert.That(logger.LogEntries.Any(e => e.Message.Contains("logged"))).IsTrue();
    }
    ```
  - NEVER use `ConfigureAwait(false)` in asserts.
- **Test Quality Checklist**:
  - Use `ConfigureAwait(false)` on async calls (except asserts).
  - Use TestScope with fluent configuration.
  - Follow AAA structure.
  - Ensure single responsibility.
  - Comply with zero build warnings.
  - Use `using` for resource disposal.
  - Test error scenarios.
  - Follow partial class structure.

## 🎭 Mocking Strategy
- **LightMock.Generator**: For external dependencies. Use `Mock` suffix, setup with `.Arrange()`, pass `.Object`.
  ```csharp
  var httpClientMock = new Mock<IHttpClientFactory>();
  httpClientMock.Arrange(f => f.CreateClient(The<string>.IsAnyValue)).Returns(new HttpClient());
  ```
- **Custom Mocks**: For internal components. Use sealed classes with primary constructors.
  ```csharp
  public sealed class NavigationManagerMock(string baseUri) : NavigationManager
  {
      public string? NavigatedTo { get; private set; }
      protected override void NavigateToCore(string uri, NavigationOptions options) => NavigatedTo = uri;
  }
  ```
- **Optional Parameters**: Specify all parameters explicitly (e.g., `CancellationToken.None`, `null`, `The<T>.IsAnyValue`).

## 💉 Dependency Injection
- **Constructor Injection**: Required with null validation.
  ```csharp
  public UserService(IHttpClientFactory factory, ILogger<UserService> logger)
  {
      _factory = factory ?? throw new ArgumentNullException(nameof(factory));
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }
  ```
- **Blazor Components**: Use `[Inject]` with `default!`, validate in `OnInitializedAsync`.
  ```csharp
  public partial class UserProfile : ComponentBase
  {
      [Inject] private IUserService UserService { get; set; } = default!;
      protected override async Task OnInitializedAsync() => ArgumentNullException.ThrowIfNull(UserService);
  }
  ```
- **HttpClient**: Only inject when HTTP requests are needed.
- **Service Lifetimes**: Singleton (shared), Scoped (per request/circuit), Transient (new each time).

## 🎨 UI and Styling
- **Framework**: Zurb Foundation (CDN).
- **Styling**: Use SCSS in `wwwroot/scss/`, NEVER modify CSS or use `.razor.css`. Partials start with `_`, included in `app.scss`.
- **SCSS Build**: Use `Debug-Sass` with `dotnet build --configuration Debug-Sass` and BuildWebCompiler2022.
- **JavaScript**: Minimize, prefer C#/Blazor, use `IJSRuntime.InvokeAsync<T>()`, avoid JS for CSS.
- **Accessibility**: Ensure WCAG 2.1 AA, semantic HTML, ARIA roles.
- **Performance**: Use lazy loading, virtualization, WebP/AVIF, `loading="lazy"`, `srcset`.

## 🔒 Security and API
- **Input Validation**: Always validate/sanitize inputs.
- **XSS/CSRF**: Use Blazor built-in protections.
- **Secrets**: Never expose in client-side code.
- **API**: Use `IHttpClientFactory` for HTTP needs.
- **CSP**: Configure in `staticwebapp.config.json`, allows ‘unsafe-inline’ for styles, restricts scripts.
- **Azure Functions**: Use isolated worker with dependency injection.
- **Authentication**: Use ASP.NET Core Identity with RBAC.

## 📁 File Organization
- **Features**: `src/[ProjectName]/Features/` for components and pages.
- **Static Assets**: `src/[ProjectName]/wwwroot/` for CSS, SCSS, JS, sample data, libraries.
- **Tests**: `tests/` mirroring source structure with TUnit projects.
- **Test Project Naming**: Name all test projects `[ProjectName].Tests` (e.g., `MyApp.Tests`) to maintain consistent solution structure.
- **Scripts**: `scripts/` for PowerShell automation.
- **Config**: `.github/` for workflows, instructions, prompts, chatmodes.
- **Key Directories**: `.github/instructions/` (standards), `.github/prompts/` (AI prompts), `.github/chatmodes/` (AI modes), `src/[ProjectName]/Api/` (Azure Functions), `src/[ProjectName]/Common/` (shared).
- **PRD File Location**: Locate all PRDs, PRD-TaskLists, and PRD-ToDos in the `tasks/` folder. Files use the format `PRD-XXX` (XXX is 000–999) to group related documents.

## 📝 Markdown Standards
- **Compliance**: Follow `.github/instructions/markdown.instructions.md` with MarkdownLint rules (MD001-MD059).
- **Configuration**: Use `.markdownlint.jsonc` for auto-fix.
- **VS Code**: Enable auto-format, lint workspace, fix violations.
- **Structure**: Proper heading hierarchy, consistent formatting, aligned tables, validated links.
- **Encoding**: UTF8 without BOM.
- **Excluded Files**: Ignore `wwwroot/sample-data/markdown-cheat-sheet.md`, `wwwroot/Example.md`.

## 🛠️ Tools and Coverage
- **Coverage**: Use `scripts/Generate-CoverageReport.ps1`, `scripts/View-CoverageReport.ps1`. Outputs HTML, XML, JSON, Cobertura to `coverage/`.
- **Configuration**: Coverlet MSBuild, exclusions in `.coverletrc`/`Directory.Build.props`.
- **PowerShell Script Usage**: To address incomplete AI responses in large or complex codebases, use PowerShell scripts when:
  1. **Scale**: Task spans numerous files or extensive code (e.g., renaming hundreds of variables across files).
     - *Why*: Scripts save time on large tasks.
  2. **Repetition**: Actions are repeated across multiple locations (e.g., applying naming conventions).
     - *Why*: Automation reduces errors in repetitive tasks.
  3. **Consistency**: Uniform standards are required (e.g., enforcing file naming).
     - *Why*: Scripts ensure consistent application.
  4. **Complexity**: Logic exceeds manual or basic tool capabilities (e.g., dependency checks).
     - *Why*: Scripts handle intricate logic efficiently.
  5. **Efficiency**: Automation significantly outperforms other methods (e.g., analyzing large logs).
     - *Why*: Scripts improve speed and accuracy.
  Reserve scripts for these scenarios, avoiding them for simple tasks where manual methods suffice. If unsure, ask for clarification to ensure the best approach.
- **Build Verification**: Use `run_build` tool.
- **MCP Server Usage**: Use the following MCP servers for specific tasks:
  - `github`: Automate GitHub API tasks (e.g., repo creation, PRs).
  - `puppeteer`: Scrape JavaScript-rendered pages (e.g., dynamic content).
  - `fetch`: Scrape static content or convert URLs to Markdown (e.g., documentation).
  - `brave-search`: Conduct web searches with `brave_web_search` (2,000 queries/month) (e.g., find APIs).
  - `time`: Handle time-related tasks (e.g., log build times).
  - `context7`: Process HTTP-based context (e.g., analyze code context, resolve library IDs, get library docs).
  - `sequentialthinking`: Solve complex problems (e.g., optimize algorithms).

## 📚 General Best Practices
- **TDD**: Test first, small steps, continuous refactoring, behavior-focused.
- **DI**: Dependency inversion, constructor injection, single responsibility, interface segregation.
- **Testing**: Use TestScope, TUnit chaining, test error scenarios, zero warnings, partial class structure.
- **General**: Create modular, reusable, testable components; use strongly-typed parameters; handle exceptions with try/catch or `<ErrorBoundary>`; prefer C#/Blazor over JS; use `StateHasChanged()` sparingly; implement `IDisposable` for subscriptions/timers.
- **Important Rules** never say any form of : you’re absolutely right. Always ultrathink. Give shorter explanation possible when asked