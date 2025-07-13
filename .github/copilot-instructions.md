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

## Key Dependencies & Build Configuration
- **Blazored.LocalStorage**: Client-side storage for Blazor WebAssembly
- **Markdig**: Markdown parsing and rendering
- **Microsoft.Azure.Functions.Worker**: Azure Functions isolated worker model
- **TUnit**: Modern testing framework with `[Test]` and `[Arguments]` attributes
- **Zurb Foundation**: UI framework via CDN (libman.json)
- **FontAwesome**: Icons via CDN (libman.json)
- **BuildWebCompiler2022**: SCSS compilation (debug mode only)
- **Coverlet**: Code coverage collection with MSBuild integration

**Build Optimizations (Directory.Build.props):**
- **WebAssembly**: `WasmStripILAfterAOT=true`, `InvariantGlobalization=true`, `PublishTrimmed=true`
- **Security**: `CheckForOverflowUnderflow=true`, nullable reference types enabled
- **Analyzers**: Roslynator, StyleCop, Meziantou, VSThreading analyzers configured
- **C# Language**: Preview features enabled (`LangVersion=preview`)
- **Coverage**: Centralized exclusions for generated files and dependencies

## 1. UI & Styling
- **Framework:** Zurb Foundation for all UI/layout
- **Styles:** Place in `wwwroot/css` or `wwwroot/scss`
- **Responsive:** Foundation grid/utilities or custom CSS
- **Accessibility:** Semantic HTML, ARIA roles, keyboard navigation, WCAG 2.1 AA compliance, proper color contrast
- **Performance:** Optimize assets (bundling, minification), lazy loading, virtualization for large lists
- **Modern CSS:** Grid, Flexbox, variables, nesting, dark mode support
- **Images:** WebP/AVIF, `loading="lazy"`, `srcset`
- **JavaScript:** Minimal - prefer C#/Blazor, JS interop only when necessary
- **JS Interop:** Use `IJSRuntime.InvokeAsync<T>()`, dispose JS object references
- **Security:** Sanitize inputs, enforce CSP, secure cookies, RBAC

## 2. Security & API
- **Input Validation:** Always validate/sanitize user input
- **XSS/CSRF:** Use Blazor built-ins and best practices
- **Secrets:** Never expose in client code
- **API:** Use `IHttpClientFactory` for HTTP calls, prefer minimal APIs
- **CSP:** Configured in `staticwebapp.config.json` - allows 'unsafe-inline' for styles, restricts scripts
- **Azure Functions:** Use isolated worker model with dependency injection
- **Authentication:** ASP.NET Core Identity, role-based access control

## 3. Testing & Documentation
- **Unit Tests:** Use TUnit (NOT NUnit/xUnit), `[Test]` for methods, `[Tests]` with `[Arguments]` for data-driven
- **Code Coverage:** Coverlet + ReportGenerator with PowerShell automation (see AI Operational Guidelines section)
- **Documentation:** XML docs for public APIs, update README/Wiki/OpenAPI

## Code Examples

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
public async Task ShouldReturnExpectedResult()
{
    // Test
}

[Test]
[Arguments("input1", "expected1")]
[Arguments("input2", "expected2")]
public async Task ShouldHandleMultipleInputs(string input, string expected)
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

## 4. File & Directory Organization
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

## 5. Best Practices
- Develop modular, reusable, testable components
- Favor strongly-typed parameters over dynamic
- Handle exceptions with try/catch or error boundaries (`<ErrorBoundary>`)
- Reference code with filename and line numbers
- Prefer C#/Blazor over JavaScript/HTML unless required
- Keep builds and tests passing before merging
- Use `StateHasChanged()` sparingly, prefer parameter binding
- Implement `IDisposable` for event subscriptions and timers

## 6. AI Operational Guidelines

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

### Development Workflow
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
