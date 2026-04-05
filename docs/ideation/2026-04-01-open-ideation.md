---
date: 2026-04-01
topic: open-improvement-ideation
focus: open-ended improvement opportunities
---

# Ideation: Project Improvement Opportunities

## Codebase Context

.NET 9 Blazor WebAssembly frontend + Azure Functions backend, deployed to Azure Static Web Apps. C# 12/13, Foundation CSS, SCSS-only styling. TUnit testing (258 tests), LightMock.Generator mocking. Feature folder architecture with partial class splits. Trunk-based development on master. Heavy agent/AI workflow integration (MCP servers, opencode, Chrome DevTools MCP). 20 PRD documents, many unfinished. Code coverage threshold is 1% (effectively zero). No performance monitoring. Phase 2 performance optimizations deferred (Service Worker, PWA, advanced caching, image optimization). Start.ps1 lacks error handling. 379-line AGENTS.md, 1000+ line README, documentation bloat. Windows-centric with heavy PowerShell reliance. Trimming warnings (IL2111) accepted but not suppressed. Mock naming conventions conflict between docs. Copilot instructions restructuring planned but incomplete.

## Ranked Ideas

### 1. Documentation Consolidation

**Description:** Split AGENTS.md (379 lines) into skill-referenced modules. Reduce main file to a routing index. Merge overlapping docs.
**Rationale:** Wall-of-text AGENTS.md degrades agent accuracy and human onboarding. Skills system already exists as the target architecture.
**Downsides:** One-time restructuring cost; risk of broken skill references during transition.
**Confidence:** 75%
**Complexity:** Medium
**Status:** Explored (2026-04-01)

### 2. Bundle Size Budget in CI

**Description:** Add CI step that fails build when Blazor WASM payload exceeds defined budget (e.g., 11MB per existing PRD-007). Integrate automated bundle size monitoring with historical trend reporting.
**Rationale:** One-time setup prevents permanent silent degradation. Forces intentional decisions about every new dependency. Combines existing budget concept with automated monitoring from multiple research sources.
**Downsides:** Requires defining and agreeing on budgets; may block legitimate feature additions. Additional CI complexity.
**Confidence:** 90%
**Complexity:** Low
**Status:** Unexplored

### 3. Core Web Vitals Smoke Test

**Description:** Chrome DevTools MCP-based CI test that loads home page and asserts LCP < 2.5s, CLS < 0.1.
**Rationale:** Leverages existing Chrome DevTools MCP infrastructure. Creates permanent performance floor without full monitoring stack.
**Downsides:** CI time increase; flaky metrics on shared runners.
**Confidence:** 65%
**Complexity:** Medium
**Status:** Unexplored

### 4. Automate Copilot Instructions Generation

**Description:** Script that extracts agent-relevant rules from AGENTS.md, test patterns, folder structure to generate `.github/copilot-instructions.md` automatically.
**Rationale:** Solves recurring planned-but-incomplete task. Keeps instructions current without manual maintenance.
**Downsides:** Script complexity; risk of generating stale output if source docs drift.
**Confidence:** 60%
**Complexity:** Medium
**Status:** Unexplored

### 5. Code Coverage Reporting and Thresholds

**Description:** Integrate code coverage tools (coverlet) into TUnit tests with automated reporting and minimum thresholds (starting from current 1% and gradually increasing).
**Rationale:** Current testing lacks visibility and enforcement. Coverage reporting would identify gaps, improve reliability, and ensure test quality as codebase grows.
**Downsides:** Build time overhead; requires gradual threshold increases to avoid blocking development.
**Confidence:** 90%
**Complexity:** Low
**Status:** Unexplored

### 6. Update and Modernize CI/CD Pipeline

**Description:** Transform outdated .github files into functional GitHub Actions workflow for automated builds, tests, and deployments with advanced caching and parallelization.
**Rationale:** AGENTS.md explicitly notes .github files are outdated and need updating. Modern CI would enable quality gates, faster builds, and automated deployments.
**Downsides:** High initial effort to audit and rewrite workflows; potential for breaking changes.
**Confidence:** 95%
**Complexity:** High
**Status:** Unexplored

### 7. API Documentation Generation for Azure Functions

**Description:** Integrate Swagger/OpenAPI generation for Azure Functions backend using Swashbuckle or NSwag. Generate and publish docs automatically during builds.
**Rationale:** Backend lacks documented APIs, hindering dev experience and integration. Automated docs would improve collaboration and enable client SDK generation.
**Downsides:** Adds maintenance overhead for API changes; potential security concerns if docs expose sensitive endpoints.
**Confidence:** 85%
**Complexity:** Medium
**Status:** Unexplored

### 8. Dependency Security Scanning

**Description:** Integrate automated vulnerability scanning into package update workflow, adding security checks to Update-PackageVersions.ps1 and CI pipeline.
**Rationale:** Project uses many external packages but lacks automated security monitoring. Supply chain security is critical for Blazor WASM + Azure Functions stack.
**Downsides:** Additional API calls and CI complexity; potential false positives requiring triage.
**Confidence:** 85%
**Complexity:** Medium
**Status:** Unexplored

### 9. Lazy Loading for Blazor Components

**Description:** Refactor large components to use Blazor's lazy loading feature, loading them on-demand rather than in initial bundle.
**Rationale:** Reduces initial download size by 20-40%, improving first-load performance without enabling AOT. Addresses documented performance concerns.
**Downsides:** Increases routing complexity; potential loading delays on first access.
**Confidence:** 85%
**Complexity:** Medium
**Status:** Unexplored

### 10. Enhance SCSS Hot Reload

**Description:** Implement automated file watching for SCSS changes during development, eliminating need for manual Debug-Sass rebuilds and improving styling workflow.
**Rationale:** Current SCSS workflow requires manual config changes and rebuilds. Hot reload would streamline development, leveraging existing BuildWebCompiler2022 setup.
**Downsides:** Potential conflicts with existing watchers; increased build complexity.
**Confidence:** 80%
**Complexity:** Low
**Status:** Unexplored

## Rejection Summary

| #   | Idea                                 | Reason Rejected                                                               |
| --- | ------------------------------------ | ----------------------------------------------------------------------------- |
| 1   | Enforce 70%+ Coverage Threshold      | Jump from 1% requires hundreds of tests; blocks CI for months; vanity metrics |
| 2   | Full Performance Monitoring Pipeline | Dashboard theater for alpha stage; duplicates deferred Phase 2 work           |
| 3   | Start.ps1 Hardening                  | AGENTS.md already documents workarounds; dev-only script; low leverage        |
| 4   | PRD Lifecycle Policy                 | Bureaucratic theater; treats symptom not root cause                           |
| 5   | Cross-Platform Dev Scripts           | Double maintenance surface; Windows-first project; problem doesn't exist yet  |
| 6   | Phase 2 PWA Implementation           | Premature optimization; cache strategy conflicts; no traffic data             |
| 7   | Advanced Build Caching               | Too similar to CI modernization; absorbed into #6                             |
| 8   | Playwright E2E Testing               | High complexity for current test maturity; defer until coverage established   |
| 9   | API Response Compression             | Azure Functions handles compression automatically; not a bottleneck           |
| 10  | Error Boundary Enhancement           | Too basic/trivial for improvement focus; standard Blazor practice             |
| 11  | Code Coverage Visualization          | Absorbed into #5 coverage reporting                                           |
| 12  | Native AOT for Tests                 | Increases bundle size; contrary to current optimization strategy              |
| 13  | Automated Vulnerability Scanning     | Absorbed into #8 security scanning                                            |
| 14  | Browser Testing for Components       | High complexity; defer until integration test foundation (#9)                 |
| 15  | Performance Regression Testing       | Too similar to #3 Core Web Vitals; absorbed into existing smoke test          |
| 16  | C# Code Analysis and Linting         | Already enforced via strict-coding-standards skill; low incremental value     |
| 17  | Integration Tests                    | High complexity for current stack; foundation needed first                    |
| 18  | Parallel Test Execution              | Absorbed into #6 CI modernization                                             |
| 19  | Automate SCSS Compilation            | Absorbed into #10 SCSS hot reload                                             |

## Session Log

- 2026-04-01: Initial ideation — 32 generated (4 agents x 8), 4 survived adversarial filtering
- 2026-04-01: Brainstorm selected — Documentation Consolidation
- 2026-04-05: Continuation ideation — 35 generated (5 agents x ~7), 6 new ideas survived adversarial filtering (merged with existing 4)
