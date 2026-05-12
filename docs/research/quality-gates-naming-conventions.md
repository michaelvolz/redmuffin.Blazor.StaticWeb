---
date: 2026-05-12
title: Quality Gates Naming Conventions and File Placement
tags:
  [research, quality-gates, naming, conventions, architecture, configuration]
description: >
  Research into community naming conventions for code quality tooling
  configuration files, with a proposal for QualityGates asset naming,
  CLI flag naming, directory structure, and a file placement decision tree.
module: quality-gates
problem_type: conventions
---

## 1. The Name "QualityGates"

### Evaluation

"QualityGates" is a recognized industry term (Martin Fowler, CI/CD
pipelines, SonarQube). It clearly communicates purpose: automated
checks that gate code from progressing until quality thresholds are met.

Alternative considered: **CodeGates** — shorter, "code" is more specific
than "quality" (quality could mean UX, performance, accessibility).

**Recommendation: keep "QualityGates."** The name is:

- Industry-standard and self-documenting
- Already invested across the codebase (namespaces, classes, docs)
- The "quality" ambiguity is minor — context makes it clear

If a rename were free, "CodeGates" has a slight edge. It is not free.
The ROI of renaming is negative.

### When to Use QualityGates

| Trigger                  | Action                                                                       |
| ------------------------ | ---------------------------------------------------------------------------- |
| Before committing code   | Run `dotnet run -- all` to verify no regressions                             |
| During cleanup sprints   | Run individual gates (`crap`, `scrap`, `dup es`) to target specific problems |
| In CI/CD                 | Run `all` with `--auto-coverage` as a deploy gate                            |
| After reading the output | Extract pure functions, write characterization tests, fix violations         |

### What We Do With Its Output

The output is actionable remediation guidance, not a pass/fail scorecard.
Each violation maps to a specific method, file, and line — the fix is
extraction (Feathers seam pattern) or test coverage (CC=3 at 0% →
44.4%+ coverage). The gates exist to make the code better, not to produce
green checkmarks.

## 2. Community Research: How Other Tools Name Config Files

### Pattern A: Tool-Name Prefix (JavaScript ecosystem)

| Tool               | Config File                       |
| ------------------ | --------------------------------- |
| ESLint             | `.eslintrc.json`, `.eslintrc.yml` |
| Prettier           | `.prettierrc`, `.prettierignore`  |
| Dependency-Cruiser | `.dependency-cruiser.js`          |
| StyleLint          | `.stylelintrc.json`               |

Convention: `.{toolname}rc.{format}` — hidden file, tool name as prefix,
`rc` suffix (run commands).

### Pattern B: Purpose-Named (.NET ecosystem)

| Tool/System  | Config File             |
| ------------ | ----------------------- |
| MSBuild      | `Directory.Build.props` |
| .NET SDK     | `global.json`           |
| NuGet        | `nuget.config`          |
| EditorConfig | `.editorconfig`         |

Convention: descriptive purpose-based name, no tool prefix, sometimes
hidden.

### Pattern C: Domain Directory

| Domain         | Location                 |
| -------------- | ------------------------ |
| GitHub Actions | `.github/workflows/`     |
| VS Code        | `.vscode/settings.json`  |
| Dependabot     | `.github/dependabot.yml` |

Convention: group related config in a domain-named directory.

### Pattern D: Tool-Specific Extension (.NET tooling)

| Tool      | Config File                |
| --------- | -------------------------- |
| NDepend   | `ProjectName.ndproj`       |
| SonarQube | `sonar-project.properties` |
| Coverlet  | `.coverletrc`              |

Convention: tool-specific extension or prefix.

### Which Pattern Fits QualityGates?

The .NET ecosystem favors **Pattern B (purpose-named)** and **Pattern C
(domain directory)**. QualityGates is a .NET tool in a .NET solution.
Pattern A (hidden dotfiles with tool prefix) is a JS convention — it
would look foreign here.

**Recommendation: hybrid of B + C.** A visible directory
(`quality-gates/` or `.quality-gates/`) grouping all quality-gates assets, with
purpose-named files inside. The directory name signals "this belongs to
QualityGates" — file names inside describe what they do.

## 3. Proposed Naming Convention

### Principle

> The filename must tell you exactly what the file is without reading its
> content. No abbreviations. No guessing.

### Directory: `quality-gates/`

Visible directory (not hidden — these files are edited regularly).
Placed at the solution root.

Why visible:

- .NET convention: `Properties/`, `wwwroot/`, `Connected Services/` are
  all visible directories for project-level config
- Developers edit these files regularly — hidden directories discourage
  discovery
- GitHub and VS Code display visible directories more prominently

Alternative considered: `.quality-gates/` (hidden). Rejected — these are
actively edited configuration files, not auto-generated cache.

### File Naming

```
{descriptive-purpose}.{format}
```

| Current Name                      | Proposed Name                            | Rationale                                                     |
| --------------------------------- | ---------------------------------------- | ------------------------------------------------------------- |
| `arch-rules.yml`                  | `architecture-rules.yml`                 | "arch" → "architecture" — no abbreviations                    |
| `/tmp/quality-gates-coverage.xml` | `/tmp/coverage-data.xml`                 | Generated artifact, prefix redundant when in `quality-gates/` |
| _(none)_                          | `quality-gates/exclusions.yml`           | Future: methods/files to exclude from gates                   |
| _(none)_                          | `quality-gates/quality-gates-config.yml` | Future: master config (thresholds, defaults)                  |

### CLI Flag Naming

| Current Flag      | Proposed Flag           | Rationale                                |
| ----------------- | ----------------------- | ---------------------------------------- |
| `--arch-config`   | `--architecture-config` | No abbreviation                          |
| `--mutate-source` | `--mutation-source`     | Consistent noun: "mutation" not "mutate" |
| `--mutate-scan`   | `--mutation-scan`       | Same                                     |
| `--project`       | _(keep)_                | Already clear                            |
| `--test-project`  | _(keep)_                | Already clear                            |
| `--coverage-file` | _(keep)_                | Already clear                            |
| `--auto-coverage` | _(keep)_                | Already clear                            |
| `--dupes`         | `--duplicates`          | "dupes" is slang, "duplicates" is formal |
| `--verbose`       | _(keep)_                | Already clear                            |
| `--changed`       | _(keep)_                | Already clear                            |

### Subcommand Naming

| Current Name | Proposed Name  | Rationale                                              |
| ------------ | -------------- | ------------------------------------------------------ |
| `crap`       | _(keep)_       | Industry-standard acronym, self-documenting in context |
| `scrap`      | _(keep)_       | Same — SCRAP is a recognized acronym                   |
| `arch`       | `architecture` | "arch" is ambiguous (archive? architecture?)           |
| `mutate`     | `mutation`     | Consistent noun form                                   |
| `dupes`      | `duplicates`   | Formal name                                            |
| `all`        | _(keep)_       | Already clear                                          |

## 4. File Placement Decision Tree

### The Two Solutions

This repository contains two distinct .NET solutions:

| Solution                          | Purpose             | SDK  | Lifecycle          |
| --------------------------------- | ------------------- | ---- | ------------------ |
| `redmuffin.Blazor.StaticWeb.slnx` | The application     | 9.0  | Changes frequently |
| `tools/redmuffin.Tools.slnx`      | Development tooling | 10.0 | Relatively stable  |

The tools solution is an **extension of the development workflow**, not
part of the application. Its configuration should live alongside it,
not mixed with application config.

### Decision Tree

```
┌─ Is this file about a solution's architecture/structure?
│  YES → Co-locate with the solution it governs
│    Main solution  → REPO_ROOT/quality-gates/
│    Tools solution → REPO_ROOT/tools/quality-gates/
│
│  NO → Is this file about HOW quality gates operate?
│    Universal config (applies to both solutions)
│      → REPO_ROOT/quality-gates/
│    Solution-specific config
│      → That solution's quality-gates/ directory
│
│  NO → Is this file auto-generated at runtime?
│    YES → /tmp/ (never committed to git)
│    NO  → Commit alongside what it governs
```

### Resulting Directory Structure

```
redmuffin.Blazor.StaticWeb/           # REPO_ROOT
├── quality-gates/                         # Main solution quality gates config
│   ├── architecture-rules.yml         #   Dependency rules for the app
│   ├── exclusions.yml                 #   (Future) Methods/files to skip
│   └── quality-gates-config.yml       #   (Future) Thresholds, defaults
│
├── src/                               # Application source code
├── tests/                             # Application test code
├── redmuffin.Blazor.StaticWeb.slnx    # Main solution file
│
├── tools/                             # Development tooling
│   ├── quality-gates/                     # Tools solution quality gates config
│   │   ├── architecture-rules.yml     #   Dependency rules for tools
│   │   └── exclusions.yml             #   (Future) Methods/files to skip
│   │
│   ├── src/                           # Tools source code
│   ├── tests/                         # Tools test code
│   └── redmuffin.Tools.slnx           # Tools solution file
│
└── /tmp/
    └── coverage-data.xml              # Auto-generated (never committed)
```

### Why Not Root-Level Files?

Putting `architecture-rules.yml` directly in the repo root:

- Pollutes root with tool config (what happens when we add 3 more config
  files?)
- No clear distinction between main-solution config and tools-solution
  config
- Doesn't scale to a `quality-gates/exclusions.yml` or future additions

The `quality-gates/` directory:

- One obvious place to find all quality gates configuration
- Mirrors .NET conventions (`Properties/`, `.github/`, `.vscode/`)
- Scales cleanly as we add more config files
- Clearly separates "application" from "tooling" concerns

### Default Discovery

The CLI should discover `architecture-rules.yml` automatically:

1. Check `--architecture-config` if provided
2. Check `<project>/quality-gates/architecture-rules.yml`
3. Check `<project>/../quality-gates/architecture-rules.yml` (parent directory)

This means running from `tools/` with `--project ../src/redmuffin.Blazor.StaticWeb`
would discover `../quality-gates/architecture-rules.yml` automatically — no
explicit `--architecture-config` flag needed for the main solution.

## 5. Migration Path

### Immediate (this session)

1. Rename `arch-rules.yml` → `quality-gates/architecture-rules.yml`
2. Rename `tools/src/redmuffin.Tools.QualityGates/arch-rules.yml`
   → `tools/quality-gates/architecture-rules.yml`
3. Update CLI defaults to discover from `quality-gates/` directory
4. Update `/tmp/quality-gates-coverage.xml` → `/tmp/coverage-data.xml`

### Short-term (next session)

5. Rename subcommands: `arch` → `architecture`, `dupes` → `duplicates`,
   `mutate` → `mutation`
6. Rename CLI flags: `--arch-config` → `--architecture-config`, etc.
7. Update all help text, README, and documentation

### Long-term (when needed)

8. Add `quality-gates/exclusions.yml` and `quality-gates/quality-gates-config.yml`
9. Add auto-discovery of `architecture-rules.yml` from parent directories

## 6. References

- Microsoft .NET Naming Guidelines:
  <https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/naming-guidelines>
- Dependency-Cruiser config conventions:
  <https://github.com/sverweij/dependency-cruiser>
- NDepend project file conventions:
  <https://www.ndepend.com/docs/ndepend-storage-and-files>
- SonarQube configuration:
  <https://docs.sonarsource.com/sonarqube-cloud/design-and-architecture/configuring-the-architecture-analysis/>
