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

**Description:** Add CI step that fails build when Blazor WASM payload exceeds defined budget (e.g., 11MB per existing PRD-007).
**Rationale:** One-time setup prevents permanent silent degradation. Forces intentional decisions about every new dependency.
**Downsides:** Requires defining and agreeing on budgets; may block legitimate feature additions.
**Confidence:** 70%
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

## Rejection Summary

| #   | Idea                                 | Reason Rejected                                                               |
| --- | ------------------------------------ | ----------------------------------------------------------------------------- |
| 1   | Enforce 70%+ Coverage Threshold      | Jump from 1% requires hundreds of tests; blocks CI for months; vanity metrics |
| 2   | Full Performance Monitoring Pipeline | Dashboard theater for alpha stage; duplicates deferred Phase 2 work           |
| 3   | Start.ps1 Hardening                  | AGENTS.md already documents workarounds; dev-only script; low leverage        |
| 4   | PRD Lifecycle Policy                 | Bureaucratic theater; treats symptom not root cause                           |
| 5   | Cross-Platform Dev Scripts           | Double maintenance surface; Windows-first project; problem doesn't exist yet  |
| 6   | Phase 2 PWA Implementation           | Premature optimization; cache strategy conflicts; no traffic data             |

## Session Log

- 2026-04-01: Initial ideation — 32 generated (4 agents x 8), 4 survived adversarial filtering
- 2026-04-01: Brainstorm selected — Documentation Consolidation
