# Standardizing Test Doubles - To Do

## Relevant Files

### Documentation

- `docs/TestingGuidelines.md` - Updated testing guidelines with test double naming conventions
- `.github/copilot-instructions.md` - Updated with test double standards for AI assistance

### Test Projects

- `tests/redmuffin.Blazor.StaticWeb.Tests/**/*.cs` - All test files updated with new naming conventions
- `tests/redmuffin.Blazor.StaticWeb.Api.Tests/**/*.cs` - API test files updated with new naming conventions

### Notes

- Tests use TUnit framework with `[Test]` attribute for test methods and `[Arguments]` for data-driven tests.
- **CRITICAL: ZERO BUILD WARNINGS POLICY** - Run `dotnet clean && dotnet build --no-restore --verbosity quiet` after EVERY C# file change (except IL2111).
- Use `dotnet test` to run all tests or `dotnet test --filter "FullyQualifiedName~[TestClassName]"` for specific test classes.
- **QUALITY CHECK**: After every step verify TestScope pattern, custom mock pattern, ConfigureAwait(false) compliance following HomeTests\*.cs files as prime example.
- **Test double naming convention** uses underscores: `Something_Mock`, `Something_Stub`, `Something_Fake`, `Something_Spy`, `Something_Dummy`
- **Strategic Mocking Approach**:
  - **LightMock.Generator**: For 3rd party/external dependencies ONLY (`IHttpClientFactory`, `ILocalStorageService`, `ILogger<T>`, external APIs)
  - **Custom Mocks**: For internal components/services (`NavigationManager`, internal services, Blazor components)
  - **NEVER NSubstitute** (deprecated)
- **LightMock Critical Fix**: ALWAYS specify ALL parameters explicitly in `Arrange()`/`Assert()` calls to avoid CS0854 errors
- **TestScope Architecture**: ALL test classes must use TestScope pattern with fluent configuration and automatic disposal
- **Partial Class Organization**: Tests in main file, helpers/mocks/TestScope in `.Helpers.cs` partial
- **TUnit Fluent Chaining**: Chain related assertions on same object, use `Assert.Multiple` for unrelated concerns
- **ConfigureAwait(false)** on ALL awaits (except at end of assert statements)
- Follow StyleCop/Meziantou analyzer rules for code quality
- **C# 13 patterns**: Primary constructors, collection expressions, modern syntax
- **File Organization**: Feature-based under `src/redmuffin.Blazor.StaticWeb/Features/`

## Tasks

- [x] 1.0 Research and Document Test Double Standards
  - [x] 1.1 Research modern C#/.NET test doubles (mocks, stubs, spies, fakes, dummies)
  - [x] 1.2 Analyze the current project for existing test double usage
  - [x] 1.3 Document findings and best practices for each test double type

- [x] 2.0 Audit Existing Test Doubles in Codebase
  - [x] 2.1 Identify all test doubles in the codebase
  - [x] 2.2 Classify them according to the new naming convention
  - [x] 2.3 Create a report outlining classifications and findings

- [x] 3.0 Create Test Double Naming and Usage Guidelines
  - [x] 3.1 Draft guidelines for naming and using test doubles, adhering to the new conventions
  - [x] 3.2 Review and refine guidelines with team members
  - [x] 3.3 Publish guidelines in `docs/TestingGuidelines.md`

- [x] 4.0 Implement Standardized Naming Convention
  - [x] 4.1 Rename existing test doubles to follow the new naming convention
  - [x] 4.2 Update test files to reflect the changes
  - [x] 4.3 Verify that all instances conform to the new standards

- [x] 5.0 Validate and Update Build/Analysis Rules
  - [x] 5.1 Review existing analyzer rules and assess for test double naming
  - [x] 5.2 Update rules to enforce new naming standards where possible
  - [x] 5.3 Validate build process and ensure zero build warnings
