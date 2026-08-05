---
date: 2026-08-05
last_updated: 2026-08-05
title: "General .NET repo scaffold checklist"
canonical_procedure: redmuffin.RepositoryTemplates/docs/specs/2026-08-05-general-dotnet-repo-scaffold-spec.md
status: relocated
tags:
  - scaffold
  - template
  - checklist
  - dotnet
  - inventory
---

# General .NET repo scaffold checklist (relocated)

> **Canonical inventory** lives in the platform repo
> `redmuffin.RepositoryTemplates`:
> `docs/general-dotnet-repo-scaffold-checklist-guide-2026-08-05.md`
> Edit that copy. This file is a discovery stub only.

Prune this list for your general .NET repo template. Keep every line you want
in the base. Delete every line you do not want. One candidate per line.

Source base: this Blazor solution’s **general** configs only. Blazor, Azure
Functions, SWA, SCSS, npm, and domain features are omitted on purpose.

## What Belongs in This File

- **Viewpoint**: You are defining what the general (non-Blazor) scaffold ships.
- **What belongs**: One include-candidate per line; short how-to; never-include
  list so domain stack does not creep back in.
- **What does NOT belong**: Full template-pack implementation code, Blazor host
  design, commit history, or per-feature domain docs. High-level platform
  mechanism (Layers A/B) does belong when decided.

## How to use

1. Delete any line under **Include** that you do not want in the base.
2. Leave the **Never include** section as a permanent deny list (or delete the
   whole section if you no longer need the reminder).
3. Remaining **Include** lines are the authoritative keep set for the scaffold.

## Include — root build and packages

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
tests/\*\*/.editorconfig — test exceptions (e.g. CA1707)
.gitignore — Visual Studio / .NET base (no Blazor path exceptions)
.gitattributes — LF for cs, json, yml, md, csproj, props, targets

## Include — analyzers (packages)

Meziantou.Analyzer
Microsoft.VisualStudio.Threading.Analyzers
Roslynator.Analyzers
Roslynator.CodeAnalysis.Analyzers
Roslynator.Formatting.Analyzers
StyleCop.Analyzers
AsyncFixer
Built-in Microsoft.CodeAnalysis.NetAnalyzers via EnableNETAnalyzers

## Include — testing packages

TUnit
TUnit.Assertions
Microsoft.Testing.Platform
LightMock.Generator

## Include — general library packages

Microsoft.Extensions.DependencyInjection.Abstractions
Microsoft.Extensions.Logging
Microsoft.Extensions.Http
Mediator.Abstractions
Mediator.SourceGenerator

## Include — GitHub

.github/dependabot.yml — NuGet weekly + cooldown + groups
.github/dependabot.yml — github-actions monthly + groups
.github/pipeline-neutral-patterns.txt
.github/workflows/codeql.yml — with pipeline-neutral skip
.github/workflows/ci.yml — generic restore/build/TUnit (new; not Azure SWA)

## Include — quality gates config

quality-gates/architecture-rules.yml — rewrite to generic zones (App/Shared/Tests)

## Include — folder structure

src/
tests/ — mirror of src/
docs/
quality-gates/
scripts/

## Include — solution skeleton projects

src/{Name}/ — class library project (net10.0 default)
tests/{Name}.Tests/ — TUnit test project
tests/{Name}.Tests — IsTestProject true

## Include — seed source types (optional kernel)

Result<T> / Result helpers
SlopwatchSuppressAttribute
AbsoluteUrlAttribute
IDelayProvider
MediatorServiceExtensions
LoggingBehavior (Mediator pipeline)

## Include — root prose files

AGENTS.md — general only (rewrite; strip Blazor/SCSS/Azure Functions)
README.md — template seed
CONCEPTS.md — empty glossary seed
CONTEXT.md

## Include — scaffold mechanism (platform, not app repo)

KEEP — decided 2026-08-05: two-layer platform (not a Blazor fork)
dotnet new template pack (Layer B) — cookie cutter: once per new repo, stamps folders/files
Redmuffin.Build NuGet package (Layer A) — shared props + analyzers; versioned; Dependabot updates all repos
GlobalPackageReference for analyzers via CPM — every project gets analyzers without repeating PackageReference
Layer B skeleton references Layer A — new repos start correct; old repos stay aligned via package bumps

## Include — ADRs (general; renumber later starting at 000)

docs/adr/0002-quality-gates-toolchain.md — quality-gates tool layout (separate solution, monolith, local feed)
docs/adr/0003-scrap-test-structural-analyzer.md — SCRAP test structural gate
docs/adr/0004-depth-structural-quality-gate.md — Depth structural gate
docs/adr/0006-test-double-hierarchy.md — pure extract / virtual override / interface / LightMock fallback
docs/adr/0007-zero-warnings-no-pragma-policy.md — TreatWarningsAsErrors; no pragma except known conflicts
docs/adr/0008-functional-csharp-standard.md — functional C# preferred style
docs/adr/0009-nuget-supply-chain-security.md — NuGet/supply-chain hardening (strip npm/SWA when porting)
docs/adr/0011-tunit-exclusive-framework.md — TUnit only; native coverage; no coverlet

## Related

- `docs/specs/2026-08-05-general-dotnet-repo-scaffold-spec.md` — canonical
  platform decisions, package IDs, feed policy, and keep-set
