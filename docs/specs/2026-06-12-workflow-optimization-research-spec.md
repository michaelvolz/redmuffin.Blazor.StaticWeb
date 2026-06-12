---
date: 2026-06-12
version: 0.1.0-draft
last_edited: "2026-06-12"
status: work-in-progress
purpose: >
  Captures all findings from a comprehensive audit of the Azure Static Web Apps
  deploy workflow and CodeQL workflow. This is a living research document — it
  may contain misinterpretations, outdated information, or incomplete analysis.
  It will later feed a formal workflow specification.
scope:
  - Current workflow step-by-step analysis
  - Optimization opportunities with confidence estimates
  - Requirements extracted from docs, commits, and design criteria
  - CodeQL workflow audit
  - Local testing feasibility
  - Pipeline-neutral classification impact on speed
exclude:
  - Actual workflow edits (this is analysis, not implementation)
  - New tool selection or vendor decisions
  - Cost analysis (GitHub Actions minutes billing)
tags:
  - workflow
  - ci-cd
  - optimization
  - azure-swa
  - blazor-wasm
  - research
---

# Azure Deploy Workflow — Optimization Research (WIP)

## What Belongs in This File

- **Viewpoint**: Developer optimizing CI/CD pipeline speed. Reader knows
  the repo structure, the Blazor WASM + Azure Functions stack, and has
  read the workflow YAML.
- **What belongs**: Step-by-step timing analysis, optimization
  opportunities ranked by confidence, requirements traced to sources,
  misinterpretation risk flags, open questions.
- **What does NOT belong**: Implementation PRs, actual YAML edits,
  pipeline-neutral pattern definitions (see `rm-github-workflows`
  skill), Dependabot configuration, local dev environment setup.

---

## 0 — Critical Viewpoint (READ FIRST)

This document is **work in progress**. The author (an LLM agent)
audited 48 git commits, 8 solution docs, 4 instruction files, and both
workflow YAML files. The analysis below is the best available synthesis,
but:

- **Timing data now available** from 20 real workflow runs
  (`docs/specs/workflow-timing-data.csv`). Job-level only — no
  step-level instrumentation. Push deploys: 230–261s. PR tests: 50–56s.
  Pipeline-neutral skips: 8–18s. Real timings vary by runner class,
  cache state, and GitHub load.
- **Some optimizations may be mutually exclusive.** Two suggestions
  might conflict in ways the analysis didn't catch.
- **Requirements extracted from docs may be outdated.** Docs written
  months ago may describe constraints that no longer apply after later
  workflow changes (e.g., the Brotli doc predates several publish-step
  refactors).
- **Confidence estimates are the author's judgment.** "≥95%" means the
  agent would implement without hesitation; "≈50%" means the tradeoff
  is real and needs human judgment.

**How to read this document:** Start at §1 (Requirements) to understand
what the workflow MUST do. Then §2 (Current State) for baseline. Then
§4 (Optimization Opportunities) for ranked changes.

**How to turn this into a spec:** Resolve the open questions in §5. Pick
the optimizations to apply. Validate timing estimates with real runs.
Write the formal spec from the confirmed requirements + chosen
optimizations.

---

## 1 — Requirements Extracted from All Sources

Every requirement traced to at least one source. Sources abbreviated:
`WF` = workflow design criteria (YAML lines 3-13), `C#` = commit
message, `D#` = solution doc.

### Hard Requirements (non-negotiable)

| ID  | Requirement                                                                                     | Source                      | Confidence |
| --- | ----------------------------------------------------------------------------------------------- | --------------------------- | ---------- |
| R1  | Tests pass before deploy — fail-fast if any test fails                                          | WF #5, D2                   | Certain    |
| R2  | Blazor publish must be trimmed — verify assembly count ≤60                                      | WF #1, C48a78749            | Certain    |
| R3  | Brotli-compressed assets must survive deploy                                                    | D2, C1fad93e6               | Certain    |
| R4  | Single `dotnet build` per run — no recompilation between test and publish                       | C8aff4b2b                   | Certain    |
| R5  | TUnit tests run via `dotnet run`, never `dotnet test`                                           | AGENTS.md, C8aff4b2b        | Certain    |
| R6  | Supply chain hardening applied: `min-release-age=1`, `ignore-scripts`, etc.                     | `.npmrc`, C63a6c280         | Certain    |
| R7  | WASM workload (`wasm-tools-net9`) required for Blazor publish                                   | C2aff370f                   | Certain    |
| R8  | `Directory.Packages.props` is single source of package versions — lock files removed 2026-06-12 | NuGet 6.12+ resolver        | Certain    |
| R9  | Pipeline-neutral changes skip deploy but NOT tests                                              | Pipeline-neutral definition | Certain    |
| R10 | `dotnet workload install` skips `wasm-tools-net10` — repo targets net9.0                        | C68dc0f6f                   | Certain    |

### Soft Requirements (design goals, tradeoffs apply)

| ID  | Requirement                                                    | Source           | Notes                                       |
| --- | -------------------------------------------------------------- | ---------------- | ------------------------------------------- |
| R11 | PR feedback as fast as possible                                | WF #2            | Current ~180s; target unclear               |
| R12 | Single job preferred over multi-job (avoid duplicate overhead) | WF #5, #6        | Tradeoff: split may be faster for PR        |
| R13 | Health check after deploy                                      | C9a7bb924        | Post-deploy only; skip on PR                |
| R14 | Cache everything cacheable                                     | WF #2, C6f7f450b | NuGet, npm, SWA CLI all cached              |
| R15 | LLM-debuggable output on failure                               | WF #8            | Artifacts uploaded, structured exit codes   |
| R16 | Zero duplicate steps                                           | WF #6            | One checkout, one restore, one publish each |

### Requirements Uncertain (may be outdated or misinterpreted)

| ID  | Claim                                                                                        | Source                                                   | Risk                                                 |
| --- | -------------------------------------------------------------------------------------------- | -------------------------------------------------------- | ---------------------------------------------------- |
| R17 | `swa deploy` with pre-built `--app-location` does NOT trigger Oryx rebuild                   | D2 says "never use swa deploy"; current workflow uses it | Brotli doc may be outdated after flag changes        |
| R18 | `Azure/static-web-apps-deploy@v1` with `skip_app_build: true` is the correct deploy method   | D2                                                       | Current workflow uses `swa deploy` — which is right? |
| R19 | `files_from_source_file` in `tj-actions/changed-files` uses flat text (one pattern per line) | `rm-github-workflows` skill                              | Confirmed from action README; not yet tested in CI   |
| R20 | CodeQL workflow's `check_changes` shell script should switch to `tj-actions/changed-files`   | `rm-github-workflows` skill                              | Architecture decision; not yet implemented           |

---

## 2 — Current State: Step-by-Step Analysis

### Workflow structure (3 jobs on critical path)

```
check_changes ──→ test_and_build_job ──→ health_check
                      │
                      ├── docs_only_changed_job (alternative path)
                      └── close_pull_request_job (PR close only)
```

### `check_changes` job (~5-8s)

| Step                                 | Time | Notes                                      |
| ------------------------------------ | ---- | ------------------------------------------ |
| `actions/checkout@v6` (full history) | ~4s  | `fetch-depth: 0` for `base_sha` comparison |
| `tj-actions/changed-files@v47.0.6`   | ~1s  | Inline `files: \|` with 30+ patterns       |
| Set `should_skip` output             | ~0s  | Shell `if [[ only_changed == "true" ]]`    |

**Issues found:**

- Inline pattern list includes `tests/**/*` and `.editorconfig` —
  both are pipeline-relevant and should NOT skip CI (BUG, R1+R9)
- `fetch-depth: 0` needed for push events; PR events could use API
- Pattern list duplicated with CodeQL workflow (DRIFT RISK)

### `test_and_build_job` (~150-180s)

| Step                                        | Time  | PR-events needed?    | Push needed?  |
| ------------------------------------------- | ----- | -------------------- | ------------- |
| `actions/checkout@v6` (shallow)             | ~2s   | Yes                  | Yes           |
| System info echo                            | ~1s   | Observability        | Observability |
| `dotnet workload install wasm-tools-net9`   | ~5s   | Yes (tests need SDK) | Yes           |
| `actions/cache@v5` (NuGet)                  | ~3s   | Yes                  | Yes           |
| `dotnet restore -p:PublishTrimmed=true`     | ~15s  | Yes                  | Yes           |
| `dotnet build -c Release`                   | ~30s  | Yes                  | Yes           |
| `dotnet run` tests (parallel)               | ~50s  | **Yes — THE GATE**   | Yes           |
| `actions/cache@v5` (npm/SWA)                | ~3s   | **NO** — wasted      | Yes           |
| Configure npm supply chain                  | ~1s   | **NO** — wasted      | Yes           |
| `npm install -g @azure/static-web-apps-cli` | 0-20s | **NO** — wasted      | Yes           |
| `dotnet publish` (parallel)                 | ~45s  | **NO** — wasted      | Yes           |
| Trimming verification                       | ~1s   | **NO** — wasted      | Yes           |
| `swa deploy`                                | ~20s  | **NO** — impossible  | Yes           |
| Summary echo                                | ~1s   | Observability        | Yes           |

**Total waste on PR events: ~65-70s** (npm cache + config + install +
publish + trim check + deploy prep). These steps are INSIDE a single
`run: |` block — they execute even though the job will exit before
`swa deploy`.

### `health_check` job (~15s)

Only runs after `test_and_build_job` succeeds. On PR events,
`test_and_build_job` runs (tests pass) → health check runs → curls
production URLs that haven't changed. Waste: ~15s.

### `docs_only_changed_job` (~1s)

Correct — only runs when `should_skip == 'true'`. For branch protection.

### CodeQL workflow (~30-60s)

| Phase                          | Time    | Notes                                      |
| ------------------------------ | ------- | ------------------------------------------ |
| `check_changes` (shell script) | ~3s     | 30+ inline patterns, `sed` glob→regex      |
| `docs_only_changed_job`        | ~1s     | Skip notification                          |
| `analyze` (matrix)             | ~30-60s | `actions` language (<1s) + `csharp` (~30s) |

**Issues found:**

- Shell script duplicates deploy workflow's pattern logic
- Zombie patterns: `spec/**`, `tasks/**`, `.trae/**` — directories
  don't exist
- `build-mode: none` for csharp (fast, but misses build-time issues)

---

## 3 — Git History: Optimization Timeline

Chronological, from oldest to newest. Each entry shows what was saved
and what tradeoff was accepted.

| Commit     | Change                                                    | Time saved             | Tradeoff                                     |
| ---------- | --------------------------------------------------------- | ---------------------- | -------------------------------------------- |
| `6abc10e5` | Removed VSTest parallel (conflicts with TUnit)            | —                      | Stability over speed                         |
| `8f7d0616` | Optimized GH Actions caching                              | Unspecified            | Added cache complexity                       |
| `60aa9699` | Optimized doc change detection                            | Unspecified            | —                                            |
| `dc183045` | Simplified SWA CLI install (removed caching complexity)   | Simpler, not faster    | Removed cache that was unreliable            |
| `edadf1b4` | Shallow clone for change detection                        | ~3s                    | Must use `fetch-depth: 1`                    |
| `6f7f450b` | Added npm caching for SWA CLI                             | ~20s (cache hit)       | Static key — never invalidates               |
| `7c1838b5` | Expanded non-code exclusions                              | —                      | Larger skip list → more skips                |
| `35758489` | Added WASM workload for SIMD                              | —                      | Required for build                           |
| `a2750936` | Fixed SWA output location path                            | Bug fix                | —                                            |
| `a74c38d6` | `wasm-tools-net9` for .NET 9 builds                       | —                      | Avoids net10 confusion                       |
| `d1f8401f` | Aligned doc-only patterns across workflows                | Reduced drift          | Didn't eliminate duplication                 |
| `86ea2ff8` | Updated to Node.js 24-compatible actions                  | —                      | Version bumps                                |
| `1fad93e6` | `skip_app_build: true` for Brotli                         | Preserved compression  | Switched from `swa deploy` to action         |
| `e1a13ecb` | Disabled AoT, restored SWA CLI                            | Massive build speedup  | AoT was 3-5× slower                          |
| `ab1a0986` | Optimized caching                                         | Unspecified            | —                                            |
| `efa03281` | Cached StaticSitesClient binary                           | ~10s                   | —                                            |
| `7d24cd07` | Added context headers + resource monitoring               | Observability          | ~1s overhead                                 |
| `2279a49d` | Removed decorative emojis, standardized output            | Observability          | —                                            |
| `fda84485` | `since_last_remote_commit` for skip logic                 | Bug fix (not speed)    | —                                            |
| `f09d39f4` | Supply chain protection in CI                             | Security               | ~1s overhead                                 |
| `9a7bb924` | Strict health check for frontend + API                    | Reliability            | ~15s post-deploy                             |
| `ad576bf0` | Exact .NET version for trimming                           | Correctness            | —                                            |
| `53225e73` | Update `changed-files` to v47.0.6                         | —                      | —                                            |
| `b8bc209d` | Fixed YAML syntax error                                   | Bug fix                | —                                            |
| `64cd0aa0` | Blazor trimming in SWA publish                            | Correctness            | —                                            |
| `07cd8377` | Aligned `global.json` SDK with GHA                        | Correctness            | —                                            |
| `3cfa3d88` | Removed `--no-restore` from Blazor publish (trimming fix) | Bug fix                | —                                            |
| `0f4e5e6d` | Restored `.npmrc` with 1-day release age                  | Supply chain           | —                                            |
| `63a6c280` | Full npm supply chain hardening                           | Security               | —                                            |
| `39051ac9` | SDK 10 with `dotnet run`, trimmed health check            | ~41s (no setup-dotnet) | —                                            |
| `cbfbdab0` | Normalized indentation                                    | Formatting             | —                                            |
| `2aff370f` | `dotnet workload restore` for wasm-tools-net9             | —                      | —                                            |
| `69866082` | Install wasm-tools-net9 directly                          | ~5s vs restore         | —                                            |
| `68dc0f6f` | **Eliminated setup-dotnet + setup-node**                  | **~57s**               | Runner dependency on ubuntu-24.04            |
| `8aff4b2b` | **Build once, parallel tests**                            | **~14s**               | Fixed build order                            |
| `b87d0e52` | **Parallel Blazor + API publish**                         | **~20s**               | Must wait for Blazor first (wasm check)      |
| `e18bf345` | API publish `--no-build`                                  | ~5s                    | Already compiled in build step               |
| `eb0e759d` | Health check curl retries for cold start                  | Reliability            | —                                            |
| `a4b05ba5` | **Check all pushed commits, not just tip**                | Bug fix                | Slightly slower diff (more commits to check) |

---

## 4 — Optimization Opportunities

Ranked by confidence × impact. Impact estimated in seconds saved per
CI run.

### High Confidence (≥80%)

| #      | Change                                                                                   | PR save  | Push save           | Risk                               |
| ------ | ---------------------------------------------------------------------------------------- | -------- | ------------------- | ---------------------------------- |
| **O1** | Gate npm/SWA/publish/deploy steps with `if: github.event_name == 'push'`                 | **~65s** | 0s                  | None — PR events don't deploy      |
| **O2** | Start npm install in background during build+tests; `wait` before publish                | 0s       | **~20s** (uncached) | npm install failures surface later |
| **O3** | Switch both workflows to `files_from_source_file: .github/pipeline-neutral-patterns.txt` | 0s       | 0s                  | Eliminates drift, no speed change  |
| **O4** | Remove `tests/**/*` and `.editorconfig` from skip list (BUG FIX)                         | —        | —                   | CI runs correctly; slightly slower |
| **O5** | Remove zombie patterns from CodeQL skip list (`spec/**`, `tasks/**`, etc.)               | 0s       | 0s                  | Cleanup, no speed change           |

### Medium Confidence (50-79%)

| #      | Change                                                                       | PR save  | Push save | Risk                                                              |
| ------ | ---------------------------------------------------------------------------- | -------- | --------- | ----------------------------------------------------------------- |
| **O6** | WASM workload: check before install (`grep -q wasm-tools-net9`)              | ~5s      | ~5s       | Trivial; zero risk                                                |
| **O7** | Gate `health_check` job on push events only                                  | **~15s** | 0s        | Production URLs unchanged on PR                                   |
| **O8** | CodeQL: switch `check_changes` to `tj-actions/changed-files` + shared file   | 0s       | 0s        | Eliminates shell script maintenance                               |
| **O9** | NuGet cache: hash `Directory.Packages.props` instead of `packages.lock.json` | 0s       | 0s        | `packages.lock.json` removed — zero perf benefit, merge conflicts |

### Lower Confidence (<50% — needs human judgment)

| #       | Change                                                          | PR save                    | Push save | Risk                                                      |
| ------- | --------------------------------------------------------------- | -------------------------- | --------- | --------------------------------------------------------- |
| **O10** | Split `test_and_build_job` into `test` + `deploy` jobs          | Unclear                    | Unclear   | Duplicate checkout/restore overhead may cancel gains      |
| **O11** | `check_changes`: use GitHub API for PR events (no full clone)   | ~3s                        | 0s        | Adds API dependency; complex error handling               |
| **O12** | Skip `dotnet workload install` entirely — pre-warm runner image | ~5s                        | ~5s       | Requires custom runner image; maintenance burden          |
| **O13** | Run `dotnet publish` in background during tests (on push)       | ~30s (overlaps with tests) | ~30s      | If tests fail, publish was wasted; rare on push to master |

---

## 5 — Resolved Questions

Research conducted 2026-06-12 using Brave Search, Microsoft Learn, GitHub
Issues, and official action repositories.

### Q1: `swa deploy` vs `Azure/static-web-apps-deploy@v1` — RESOLVED

**The current workflow is correct.** `swa deploy` with `--app-location`
pointing at a pre-built folder (`bin/Release/publish`) does NOT trigger
Oryx rebuild. Evidence from three independent sources:

- **Microsoft Learn (2024-11-04)**: `skip_app_build: true` with
  `Azure/static-web-apps-deploy@v1` still downloads the 2GB Docker
  image. `app_location` must point to build output, not source.
- **Azure SWA team (Issue #1601, 2025)**: Recommended `swa deploy` as
  the workaround when Oryx had bugs — it deploys pre-built content
  directly with `StaticSitesClient`.
- **shibayan/swa-deploy@v1 (2026-04-23)**: Built specifically to replace
  both `swa deploy` and `Azure/static-web-apps-deploy@v1` in CI/CD.
  Wraps `StaticSitesClient` with automatic caching. Lighter than both.

The Brotli doc (D2) saying "never use `swa deploy`" is **OUTDATED**.
It was written when `swa deploy` pointed at source code. The current
workflow points at `bin/Release/publish` — pre-built, pre-compressed
assets are deployed as-is. Brotli is preserved.

**Better alternative exists**: `shibayan/swa-deploy@v1` — same behavior
as `swa deploy` but auto-caches `StaticSitesClient` and has zero Docker
overhead. Worth evaluating as a future optimization.

| Method                                  | Docker overhead | Oryx build |  Brotli safe  | Cache built-in |
| --------------------------------------- | :-------------: | :--------: | :-----------: | :------------: |
| `Azure/static-web-apps-deploy@v1`       |    YES (2GB)    |    YES     | NO (rebuilds) |   Partially    |
| `Azure/...` + `skip_app_build: true`    |    YES (2GB)    |     NO     |      YES      |   Partially    |
| `swa deploy --app-location <pre-built>` |       NO        |     NO     |      YES      |     Manual     |
| `shibayan/swa-deploy@v1`                |       NO        |     NO     |      YES      |   Automatic    |

### Q2: Actual timing data — PARTIALLY RESOLVED

20 most recent workflow runs fetched from GitHub API. Full dataset:
`docs/specs/workflow-timing-data.csv`.

**Summary of measured timings:**

| Scenario                     | Runs observed | Total time (range) |    Main job     |
| ---------------------------- | :-----------: | :----------------: | :-------------: |
| Pipeline-neutral skip        |       8       |       8–18s        |     Skipped     |
| Push to master (full deploy) |       6       |      230–261s      |    220–250s     |
| PR (tests only, failed)      |       1       |        56s         |       50s       |
| PR (tests only, passed)      |       1       |        30s         | N/A (fast pass) |

**Key finding**: The fastest observed deploy is ~230s. PR test runs
are 50–56s. The gap between PR and push (230 - 56 = ~174s) is larger
than estimated. However, the PR failure was at tests — on a passing PR,
the job would still run npm cache, npm config, publish before realizing
it can't deploy.

No instrumented step-level timings available from the API — only
job-level `startedAt`/`completedAt`.

### Q3: `close_pull_request_job` — RESOLVED

**KEEP IT.** Research confirms it is required:

- **Microsoft docs**: "Once the pull request is closed, the
  pre-production environment is automatically deleted." However, the
  generated workflow includes an explicit `action: "close"` job as the
  teardown hook. Per 2026-05-06 analysis: "the generated close job makes
  the teardown explicit in workflow code instead of defining a separate
  lifecycle model."
- **Known race condition (Issue #898)**: If a PR is merged before the
  deploy job finishes, the close job runs first, then the deploy creates
  a new preview that's never cleaned up. Removing the close job makes
  this worse, not better.
- **Known failure mode (#1635, #1638)**: `No matching static site found`
  when deployment auth is set to GitHub OIDC instead of deployment
  token. Fix: ensure deployment token auth or add `github_id_token`.
- **Can be optimized**: Currently uses `Azure/static-web-apps-deploy@v1`
  which downloads a 2GB Docker image for a single API call.
  `shibayan/swa-deploy` could replace it with a lighter alternative.

### Q4: Background npm install — RESOLVED

**SAFE with `ignore-scripts=true`.** Risk analysis:

| Risk                           | Severity | Mitigation                                                                                           |        Status        |
| ------------------------------ | :------: | ---------------------------------------------------------------------------------------------------- | :------------------: |
| npm hangs (postinstall script) |   HIGH   | `ignore-scripts=true` in `.npmrc` prevents all install scripts                                       |   Already in place   |
| Delayed failure reporting      |   LOW    | npm failure surfaces at `wait` (during publish). No user is watching CI live. Error preserved in log |      Acceptable      |
| Output interleaving            |   LOW    | npm output mixed with build/test logs. Can redirect to file                                          |  Opt-in improvement  |
| Race with `wait` timeout       |   LOW    | Without timeout, a hung npm causes job timeout after 6h. Add `timeout 300 wait $pid \|\| ...`        | Needs implementation |

**Research sources**: npm v12 (July 2026) defaults `allowScripts` to
off — eliminates the postinstall hang risk entirely.
`step-security/background-action` exists for this pattern but is
overkill for a simple `npm install -g` with no scripts.

**Verdict**: Implement with `timeout 300` wrapper. No dependency on
external actions needed.

### Q5: CodeQL `build-mode: none` vs `autobuild` — UNRESOLVED

No direct comparison found. `build-mode: none` uses precompiled
assemblies (faster). `build-mode: autobuild` compiles first (detects
compiler warnings as security signals). The workflow already compiles
during build — so `none` processes the same assemblies. Likely
equivalent.

### Q6: `packages.lock.json` merge strategy — UNRESOLVED

Theory is sound: accept lock file from one side, force re-evaluate
restore to generate correct merged result. Not tested with a real
conflict.

---

## 6 — Sources

### Solution docs (`docs/solutions/`)

- `workflow-issues/github-actions-workflow-output-optimization.md` —
  emoji policy, resource monitoring
- `workflow-issues/brotli-compression-not-reaching-azure-swa-production.md` —
  Brotli deploy method, `skip_app_build`
- `workflow-issues/multi-test-project-coverage-merge.md` — coverage
  merging for quality gates
- `workflow-issues/changelog-automation.md` — changelog git-log parsing
- `workflow-issues/pre-commit-verification-workflow.md` —
  **SUPERSEDED** (promoted to AGENTS.md)
- `tooling-decisions/crap-quality-gates-pipeline.md` — separate
  solution for tools
- `tooling-decisions/nuget-package-update-strategy.md` — Dependabot,
  lock file strategy
- `logic-errors/ci-docs-branch-multi-commit-fix.md` — `base_sha` fix
- `architecture-patterns/pipeline-neutral-classification.md` — this
  session's compound doc

### Instruction files

- `AGENTS.md` — PRE-COMMIT VERIFICATION, code file taxonomy, command
  reference
- `CONTEXT.md` — pipeline-neutral domain terms
- `rm-build-config` skill — local build commands, TreatWarningsAsErrors
- `rm-github-workflows` skill — workflow architecture, decision tree

### Workflow files

- `.github/workflows/azure-static-web-apps-lively-cliff-0945be603.yml`
  (394 lines)
- `.github/workflows/codeql.yml`

### Timing data

- `docs/specs/workflow-timing-data.csv` — 20 runs from
  2026-06-06 to 2026-06-12 with run ID, date, event, result,
  total seconds, and notes

### Commit history

48 commits touching the deploy workflow, earliest `6abc10e5`, latest
`15db6308`. All read and analyzed for time savings and tradeoffs.

---

## Related

- `docs/solutions/architecture-patterns/pipeline-neutral-classification.md` —
  pipeline-neutral taxonomy from this session
- `docs/solutions/logic-errors/ci-docs-branch-multi-commit-fix.md` —
  prior `base_sha` fix
- `docs/solutions/tooling-decisions/nuget-package-update-strategy.md` —
  lock file merge strategy
