---
mode: 'agent'
description: 'Generating a Task List from a PRD'
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
3.  **Phase 1: Generate Parent Tasks:** Based on the PRD analysis, create the file and generate the main, high-level tasks required to implement the feature. Use your judgement on how many high-level tasks to use. It's likely to be about 5. Present these tasks to the user in the specified format (without sub-tasks yet). Inform the user: "I have generated the high-level tasks based on the PRD. Ready to generate the sub-tasks? Respond with 'Go' to proceed."
4.  **Wait for Confirmation:** Pause and wait for the user to respond with "Go".
5.  **Phase 2: Generate Sub-Tasks:** Once the user confirms, break down each parent task into smaller, actionable sub-tasks necessary to complete the parent task. Ensure sub-tasks logically follow from the parent task and cover the implementation details implied by the PRD.
6.  **Identify Relevant Files:** Based on the tasks and PRD, identify potential files that will need to be created or modified. List these under the `Relevant Files` section, including corresponding test files if applicable.
7.  **Generate Final Output:** Combine the parent tasks, sub-tasks, relevant files, and notes into the final Markdown structure.
8.  **Save Task List:** Save the generated document in the `/tasks/` directory with the filename `PRD-XXX-ShortTitle-ToDo.md`, where `XXX` and `ShortTitle` match the corresponding PRD file (e.g., if the input was `PRD-002-AuthSystem.md`, the output is `PRD-002-AuthSystem-ToDo.md`).

## Output Format

The generated task list _must_ follow this structure:

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
  - [ ] 1.2 [Sub-task description 1.2]
- [ ] 2.0 Parent Task Title
  - [ ] 2.1 [Sub-task description 2.1]
- [ ] 3.0 Parent Task Title (may not require sub-tasks if purely structural or configuration)
```

## Technology Context

This task list is for a Blazor WebAssembly .NET 9 application with the following characteristics:
*   **Frontend:** Blazor WebAssembly with Zurb Foundation for UI
*   **Backend:** Azure Functions (.NET 8) for API endpoints  
*   **Testing:** TUnit framework with `[Test]` attribute (NOT NUnit/xUnit/MSTest)
*   **Mocking:** LightMock.Generator ONLY (NSubstitute deprecated)
*   **Architecture:** Feature-based organization under `src/redmuffin.Blazor.StaticWeb/Features/`
*   **Styling:** SCSS with Foundation framework using `@use` directives
*   **Storage:** Browser-based storage via `Blazored.LocalStorage` and `IJSRuntime`
*   **Build:** .NET 9 with WebAssembly optimizations (`WasmStripILAfterAOT=true`, `InvariantGlobalization=true`, `PublishTrimmed=true`)
*   **Code Quality:** Zero build warnings policy (except IL2111), StyleCop/Meziantou analyzers enforced
*   **Project Structure:** 
    - Main Blazor app: `src/redmuffin.Blazor.StaticWeb/`
    - Azure Functions API: `src/redmuffin.Blazor.StaticWeb.Api/`
    - Shared models/DTOs: `src/redmuffin.Blazor.StaticWeb.Common/`
    - Tests: `tests/redmuffin.Blazor.StaticWeb.Tests/` and `tests/redmuffin.Blazor.StaticWeb.Api.Tests/`

## Interaction Model

The process explicitly requires a pause after generating parent tasks to get user confirmation ("Go") before proceeding to generate the detailed sub-tasks. This ensures the high-level plan aligns with user expectations before diving into details.

## Target Audience

Assume the primary reader of the task list is a **junior developer** familiar with Blazor WebAssembly, .NET 9, and the existing project structure who will implement the feature following established patterns and conventions.
