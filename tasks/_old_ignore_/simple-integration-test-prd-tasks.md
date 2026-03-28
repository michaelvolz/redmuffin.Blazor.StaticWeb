# Simple Integration Test PRD Tasks

> **📚 For comprehensive technical knowledge and patterns gathered during implementation, see [simple-integration-test-prd-tasks-2.md](./simple-integration-test-prd-tasks-2.md)**

## Relevant Files

### Blazor Components

- `src/redmuffin.Blazor.StaticWeb/Features/Home/Home.razor` - Main Blazor component for the homepage.
- `src/redmuffin.Blazor.StaticWeb/Features/Home/Home.razor.cs` - Code-behind for Home component with dummy code for testing, enhanced with cascading parameters and authorization support.

### Tests

- `tests/redmuffin.Blazor.StaticWeb.Tests/Integration/HomePageIntegrationTests.cs` - TUnit tests for homepage integration.
- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Home/HomeTests.cs` - TUnit unit tests for Home component with advanced TestScope system, including comprehensive cascading parameter and authorization mock tests, plus advanced non-obvious testing scenarios.
- `tests/redmuffin.Blazor.StaticWeb.Tests/CodeQuality/BlazorCodeBehindEnforcementTests.cs` - TUnit tests enforcing code-behind preference over inline @code blocks.
- `tests/redmuffin.Blazor.StaticWeb.Tests/CodeQuality/BlazorCodeBehindEnforcementTests.Helpers.cs` - Helper methods for file system operations and path management in code-behind enforcement tests.

### Documentation

- `docs/bunit-aot-compatibility-report.md` - Comprehensive report on bUnit compatibility with AoT compilation for faster TUnit test execution, including performance benchmarks, recommendations, and conditional AoT configuration guidance.

### Build Scripts

- `scripts/test-build-common.ps1` - Shared functions and utilities for AoT test build scripts.
- `scripts/test-build-fast.ps1` - Fast development test builds with AoT disabled (9.4s build time).
- `scripts/test-build-aot.ps1` - Production parity test builds with AoT enabled (11.1s build time).
- `scripts/test-build-ci.ps1` - CI/CD simulation builds with full environment setup and coverage.

## Tasks

- [x] 1.0 Set up testing environment with TUnit, LightMock.Generator, and bUnit
  - [x] 1.1 Add TUnit, TUnit.Assertions, LightMock.Generator, and bUnit packages to the test project.
  - [x] 1.2 Configure test project for .NET 9 compatibility and Blazor WebAssembly testing.
  - [x] 1.3 Create necessary test helpers or base classes for integration and unit tests.
  - [x] 1.4 Do not use any test helpers already created. Separate the new testcode from the rest of the tests.
  - [x] 1.5 Separate the new testcode from the rest of the tests. Duplicate code can be created if needed to avoid using existing testcode.
  - [x] 1.6 Verify setup by running a simple passing test.

- [x] 2.0 Update Home.razor and create code-behind Home.razor.cs with dummy code for testing
  - [x] 2.1 Move existing logic from Home.razor to Home.razor.cs.
  - [x] 2.2 Add dummy lifecycle methods (OnInitializedAsync, OnParametersSetAsync, OnAfterRenderAsync) with logging.
  - [x] 2.3 Inject dependencies like ILogger, NavigationManager, and IHttpClientFactory.
  - [x] 2.4 Add event handlers, e.g., button click with async operations.
  - [x] 2.5 Ensure code follows project standards and zero warnings policy.
  - [x] 2.6 Ensure code follows clean code and clean architecture rules and has excellent naming conventions.

- [x] 3.0 Implement basic integration test for homepage accessibility
  - [x] 3.1 Create HomePageIntegrationTests.cs in Integration folder.
  - [x] 3.2 Write test to verify homepage returns 200 OK using TestServer or equivalent.
  - [x] 3.3 Add tests for page title, heading content, and emoji presence.
  - [x] 3.4 Test redirection logic for wrong port and no redirection for correct port.
  - [x] 3.5 Follow AAA pattern and use descriptive names.

- [x] 4.0 Develop unit tests for lifecycle methods, events, and dependencies
  - [x] 4.1 Create HomeTests.cs in Features/Home folder.
  - [x] 4.2 Write tests for OnInitialized, OnParametersSetAsync, OnAfterRenderAsync using bUnit.
  - [x] 4.3 Test event handling like button clicks.
  - [x] 4.4 Mock and assert injected dependencies like ILogger and NavigationManager.
  - [x] 4.5 Use LightMock.Generator for mocks, ensuring explicit parameter specification.

- [x] 5.0 Add advanced unit tests for error handling, JS interop, accessibility, and more
  - [x] 5.1 Implement tests for error handling in lifecycle methods.
  - [x] 5.2 Add JS interop mocking and failure scenario tests.
  - [x] 5.3 Write accessibility assertions using bUnit's semantic checks.
  - [x] 5.4 Test cascading parameters and authorization mocks.
  - [x] 5.5 Ensure tests cover non-obvious scenarios from research.

- [x] 6.0 Verify bUnit compatibility with AoT compilation for faster TUnit execution
  - [x] 6.1 Configure test projects for AoT with `<RunAOTCompilation>true</RunAOTCompilation>` to optimize TUnit performance.
  - [x] 6.2 Run bUnit tests in AoT mode and verify they pass with improved performance.
  - [x] 6.3 Document any issues or adjustments needed for AoT compatibility and measure performance improvements.
  - [x] 6.5 **HIGH PRIORITY: Implement Conditional AoT Configuration for Development vs CI/CD**
    - [x] 6.5.1 Add MSBuild condition to enable AoT only in CI/CD environments or when explicitly requested.
    - [x] 6.5.2 Create development-friendly configuration that disables AoT for faster local builds.
    - [x] 6.5.3 Add build scripts/aliases that allow developers to easily toggle AoT mode.
    - [x] 6.5.4 Update documentation with guidance on when to use each mode.
    - [x] 6.5.5 Ensure CI/CD pipelines always use AoT for production parity testing.

- [ ] 7.0 Enforce code-behind preference and update PRD accordingly
  - [x] 7.1 Implement a test or linting rule to check for no inline @code blocks.
  - [x] 7.2 Update PRD with any new sections or notes from implementation.
  - [x] 7.3 Verify all .razor files use code-behind where applicable.
  - [ ] 7.4 Integrate enforcement into CI/CD pipeline.

- [ ] 8.0 Migrate LoggerMessage delegates to partial class structure (HIGH PRIORITY)
  - [x] 8.1 Migrate LogHelpers.cs to use partial class pattern with .Logging.cs separation.
  - [x] 8.2 Migrate Redirect.razor.cs (25+ LoggerMessage delegates) to Redirect.Logging.cs.
  - [x] 8.3 Migrate GetOpenGraphImages.cs LoggerMessage attributes to partial class pattern.
  - [x] 8.4 Verify zero build warnings after all migrations.
  - [x] 8.5 Update any service registrations or references as needed.

- [ ] 9.0 Migrate test classes to partial class structure with TestScope separation (HIGH PRIORITY)
  - [ ] 9.1 Identify all test classes that need TestScope/.Helpers.cs separation.
  - [ ] 9.2 Migrate HomeTests.cs to HomeTests.cs + HomeTests.Helpers.cs pattern.
    - [ ] 9.2.1 Extract TestScope class with all WithXXX fluent methods to HomeTests.Helpers.cs.
    - [ ] 9.2.2 Move CreateTestScope, CreateMockAuthenticationState, CreateFailingHttpTestScope factory methods to .Helpers.cs.
    - [ ] 9.2.3 Move NavigationManagerMock, TestLogger, TestHttpClientFactory mock classes to .Helpers.cs.
    - [ ] 9.2.4 Move ALL private helper methods, properties, fields to .Helpers.cs.
    - [ ] 9.2.5 Keep ONLY [Test] methods in main HomeTests.cs file.
    - [ ] 9.2.6 Verify all tests pass and maintain zero build warnings.
  - [ ] 9.3 Migrate ArticlesTests.cs to ArticlesTests.cs + ArticlesTests.Helpers.cs pattern.
    - [ ] 9.3.1 Extract MockJSRuntime, MockNavigationManager classes to ArticlesTests.Helpers.cs.
    - [ ] 9.3.2 Move SetPrivateProperty, InvokePrivateMethodAsync utility methods to .Helpers.cs.
    - [ ] 9.3.3 Move ALL non-[Test] infrastructure to .Helpers.cs (fields, properties, mocks, utilities).
    - [ ] 9.3.4 Keep ONLY [Test] methods in main ArticlesTests.cs file.
    - [ ] 9.3.5 Verify all tests pass and maintain zero build warnings.
  - [ ] 9.4 Migrate other large test classes (>500 lines) to partial class structure.
    - [ ] 9.4.1 Check BlazorCodeBehindEnforcementTests.cs (already has .Helpers.cs - verify pattern compliance).
    - [ ] 9.4.2 Check HomePageIntegrationTests.cs for size and complexity.
    - [ ] 9.4.3 Check OpenGraphIntegrationTests.cs and OpenGraphPerformanceTests.cs for migration needs.
    - [ ] 9.4.4 Apply same pattern: [Test] methods only in main, ALL infrastructure in .Helpers.cs.
  - [ ] 9.5 Ensure all test classes follow [Test] methods only in main file, infrastructure in .Helpers.cs.
    - [ ] 9.5.1 Audit all test files to ensure ZERO non-[Test] members in main files.
    - [ ] 9.5.2 Ensure TestScope classes use C# 13 primary constructor pattern.
    - [ ] 9.5.3 Ensure all factory methods follow CreateXXXTestScope naming convention.
    - [ ] 9.5.4 Ensure all mock classes follow XXXMock naming convention.
  - [ ] 9.6 Verify TestScope architecture is consistent across all test classes.
    - [ ] 9.6.1 Ensure all TestScope classes implement IDisposable properly.
    - [ ] 9.6.2 Ensure all TestScope classes have fluent WithXXX builder methods.
    - [ ] 9.6.3 Ensure consistent factory method patterns across all test helpers.
    - [ ] 9.6.4 Verify proper service registration and mock setup patterns.
  - [ ] 9.7 Update copilot-instructions.md with finalized test partial class standards.
    - [ ] 9.7.1 Document the completed migration patterns with concrete examples.
    - [ ] 9.7.2 Update TestScope architecture documentation.
    - [ ] 9.7.3 Update file naming convention examples.
    - [ ] 9.7.4 Update best practices based on completed migrations.
  - [ ] 9.8 Verify zero build warnings and all tests pass after migrations.
    - [ ] 9.8.1 Run full build: `dotnet clean && dotnet build --no-restore --verbosity quiet`.
    - [ ] 9.8.2 Run full test suite: `dotnet test` - verify 100% pass rate.
    - [ ] 9.8.3 Verify zero build warnings (except IL2111 which is acceptable).
    - [ ] 9.8.4 Run test coverage reports to ensure no functionality lost during migration.

## 🚨 CRITICAL NOTE FOR FUTURE AI ASSISTANTS

**THE TASK LIST ABOVE IS THE MOST IMPORTANT PART OF THIS FILE.**

**NEVER DELETE OR REMOVE THE TASK LIST** - it tracks project progress and implementation status. The task list must be preserved and updated, never removed. Any changes to this file should maintain the complete task list structure.
