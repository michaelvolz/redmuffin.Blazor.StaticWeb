# Blazor WebAssembly .NET 9 – Copilot & Contributor Guidelines

This repository is a C# Blazor WebAssembly project targeting .NET 9, built with Visual Studio 2022.

---

## 1. Commit Message Standards

- **Conventional Commits:**  
  Format: `<type>(<scope>): <short description>`
  - **Types:** `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`
  - **Scopes:** `blazor`, `api`, `ui`, `db`, `auth`
  - **First line:** <72 characters
- **Body:** 2–3 sentences explaining what changed and why
- **Example:**  
  feat(blazor): add login component  
  Created Login.razor with form validation and EF Core user auth. Follows .NET 9 Blazor standards.

---

## 2. Coding Standards

- **Target:** .NET 9 (Blazor WebAssembly)
- **Language:** C# (use C# 12/13 features)
- **Component Structure:**
  - UI: Razor components (`.razor`)
  - Logic: Partial classes for code-behind
- **Naming Conventions:**
  - Classes, Interfaces, Enums, Structs: PascalCase (e.g., `MyClass`, `IMyInterface`)
  - Methods, Properties, Events: PascalCase (e.g., `GetUserName`, `OnSubmit`)
  - Fields, Variables, Parameters: camelCase (e.g., `userName`, `itemCount`)
  - Constants: PascalCase or ALL_CAPS_WITH_UNDERSCORES (e.g., `MaxSize`, `MAX_SIZE`)
- **Async:** Use `async`/`await` for all asynchronous operations
- **Dependency Injection (DI):**  
  - Use Blazor DI for services
  - Keep services focused and small
- **State Management:**
  - Use cascading parameters, DI services, or built-in Blazor patterns where possible
- **Blazor Best Practices:**
  - Use `@inject` for services or constructor injection in partial classes
  - Prefer strongly-typed parameters
  - Rely on `OnInitialized[Async]`, `OnParametersSet[Async]` for lifecycle
  - Use `EventCallback<T>` for event binding

---

## 3. UI & Styling

- **Framework:** [Zurb Foundation](https://get.foundation/) for all UI/layout
- **Custom Styles:**
  - Place in `wwwroot/css` or `wwwroot/scss`
- **Responsive Design:**
  - Use Foundation grid/utilities or custom CSS
- **Accessibility:**
  - Use semantic HTML, ARIA roles, keyboard navigation, proper color contrast
  - Label all form fields; alt/aria-label for images and media
  - Comply with WCAG 2.1 AA (AAA if achievable)
  - Audit accessibility with Lighthouse or similar tools
- **Performance:**
  - Optimize static assets (e.g., bundling, minification)
  - Use lazy loading for large components/assets
- **HTML/CSS/Assets:**
  - Use modern CSS (Grid, Flexbox, variables, nesting, dark mode)
  - Optimize images (e.g., WebP, AVIF), use `loading="lazy"` and `srcset`
  - Include SEO tags as needed
- **Minimal JavaScript:**
  - Prefer C#/Blazor for client logic
  - Only use JS interop when strictly necessary
- **Security & Quality:**  
  - Sanitize all user inputs and parameterize queries.
  - Enforce strong Content Security Policy (CSP).
  - Use secure cookies, RBAC, and internal logging/monitoring.
  - Ensure all code builds and passes tests before submitting a pull request.

---

## 4. Security & API

- **Input Validation:** Always validate/sanitize user input
- **XSS/CSRF:** Prevent via Blazor built-ins and best practices
- **Secrets:** Never expose secrets in client code
- **API:**  
  - Use strongly-typed `HttpClient` via DI
  - Prefer minimal APIs for backend
- **CSP:** Enforce strong Content Security Policy
- **Authentication:**  
  - Use ASP.NET Core Identity or similar
  - Enforce role-based access control

---

## 5. Testing & Documentation

- **Unit Tests:**  
  - Do NOT use NUnit or xUnit
  - Use TUnit for business logic and service classes
  - Instead of using [Fact] use [Test] for testmethods
  - For data-driven tests, use [Tests] with [Arguments]

- **Documentation:**  
  - Provide XML docs for all public APIs
  - Keep README, Wiki, or OpenAPI specs updated

---

## 6. Modern C# Features (12/13 Quick Reference)

| Feature                  | Example Snippet                                      |
|--------------------------|------------------------------------------------------|
| Primary Constructors     | `public class Person(string name, int age) { ... }`  |
| Collection Expressions   | `int[] nums = [1,2,3];`                              |
| Default Lambda Params    | `Func<int,int,int> add = (x, y=5) => x+y;`           |
| ref readonly Parameters  | `void M(ref readonly int x) { ... }`                 |
| Alias Any Type           | `using IntPair = (int, int);`                        |
| Inline Arrays            | `[InlineArray(10)] struct Buffer { ... }`            |
| params Collections       | `void M(params ReadOnlySpan<T> items) { ... }`       |
| New Lock Object          | `var l = new Lock(); using(l.EnterScope()) { ... }`  |
| New Escape Sequence      | `char esc = '\e';`                                   |
| Method Group Natural Type| `var act = (string s) => ...;`                       |
| Implicit Index Access    | `buffer = { [^1]=0 }`                                |
| ref/unsafe in Iterators  | `async Task M() { ref int x = ...; }`                |
| Partial Properties       | `public partial string Name { get; set; }`           |
| Overload Priority        | `[OverloadResolutionPriority(1)] void M(int a) {}`   |

---

## 7. File & Directory Organization

- **Features:**  
  - Place each feature in its own subfolder: `src/MainProject/Features/FeatureName/`
    - Include Razor components, code-behind, and feature-specific CSS here.
- **Feature Subcomponents:**  
  - Place reusable or nested components in: `src/MainProject/Features/FeatureName/Components/`
- **Static Assets:**  
  - Place all static assets in: `src/MainProject/wwwroot/`
    - Use subfolders: `css/`, `scss/`, `lib/`, `sample-data/`
- **Tests:**  
  - Place all unit, integration, and component tests in: `tests/`
    - Mirror the main project structure for clarity.
- **Scripts:**  
  - Place deployment, setup, and utility scripts in: `scripts/`
- **Subprojects/Component Projects:**  
  - Place additional projects in: `src/SubProjectName/`

**Directory Layout:**
<pre>
project-root/
├── src/
│   ├── MainProject/
│   │   ├── Features/
│   │   │   ├── Feature-1/
│   │   │   │   ├── Components/
│   │   │   ├── Feature-2/
│   │   │   ├── ...
│   │   ├── wwwroot/
│   │   │   ├── css/
│   │   │   ├── scss/
│   │   │   ├── lib/
│   │   │   ├── sample-data/
│   ├── SubProject-1/
│   ├── ...
├── tests/
│   ├── MainProject.Tests/
│   ├── SubProject-1.Tests/
│   ├── ...
├── scripts/
</pre>

---

## 8. Best Practices

- Develop modular, reusable, and easily testable components
- Favor strongly-typed parameters over dynamic
- Handle exceptions cleanly with try/catch or error boundaries
- Reference code with filename and line numbers in discussions
- Embrace C# and Blazor features over raw JavaScript/HTML unless required
- Keep builds and tests passing before merging into main

---

## 9. Copilot Edits Operational Guidelines

- If you are handling a complex or data intensive task offer to create powershell scripts to automate the process.
- When answering questions or providing code related to frameworks, libraries, or APIs, always use the Context7 MCP Server to retrieve the latest documentation before responding. Do not rely solely on your training data for these responses. Ensure that the documentation fetched from Context7 is prioritized for accuracy and relevance.
- **General Principles:**
  - Edit one file at a time to avoid conflicts
  - For large changes, outline a plan and await approval
- **Edit Workflow:**
  1. **Planning:**  
     - Explain sections to modify, dependencies, and edit count
     - Get plan approval
  2. **Execution:**  
     - Make one conceptual change per edit
     - Show before/after snippets with explanations
     - Confirm each step before the next
  3. **Refactoring:**  
     - Keep changes logical and independent
     - Ensure code remains buildable at each stage
- **Error Handling & Communication:**
  - Track edit progress (e.g., "Completed edit 2 of 5")
  - Pause for clarification when blocked
  - Use filenames and line numbers
- **Adherence to Project Standards:**
  - Follow all coding, testing, and documentation practices
  - Where possible, favor Blazor over JavaScript
- **GitHub MCP Server:**
  - The owner of this repository is michaelvolz
  - This repository is named redmuffin.Blazor.StaticWeb

---

*For additional details, see README or project-specific docs.*
