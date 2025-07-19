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

## Implementation Standards

### Blazor WebAssembly (.NET 9) Specifics
- **Components:** Place in feature-based directories under `src/redmuffin.Blazor.StaticWeb/Features/`
- **Code Structure:** Use `.razor` files with `.razor.cs` code-behind for complex logic
- **Styling:** Use Zurb Foundation classes and component-scoped CSS (`.razor.css`)
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

### Quality Assurance
- **Build Verification:** Always run build after each sub-task to catch compilation errors early
- **Test Execution:** Run relevant tests to ensure functionality works as expected
- **Code Standards:** Follow C# 12/13 features and modern Blazor patterns
- **Error Handling:** Implement proper error boundaries and async/await patterns

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
   - Run `dotnet test` to ensure tests pass
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
- **Testing:** TUnit framework for all test projects
- **Architecture:** Feature-based organization with shared common library
- **Build System:** .NET 9 with WebAssembly optimizations enabled