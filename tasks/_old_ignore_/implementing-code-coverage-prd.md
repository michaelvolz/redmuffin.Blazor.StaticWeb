# Implementing Code Coverage PRD

## Introduction/Overview

This document outlines the plan to implement comprehensive code coverage within the Blazor WebAssembly .NET 9 project. The aim is to ensure full visibility into test coverage, identify untested areas, and improve code quality.

## Goals

- Achieve as close to 100% code coverage as possible for C# code.
- Generate coverage reports in HTML and XML/JSON formats.
- Integrate code coverage into the local development workflow.
- Track coverage trends over time.

## User Stories

- **As a developer**, I want to see which lines of code are covered by tests to understand untested areas easily.
- **As a team lead**, I want to track coverage trends to ensure continuous improvement in test practices.

## Functional Requirements

1. The system must generate code coverage reports in HTML and XML/JSON formats.
2. The system must exclude third-party code from coverage reports.
3. The system must include all existing TUnit tests in the coverage calculation.
4. The system should produce reports quickly and integrate them into the local development process.

## Non-Goals

- The system will not enforce coverage thresholds initially.
- The system will not include automated integration with CI/CD pipelines at this stage.

## Design Considerations

- Utilize best practice tools for .NET 9, ensuring compatibility with Blazor and Azure Functions.
- Coverage integration should be smooth with TUnit testing framework.
- Implement coverage tracking without blocking builds initially.

## Technical Considerations

- **Tooling**: Preferred contemporary tools include Coverlet with integration into .NET 9 build processes.
- **Exclusions**: Exclude third-party libraries and generated code from coverage data.
- **Integration**: Begin with local tools, potential future integration into GitHub Actions CI/CD workflows.
- **Test Runner**: Utilize TUnit to run tests, ensuring all tests are accounted for in coverage.

## Success Metrics

- Track coverage percentage improvements.
- Monitor code coverage over time to identify trends.

## Implementation Notes

- Set up scripts to facilitate local coverage generation via `dotnet` tools.
- Instruments code to exclude defined non-goal files using configuration.
- Focus on clear documentation to guide developers in running coverage reports.

## Open Questions

- Should we consider specific visual representation tools for long-term coverage trends?
- What would be the best practice approach for expanding coverage into CI/CD?
