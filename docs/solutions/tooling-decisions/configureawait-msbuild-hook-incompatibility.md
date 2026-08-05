---
module: tooling-decisions
tags:
  [
    configureawait,
    msbuild,
    msbuildworkspace,
    analyzer,
    auto-fix,
    architecture,
    dead-end,
    deadlock,
  ]
problem_type: architecture
date: 2026-05-17
last_updated: 2026-08-05
status: resolved
---

# The `.targets` Hook Is Fundamentally Incompatible With MSBuildWorkspace

> **Resolution (2026-06-08, refreshed 2026-08-05):** Path A is the only delivery
> path: post-edit harness hooks run the published
> `ConfigureAwaitFixer.exe --fix` (Grok: `~/.grok/hooks/bin/code-formatters.json`;
> earlier OpenCode: `tool.execute.after`). That path is outside MSBuild entirely
> and has zero deadlock risk. The repo `.targets` file and PackageReference that
> once looked like a secondary safety net were **removed in `c3c141b1`** — they
> never packed a working MSBuild import and are not a live net. See
> `configureawait-fixer-nuget-targets-removal.md`. `TreatWarningsAsErrors` remains
> gated on `DotNetWatchBuild` so CA2007 is a hard error during `dotnet build` but
> a warning during `dotnet watch`. The deadlock analysis below remains the
> authoritative reference for why `.targets` + MSBuildWorkspace is architecturally
> impossible.

## Problem

We want a transparent auto-fix on every `dotnet build`: write `await
FooAsync()`, run `dotnet build`, get zero CA2007 errors, and find
`.ConfigureAwait(false)` on the await. The fixer must use ONLY the
official Microsoft CA2007 analyzer (never heuristic detection). Phase 2
vision: generic framework that loads any analyzer DLL and applies
CodeFixProviders.

## Three `.targets` Timing Approaches — All Failed

### Approach 1: `AfterTargets="CoreCompile"`

Fixer runs after compilation finishes. Works — files are fixed. But the
CA2007 error is already visible in build output. Developer must run
`dotnet build` a second time. Defeats the purpose.

### Approach 2: `BeforeTargets="CoreCompile"` + BuildHost Crash

Fixer runs before compilation starts. MSBuildWorkspace.OpenProjectAsync
crashes with:

```
System.Exception: The build host could not be found at
'.../BuildHost-netcore/Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost.dll'
```

Root cause: `Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost.dll`
was missing from our NuGet package. We only extracted analyzer DLLs from
the NetAnalyzers nupkg but not the BuildHost subprocess that
MSBuildWorkspace spawns to evaluate projects.

**Fix applied:** Added BuildHost extraction from the Workspaces.MSBuild
nupkg (`contentFiles/any/any/BuildHost-netcore/*`) to the
`CopyAnalyzerDll` MSBuild target and the `None` ItemGroup for nupkg
inclusion. BuildHost crash eliminated.

### Approach 3: `AfterTargets="ResolveReferences"` + `BeforeTargets="CoreCompile"` — Deadlock

After fixing BuildHost, the fixer runs from the `.targets` hook but
hangs indefinitely. `dotnet build` never completes. Root cause: a
**deadlock between two MSBuild processes evaluating the same project
simultaneously.**

## The Real Problem: Process Deadlock, Not Performance

### What Actually Happens

1. User runs `dotnet build Project.csproj`
2. MSBuild process (parent) starts evaluating `Project.csproj`
3. `.targets` hook fires: `Exec Command="dotnet ConfigureAwaitFixer.dll ProjectDir/"`
4. Fixer process (child of dotnet, not of MSBuild) starts
5. Fixer calls `MSBuildWorkspace.OpenProjectAsync("ProjectDir/Project.csproj")`
6. MSBuildWorkspace spawns a **second** MSBuild process (BuildHost)
7. BuildHost tries to evaluate `Project.csproj`
8. Parent MSBuild also holds evaluation state for `Project.csproj`
9. **Deadlock** — both MSBuild processes wait on each other

### Evidence

The fixer works correctly from every context **except** the `.targets`
hook during an active `dotnet build`:

| Context                                       | Result            |
| --------------------------------------------- | ----------------- |
| Direct CLI on any project (simple or complex) | Works, sub-second |
| From debug build output on any project        | Works, sub-second |
| From NuGet cache on any project               | Works, sub-second |
| From `.targets` hook during `dotnet build`    | **Deadlocks**     |

The fixer completes in under 2 seconds on every project tested — simple
or complex. The deadlock is specific to the `.targets` hook because
that's the only context where two MSBuild instances evaluate the same
project simultaneously.

### Why This Is Expected

MSBuildWorkspace was designed for IDE tools (Visual Studio, OmniSharp,
Rider) that analyze projects between builds — not during builds. The
API explicitly creates a separate MSBuild process (BuildHost) to
evaluate projects. Two MSBuild processes cannot simultaneously evaluate
the same project file — MSBuild acquisition of project-level state
is not reentrant.

This is documented behavior in the MSBuildWorkspace design:

- `MSBuildWorkspace` creates an out-of-process BuildHost (separate
  `dotnet` process)
- The BuildHost loads the project via MSBuild evaluation
- MSBuild project evaluation is not thread-safe and not reentrant
- Calling `OpenProjectAsync` on a project currently being built by
  a parent MSBuild process produces undefined behavior (deadlock)

## The BuildHost Packaging Fix

For reference, here is how the BuildHost was correctly included in
the nupkg. This fix eliminated the crash (Approach 2) but did not
resolve the deadlock (Approach 3).

### Extraction from Workspace nupkg

In `ConfigureAwaitFixer.csproj`:

```xml
<PackageReference Include="Microsoft.CodeAnalysis.Workspaces.MSBuild"
                  GeneratePathProperty="true" />
```

```xml
<Target Name="CopyAnalyzerDll" AfterTargets="Build">
  <PropertyGroup>
    <_WorkspaceNupkg>$(PkgMicrosoft_CodeAnalysis_Workspaces_MSBuild)/
      microsoft.codeanalysis.workspaces.msbuild.5.3.0.nupkg</_WorkspaceNupkg>
  </PropertyGroup>
  <Exec Command="unzip -o -j &quot;$(_WorkspaceNupkg)&quot;
         &quot;contentFiles/any/any/BuildHost-netcore/*&quot;
         -d &quot;$(OutputPath)BuildHost-netcore/&quot;"
        ContinueOnError="true" />
</Target>
```

### Packaging for NuGet

```xml
<None Include="publish/BuildHost-netcore/*"
      Pack="true"
      PackagePath="tools/net10.0/any/BuildHost-netcore"
      Visible="false" />
```

BuildHost files end up at `tools/net10.0/any/BuildHost-netcore/`
in the nupkg, where MSBuildWorkspace expects to find them relative
to the executing assembly's directory.

## Viable Paths Forward

### Path A: Pre-Build Tool (Two Commands)

```bash
dotnet fix-configureawait && dotnet build
```

The fixer runs outside the MSBuild build, opens the project via
MSBuildWorkspace, fixes all CA2007 violations, and exits. Then
`dotnet build` runs on clean files — zero CA2007 errors.

**Advantages:** Full type resolution, official analyzer, generic
framework for any diagnostic. Fixer pays the MSBuildWorkspace cost
once per invocation (not once per build).

**Disadvantages:** Two commands, not one. But each command is fast —
the fixer runs once, the build runs on clean files.

### Path B: Roslyn DiagnosticSuppressor (In-Process During CoreCompile)

Write a Roslyn `DiagnosticSuppressor` that suppresses CA2007 and
applies the fix inline. Runs in-process during CoreCompile — no
separate process, no deadlock. Has access to the in-progress
compilation.

**Advantages:** Single build, single command, zero overhead.

**Disadvantages:** DiagnosticSuppressor can only suppress all instances
of a diagnostic or none — can't selectively suppress. If the fixer
misses an instance (e.g., in generated files), it's silently hidden.
Modifying source files during compilation is fragile.

### Path C: Syntax-Only with SDK Analyzer DLLs

Parse `.cs` files as syntax trees. Create a `CSharpCompilation` with
BCL references from the runtime (no MSBuildWorkspace). Load the
official NetAnalyzers DLL and run diagnostics against this lightweight
compilation. Apply fixes to syntax trees and write back.

**Advantages:** Single build, no deadlock, fast (no MSBuildWorkspace),
uses official analyzer detection.

**Disadvantages:** Cannot resolve NuGet types — the compilation only
has BCL references. Works for CA2007 (only needs `Task`/`ValueTask`
type resolution from BCL) but doesn't scale to analyzers that inspect
project-specific types. Violates Phase 2 generic framework vision.

### Path D: Roslynator CLI (When SDK 10 Compatible)

Roslynator CLI (`roslynator fix`) is the ideal architecture —
supports any analyzer DLL and any CodeFixProvider, reuses cached
compilation. Currently broken on SDK 10 due to
`System.Composition.AttributedModel` removal from the shared runtime.

**Advantages:** Nothing to build, proven architecture.

**Disadvantages:** Unknown fix timeline. Building our own equivalent
is a Roslynator-lite (months of work).

## The Generic Framework Conflict

The original Phase 2 vision — "fix ANY analyzer error with its
CodeFixProvider in one pass" — requires a **full compilation** with all
project references and NuGet types. The CodeFixProvider infrastructure
(`ApplyChanges`, `Solution` transformations) needs the full Roslyn
workspace model.

The `.targets` hook cannot provide this because MSBuildWorkspace
(which creates the full compilation) cannot run during an active
MSBuild build of the same project. The two requirements are in direct
tension:

- **Single `dotnet build`** → fixer must run during build → can't use
  MSBuildWorkspace
- **Generic for all analyzers** → needs full compilation → needs
  MSBuildWorkspace

These requirements can only be satisfied by running the fixer **outside**
the build — Path A (pre-build tool) or Path D (Roslynator CLI).

## Recommendation

**Path A: Pre-build tool.** The fixer runs as a standalone `dotnet fix`
command that uses MSBuildWorkspace to load the project, runs all
registered analyzers, fixes diagnostics with mechanical syntax rewrites,
and exits. Then `dotnet build` runs on clean files.

The workflow:

1. Developer writes code, including `await FooAsync()` without
   `.ConfigureAwait(false)`
2. Developer runs `dotnet fix && dotnet build`
3. Build completes with 0 errors, 0 warnings
4. Source file has `.ConfigureAwait(false)` added

Alternatively, wrap in a shell alias: `alias db='dotnet fix && dotnet build'`.

The fixer can also be wired as a pre-commit hook (runs before every
commit, ensures zero CA2007 violations reach the repository).

### Implemented resolution: OpenCode plugin

Path A was implemented as an OpenCode plugin that runs on every `.cs`
file write/edit via `tool.execute.after` hook — no developer action
needed, no manual `dotnet fix` step. Key design:

- **Outside MSBuild:** Hooks invoke the published binary
  (`~/.local/bin/ConfigureAwaitFixer/ConfigureAwaitFixer.exe --fix`),
  completely avoiding the MSBuild process tree and deadlock.
- **`isBuildActive()` guard (OpenCode-era):** If a `dotnet` build was in
  progress, the plugin skipped the fixer so it would not open
  `MSBuildWorkspace` against a live build. There is no build-time `.targets`
  catch-up path after `c3c141b1` — pre-commit `dotnet build` with
  `TreatWarningsAsErrors` is the gate for any miss.
- **Dual-hook pattern:** `tool.execute.after` (agent writes, no debounce)
  and `file.edited` (external saves, 300ms debounce). Grok uses the
  PostToolUse pipeline entry (generic formatters list) that calls the same WinExe.
- **`TreatWarningsAsErrors` gating:** `Directory.Build.props` disables
  `TreatWarningsAsErrors` during `dotnet watch` (via `DotNetWatchBuild`
  property). Rare hook misses produce warnings during watch, not
  broken hot-reload loops. Pre-commit `dotnet build` remains strict.

**Do not resurrect** a repo `.targets` + `MSBuildWorkspace` path as a
"secondary safety net." The timing experiments below show why every
in-build timing failed; packaging removal is documented in
`configureawait-fixer-nuget-targets-removal.md`.

## Key Decisions Documented

- `.targets` hook + MSBuildWorkspace = **deadlock.** Two MSBuild
  processes cannot evaluate the same project simultaneously. Do not
  attempt this combination.
- MSBuildWorkspace is designed for IDE tooling (between-build analysis),
  not for in-build hooks.
- BuildHost DLL must be included in any tool that uses MSBuildWorkspace.
  Extract from the Workspaces.MSBuild nupkg at build time.
- NuGet analyzer DLLs must be extracted from `.nupkg` (SFS convention
  prevents materialization on disk during restore).
- `CSharp.Workspaces` PackageReference required for MSBuildWorkspace
  C# language registration.
- CA2007 diagnostic span covers the inner expression (e.g.,
  `Task.Delay(100)`), not the outer `AwaitExpressionSyntax`. Walk up
  via `AncestorsAndSelf().OfType<AwaitExpressionSyntax>()`.
- CA2007 is `severity=none` by default in .NET 10. Test fixtures need
  explicit `.editorconfig` enabling the diagnostic.
- The `.targets` `ContinueOnError=true` attribute does NOT timeout the
  child process — it only ignores non-zero exit codes. A hung child
  process hangs the entire build indefinitely.
