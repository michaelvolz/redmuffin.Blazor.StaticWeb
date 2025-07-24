---
mode: 'agent'
description: 'Execute Task List'
---
# Execute Task List
Guidelines for managing task lists in markdown files to track progress on completing a PRD in a Blazor WebAssembly .NET 9 application

## Task Implementation
- **One sub-task at a time:** Do **NOT** start the next sub‑task until you ask the user for permission and they say "yes" or "y"
- **Completion protocol:**  
  1. When you finish a **sub‑task**, immediately mark it as completed by changing `[ ]` to `[x]`.  
  2. If **all** subtasks underneath a parent task are now `[x]`, also mark the **parent task** as completed.  
- Stop after each sub‑task and wait for the user's go‑ahead.

## Task List Maintenance

1. **Update the task list as you work:**
   - Mark tasks and subtasks as completed (`[x]`) per the protocol above.
   - Add new tasks as they emerge during implementation.

2. **Maintain the "Relevant Files" section:**
   - List every file created or modified with their full project paths.
   - Give each file a one‑line description of its purpose.
   - Organize by category (Blazor Components, Azure Functions, Tests, etc.).

## Task Completion Protocol

1. **Mark Completed Tasks:** When a task is completed, update the task list by changing `- [ ]` to `- [x]` for the completed task.
2. **Update Relevant Files:** Ensure all files mentioned in the task are properly created or modified.
3. **Verify Build Quality:** Run `dotnet clean && dotnet build --no-restore --verbosity quiet` to ensure zero build warnings (except IL2111).
4. **Run Tests:** Execute `dotnet test` to ensure all tests pass and implementation works correctly.
5. **Update Documentation:** If the task affects user-facing features, update relevant documentation.
6. **Update Task List File:** Save the updated task list back to `/tasks/PRD-XXX-ShortTitle-ToDo.md`.

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

### Azure Functions (.NET 8) API
- **Functions:** Place in `src/redmuffin.Blazor.StaticWeb.Api/Functions/`
- **Models:** Share common models via `src/redmuffin.Blazor.StaticWeb.Common/`
- **Testing:** Create corresponding test files in `tests/redmuffin.Blazor.StaticWeb.Api.Tests/Functions/`

### Testing Requirements
- **Framework:** Use TUnit (NOT NUnit/xUnit/MSTest)
- **Test Methods:** Use `[Test]` attribute for test methods
- **Data-Driven Tests:** Use `[Arguments]` for parametrized tests
- **Test Organization:** Mirror the main project structure in test projects
- **Validation:** Run `dotnet test` after each implementation to ensure tests pass
- **Build Validation:** Use `dotnet build` to ensure no compilation errors
- **Testing Framework:** TUnit with `[Test]` attribute (NOT NUnit/xUnit/MSTest)
- **Mocking:** LightMock.Generator ONLY (NSubstitute deprecated)

### Quality Assurance
- **Build Verification:** Always run build after each sub-task to catch compilation errors early
- **Test Execution:** Run relevant tests to ensure functionality works as expected
- **Code Standards:** Follow C# 12/13 features and modern Blazor patterns
- **Error Handling:** Implement proper error boundaries and async/await patterns
- **Code Style:** Follow StyleCop/Meziantou analyzer rules and C# naming conventions
- **Error Handling:** Implement proper exception handling with `ConfigureAwait(false)` for async methods
- **Performance:** Use async/await patterns for I/O operations and optimize Blazor rendering
- **Build Quality:** Maintain zero build warnings policy (except IL2111)

## AI Instructions

When working with task lists, the AI must:

1. **Before starting any sub-task:**
   - Check which sub‑task is next in the list
   - Verify understanding of the requirements
   - Confirm the implementation approach aligns with Blazor WebAssembly patterns

2. **During implementation:**
   - Follow feature-based organization principles
   - Use proper Blazor component patterns and lifecycle methods
   - Implement appropriate error handling and loading states
   - Apply Zurb Foundation styling consistently
   - Create tests using TUnit framework with proper assertions

3. **After implementing each sub‑task:**
   - Run `dotnet build` to verify compilation
   - Run `dotnet clean && dotnet test` to ensure tests pass
   - Update the task list file marking the sub-task as `[x]` completed
   - Update the "Relevant Files" section with any new or modified files
   - Add newly discovered tasks if they emerge
   - **Pause and request permission** before proceeding to the next sub-task

4. **Task completion protocol:**
   - Mark each finished **sub‑task** `[x]`
   - Mark the **parent task** `[x]` once **all** its subtasks are `[x]`
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
- **Backend:** Azure Functions .NET 8 with isolated worker model
- **Testing:** TUnit framework with `[Test]` attribute (NOT NUnit/xUnit/MSTest)
- **Mocking:** LightMock.Generator ONLY (NSubstitute deprecated)
- **Architecture:** Feature-based organization with shared common library
- **Styling:** SCSS with Foundation framework using `@use` directives
- **Build:** .NET 9 with WebAssembly optimizations (`WasmStripILAfterAOT=true`, `InvariantGlobalization=true`, `PublishTrimmed=true`)
- **Code Quality:** Zero build warnings policy (except IL2111), StyleCop/Meziantou analyzers enforced