---
date: 2026-08-05
version: 1.1.0
last_updated: 2026-08-05
title: General .NET repo scaffold platform
status: relocated
canonical_procedure: redmuffin.RepositoryTemplates/docs/specs/2026-08-05-general-dotnet-repo-scaffold-spec.md
purpose: >
  Normative decisions and keep-set file list for the general (non-Blazor)
  .NET repository scaffold platform: Layer A standards package, Layer B
  template package, naming, feed, RiverBooks-shaped application structure,
  and generated-repo inventory.
scope:
  - Platform architecture (Layer A living standards + Layer B cookie cutter)
  - Repository path and name for the platform home
  - NuGet package IDs for Layer A and Layer B
  - Casing rules for redmuffin-prefixed names
  - Feed policy for v1
  - RiverBooks-shaped application structure (not full DDD) as the default
  - Keep-set file and config list stamped into new product repos
  - Explicit never-include deny list
  - Deferred and open items for implementation
exclude:
  - Full Domain-Driven Design tactical patterns as a required model
  - Blazor, Azure Functions, SWA, SCSS, or npm scaffold content in Layer B
  - Step-by-step implementation of the template pack projects
  - Product feature domain code from redmuffin.Blazor.StaticWeb
  - Public NuGet publishing beyond local feed for v1
  - ADR renumbering procedure (deferred)
canonical_for:
  - general-dotnet-repo-scaffold
  - redmuffin-default-sdk
  - redmuffin-default-template
tags:
  - scaffold
  - template
  - nuget
  - platform
  - checklist
  - riverbooks
  - mediator
---

# General .NET repo scaffold platform (relocated)

> **Canonical home moved** to platform repo
> `redmuffin.RepositoryTemplates`:
> `docs/specs/2026-08-05-general-dotnet-repo-scaffold-spec.md`
> (and `AGENTS.md` / checklist there). Edit that copy. This file is a
> discovery stub only.

## What Belongs in This File

- **Viewpoint**: Implementers building `redmuffin.RepositoryTemplates` and
  anyone generating a new general .NET repo from Layer B
- **What belongs**: Locked platform decisions, package and repo names,
  casing rules, feed policy, Layer A/B roles, RiverBooks structure default,
  keep-set file list, never-include deny list, open items still required
  before code
- **What does NOT belong**: Full DDD playbooks, Blazor/SWA/SCSS/npm product
  stack in the general template, product domain features, session chat,
  commit history, full template source trees

## 0 — Critical Viewpoint (READ FIRST)

1. **Two layers, one platform repo.** Living standards (Layer A) and the
   cookie cutter (Layer B) ship from one Git home. They are not a fork of
   the Blazor product solution.
2. **Repo name ≠ package ID.** The platform repository identity is separate
   from both NuGet package IDs (same idea as namespace ≠ class name).
3. **Only `redmuffin` is forced lowercase.** Every other name segment uses
   normal product casing. Do not invent all-lowercase segments from the
   prefix alone.
4. **v1 feed is local only** for `redmuffin.Default.*` packages. Generated
   product repos still use nuget.org for third-party packages.
5. **The keep-set below is authoritative.** If a line is removed from the
   keep-set, it is out of scope for the scaffold. Prune first; implement
   only what remains.
6. **RiverBooks structure for everything from now on.** Application layout
   is Ardalis RiverBooks **structure** (modules, Mediator, Result, tests
   mirror `src`) — **not** full Domain-Driven Design. This product repo
   already has most of that shape; new work and new repos follow it.

## 1 — Scope and Definitions

| Term                 | Meaning                                                                                                                                            |
| -------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Platform repo**    | Git repository that holds Layer A and Layer B sources                                                                                              |
| **Layer A**          | Versioned NuGet of shared MSBuild defaults, analyzer wiring, and policy consumed for the life of product repos                                     |
| **Layer B**          | `PackageType=Template` pack; installed once per machine/user; stamps a new product skeleton that references Layer A                                |
| **Product repo**     | A repo created by Layer B (not the platform repo itself)                                                                                           |
| **Keep-set**         | Include lines that remain after pruning; sole include authority for what the template stamps                                                       |
| **Local feed (v1)**  | Folder or machine-local NuGet source for `redmuffin.Default.SDK` and `redmuffin.Default.Template` only                                             |
| **RiverBooks shape** | Modular homes, Mediator as application API, `Result`/`Result<T>` at expected-failure boundaries, tests mirror `src` — structure only, not full DDD |

## 2 — Locked Decisions

### 2.1 Platform architecture

| Decision                   | Value                                                                                                                                                                                                             |
| -------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Shape                      | Two-layer platform (not a Blazor product fork)                                                                                                                                                                    |
| Layer A role               | Living standards: MSBuild defaults (Nullable, TreatWarningsAsErrors, AnalysisMode, and related), analyzer wiring, optional shared editorconfig fragments; continuous policy via Dependabot bumps on product repos |
| Layer B role               | Cookie cutter: `dotnet new install` → stamp skeleton once (`src/`, `tests/`, CI, Dependabot, ADR docs, seed files, wire Layer A). Not a runtime project dependency after generation                               |
| Coupling                   | Layer B skeleton references Layer A; tighten rules by bumping A; improve skeleton by shipping a new B (affects new repos only)                                                                                    |
| Analyzers in product repos | Prefer `GlobalPackageReference` via Central Package Management so every project gets analyzers without repeating `PackageReference`                                                                               |

### 2.2 Repository identity

| Decision           | Value                                                                                          |
| ------------------ | ---------------------------------------------------------------------------------------------- |
| Platform repo name | `redmuffin.RepositoryTemplates`                                                                |
| Custom repo naming | Product repo names are `redmuffin.` + a clear unique product name (no machine path convention) |
| Repo vs package    | Never reuse the repository name as either Layer A or Layer B package ID                        |

### 2.3 Package identity

| Layer | Role                   | Package ID                   |
| ----- | ---------------------- | ---------------------------- |
| **A** | Living standards       | `redmuffin.Default.SDK`      |
| **B** | Cookie cutter template | `redmuffin.Default.Template` |

Rejected placeholders (do not restore): `redmuffin.Build`, `redmuffin.Templates`, bare names that equal the platform repo name.

### 2.4 Casing

| Segment         | Rule                                                                                                               |
| --------------- | ------------------------------------------------------------------------------------------------------------------ |
| `redmuffin`     | Always lowercase                                                                                                   |
| All other words | Normal product casing as written (e.g. `Default`, `SDK`, `Template`, `RepositoryTemplates`, `Blazor`, `StaticWeb`) |
| Agent rule      | Do not guess casing; do not force the rest of a name lowercase because the prefix is lowercase                     |

### 2.5 Feed policy (v1)

| Decision                                                 | Value                                                                                                 |
| -------------------------------------------------------- | ----------------------------------------------------------------------------------------------------- |
| `redmuffin.Default.SDK` and `redmuffin.Default.Template` | Local feed only for v1 — no public NuGet publish required                                             |
| Third-party packages in product repos                    | nuget.org (and package source mapping as in keep-set)                                                 |
| QualityGates local tools feed                            | Product keep-set may still use `./tools/nupkgs` for local tools (orthogonal to platform package feed) |

### 2.6 Application structure (RiverBooks shape)

| Decision                  | Value                                                                                                                                                                                                         |
| ------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Default for all new work  | RiverBooks-shaped modular structure from now on — product repos and scaffold-generated repos                                                                                                                  |
| Not required              | Full DDD (aggregates, repositories-as-DDD-pattern, ubiquitous-language ceremony as a mandatory model)                                                                                                         |
| Required structure        | Modules for reusable capability code; composition root/host; tests that mirror `src`                                                                                                                          |
| Required application API  | Mediator for use-case requests, pipeline behaviors, and host decoupling                                                                                                                                       |
| Required error model      | Shared `Result` / `Result<T>` at expected-failure module boundaries                                                                                                                                           |
| UI hosts (when present)   | Three homes: **Modules** (no UI markup), **Pages** (route), **Components** (shared UI). Detail: ADR 0013                                                                                                      |
| General / non-UI scaffold | Stamp Modules triad (+ Contracts when public API), Common (kernel + pipeline), host composition, Mediator, Result, mirrored tests — no Blazor Pages/Components unless a UI template is explicitly added later |
| This product repo         | Already mostly on this shape (structure + Mediator + Result); finish remaining modularization against the roadmap, do not invent a parallel style                                                             |
| One capability            | One module; never fold two capabilities into one module unless the user explicitly authorizes the merge                                                                                                       |
| Cross-capability reuse    | Project reference, not nested folders                                                                                                                                                                         |

Canonical depth for the Blazor product:

- `docs/adr/0013-riverbooks-modular-layout-and-result.md`
- `docs/specs/2026-08-03-riverbooks-modularization-roadmap-spec.md`
- `docs/modular-monolith-module-guide-2026-08-03.md`

### 2.7 Testing and coverage

| Decision       | Value                                                  |
| -------------- | ------------------------------------------------------ |
| Test framework | TUnit only (see ADR 0011 keep-set entry)               |
| Coverage       | TUnit / Microsoft Testing Platform native coverage     |
| Coverlet       | Out of scope — removed from general scaffold inventory |
| Test layout    | Always mirror `src` (RiverBooks / product hard rule)   |

### 2.8 Scope rule for keep-set lines

If a line is deleted from the keep-set, treat it as out of scope. Do not
re-add from memory or from the Blazor product repo without an explicit
decision to restore it.

### 2.9 Domain stack deny list

Never stamp into the general scaffold:

- Blazor WebAssembly host or Blazor-specific project types
- Azure Functions / SWA deployment units and workflows
- SCSS / Sass pipeline and `app.min.css` production flow
- npm / Node frontend toolchain
- Product domain features from `redmuffin.Blazor.StaticWeb`
- A parallel architecture style that competes with RiverBooks shape
  (flat “one lib + one test” only is not the destination skeleton)

## 3 — Keep-set File List

Authoritative include inventory for what Layer B stamps (and what Layer A
may carry as shared policy). One candidate per line. Source base: general
configs distilled from the Blazor solution, domain stack omitted.

### 3.1 Root build and packages

```text
Directory.Build.props — Nullable enable
Directory.Build.props — ImplicitUsings enable
Directory.Build.props — Authors / Version / FileVersion / AssemblyVersion
Directory.Build.props — NeutralLanguage
Directory.Build.props — GenerateDocumentationFile false
Directory.Build.props — EnableNETAnalyzers true
Directory.Build.props — AnalysisLevel latest
Directory.Build.props — AnalysisMode recommended
Directory.Build.props — AnalysisModeGlobalization None
Directory.Build.props — AnalysisModeSecurity All
Directory.Build.props — AnalysisModeReliability All
Directory.Build.props — AnalysisModePerformance All
Directory.Build.props — TreatWarningsAsErrors true (except DotNetWatchBuild)
Directory.Build.props — WarningsAsErrors Nullable
Directory.Build.props — LangVersion preview
Directory.Build.props — Debug: CheckForOverflowUnderflow false, Deterministic false, ProduceReferenceAssembly false
Directory.Build.props — Release: CheckForOverflowUnderflow true, Deterministic true, ProduceReferenceAssembly true
Directory.Build.props — ExcludeFromCodeCoverage patterns (obj/bin/Migrations)
Directory.Build.props — RestorePackagesWithLockFile false
Directory.Packages.props — ManagePackageVersionsCentrally true
Directory.Packages.props — CentralPackageTransitivePinningEnabled true
global.json — SDK pin + rollForward latestMinor
nuget.config — clear sources; nuget.org only
nuget.config — packageSourceMapping for nuget.org
nuget.config — local-tools feed ./tools/nupkgs (QualityGates)
_.slnx — solution file (skeleton: app + tests)
_.sln.DotSettings — minimal shared Rider/ReSharper words
.editorconfig — C# style, naming, analyzer severities
tests/**/.editorconfig — test exceptions (e.g. CA1707)
.gitignore — Visual Studio / .NET base (no Blazor path exceptions)
.gitattributes — LF for cs, json, yml, md, csproj, props, targets
```

### 3.2 Analyzer packages

```text
Meziantou.Analyzer
Microsoft.VisualStudio.Threading.Analyzers
Roslynator.Analyzers
Roslynator.CodeAnalysis.Analyzers
Roslynator.Formatting.Analyzers
StyleCop.Analyzers
AsyncFixer
Built-in Microsoft.CodeAnalysis.NetAnalyzers via EnableNETAnalyzers
```

### 3.3 Testing packages

```text
TUnit
TUnit.Assertions
Microsoft.Testing.Platform
LightMock.Generator
```

### 3.4 General library packages

```text
Microsoft.Extensions.DependencyInjection.Abstractions
Microsoft.Extensions.Logging
Microsoft.Extensions.Http
Mediator.Abstractions
Mediator.SourceGenerator
```

### 3.5 GitHub

```text
.github/dependabot.yml — NuGet weekly + cooldown + groups
.github/dependabot.yml — github-actions monthly + groups
.github/pipeline-neutral-patterns.txt
.github/workflows/codeql.yml — with pipeline-neutral skip
.github/workflows/ci.yml — generic restore/build/TUnit (new; not Azure SWA)
```

### 3.6 Quality gates config

```text
quality-gates/architecture-rules.yml — rewrite to generic zones (App/Shared/Tests)
```

### 3.7 Folder structure (RiverBooks-shaped)

```text
src/
src/{Solution}.Common/ — kernel types, Result, pipeline behaviors
src/{Solution}.Modules/{Module}.Contracts/ — public queries, responses, ports
src/{Solution}.Modules/{Module}/ — handlers, services, DI (no UI markup)
src/{Solution}/ — host / composition root
tests/ — mirror of src/ (Modules, host, Common as present)
docs/
quality-gates/
scripts/
```

UI-only homes (`Pages/`, `Components/`) are **not** in the general
non-Blazor template; they apply when the host has UI (this product — ADR
0013).

### 3.8 Solution skeleton projects

```text
src/{Solution}.Common/ — class library (net10.0 default); Result + Mediator pipeline
src/{Solution}.Modules/{Module}.Contracts/ — class library; public API types
src/{Solution}.Modules/{Module}/ — class library; implementations + handlers
src/{Solution}/ — host composition root (project type depends on host; general default is library or console — not Blazor)
tests/{Solution}.Modules/{Module}.Tests/ — TUnit; IsTestProject true
tests mirror for Common/host when they hold testable surface
```

### 3.9 Seed source types

**Required by RiverBooks shape (§2.6):**

```text
Result / Result<T> helpers
MediatorServiceExtensions (or equivalent host registration)
LoggingBehavior (Mediator pipeline) — or equivalent first pipeline behavior
```

**Optional kernel** (not locked for v1 — promote only by explicit decision):

```text
SlopwatchSuppressAttribute
AbsoluteUrlAttribute
IDelayProvider
```

### 3.10 Root prose files

```text
AGENTS.md — general only (rewrite; strip Blazor/SCSS/Azure Functions)
README.md — template seed
CONCEPTS.md — empty glossary seed
CONTEXT.md
```

### 3.11 Scaffold mechanism (platform repo, not product repo)

```text
Two-layer platform (Layer A + Layer B) in redmuffin.RepositoryTemplates
redmuffin.Default.Template — Layer B cookie cutter pack
redmuffin.Default.SDK — Layer A shared props + analyzers; versioned
GlobalPackageReference for analyzers via CPM
Layer B skeleton references Layer A
```

### 3.12 ADRs (general; renumber later starting at 000)

Port from product docs when building Layer B content. Renumber is deferred.

```text
docs/adr/0002-quality-gates-toolchain.md
docs/adr/0003-scrap-test-structural-analyzer.md
docs/adr/0004-depth-structural-quality-gate.md
docs/adr/0006-test-double-hierarchy.md
docs/adr/0007-zero-warnings-no-pragma-policy.md
docs/adr/0008-functional-csharp-standard.md
docs/adr/0009-nuget-supply-chain-security.md — strip npm/SWA when porting
docs/adr/0011-tunit-exclusive-framework.md — TUnit only; no Coverlet
docs/adr/0013-riverbooks-modular-layout-and-result.md — structure + Result (not full DDD)
```

## 4 — Open Items (not locked)

These do not block writing this spec; they must be decided before or during
implementation:

| Topic                                 | Notes                                                                  |
| ------------------------------------- | ---------------------------------------------------------------------- |
| `dotnet new` short name               | Not set                                                                |
| Package versioning scheme             | Not set (e.g. start at `1.0.0`; how A bumps for consumers)             |
| Local feed path for platform packages | Not set (e.g. `./nupkgs` on the platform repo vs a fixed machine path) |
| Optional seed kernel remainder (§3.9) | `IDelayProvider` and attributes in or out of v1                        |
| General host project type             | Library vs console vs other non-Blazor host for Layer B default        |
| ADR renumber to 000x                  | Explicitly deferred                                                    |
| Platform repo Git remote              | Local path only so far                                                 |
| Platform-repo CI                      | Not set                                                                |
| Authors / license metadata            | Not set                                                                |

## 5 — Verification

| Check            | Pass criteria                                                                                                                                     |
| ---------------- | ------------------------------------------------------------------------------------------------------------------------------------------------- |
| Package IDs      | Product and platform docs name `redmuffin.Default.SDK` and `redmuffin.Default.Template` only                                                      |
| Casing           | Every new `redmuffin.*` name keeps `redmuffin` lowercase and does not force other segments lowercase                                              |
| Feed (v1)        | Pack and install of A/B use a local source; no requirement to publish A/B to nuget.org                                                            |
| Keep-set         | Layer B content maps 1:1 to remaining keep-set lines; deleted lines are absent                                                                    |
| RiverBooks shape | Skeleton has Modules (+ Contracts when public), Common with Result, Mediator wiring, tests under mirrored paths — not a single flat lib-only tree |
| Deny list        | Generated product tree has no Blazor, Functions, SWA, SCSS, or npm scaffold                                                                       |
| Coverlet         | No Coverlet package reference in scaffold inventory                                                                                               |

## Related

- `docs/general-dotnet-repo-scaffold-checklist-guide-2026-08-05.md`
- `docs/adr/0013-riverbooks-modular-layout-and-result.md`
- `docs/specs/2026-08-03-riverbooks-modularization-roadmap-spec.md`
- `docs/modular-monolith-module-guide-2026-08-03.md`
- `docs/adr/0011-tunit-exclusive-framework.md`
