---
date: 2026-04-03
title: "Implementing Code Coverage with Coverlet and TUnit"
tags: [code-coverage, testing, coverlet, tunit, dotnet]
problem_type: testing-infrastructure
---

## Problem

The project had no visibility into which lines of code were covered by tests. Developers couldn't identify untested areas, track coverage trends, or measure the impact of new tests on overall coverage.

## Root Cause

No code coverage tooling was configured. The TUnit test suite ran without coverage instrumentation, so no coverage data was generated.

## Solution

Integrated Coverlet as the code coverage tool alongside the existing TUnit testing framework:

- **Reports**: HTML and XML/JSON formats for both human review and CI/CD integration
- **Exclusions**: Third-party code and generated files excluded from coverage data
- **Local workflow**: Scripts to generate coverage reports via `dotnet test` with Coverlet collectors
- **No enforcement initially**: Coverage reports are informational only — no failing builds on low coverage

### Technical Approach

- Coverlet integrates with the .NET 9 build process via `dotnet test --collect:"XPlat Code Coverage"`
- All existing TUnit tests are included in coverage calculation
- Blazor WebAssembly and Azure Functions projects are both covered

## Prevention

- Run coverage reports regularly to track trends over time
- Consider setting coverage thresholds once the codebase matures
- Document the coverage report generation workflow for all developers
- Future: integrate with GitHub Actions for automated coverage reporting on PRs
