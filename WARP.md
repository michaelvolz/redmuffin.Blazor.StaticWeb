# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Repository Essentials

- Solution: redmuffin.Blazor.StaticWeb.sln
- SDK pin: global.json targets .NET 9 ("version": "9.0.*"). Azure Functions project is .NET 8.
- Configurations: Debug, Debug-Sass, Release (see solution + Directory.Build.props).
- Projects:
  - src/redmuffin.Blazor.StaticWeb (Blazor WebAssembly, .NET 9)
  - src/redmuffin.Blazor.StaticWeb.Api (Azure Functions, .NET 8, isolated)
  - src/redmuffin.Blazor.StaticWeb.Common (shared library)
  - src/SwaLauncher (helper to launch SWA/local stack)
  - tests/redmuffin.Blazor.StaticWeb.Tests (TUnit)
  - tests/redmuffin.Blazor.StaticWeb.Api.Tests (TUnit)

## Commands (pwsh)

Quality and build
- Always clean first (project rule):
  dotnet clean && dotnet build --no-restore --verbosity quiet
- Build solution:
  dotnet build redmuffin.Blazor.StaticWeb.sln
- Build specific projects:
  dotnet build src/redmuffin.Blazor.StaticWeb/
  dotnet build src/redmuffin.Blazor.StaticWeb.Api/

Tests (TUnit)
- Run all tests:
  dotnet test
- Run tests in a specific test project:
  dotnet test tests/redmuffin.Blazor.StaticWeb.Tests/
- Run a single test by fully qualified name (adjust to your namespace/class/test):
  dotnet test --filter "FullyQualifiedName~Namespace.ClassName.TestMethod"
- Run a whole test class:
  dotnet test --filter "FullyQualifiedName~Namespace.ClassName"

Git workflow
- Show diffs without pager (project rule):
  git --no-pager diff

Coverage (if scripts exist in scripts/)
- Generate coverage report:
  .\scripts\Generate-CoverageReport.ps1
- View coverage report:
  .\scripts\View-CoverageReport.ps1

## Analyzer and lint context

- Directory.Build.props enables analyzers in Debug only:
  - Meziantou.Analyzer, StyleCop.Analyzers, Roslynator, VS Threading, AsyncFixer, Microsoft.AspNetCore.Components analyzers
- Zero build warnings policy (except IL2111). Treat warnings seriously; keep builds clean in Debug.

## Execution and local dev

Two primary workflows are supported (from README):
- Full stack via Visual Studio multi-project startup ("Start both"): launches Blazor WASM, Azure Functions, and SwaLauncher; app at http://localhost:4280
- Simplified frontend-only workflow: set redmuffin.Blazor.StaticWeb as startup; app at http://localhost:5233; mock data seamlessly replaces API calls when Functions are unavailable

Note: The SWA CLI is used via IDE/SwaLauncher; there are no repo-root CLI scripts to start the full stack directly from terminal beyond building/running projects.

## Architecture overview (big picture)

- Client: Blazor WebAssembly (.NET 9)
  - Feature-based structure under src/redmuffin.Blazor.StaticWeb/Features
  - SCSS-driven styling under src/redmuffin.Blazor.StaticWeb/wwwroot/scss
  - CSS is auto-generated from SCSS; do not hand-edit generated CSS
- Serverless API: Azure Functions (.NET 8, isolated) in src/redmuffin.Blazor.StaticWeb.Api
  - HTTP-triggered endpoints, DI-friendly
  - Shared models/utilities live in src/redmuffin.Blazor.StaticWeb.Common
- Tests: TUnit across two test projects mirroring the main projects
  - LightMock.Generator for mocks (NSubstitute deprecated)
  - TestScope pattern and partial class organization (see docs/TestingGuidelines.md)
- Build orchestration and quality gates
  - Centralized properties in Directory.Build.props (WasmStripILAfterAOT, InvariantGlobalization, trimming, analyzer packages, watch patterns, warnings-as-errors for Nullable)
  - SDK version pinning via global.json

## Critical repo rules for AI/agents (read and comply)

- Read .github/copilot-instructions.md before changing code. Key points:
  - Zero build warnings mandate: after every C# edit, run: dotnet clean && dotnet build --no-restore --verbosity quiet; IL2111 is the single allowed warning
  - Testing: Use TUnit only ([Test], [Arguments]); do not use xUnit/NUnit/MSTest
  - UI and styling: follow the separate UI styling rules in .github/copilot-instructions_uistyling.md
  - Use git --no-pager diff for diffs
  - Project uses feature-based organization and Azure Static Web Apps for deployment
- CSS/SCSS policy:
  - From README and project rules: CSS is auto-generated; author styles in SCSS only under wwwroot/scss
  - Separate user rule: do not change CSS files except .razor.css; prefer SCSS for global styling

## Notes for running agents

- Prefer solution-wide commands against redmuffin.Blazor.StaticWeb.sln
- Honor the Debug-only analyzer setup; build in Debug during development to catch issues
- For API integration tests or end-to-end flows, use the full-stack VS profile (http://localhost:4280)
- When filtering tests, rely on FullyQualifiedName for precision

## Sources referenced by this guide

- README.md (project workflows, structure, tooling)
- .github/copilot-instructions.md (critical agent rules)
- Directory.Build.props (analyzers, build options)
- global.json (SDK pin)
- Solution file (project topology)

