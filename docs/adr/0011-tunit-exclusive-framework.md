---
date: 2026-05-30
status: accepted
---

# TUnit as Exclusive Test Framework

TUnit is the sole test framework for all test projects in the repo. No xUnit,
NUnit, or MSTest projects exist, and no multi-framework abstraction layer is
needed. All test patterns, infrastructure, and coverage tooling converge on
TUnit's API.

## Decision

- **Test framework**: TUnit (class-level `[TestFixture]`, method-level `[Test]`
  / `[Arguments]`, `[NotInParallel]` for mutation tests, cancellation token
  via `CancellationToken` parameter).
- **Coverage**: TUnit-native via `Microsoft.Testing.Extensions.CodeCoverage`
  — not coverlet, not OpenCover.
- **Coverage format**: Cobertura XML (native TUnit output format), consumed
  directly by the CRAP and Mutation quality gates.
- **Test execution**: `dotnet run --project <test-project>` with `--coverage`
  flags — no dedicated test runner configuration.

## Considered Options

**xUnit with coverlet.**
Rejected. TUnit provides a more modern API surface: source-generated test
discovery (no reflection-based discovery at runtime), built-in parallel
execution by default (xUnit requires explicit `[Collection]` opt-in), and
first-class `CancellationToken` support. Coverlet is also in maintenance mode
— TUnit's native coverage pipeline is actively developed.

**Multi-framework support (abstraction layer over xUnit + TUnit + NUnit).**
Rejected as premature. Adding an `ITestFramework` abstraction before a second
framework is needed would create speculative generality. The repo exclusively
uses TUnit — an abstraction layer would be unused code that must be maintained,
tested, and kept in sync with TUnit's API surface.

**NUnit or MSTest.**
Not seriously considered. NUnit's API surface is smaller and its due-date
support is less capable than TUnit's. MSTest has no equivalent of
`[Arguments]` for data-driven tests without ceremony.

**coverlet for Cobertura output.**
Rejected. `Microsoft.Testing.Extensions.CodeCoverage` produces Cobertura XML
natively from TUnit's `--coverage` flag — no additional tooling, no
configuration, no version drift between the test runner and the coverage
collector. Coverlet requires separate package installation and its own CLI
flags.

## Consequences

- Test discovery is TUnit-mechanism only — the SCRAP gate's
  `TestMethodParser.FindTests` and `TestClassDiscovery` in the mutation runner
  are TUnit-specific.
- Coverage for CRAP and Mutation gates uses TUnit's native `--coverage
--coverage-output-format cobertura` flags.
- Test patterns are documented in `rm-testing` as TUnit-only — no
  multi-framework conditional guidance needed.
- Mutation runner uses `[NotInParallel]` to serialize disk-state-mutating
  tests (TUnit's default parallel behavior would otherwise race).
- The pre-commit `dotnet clean && dotnet build && dotnet run --project tests/...`
  command is consistent across all test projects.
- No `xUnit` or `NUnit` NuGet packages exist in any test project — a single
  `TUnit` package reference covers all test infrastructure.
