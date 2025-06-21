# Blazor WebAssembly .NET 9 – AI Coding Instructions

Target: .NET 9 Blazor WebAssembly, Visual Studio 2022

## 1. Commit Standards
- Format: `<type>(<scope>): <description>` (<72 chars)
- Types: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`
- Scopes: `blazor`, `api`, `ui`, `db`, `auth`
- Body: 2-3 sentences explaining changes

## 2. Coding Standards
- **Target:** .NET 9 Blazor WebAssembly, C# 12/13 features
- **Structure:** UI (.razor), Logic (partial classes)
- **Naming:** PascalCase (classes, methods, properties), camelCase (fields, variables, parameters)
- **Async:** Always use `async`/`await`
- **DI:** Blazor DI for services, keep focused and small
- **State:** Cascading parameters, DI services, built-in Blazor patterns
- **Best Practices:** `@inject` for services, strongly-typed parameters, `OnInitialized[Async]`/`OnParametersSet[Async]`, `EventCallback<T>`

## 3. UI & Styling
- **Framework:** Zurb Foundation for all UI/layout
- **Styles:** Place in `wwwroot/css` or `wwwroot/scss`
- **Responsive:** Foundation grid/utilities or custom CSS
- **Accessibility:** Semantic HTML, ARIA roles, keyboard navigation, WCAG 2.1 AA compliance, proper color contrast
- **Performance:** Optimize assets (bundling, minification), lazy loading
- **Modern CSS:** Grid, Flexbox, variables, nesting, dark mode support
- **Images:** WebP/AVIF, `loading="lazy"`, `srcset`
- **JavaScript:** Minimal - prefer C#/Blazor, JS interop only when necessary
- **Security:** Sanitize inputs, enforce CSP, secure cookies, RBAC

## 4. Security & API
- **Input Validation:** Always validate/sanitize user input
- **XSS/CSRF:** Use Blazor built-ins and best practices
- **Secrets:** Never expose in client code
- **API:** Strongly-typed `HttpClient` via DI, prefer minimal APIs
- **CSP:** Enforce strong Content Security Policy
- **Authentication:** ASP.NET Core Identity, role-based access control

## 5. Testing & Documentation
- **Unit Tests:** Use TUnit (NOT NUnit/xUnit), `[Test]` for methods, `[Tests]` with `[Arguments]` for data-driven
- **Documentation:** XML docs for public APIs, update README/Wiki/OpenAPI

## 6. Modern C# Features (12/13)
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

## 7. File & Directory Organization
- **Features:** `src/MainProject/Features/FeatureName/` - Include Razor components, code-behind, feature-specific CSS
- **Feature Subcomponents:** `src/MainProject/Features/FeatureName/Components/`
- **Static Assets:** `src/MainProject/wwwroot/` - Use subfolders: `css/`, `scss/`, `lib/`, `sample-data/`
- **Tests:** `tests/` - Mirror main project structure
- **Scripts:** `scripts/` - Deployment, setup, utility scripts
- **Subprojects:** `src/SubProjectName/`

**Structure:**
```
project-root/
├── src/
│   ├── MainProject/
│   │   ├── Features/FeatureName/Components/
│   │   ├── wwwroot/css|scss|lib|sample-data/
│   ├── SubProject-1/
├── tests/MainProject.Tests/
├── scripts/
```

## 8. Best Practices
- Develop modular, reusable, testable components
- Favor strongly-typed parameters over dynamic
- Handle exceptions with try/catch or error boundaries
- Reference code with filename and line numbers
- Prefer C#/Blazor over JavaScript/HTML unless required
- Keep builds and tests passing before merging

## 9. AI Operational Guidelines
- Offer PowerShell scripts for complex/data-intensive tasks
- Use Context7 MCP Server for framework/library documentation - prioritize over training data
- **Workflow:**
  - Edit one file at a time to avoid conflicts
  - For large changes: outline plan, get approval, make incremental edits
  - Track progress (e.g., "Edit 2 of 5"), pause for clarification when blocked
  - Keep code buildable at each stage
- **Standards:** Follow all coding/testing practices, prefer Blazor over JavaScript
- **Repository:** Owner: `michaelvolz`, Name: `redmuffin.Blazor.StaticWeb`
