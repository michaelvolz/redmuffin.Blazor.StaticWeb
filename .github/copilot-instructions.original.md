# AI Code Assistant Instructions

**FOR AI CODE ASSISTANTS ONLY** - Technical guidelines for AI assistants. Human developers should refer to README.md.

## Project Overview
- **Frontend**: Blazor WebAssembly (.NET 9)
- **Backend**: Azure Functions (.NET 8)
- **Shared**: Common library (.NET 9)
- **IDE**: Visual Studio 2022
- **Language**: C# 13 (preview features enabled)
- **Testing**: TUnit framework
- **Build**: WebAssembly optimizations
- **Deployment**: Azure Static Web Apps

## Key Dependencies
- **Blazored.LocalStorage**: Client-side storage
- **Markdig**: Markdown parsing
- **Microsoft.Azure.Functions.Worker**: Azure Functions
- **TUnit**: Testing framework
- **Zurb Foundation**: UI framework
- **Analyzers**: Roslynator, StyleCop, Meziantou, VSThreading
- **FontAwesome**: Icons
- **BuildWebCompiler2022**: SCSS compilation
- **Coverlet**: Code coverage

## Build Optimizations
- **WebAssembly**: `WasmStripILAfterAOT=true`, `InvariantGlobalization=true`, `PublishTrimmed=true`
- **Security**: `CheckForOverflowUnderflow=true`, nullable reference types enabled
- **Analyzers**: Integrated Roslynator, StyleCop, Meziantou, VSThreading
- **Coverage**: Centralized exclusions

## Test-Driven Development (TDD)
### Workflow
1. Write failing TUnit test(s).
2. Run `dotnet test` to confirm failure.
3. Implement minimal code to pass.
4. Refactor while keeping tests green.

### Best Practices
- One test per behavior.
- Use descriptive test names.
- Test edge cases and errors.
- Mock dependencies.
- Focus on public interfaces.

### Blazor Component Testing
- Test parameters, events, rendering, and interactions.
- Use `TestContext` for testing.

## Dependency Injection (DI)
### Guidelines
- Prefer constructor injection.
- Validate parameters.
- Use interfaces.
- Avoid service locator pattern.

### Service Registration
- **Singleton:** Shared instance.
- **Scoped:** Per request/circuit.
- **Transient:** New instance each time.

## Coding Standards
- **Private Fields:** Use `_` prefix.
- **var Usage:** Only when type is clear.
- **Line Length:** 160 characters max.
- **Braces:** Always use braces.

## UI & Styling
- **Framework:** Zurb Foundation.
- **SCSS Only:** Use `wwwroot/scss/`.
- **Accessibility:** Semantic HTML, ARIA roles, WCAG 2.1 AA compliance.
- **Performance:** Optimize assets, lazy loading, virtualization.
- **Modern CSS:** Grid, Flexbox, variables, nesting, dark mode.

## Security & API
- Validate/sanitize input.
- Use Blazor for XSS/CSRF.
- Never expose secrets.
- Use `IHttpClientFactory` for API calls.
- Configure CSP in `staticwebapp.config.json`.

## Testing & Documentation
- **TDD:** Write tests first.
- **Mock Dependencies:** Use constructor injection.
- **Test Naming:** Use underscores.
- **Code Coverage:** Use Coverlet + ReportGenerator.

## Modern C# Features
| Feature | Example |
|---------|---------|
| Primary Constructors | `public class Person(string name, int age) { ... }` |
| Collection Expressions | `int[] nums = [1,2,3];` |
| Default Lambda Params | `Func<int,int,int> add = (x, y=5) => x+y;` |
| Alias Any Type | `using IntPair = (int, int);` |
| Inline Arrays | `[InlineArray(10)] struct Buffer { ... }` |

## File & Directory Organization
- **Features:** `src/redmuffin.Blazor.StaticWeb/Features/`
- **Static Assets:** `src/redmuffin.Blazor.StaticWeb/wwwroot/`
- **Tests:** `tests/`
- **Scripts:** `scripts/`
- **Configuration:** `.github/`

## Best Practices
- Write modular, reusable components.
- Favor strongly-typed parameters.
- Handle exceptions with try/catch.
- Prefer C#/Blazor over JavaScript.
- Use `StateHasChanged()` sparingly.

## AI Operational Guidelines
### Known Build Warnings
- **IL2111 Warnings:** Safe to ignore during Blazor WebAssembly compilation.

### Build Warning Prevention
- Follow StyleCop, Meziantou, and Microsoft Code Analysis guidelines.

### Development Workflow
- Run `dotnet test` before commits.
- Use PowerShell scripts for coverage.
- Follow coding/testing practices.

### Excluded Files
- `src/redmuffin.Blazor.StaticWeb/wwwroot/sample-data/markdown-cheat-sheet.md`
- `src/redmuffin.Blazor.StaticWeb/wwwroot/Example.md`
