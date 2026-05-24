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

## Context

The project had an implicit assumption: zero LSP diagnostics after an edit meant the build was clean.
Code was committed without running `dotnet build` or tests. CI caught the failures — broken builds,
cross-file reference breakage, test regressions. SCSS changes landed in `main` without recompiling
`app.min.css`, so the deployed site served stale styles for hours.

The assumption was unwritten. No AGENTS.md section defined what a "build-verified" edit required.
LSP diagnostics are per-file only — they cannot detect cross-file reference breakage, test failures,
or runtime errors. A clean edit output is a pre-check, not a replacement for build verification.

## Guidance

The **PRE-COMMIT VERIFICATION** section in AGENTS.md enforces a mandatory verification workflow
before every commit that touches code files.

### Code file definition

Eight categories of files whose changes can break the build or any test:

| Category      | Extensions                                                       | Rationale                                           |
| ------------- | ---------------------------------------------------------------- | --------------------------------------------------- |
| C# source     | `.cs`                                                            | Compilation, test logic, analyzers                  |
| Razor markup  | `.razor`                                                         | Compilation, bUnit selectors, rendering             |
| Project/build | `.csproj`, `.props`, `.targets`                                  | Compilation, package resolution                     |
| Solution      | `.slnx`                                                          | Project discovery                                   |
| SCSS          | `.scss`                                                          | CSS output affects bUnit DOM assertions             |
| Config/CI     | `.yml`, `.jsonc`, `.editorconfig`                                | Analyzer rules affect build, CI steps affect deploy |
| PowerShell    | `.ps1`                                                           | Build scripts, package tooling                      |
| NuGet         | `Directory.Packages.props`, `nuget.config`, `packages.lock.json` | Package resolution                                  |

### LSP scope clarification

Zero diagnostics means the **file itself** is clean. It does not mean the build passes or tests pass.
LSP is per-file only — cross-file breakage, test failures, and runtime errors are invisible to it.

### Verification workflow

```
edit → LSP confirms zero diagnostics → dotnet build → tests → commit
```

Never skip a step. Never batch-commit multiple changes without re-running the full build+test chain.

### SCSS production output

Every `.scss` change must be accompanied by a recompiled `app.min.css`:

```bash
sass --style=compressed --no-source-map scss/app.scss:wwwroot/css/app.min.css
```

The sass watcher only handles dev CSS. Production minified CSS is your responsibility.
Commit the recompiled `app.min.css` alongside the `.scss` change that produced it.

SCSS-only changes may use `--no-build` for tests (C# didn't change), but tests must still run —
bUnit DOM assertions depend on the compiled CSS output.

## Why This Matters

Without the verification workflow, broken code enters the repo. CI catches it, but the feedback
loop is slow — a failed CI run takes minutes and the developer has moved on. The pre-commit gate
catches failures at the point of change, when the context is fresh and the fix is cheapest.

The stale CSS bug is particularly insidious: the SCSS change compiles correctly in dev (sass
watcher), the dev site looks correct, but production serves the old `app.min.css` because it was
never recompiled. No CI step validates CSS content parity — the gap is silent until a user notices.

## When to Apply

- **Before every commit** that touches any file in the code file table above.
- **After every `.scss` change**: run the production sass compile command and verify the resulting
  `app.min.css` appears in `git diff --stat`.
- **SCSS-only changes**: `dotnet run --project tests/... -- --no-build` is safe. Never skip tests
  entirely — bUnit DOM assertions still depend on compiled CSS output.
