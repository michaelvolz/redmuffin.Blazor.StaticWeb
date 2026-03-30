---
name: _tasklist-from-prd
description: Generating a hierarchical task list from an existing PRD. Use this skill when the user asks to create a task list, todo list, task breakdown, or implementation plan from a PRD. Triggers on mentions of task list, todo from PRD, break down PRD, implementation tasks, or task breakdown.
---

# Generating a Task List from a PRD

## Goal

To guide an AI assistant in creating a detailed, step-by-step task list in Markdown format based on an existing Product Requirements Document (PRD). The task list should guide a developer through implementation in a Blazor WebAssembly .NET 9 application.

## Output

- **Format:** Markdown (`.md`)
- **Location:** `/tasks/`
- **Filename:** `PRD-XXX-ShortTitle-ToDo.md` where:
  - `XXX` is a three-digit number that matches the corresponding PRD file
  - `ShortTitle` matches the short title from the PRD file
  - Example: If PRD is `PRD-002-AuthSystem.md`, task list is `PRD-002-AuthSystem-ToDo.md`

## Process

1.  **Receive PRD Reference:** The user points the AI to a specific PRD file
2.  **Analyze PRD:** The AI reads and analyzes the functional requirements, user stories, and other sections of the specified PRD. Extract the H1 headline from the PRD file to use as the title for the task list.
3.  **Generate Parent Tasks:** Based on the PRD analysis, generate the main, high-level tasks required to implement the feature. Use your judgement on how many high-level tasks to use. It is likely to be about 5.
4.  **Generate Sub-Tasks:** Break down each parent task into smaller, actionable sub-tasks necessary to complete the parent task. Ensure sub-tasks logically follow from the parent task and cover the implementation details implied by the PRD.
5.  **Add Dependencies:** Identify dependencies between tasks. A sub-task should list which tasks it depends on. This helps developers understand execution order.
6.  **Add Quality Gates:** After each sub-task, include a quality gate checklist item. Quality gates verify the work before proceeding to the next task. All quality gates must pass before moving to dependent tasks.
7.  **Identify Relevant Files:** Based on the tasks and PRD, identify potential files that will need to be created or modified. List these under the `Relevant Files` section, including corresponding test files if applicable.
8.  **Generate Final Output:** Combine the parent tasks, sub-tasks, dependencies, quality gates, relevant files, and notes into the final Markdown structure.
9.  **Save Task List:** Save the generated document in the `/tasks/` directory with the filename `PRD-XXX-ShortTitle-ToDo.md`, where `XXX` and `ShortTitle` match the corresponding PRD file (e.g., if the input was `PRD-002-AuthSystem.md`, the output is `PRD-002-AuthSystem-ToDo.md`).

## Output Format

The generated task list must follow this structure:

```markdown
# [H1 Headline from Main PRD File] - To Do

## Relevant Files

### Blazor Components

- `src/redmuffin.Blazor.StaticWeb/Features/[FeatureName]/[ComponentName].razor` - Main Blazor component for [feature description].
- `src/redmuffin.Blazor.StaticWeb/Features/[FeatureName]/[ComponentName].razor.cs` - Code-behind for [ComponentName] component.
- `src/redmuffin.Blazor.StaticWeb/Features/[FeatureName]/Components/[SubComponent].razor` - Child component for [specific functionality].

### Azure Functions (API)

- `src/redmuffin.Blazor.StaticWeb.Api/Functions/[FunctionName].cs` - Azure Function for [API endpoint description].

### Shared/Common

- `src/redmuffin.Blazor.StaticWeb.Common/Models/[ModelName].cs` - Shared data models.
- `src/redmuffin.Blazor.StaticWeb.Common/DTOs/[DtoName].cs` - Data transfer objects for API communication.

### Styles

- `src/redmuffin.Blazor.StaticWeb/wwwroot/scss/features/[feature-name]/[component-name].scss` - Feature-specific SCSS using `@use` directives.
- `src/redmuffin.Blazor.StaticWeb/wwwroot/scss/features/[feature-name]/_index.scss` - Feature SCSS index file.

### JavaScript (if needed)

- `src/redmuffin.Blazor.StaticWeb/wwwroot/js/[feature-name].js` - JavaScript for complex client-side interactions.

### Tests

- `tests/redmuffin.Blazor.StaticWeb.Tests/Features/[FeatureName]/[ComponentName]Tests.cs` - TUnit tests for Blazor components.
- `tests/redmuffin.Blazor.StaticWeb.Api.Tests/Functions/[FunctionName]_Tests.cs` - TUnit tests for Azure Functions.

### Notes

- Tests use TUnit framework with `[Test]` attribute for test methods and `[Arguments]` for data-driven tests.
- Mocking uses LightMock.Generator ONLY (NSubstitute deprecated).
- Use `dotnet clean && dotnet build --no-restore --verbosity quiet` to verify zero build warnings (except IL2111).
- Use `dotnet test` to run all tests or `dotnet test --filter "FullyQualifiedName~[TestClassName]"` for specific test classes.
- Blazor components follow feature-based organization under `src/redmuffin.Blazor.StaticWeb/Features/`.
- Azure Functions use isolated worker model with dependency injection.
- Use Zurb Foundation classes for consistent UI styling.
- Component styling uses feature-based SCSS with `@use` directives.
- All async methods must use `ConfigureAwait(false)` and proper error handling.
- Follow StyleCop/Meziantou analyzer rules for code quality.

## Tasks

- [ ] 1.0 Parent Task Title
  - [ ] 1.1 [Sub-task description 1.1]
    - **Dependencies:** None
    - **Quality Gate:** `dotnet build --verbosity quiet` passes with zero warnings (except IL2111). All new code follows StyleCop/Meziantou analyzer rules. `dotnet test` passes for related tests.
  - [ ] 1.2 [Sub-task description 1.2]
    - **Dependencies:** 1.1
    - **Quality Gate:** Build passes. Tests pass. Custom mock pattern follows `[ClassName]_[Type]` convention if applicable. `ConfigureAwait(false)` used on all async calls.
- [ ] 2.0 Parent Task Title
  - [ ] 2.1 [Sub-task description 2.1]
    - **Dependencies:** 1.2
    - **Quality Gate:** Build passes. Tests pass. Feature-specific SCSS uses `@use` directives. Component follows feature-based organization.
  - [ ] 2.2 [Sub-task description 2.2]
    - **Dependencies:** 2.1
    - **Quality Gate:** `dotnet test` shows 100% pass rate. Zero build warnings. Code review for [Test] attribute, LightMock usage, and async patterns.
- [ ] 3.0 Parent Task Title
  - [ ] 3.1 [Sub-task description 3.1]
    - **Dependencies:** 2.2
    - **Quality Gate:** All tests pass. Zero build warnings. Component follows proper lifecycle method usage (`OnInitializedAsync`, `OnParametersSetAsync`).
```

## Technology Context

This task list is for a Blazor WebAssembly .NET 9 application with the following characteristics:

- **Frontend:** Blazor WebAssembly with Zurb Foundation for UI
- **Backend:** Azure Functions (.NET 9) for API endpoints
- **Testing:** TUnit framework with `[Test]` attribute (NOT NUnit/xUnit/MSTest)
- **Mocking:** LightMock.Generator ONLY (NSubstitute deprecated)
- **Architecture:** Feature-based organization under `src/redmuffin.Blazor.StaticWeb/Features/`
- **Styling:** SCSS with Foundation framework using `@use` directives
- **Storage:** Browser-based storage via `Blazored.LocalStorage` and `IJSRuntime`
- **Build:** .NET 9 with WebAssembly optimizations (`WasmStripILAfterAOT=true`, `InvariantGlobalization=true`, `PublishTrimmed=true`)
- **Code Quality:** Zero build warnings policy (except IL2111), StyleCop/Meziantou analyzers enforced
- **Project Structure:**
  - Main Blazor app: `src/redmuffin.Blazor.StaticWeb/`
  - Azure Functions API: `src/redmuffin.Blazor.StaticWeb.Api/`
  - Shared models/DTOs: `src/redmuffin.Blazor.StaticWeb.Common/`
  - Tests: `tests/redmuffin.Blazor.StaticWeb.Tests/` and `tests/redmuffin.Blazor.StaticWeb.Api.Tests/`

## Quality Gate Rules

Quality gates are mandatory and appear after every sub-task. They ensure work is correct before proceeding to dependent tasks.

### Build Gate

- `dotnet build --verbosity quiet` passes with zero warnings (except IL2111)
- `dotnet build --no-restore` succeeds for fast verification

### Test Gate

- `dotnet test` passes 100% for related test classes
- Use `dotnet test --filter "FullyQualifiedName~[TestClassName]"` for targeted testing

### Code Quality Gate

- StyleCop/Meziantou analyzer rules followed
- `ConfigureAwait(false)` on all async calls (except asserts)
- Proper null checking using `is null` or `is not null` (NOT `== null`)
- File-scoped namespace declarations
- Single-line using directives

### Mocking Gate

- External dependencies mocked with LightMock.Generator
- Custom mock pattern: `[ClassName]_[Type]` (e.g., `NavigationManager_Mock`)
- All optional parameters specified explicitly

### Component Gate

- Feature-based organization under `src/redmuffin.Blazor.StaticWeb/Features/`
- Proper lifecycle method usage (`OnInitializedAsync`, `OnParametersSetAsync`)
- Zurb Foundation classes for UI styling
- Feature-specific SCSS with `@use` directives

## Target Audience

Assume the primary reader of the task list is a **junior developer** familiar with Blazor WebAssembly, .NET 9, and the existing project structure who will implement the feature following established patterns and conventions.
