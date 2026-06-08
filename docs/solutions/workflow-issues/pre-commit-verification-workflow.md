---
module: AGENTS.md
date: 2026-05-24
problem_type: workflow_issue
component: development_workflow
severity: high
applies_when:
  - Editing any code file (C#, Razor, SCSS, project, config, NuGet lock)
  - Deciding whether to build and test before commit
  - Working with SCSS and committing production minified output
symptoms:
  - LSP-clean files treated as proof the build passes
  - Commits skipped the build step when LSP showed zero diagnostics
  - SCSS changes committed without recompiling app.min.css — stale CSS served
root_cause: inadequate_documentation
resolution_type: documentation_update
related_components:
  - SCSS Pipeline
  - Quality Gates
tags:
  - pre-commit
  - build-verification
  - lsp-diagnostics
  - scss
  - commit-workflow
---

## Current

> **Superseded (2026-06-08):** The pre-commit verification workflow has been
> promoted to AGENTS.md §PRE-COMMIT VERIFICATION — the canonical single source.
> This doc is retained as a historical snapshot of the original problem
> statement. All enforcement rules, file tables, and workflows now live in
> AGENTS.md.
