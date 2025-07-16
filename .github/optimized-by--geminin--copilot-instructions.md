# AI Assistant Rules (Ultra-Compact)

**AI ONLY.** Humans: see `README.md`.

## 1. Tech & Arch
- **Stack**: .NET 9 Blazor WASM (Frontend) | .NET 8 Azure Functions (Backend) | .NET 9 Shared Lib.
- **Lang/Tools**: C# 13 (preview), TUnit, VS 2022.
- **Build**: Wasm optimizations ON (`WasmStripILAfterAOT`, `PublishTrimmed`).
- **Deploy**: Azure Static Web Apps w/ strict CSP.
- **Libs**: Blazored.LocalStorage, Markdig, TUnit, Zurb Foundation, FontAwesome.
- **Analyzers**: Roslynator, StyleCop, Meziantou, VSThreading (all mandatory).

## 2. Core Directives: TDD, DI, Standards

### TDD (Test-Driven Development)
- **Cycle**: Red (fail test) ? Green (pass test) ? Refactor.
- **Workflow**: Before coding, write a failing TUnit test, run `dotnet test` to confirm fail, code to pass, refactor.
- **Focus**: Test public APIs/contracts ONLY, not private implementation. Ensures refactor-safe tests.
- **Rules**: 1 behavior/test, name `Should_Do_X_When_Y`, use constructor injection for mocks.

### DI (Dependency Injection)
- **Pattern**: Constructor injection is mandatory. Validate all injected services for null.
- **Example**: `public MyService(ILogger l) => _logger = l ?? throw new ArgumentNullException(nameof(l));`
- **Blazor**: Use `[Inject]`, then `ArgumentNullException.ThrowIfNull(Service);` in `OnInitializedAsync`.
- **Rule**: Inject `IHttpClientFactory` only if service makes external HTTP calls.

### Code & UI Standards
- **C#**: `_privateFields`, `var` for obvious types, 160-char limit, always use `{}`.
- **UI**: Zurb Foundation for all layout.
- **SCSS ONLY**: **NEVER** use `.razor.css` or edit CSS. All styles in `_*.scss` partials in `wwwroot/scss/`, imported into `app.scss`.
- **JS**: Minimize. Prefer Blazor/C#. Use `IJSRuntime` only when no C# alternative exists.

## 3. Security & API
- **Security**: Sanitize all user input. No client-side secrets. Use ASP.NET Core Identity for Auth/RBAC.
- **API**: Use `IHttpClientFactory` for external HTTP. CSP is configured in `staticwebapp.config.json`.

## 4. Testing & Build Quality

### TUnit
- **Tests**: `[Test] public async Task Should_Do_Work()`, `[Test] [Arguments("in", "out")] public async Task Should_Handle(string i, string o)`
- **Components**: Use `TestContext` to render & mock: `ctx.Services.AddSingleton(mock.Object);`

### Build Warning Checklist (Enforced)
- **SA1402**: 1 type/file.
- **SA1208/10**: Order `using`s (System first, then alpha).
- **SA1201-14**: Member order: fields?props?ctors?methods.
- **MA0016**: Use `IEnumerable<T>` for public APIs.
- **MA0074**: Always use `StringComparison`.
- **MA0004/CA2007**: Use `.ConfigureAwait(false)` in libs.
- **MA0051**: Methods < 60 lines.
- **CS0219**: No unused variables.
- **CA1854**: Use `TryGetValue` for Dictionaries.
- **INFO**: Ignore `IL2111` warnings in `.g.cs` files (safe Blazor trimming artifact).

## 5. Files & AI Ops

### Dirs
- **Features**: `src/redmuffin.Blazor.StaticWeb/Features/`
- **Assets**: `src/redmuffin.Blazor.StaticWeb/wwwroot/`
- **Tests**: `tests/`
- **Config**: `.github/`

### AI Ops
- **Workflow**: TDD is mandatory. `dotnet test` before commit. Ask if unclear. Edit 1 file at a time.
- **Tools**: Use `resolve-library-id`?`get-library-docs`, `fetch`, `brave_search`, `sequentialthinking`.
- **Repo**: `michaelvolz/redmuffin.Blazor.StaticWeb`.
- **Files**: UTF-8 w/ BOM for `.md`. Ignore `wwwroot/sample-data/`.
- **Deep Dives**: See `.github/instructions/` for tech-specific rules.
