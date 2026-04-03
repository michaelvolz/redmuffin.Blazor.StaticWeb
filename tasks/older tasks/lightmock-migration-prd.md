# Product Requirements Document (PRD): LightMock Migration

## Updated PRD with Integrated Details

### Introduction/Overview

The goal of this migration is to replace all instances of NSubstitute with LightMock.Generator in the test suite of the `redmuffin.Blazor.StaticWeb` project. This migration will be performed incrementally, ensuring that each test is migrated, verified, and error-free before proceeding to the next. The primary objective is to maintain high-quality, professional tests that focus on behavior rather than implementation, while adhering to the project's testing standards.

### Goals

1. Replace all NSubstitute mocks with LightMock.Generator in the test suite.
2. Ensure each migrated test is error-free and maintains in general the same test criteria as the original. Differences are allowed to make it more robust and more behavior testing.
3. Focus on testing behavior rather than implementation.
4. Mock as little as possible, preferring real objects and classes when feasible.
5. Incrementally migrate tests, starting with those that use NSubstitute the least and are not skipped.
6. Remove NSubstitute completely from the project once all tests are migrated.

### User Stories

- **As a developer**, I want to migrate tests incrementally so that I can ensure each test is robust and error-free.
- **As a developer**, I want to replace NSubstitute with LightMock.Generator to align with the project's testing framework standards.
- **As a developer**, I want to focus on behavior-driven tests to ensure the tests are meaningful and maintainable.

### Functional Requirements

1. Migrate tests incrementally, starting with those that use NSubstitute the least and are not skipped.
2. Ensure each migrated test is error-free and adheres to the same test criteria as the original.
3. Avoid shortcuts or hacks during the migration process.
4. Focus on mocking only external dependencies, using real objects and classes when possible.
5. Maintain robust and professional tests throughout the migration process.
6. All variables, fields, or properties representing mocks must use the `Mock` suffix (e.g., `_userProfileMock` for a mock of `IUserProfile`).
7. Tests must use the Arrange-Act-Assert pattern for clarity and consistency.
8. All assertions must be async and awaited.
9. Remove NSubstitute from the project once all tests are migrated.

### Working Order

The migration process will follow this detailed working order:

1. **Start with Non-Skipped Tests:**
   - Identify all tests that are not skipped and use NSubstitute.
   - Begin with the test that uses the least number of NSubstitute mocks.
   - Migrate the test to LightMock.Generator, ensuring it is error-free and adheres to the same test criteria as the original.
   - Proceed to the next test with slightly more NSubstitute usage and repeat the process.

2. **Unskip Tests Incrementally:**
   - Once all non-skipped tests using NSubstitute are migrated, identify the skipped test with the least NSubstitute usage.
   - Unskip the test and migrate it to LightMock.Generator.
   - Ensure the test is error-free and adheres to the same test criteria as the original.
   - Repeat this process incrementally, unskipping and migrating tests one by one, starting with those that use the least NSubstitute mocks.

3. **Continue Until Completion:**
   - Continue migrating tests in this incremental manner until all tests using NSubstitute are migrated to LightMock.Generator.
   - Ensure that each test is robust, professional, and behavior-driven.

4. **Remove NSubstitute:**
   - Once all tests are migrated, remove NSubstitute from the project dependencies.
   - Verify that the project builds successfully and all tests pass without errors.

5. **Final Verification:**
   - Perform a final review of all migrated tests to ensure they meet the project's testing standards.
   - Confirm that all tests are behavior-driven and mock only external dependencies where necessary.

### Design Considerations

- Follow the TUnit framework and LightMock.Generator guidelines as outlined in the `./github/copilot-instructions.md` file.
- Use the example test implementation in `tests/redmuffin.Blazor.StaticWeb.Tests/Services/ImageValidationServiceTestsLightMock.cs` as a reference.
- Ensure tests are behavior-driven and not implementation-focused.
- Mock as little as possible, preferring real objects and classes when feasible.
- Adhere to the project's testing standards, including the Arrange-Act-Assert pattern and dependency injection practices.

### File Naming and Backup Process

- **File Naming for Migrated Tests:**
  - For each test file being migrated, create a new file with the same name but with the `LightMock` suffix (e.g., `TestServiceTests.cs` becomes `TestServiceTestsLightMock.cs`).
  - Perform all migration work in the new `LightMock`-suffixed file.

- **Backup Original Files:**
  - Once all tests in the new `LightMock`-suffixed file are complete and verified, rename the original test file by appending `.backup` to its extension (e.g., `TestServiceTests.cs` becomes `TestServiceTests.cs.backup`).

- **Migration Workflow:**
  1. Identify the test file to migrate.
  2. Create a new file with the `LightMock` suffix.
  3. Migrate tests incrementally within the new file, starting with the least NSubstitute usage.
  4. Verify that each migrated test is error-free and adheres to the same test criteria as the original.
  5. Once all tests in the new file are complete, rename the original file to include the `.backup` extension.

- **Lifecycle Hooks:**
  - Use `[Before(Test)]` and `[After(Test)]` for setup and teardown logic.

- **Data-Driven Testing:**
  - Leverage TUnit's data-driven attributes like `[Arguments]` for repetitive test cases.

- **Dispose Resources:**
  - Ensure all disposable resources are cleaned up using `IDisposable` or `[After(Test)]`.

- **Final Cleanup:**
  - After all test files are migrated and original files are backed up, remove NSubstitute from the project dependencies.
  - Ensure the project builds successfully and all tests pass without errors.

### Technical Considerations

- Start with tests that use NSubstitute the least and are not skipped.
- Incrementally unskip tests with the least NSubstitute usage and migrate them.
- Ensure each test is migrated one by one, maintaining the same test criteria as the original.
- Use LightMock.Generator for mocking external dependencies.
- Avoid brittle tests by focusing on behavior-driven testing.
- Ensure all tests are error-free before proceeding to the next.
- TUnit tests are parallel by default. Use `[NotInParallel]` only when necessary.

### Success Metrics

- All tests using NSubstitute are successfully migrated to LightMock.Generator.
- NSubstitute is completely removed from the project.
- All migrated tests are error-free and maintain the same test criteria as the original.
- Tests are robust, professional, and behavior-driven.

### Implementation Notes

- Follow the TUnit framework and LightMock.Generator guidelines as outlined in the `./github/copilot-instructions.md` file.
- Use the example test implementation in `tests/redmuffin.Blazor.StaticWeb.Tests/Services/ImageValidationServiceTestsLightMock.cs` as a reference.
- Incrementally migrate tests, starting with those that use NSubstitute the least and are not skipped.
- Ensure each test is migrated one by one, maintaining the same test criteria as the original.
- Mock as little as possible, preferring real objects and classes when feasible.
- Adhere to the project's testing standards, including the Arrange-Act-Assert pattern and dependency injection practices.
