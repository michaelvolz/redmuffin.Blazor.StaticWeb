# Product Requirements Document (PRD): Simple Integration Test for Blazor Homepage

## Introduction

This PRD outlines the requirements for creating a simple integration test in a .NET 9 Blazor project. The test will serve as a prime example for future tests, adhering to best testing practices using TUnit and LightMock.Generator. It focuses on verifying access to the homepage with a 200 status code, using the latest C# syntax and structured tests that are short, test one thing, and mock only external dependencies.

## Goals

- Create an exemplary integration test that follows TDD and best practices for .NET 9 Blazor C# projects.
- Use TUnit for testing and LightMock.Generator for mocking.
- Ensure tests are perfectly structured: short, focused on one thing, using mocks/stubs only for external/third-party/framework dependencies.
- Verify homepage access returns HTTP 200 status code.
- No special aspects beyond basic 200 status verification.
- Incorporate all relevant user stories.
- Reference documentation: TUnit via Context7 (/thomhurst/tunit), LightMock.Generator at [LightMock.Generator docs].
- Always check /b:/DevDrive-Projects/redmuffin.Blazor.StaticWeb/.github/copilot-instructions.md and /b:/DevDrive-Projects/redmuffin.Blazor.StaticWeb/.github/prompts/csharp-testing.prompt.md before creating or changing test code.
- Handle all build errors and warnings (except IL\* warnings) immediately.

## User Stories

As a developer, I want to:

- Verify that the Blazor homepage loads successfully with a 200 status code so that basic application accessibility is confirmed.
- Ensure the test setup uses TUnit attributes correctly for integration testing in Blazor.
- Mock any necessary external dependencies using LightMock.Generator to isolate the test.
- Follow AAA (Arrange, Act, Assert) pattern for clear test structure.
- Use descriptive naming conventions for tests and mocks.
- Handle basic edge cases like network failures or invalid responses without overcomplicating the simple test.
- Maintain zero build warnings in the test project.

## Functional Requirements

- Implement the test in the integration tests folder (e.g., tests/redmuffin.Blazor.StaticWeb.Tests/Integration/).
- Use TUnit's [Test] attribute and any necessary setup/teardown methods.
- For Blazor-specific testing, utilize TestContext or equivalent for rendering components if needed.
- Assert that accessing the homepage returns 200 OK.
- Structure tests to be short and focused.
- Use LightMock.Generator for mocking (e.g., `Mock<HttpClient>`).
- Follow naming conventions as per project prompts.
- No API integrations or client-side storage in this test.

## Non-Goals

- Complex scenario testing beyond basic homepage access.
- Performance or load testing.
- UI-specific assertions (e.g., element presence).
- Integration with external APIs or storage.

## Design and Technical Considerations

- Locate tests in tests/redmuffin.Blazor.StaticWeb.Tests/Integration/.
- Leverage .NET 9 features and modern C# syntax.
- Ensure compatibility with Blazor's hosting model.
- Reference TUnit docs for integration testing patterns (e.g., DependsOn attribute).
- Use LightMock.Generator for mocks, noting it cannot return exceptions.
- Aim for zero build warnings/errors.

## Best Practices

Based on research into TDD and testing for .NET 9 Blazor C# projects, incorporating actionable info from README.md:

### TDD Principles

- Follow Red-Green-Refactor cycle: Write failing test (Red), make it pass minimally (Green), refactor while keeping tests green.
- Test-first approach: Write tests before production code.
- Incremental development in small, testable steps.
- Use TUnit for modern, fast testing optimized for .NET.
- Design services with constructor injection for easy testing.
- Use TestContext for Blazor component testing.
- Test Azure Functions with HTTP triggers and DI.

### LightMock.Generator Usage

- For interfaces with optional parameters (e.g., `CancellationToken`), always specify ALL parameters explicitly in `Arrange/Assert` calls.
- Use `CancellationToken.None` or `The<T>.IsAnyValue` for matching.
- Example: `_mock.Arrange(f => f.GetItemAsync<T>("ns", "key", CancellationToken.None)).Returns(Task.FromResult<T>(value));`

### Testing Behavior

- Test public contracts: methods, parameters, returns.
- Avoid testing internals: private methods, data structures.
- Focus on observable behavior through public interfaces.
- Use mocks for external dependencies to isolate units.
- Write tests that survive refactoring of implementation.

### General Guidelines

- One test at a time, small steps.
- Descriptive test names with underscores (e.g., `Should_Return200_When_HomepageAccessed`).
- Keep tests fast, independent, and repeatable.
- Integrate with CI/CD for automated runs.
- Maintain high coverage focusing on meaningful tests.
- Reference: Microsoft Blazor testing docs. <https://learn.microsoft.com/en-us/aspnet/core/blazor/test>

## Success Metrics

- The test passes consistently.
- Code adheres to best practices and project guidelines.
- Success is determined when the user confirms it is done.

## Implementation Notes

- Start with TDD: Write failing test, implement minimal code to pass, refactor.
- Edge cases: Consider network failures or invalid responses as basic scenarios.
- No further open questions based on user feedback.

### Updating Home.razor.cs for Testing

During testing, update Home.razor.cs smartly with precise, clean dummy code to generate sufficient test scenarios. Focus on:

- Adding minimal methods for untested lifecycles (e.g., OnParametersSetAsync with logging).
- Introducing injectable services (e.g., IHttpClientFactory) with simple usage.
- Including event handlers (e.g., async button click with error simulation).
  Ensure updates are concise, follow existing patterns, and enable tests without unnecessary complexity.

## Code-Behind Preference

- Always use code-behind files (e.g., .razor.cs) instead of inline @code blocks in .razor files for better separation of concerns, maintainability, and testability.
- Implement a test or linting rule to verify that no inline @code blocks exist in .razor files. This can be done via a regex search in CI/CD or a custom test that scans files for '@code' patterns.

## bUnit Integration and AoT Compatibility

Reintroduce bUnit as the primary unit testing library for Blazor components, as it supports rendering, lifecycle control, event triggering, and semantic HTML verification. <mcreference link="https://bunit.dev/" index="2">2</mcreference> bUnit is compatible with .NET 8 and works with AoT compilation for Blazor WebAssembly, though AoT increases app size and build time but improves runtime performance. <mcreference link="https://learn.microsoft.com/en-us/aspnet/core/blazor/webassembly-build-tools-and-aot?view=aspnetcore-9.0" index="3">3</mcreference> <mcreference link="https://learn.microsoft.com/en-us/aspnet/core/fundamentals/native-aot?view=aspnetcore-9.0" index="4">4</mcreference> <mcreference link="https://www.reddit.com/r/Blazor/comments/1dct0v8/does_bunit_work_with_dotnet8/" index="5">5</mcreference> Test AoT compatibility by publishing the app with <RunAOTCompilation>true</RunAOTCompilation> and running bUnit tests.

---

## Additional Test Suggestions

Based on extended research into advanced Blazor testing patterns from prominent repositories and docs, focusing on non-obvious but important tests (e.g., error handling, accessibility, JS interop): <mcreference link="https://github.com/bUnit-dev/bUnit" index="1">1</mcreference> <mcreference link="https://bunit.dev/" index="2">2</mcreference>

1. **Verify Page Title**: Test that the rendered page has the title 'Home' using TestServer to simulate the response and assert on the document title.
2. **Check Heading Content**: Ensure the h1 element contains 'redmuffin.StaticWeb' and the Font Awesome rocket icon is present with correct styles.
3. **Emoji Div Presence**: Confirm the div with style 'font-size:2rem;' exists and contains the specific sequence of emojis (e.g., 😀 😃 etc.).
4. **Redirection Logic on Wrong Port**: Simulate a request from localhost on a port other than 4280 and verify that NavigationManager redirects to '<http://localhost:4280>'.
5. **No Redirection on Correct Port**: Test that no redirection occurs when accessing from localhost:4280 or non-localhost hosts.
6. **Lifecycle: OnInitialized**: Verify that OnInitialized logs information and performs redirection if conditions met, using mocks for NavigationManager and ILogger.
7. **Lifecycle: OnParametersSetAsync**: Test that OnParametersSetAsync is called after parameters are set, logs information, and completes async operation.
8. **Lifecycle: OnAfterRenderAsync**: Check behavior on first and subsequent renders, ensuring appropriate logging for each case.
9. **Event Handling: Button Click**: Simulate clicking the 'Click me' button and verify that HandleClick logs 'Button clicked'.
10. **Injected Dependency: ILogger**: Mock ILogger and assert that log methods are called during lifecycle events and event handlers.
11. **Error Handling in Lifecycle**: Test error boundaries by throwing exceptions in OnInitializedAsync and verifying ErrorContent rendering.
12. **JS Interop Mocking**: Mock IJSRuntime invocations and test failure scenarios, like rejected promises.
13. **Accessibility Assertions**: Verify ARIA attributes and keyboard navigation using bUnit's semantic checks.
14. **Cascading Parameters**: Test component behavior with different cascading values and updates.
15. **Authorization Mocks**: Mock IAuthorizationService to test protected components under various auth states.

These advanced tests cover non-obvious scenarios like error resilience and accessibility, building on common patterns. <mcreference link="https://github.com/bUnit-dev/bUnit" index="1">1</mcreference> <mcreference link="https://bunit.dev/" index="2">2</mcreference> <mcreference link="https://learn.microsoft.com/en-us/aspnet/core/blazor/test?view=aspnetcore-9.0" index="3">3</mcreference> They align with the PRD, using bUnit for isolation.

[LightMock.Generator docs]: https://raw.githubusercontent.com/anton-yashin/LightMock.Generator/refs/heads/main/README.md
