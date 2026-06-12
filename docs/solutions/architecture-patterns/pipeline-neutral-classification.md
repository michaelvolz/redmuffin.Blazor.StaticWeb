---
module: github-workflows
date: 2026-06-12
problem_type: architecture_pattern
component: development_workflow
severity: high
applies_when:
  - When creating or modifying GitHub Actions workflows with path filtering
  - When classifying new file types for CI/CD triggering
  - When setting up local workflow testing for path-filtered workflows
tags:
  - github-actions
  - pipeline-neutral
  - file-classification
  - ci-cd
  - tj-actions
  - wrkflw
  - changed-files
  - deny-list
---

# Pipeline-Neutral File Classification for GitHub Actions CI/CD

## Context

The repo's two GitHub Actions workflows (Azure deploy and CodeQL) had
independent, duplicated skip logic for determining whether a change
should trigger CI. Both workflows contained hardcoded inline lists
of file patterns for what to skip, but those lists had drifted apart
and contained misclassifications:

- `tests/**/*` was skippable — but test files affect CI runs; skipping
  them would let broken tests merge undetected.
- `.editorconfig` was skippable — but analyzer rules in `.editorconfig`
  control Roslyn behavior, which changes compiled IL output.
- `.csproj`, `.props`, `.targets`, `TrimmerRoots.xml`, `libman.json`,
  and `compilerconfig.json.defaults` were classified as publish content
  ("Source") when they are build configuration files that must trigger CI.
- The CodeQL workflow contained zombie patterns (`spec/**`, `tasks/**`,
  `.trae/**`) for directories that don't exist in the repo.
- `.opencode/**` appeared in both skip lists but is gitignored — a
  no-op pattern signaling drift was unchecked.
- The CodeQL workflow used a `yq` + `sed` shell script to parse a YAML
  pattern file, while the deploy workflow used inline `files: |` — two
  different consumption mechanisms for the same logic.

There was no shared source of truth, no decision tree for classifying
new files, and no skill governing the workflow architecture.

## Guidance

The `rm-github-workflows` skill encodes a governance framework with
four pillars:

### 1. Shared flat pattern file — single source of truth

`.github/pipeline-neutral-patterns.txt` — one glob pattern per line,
`#` comments. Both workflows read it via `tj-actions/changed-files`
with `files_from_source_file`:

```yaml
- uses: tj-actions/changed-files@v47
  id: check
  with:
    base_sha: ${{ github.event_name == 'pull_request' && github.event.pull_request.base.sha || github.event.before }}
    files_from_source_file: .github/pipeline-neutral-patterns.txt
```

### 2. Deny-list architecture — "run CI" is the safe default

Only explicitly classified pipeline-neutral files skip CI. A file not
in the pattern file triggers CI. This is safer than an allow-list
(inclusion list), where a missing entry would silently skip CI for
changes that matter. The cost asymmetry:

| Mistake                               | Consequence                                            | Severity  |
| ------------------------------------- | ------------------------------------------------------ | --------- |
| Pipeline-relevant file marked neutral | CI silently skipped — broken builds/tests reach `main` | Dangerous |
| Pipeline-neutral file marked relevant | CI runs unnecessarily — wastes ~2 minutes              | Harmless  |

The architecture errs toward the cheap failure mode.

### 3. Six-question decision tree

Classify every new file type or directory. Start at Q1, stop at first
match:

```
Q1: Is this file's compilation output or content included in the
    dotnet publish output folder?
    ├─ YES → PIPELINE-RELEVANT (source). Stop.
    └─ NO → Q2

Q2: Is this file read by MSBuild, NuGet, Roslyn, the C# compiler, or any
    build tool during dotnet build, dotnet restore, or dotnet publish?
    ├─ YES → PIPELINE-RELEVANT (config). Stop.
    └─ NO → Q3

Q3: Is this file a GitHub Actions workflow file?
    ├─ YES → PIPELINE-RELEVANT (config). Stop.
    └─ NO → Q4

Q4: Is this file read by the CI runner to determine its behavior
    (npm config, environment setup, tool selection)?
    ├─ YES → PIPELINE-RELEVANT (config). Stop.
    └─ NO → Q5

Q5: Is this file gitignored and never committed to the repository?
    ├─ YES → Irrelevant. Not in the repo. Stop.
    └─ NO → Q6

Q6: Would changing only this file produce a different CI run result
    OR a different deployment artifact?
    ├─ YES → PIPELINE-RELEVANT. Re-trace Q1-Q4 — you missed something.
    └─ NO → PIPELINE-NEUTRAL. Add to the shared file with justification.
```

### 4. Verification gate

Before finalizing any pattern change, run tool-call verifications:

```powershell
# Trace the decision tree in stdout — never mentally
Write-Output "Decision tree trace for: <pattern>"

# Fail if pattern appears in any workflow file (must only be in shared file)
$wfMatch = rg -c "<pattern>" .github/workflows/
if ($wfMatch -gt 0) { Write-Error "FAIL: pattern found in workflow files" }

# Fail if pattern is missing from shared file
$sharedMatch = rg -c "<pattern>" .github/pipeline-neutral-patterns.txt
if ($sharedMatch -eq 0) { Write-Error "FAIL: pattern missing from shared file" }
```

### Critical constraints

- **Job-level filtering only**: Workflows with required status checks
  must use job-level `needs` + `if` gating, not `paths-ignore` at the
  trigger level. `paths-ignore` leaves status checks "Pending" forever.
- **Gitignore cross-check**: Any pattern matching only gitignored files
  must be removed — Q5 catches them. Overlap indicates dead patterns.
- **Policy directories**: `tools/**` and `scripts/**` are pipeline-neutral
  by policy — local dev only, never called by CI.

## Why This Matters

The skill was built over multiple iteration cycles that found 23+
issues: wrong file classifications, format incompatibility with
`tj-actions/changed-files`, cross-file stale references, zombie
patterns, cognitive-step verifications, and priming hazards.

Without the skill, every file classification decision is intuition-based
(how `tests/**/*` and `.editorconfig` entered the skip list). With the
skill, every decision follows a documented, repeatable process. The
shared pattern file eliminates drift. The deny-list architecture means
classification mistakes fail safe.

The false-negative risk (CI should run but doesn't) is the catastrophic
failure mode. The architecture and decision tree prevent it at two
levels: Q6 catches Q1-Q4 oversights, and the deny-list default
("not in the file → CI runs") catches everything else.

## When to Apply

- Adding a new file type or top-level directory to the repository
- Debugging CI runs that were incorrectly skipped or triggered
- Modifying or adding a GitHub Actions workflow file
- Reviewing PRs that touch `.github/pipeline-neutral-patterns.txt`
- A status check hangs on a PR (branch protection waiting on a skipped
  workflow that used `paths-ignore`)

## Examples

### Before: duplicated, drifted inline skip lists

Deploy workflow had inline `files: |` with one list. CodeQL had a shell
script with `yq` + `sed`, a different list, and zombie patterns.
`tests/**/*` and `.editorconfig` were in both.

### After: single source of truth consumed uniformly

Both workflows use identical `files_from_source_file` references.
Pattern list lives in one file. A single change updates both workflows.

### Decision tree: classifying `tools/`

1. Q1: Affects publish output? No — tools are separate projects.
2. Q2: Read by MSBuild? No — tools build separately; CI reads pre-built
   `.nupkg` files from `tools/nupkgs/`, not the source.
3. Q3: Workflow file? No.
4. Q4: Read by CI runner? No — CI never calls tools source code.
5. Q5: Gitignored? No — tools source is committed.
6. Q6: Would changing it change CI result or deploy artifact? No — CI
   reads the pre-built `.nupkg`, not the source. **Pipeline-neutral.**

Result: `tools/**` goes in `.github/pipeline-neutral-patterns.txt`.
Changes to quality gates tooling won't trigger app CI.

### Decision tree: classifying `.editorconfig`

1. Q1: Affects publish output? No — `.editorconfig` is not deployed.
2. Q2: Read by MSBuild/Roslyn? **Yes** — Roslyn reads `.editorconfig`
   for analyzer severity, which changes compiled IL output.
   **Pipeline-relevant (config). Stop.**

Result: `.editorconfig` must NEVER appear in the pipeline-neutral
pattern file. It is listed in the skill's NEVER section.

## Related

- `rm-github-workflows` skill: `~/.config/opencode/skills/redmuffin-guides/rm-github-workflows/SKILL.md`
- Architecture decisions: `~/.config/opencode/skills/redmuffin-guides/rm-github-workflows/ARCHITECTURE.md`
- `ci-docs-branch-multi-commit-fix.md` — prior fix using same `base_sha` + gate job pattern; partially superseded by shared file architecture
- `CONTEXT.md` — term "pipeline-neutral" added to project glossary
