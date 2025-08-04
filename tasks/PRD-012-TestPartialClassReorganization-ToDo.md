# Test Partial Class Reorganization - To Do

## Relevant Files

- tasks/PRD-012-TestPartialClassReorganization-ToDo-MigrationMapping.md will have the complete migration mapping of all testnames, testfiles and their corresponding category
- docs/TestCategorizationRules.md shows the testcategorization rules in detailed form and should be consulted before cetegorizing any test

### Blazor Components

- `tests/redmuffin.Blazor.StaticWeb.Tests/*.cs*` - Test files.

### Azure Functions (API)

- `tests/redmuffin.Blazor.StaticWeb.Api.Tests/*.cs` - Test files.

### Tests

- All existing `*.Tests.Helpers.cs` files remain unchanged - TestScope, mocks, and utilities.

### Notes

- We have 2 test projects, dotnet test shows 258 tests including [Arguments] tests
- Tests use TUnit framework with `[Test]` attribute for test methods and `[Arguments]` for data-driven tests.
- Use `dotnet clean && dotnet build --no-restore --verbosity quiet` to verify zero build warnings
- Use `dotnet test` to run all tests or `dotnet test --filter "FullyQualifiedName~[TestClassName]"` for specific test classes.
- **QUALITY CHECK**: After every step verify TestScope pattern, custom mock pattern, ConfigureAwait(false) compliance following HomeTests*.cs files as prime example.
- All test files follow partial class organization with proper namespace declarations.
- Only create partial class files that will contain moved tests (no empty files).
- Preserve all existing test logic, attributes, and functionality during reorganization.
- Follow StyleCop/Meziantou analyzer rules for code quality.
- All async methods must use `ConfigureAwait(false)` and proper error handling.
- Maintain existing using statements and dependencies in moved tests.
- The tests must be as original as possible in the new location

## Tasks

- [x] 1.0 Inventory Existing Tests
  - [x] 1.1 Scan 100% of ALL test files in the project and catalog each test method with current location
    - [x] 1.1.1 You can use powershell to make it 100% correct. Anything than 100% is unacceptable
  - [x] 1.2 Apply categorization rules to determine target partial class for each test method
  - [x] 1.3 Create migration mapping document showing source and destination for each test 
    - [x] 1.3.1 Name it tasks/PRD-012-TestPartialClassReorganization-ToDo-MigrationMapping.json (JSON format for efficiency)
  - [x] 1.4 Identify which partial class files need to be created based on test categorization (38 partial class files needed)
  - [x] 1.5 Validate categorization rules against actual test method names and functionality

- [x] 2.0 Create Partial Class Files
  - [x] 2.1 Generate required `[TestClass].EdgeCases.cs` files with proper namespace and class declarations
  - [x] 2.2 Generate required `[TestClass].Infrastructure.cs` files with proper namespace and class declarations
  - [x] 2.3 Generate required `[TestClass].Behavior.cs` files with proper namespace and class declarations
  - [x] 2.4 Ensure all partial class files use `public sealed partial class` declaration pattern
  - [x] 2.5 Verify proper using statements are included in each new partial class file

- [x] 2.5 Migrate Old Test Structure (PREREQUISITE)
  - [x] 2.5.1 Identify test classes that still have helper classes embedded (not in separate .Helpers.cs files)
  - [x] 2.5.2 Extract helper classes from HomePageIntegrationTests to HomePageIntegrationTests.Helpers.cs
  - [x] 2.5.3 Extract helper classes from any other non-compliant test classes to separate .Helpers.cs files
  - [x] 2.5.4 Verify all test classes follow the partial class structure before proceeding with categorization

- [ ] 3.0 Move Tests Into Categories
  - [x] 3.1 Move EdgeCases tests (error, exception, null, invalid scenarios) to appropriate `.EdgeCases.cs` files
    - [x] 3.1.1 ArticlesApiVerification_Tests (no EdgeCases tests needed)
    - [x] 3.1.2 ArticlesPageCacheTests (2 EdgeCases tests moved)
  - [x] 3.2 Move Infrastructure tests (lifecycle, logging, DI, disposal) to appropriate `.Infrastructure.cs` files
    - [x] 3.2.1 ArticlesApiVerification_Tests (2 Infrastructure tests moved)
    - [x] 3.2.2 ArticlesPageCacheTests (4 Infrastructure tests moved)
  - [x] 3.3 Move Behavior tests (user interactions, clicks, workflows) to appropriate `.Behavior.cs` files
    - [x] 3.3.1 ArticlesApiVerification_Tests (no Behavior tests needed)
    - [x] 3.3.2 ArticlesPageCacheTests (2 Behavior tests moved)
  - [ ] 3.4 Leave remaining basic functionality tests in main `[TestClass].cs` files
    - [x] 3.4.1 ArticlesApiVerification_Tests (2 basic functionality tests remain)
    - [x] 3.4.2 ArticlesPageCacheTests (1 basic functionality test remains)
    - [x] 3.4.3 ArticlesTests (completed - EdgeCases and Infrastructure partial files created)
    - [x] 3.4.4 CallApiExampleTests (completed - EdgeCases and Infrastructure partial files created)
    - [ ] 3.4.5 Continue with compliant test classes only (skip HomePageIntegrationTests until 2.5 is complete)
  - [ ] 3.5 Ensure each moved test maintains all original attributes, logic, and dependencies
  - [ ] 3.6 Remove moved test methods from original files to prevent duplication

- [ ] 4.0 Validate and Verify
  - [ ] 4.1 Run `dotnet clean && dotnet build --no-restore --verbosity quiet` to check for build errors
  - [ ] 4.2 Run `dotnet test` to ensure all tests pass after reorganization
  - [ ] 4.3 Verify no new build warnings introduced during reorganization process
  - [ ] 4.4 Confirm all test methods are properly categorized according to established rules
  - [ ] 4.5 Validate that existing `.Helpers.cs` files remain unchanged and functional
  - [ ] 4.6 Check that TestScope and mock patterns are preserved across all moved tests

- [ ] 5.0 Final Documentation
  - [ ] 5.1 Update any internal documentation references to moved test methods
  - [ ] 5.2 Verify that categorization aligns with rules defined in project standards
  - [ ] 5.3 Document the final organization structure for future test development
  - [ ] 5.4 Confirm that only non-empty partial class files were created
  - [ ] 5.5 Update this task list to mark all items as completed
