---
name: _execute-tasklist
description: Execute tasks from a PRD task list in a Blazor WebAssembly .NET 9 application. Use this skill when the user asks to execute a task list, implement a PRD, run through a ToDo file, or work through implementation tasks. Triggers on mentions of execute tasklist, implement PRD, work through tasks, or run through todo list.
---

# Execute Task List

Guidelines for managing task lists in markdown files to track progress on completing a PRD in a Blazor WebAssembly .NET 9 application.

## Task Implementation

- Execute tasks sequentially, one sub-task at a time
- Complete each sub-task fully before proceeding to the next
- After each sub-task, run quality gates (build + test) before continuing
- Update the task list file after each completed sub-task

## Task List Maintenance

1. **Update the task list as you work:**
   - Mark tasks and subtasks as completed (`[x]`) per the protocol below.
   - Add new tasks as they emerge during implementation.

2. **Maintain the "Relevant Files" section:**
   - List every file created or modified with their full project paths.
   - Give each file a one-line description of its purpose.
   - Organize by category (Blazor Components, Azure Functions, Tests, etc.).

## Task Completion Protocol

1. **Mark Completed Tasks:** When a task is completed, update the task list by changing `- [ ]` to `- [x]` for the completed task.
2. **Update Relevant Files:** Ensure all files mentioned in the task are properly created or modified.
3. **Verify Build Quality:** Run `dotnet clean && dotnet build --no-restore --verbosity quiet` to ensure zero build warnings (except IL2111).
4. **Run Tests:** Execute `dotnet test` to ensure all tests pass and implementation works correctly.
5. **Update Documentation:** If the task affects user-facing features, update relevant documentation.
6. **Update Task List File:** Save the updated task list back to `/tasks/PRD-XXX-ShortTitle-ToDo.md`.

## Quality Gates

After each sub-task, all of the following must pass before proceeding to the next sub-task:

### Build Gate

- Run `dotnet build --verbosity quiet` - must pass with zero warnings (except IL2111)
- If build fails, fix the error immediately before proceeding

### Test Gate

- Run `dotnet test` - must show 100% pass rate
- If tests fail, fix the failures immediately before proceeding
- For targeted testing use `dotnet test --filter "FullyQualifiedName~[TestClassName]"`

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

## Implementation Standards

### Blazor WebAssembly (.NET 9) Specifics

- **Components:** Place in feature-based directories under `src/redmuffin.Blazor.StaticWeb/Features/`
- **Code Structure:** Use `.razor` files with `.razor.cs` code-behind for complex logic
- **Styling:** Use Zurb Foundation classes and feature-based SCSS with `@use` directives
- **Foundation Classes:** Leverage Zurb Foundation for consistent UI components
- **SCSS Organization:** Follow feature-based structure under `wwwroot/scss/features/`
- **State Management:** Leverage Blazor's built-in patterns, cascading parameters, and DI services
- **HTTP Client:** Use `[Inject] private HttpClient Http { get; set; } = default!;` for API calls
- **JavaScript Interop:** Use `IJSRuntime` for browser APIs and local storage interactions

### Azure Functions (.NET 9) API

- **Functions:** Place in `src/redmuffin.Blazor.StaticWeb.Api/Functions/`
- **Models:** Share common models via `src/redmuffin.Blazor.StaticWeb.Common/`
- **Testing:** Create corresponding test files in `tests/redmuffin.Blazor.StaticWeb.Api.Tests/Functions/`

### Testing Requirements

- **Framework:** TUnit with `[Test]` attribute (NOT NUnit/xUnit/MSTest)
- **Data-Driven Tests:** Use `[Arguments]` for parametrized tests
- **Mocking:** LightMock.Generator ONLY (NSubstitute deprecated)
- **Test Organization:** Mirror the main project structure in test projects
- **Validation:** Run `dotnet test` after each implementation to ensure tests pass

## AI Instructions

When working with task lists, the AI must:

1. **Before starting any sub-task:**
   - Check which sub-task is next in the list
   - Verify understanding of the requirements
   - Confirm the implementation approach aligns with Blazor WebAssembly patterns

2. **During implementation:**
   - Follow feature-based organization principles
   - Use proper Blazor component patterns and lifecycle methods
   - Implement appropriate error handling and loading states
   - Apply Zurb Foundation styling consistently
   - Create tests using TUnit framework with proper assertions

3. **After implementing each sub-task:**
   - Run `dotnet build` to verify compilation
   - Run `dotnet test` to ensure tests pass
   - Update the task list file marking the sub-task as `[x]` completed
   - Update the "Relevant Files" section with any new or modified files
   - Add newly discovered tasks if they emerge

4. **Task completion protocol:**
   - Mark each finished sub-task `[x]`
   - Mark the parent task `[x]` once all its subtasks are `[x]`
   - Keep "Relevant Files" section accurate and up to date
   - Provide a brief summary of what was accomplished

5. **Quality checkpoints:**
   - Ensure all created components follow the established project patterns
   - Verify API integrations use proper HttpClient patterns
   - Confirm tests provide adequate coverage using TUnit framework
   - Validate that styling uses Foundation framework consistently

## Project Context

This execution environment targets:

- **Frontend:** Blazor WebAssembly .NET 9 with Zurb Foundation
- **Backend:** Azure Functions .NET 9 with isolated worker model
- **Testing:** TUnit framework with `[Test]` attribute (NOT NUnit/xUnit/MSTest)
- **Mocking:** LightMock.Generator ONLY (NSubstitute deprecated)
- **Architecture:** Feature-based organization with shared common library
- **Styling:** SCSS with Foundation framework using `@use` directives
- **Build:** .NET 9 with WebAssembly optimizations (`WasmStripILAfterAOT=true`, `InvariantGlobalization=true`, `PublishTrimmed=true`)
- **Code Quality:** Zero build warnings policy (except IL2111), StyleCop/Meziantou analyzers enforced
