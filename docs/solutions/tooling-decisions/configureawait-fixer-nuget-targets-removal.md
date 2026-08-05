---
title: "ConfigureAwaitFixer NuGet and .targets removal: hooks own delivery"
date: 2026-08-05
category: tooling-decisions
module: configureawait-fixer
problem_type: tooling_decision
component: tooling
severity: medium
applies_when:
  - "Choosing delivery for a local developer tool: NuGet build-time package vs harness hooks plus a published binary"
  - "A tool must outlive the process that launched it (warm daemon spawned from a hook)"
  - "A PackageReference is CI-gated and restores only from a machine-local package cache"
  - "Considering a secondary MSBuild .targets path for a tool whose primary path is post-edit hooks"
tags:
  - configureawait
  - nuget
  - msbuild-targets
  - delivery-model
  - hooks
  - job-object
  - packaging
  - tooling
related_components:
  - development_workflow
  - documentation
---

# ConfigureAwaitFixer NuGet and .targets removal: hooks own delivery

## Context

ConfigureAwaitFixer is the repo’s Roslyn-based CA2007 fixer under
`tools/src/redmuffin.Tools.ConfigureAwaitFixer/`. It rewrites `.cs` files after
agent edits (and can run one-shot from the CLI). That role sits outside the
normal build: MSBuild does not rewrite author source mid-compile and feed the
rewrite into the same compilation.

For most of the tool’s history (see
`docs/solutions/developer-experience/automated-configureawait-fixer.md` and the
2026-05/06 research), delivery was dual-channel:

1. **Primary — harness post-edit hooks.** Grok PostToolUse / formatters invoke
   a deployed binary (`ConfigureAwaitFixer.exe --fix {{file}}`); OpenCode used a
   `tool.execute.after` plugin path historically. Hooks already owned real
   auto-fix after writes.
2. **Secondary (nominal) — local NuGet package plus a repo `.targets` file.** A
   development-dependency package `redmuffin.Tools.ConfigureAwaitFixer` (version
   1.0.2) was referenced from root `Directory.Build.props` and version-pinned in
   `Directory.Packages.props`. A sibling targets file lived at
   `tools/src/redmuffin.Tools.ConfigureAwaitFixer/build/redmuffin.Tools.ConfigureAwaitFixer.targets`
   (historical — removed by this fix in `c3c141b1`) and _intended_ to run the
   fixer during `CoreCompile` (After `ResolveReferences`, Before `CoreCompile`,
   CI skip). **In the 1.0.2 pack that consumers restored, that targets file was
   not included:** pack items only shipped `tools/net10.0/any/*` (fixer +
   analyzers + BuildHost). The PackageReference therefore restored a package
   with no `build/` assets and was **inert at build time**, while still
   coupling restore to a machine-local global-packages cache.

The MSBuild path was never a true in-build auto-fix even when targets ran.
Earlier designs that opened `MSBuildWorkspace` from a `.targets` hook
**deadlocked** (parent MSBuild plus BuildHost evaluating the same project).
That analysis is
`docs/solutions/tooling-decisions/configureawait-msbuild-hook-incompatibility.md`.
Even the simpler Exec-based targets could not host the warm daemon the hook path
needs, and they could not rewrite source in a way that affects the _current_
compilation. Calling the targets a “secondary safety net” overstated both
packing and usefulness (related learnings refreshed 2026-08-05).

Commit `c3c141b1` removed the package-plus-targets surface entirely: the gated
`PackageReference`, the CPM pin, pack metadata on the project (`IsPackable`
false), pack items for README/publish into a nupkg, and the orphan `.targets`
file. The commit message states the delivery rationale: the hook owns delivery,
and a build-time plugin cannot outlive the Job Object boundary that kills hook
descendants.

## Guidance

**Hooks own delivery for editor-time tools; do not keep a second net for
comfort.** When the real fix path is per-edit hooks plus a published executable,
an extra MSBuild channel does not close a gap. It splits debugging (which path
ran?), pays restore/pack cost, and falsely labels itself a “secondary safety
net.” A build-time target cannot rewrite the file mid-compilation and have that
rewrite feed the same compile. Post-edit hooks already perform the only useful
role: fix the file after the write.

**Match delivery to process lifetime.** Warm fixing (cold on the order of
several seconds vs warm on the order of hundreds of milliseconds) needs a daemon
that survives the hook process. The harness Job Object kills hook descendants;
only a detached spawn (Task Scheduler) plus a WinExe client/daemon can meet that
requirement. That story is
`docs/solutions/performance-issues/configureawait-daemon-job-object-detached-spawn.md`.
A NuGet asset restored into a build cannot host that daemon. Hooks plus a
published WinExe can.

**Do not gate solution restore on a machine-local tool cache.** The 1.0.2
package was not on nuget.org and was not a committed artifact under
`tools/nupkgs/` (unlike QualityGates packages). Clearing the local global
packages folder broke `dotnet restore` for the main solution until someone
repacked — for a package that did not even import build targets. Restore-time
packaging for a tool that must run _outside_ MSBuild is pure cost when the
package carries no meaningful build assets for consumers.

**Keep enforcement and fixing separate.** CA2007 enforcement at build time
remains NetAnalyzers plus `TreatWarningsAsErrors` (and watch gating) in
`Directory.Build.props`. The fixer edits files; hooks run the fixer after
edits. The removed integration blurred that line without ever delivering a real
in-build fix.

**When deleting pack surface, keep deployment staging.** `ConfigureAwaitFixer.csproj`
still uses `CopyAnalyzerDll` to stage NetAnalyzers DLLs, MSBuildWorkspace
`BuildHost-netcore`, and fixer outputs into
`tools/src/redmuffin.Tools.ConfigureAwaitFixer/publish/` for copy to
`~/.local/bin/ConfigureAwaitFixer/`. Packaging is gone; deploy staging is not.

## Why This Matters

1. **Restore hygiene.** After removal, the main solution’s restore graph does
   not depend on ConfigureAwaitFixer existing in any package cache.
2. **Architecture honesty.** Dual delivery with an inert package and an orphan
   `.targets` file taught agents that a “secondary safety net” still mattered.
   It did not; the only path that can host warm, Job-Object-safe fixing is hooks
   plus a published binary.
3. **Single failure surface.** One invocation path keeps timeouts, logs, and
   FATAL behavior attributable (daemon log, process list, Morpheus hook failure
   JSONL) instead of inventing a second channel that never ran.
4. **CI clarity.** The PackageReference was already gated off CI; the
   package only existed on dev machines where hooks already ran. On those
   machines it still did not run the fixer at build time (no packed `build/`
   assets). The nominal second net was restore risk without fix value.

## When to Apply

- Choose **hooks + published binary** when the tool rewrites files per edit and
  must stay warm across hook invocations.
- Before adding a “secondary” MSBuild integration for a hook-primary tool,
  require a gap the hooks cannot close. “Safety net” alone is not a gap.
- If a package would restore only from a machine-local cache, or is gated off
  CI, ask whether packaging is doing anything hooks do not already do.
- Keep local NuGet feeds for tools that real consumers restore (for example
  QualityGates under `tools/nupkgs/`). The lesson is not “no packages”; it is
  “no packages for tools that must run outside MSBuild and are delivered by
  hooks.”

## Examples

### Before — gated PackageReference (removed in `c3c141b1`)

Root `Directory.Build.props` (dev-only condition, not on CI / GitHub Actions /
the fixer project itself):

```xml
<PackageReference Include="redmuffin.Tools.ConfigureAwaitFixer">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

`Directory.Packages.props` pinned
`<PackageVersion Include="redmuffin.Tools.ConfigureAwaitFixer" Version="1.0.2" />`.

### After — no package surface

- No `redmuffin.Tools.ConfigureAwaitFixer` entry in `Directory.Build.props` or
  `Directory.Packages.props`.
- `tools/src/redmuffin.Tools.ConfigureAwaitFixer/ConfigureAwaitFixer.csproj`
  sets `<IsPackable>false</IsPackable>` and `<OutputType>WinExe</OutputType>`;
  pack items for README and `publish/*` into a nupkg are gone.
- Targets file removed by this fix (`c3c141b1`; historical citation only):
  `tools/src/redmuffin.Tools.ConfigureAwaitFixer/build/redmuffin.Tools.ConfigureAwaitFixer.targets`
  (CoreCompileDependsOn / After ResolveReferences / Before CoreCompile; Exec
  `dotnet …ConfigureAwaitFixer.dll` on `$(MSBuildProjectDirectory)`; CI skip).
  Not packed into the 1.0.2 nupkg — pack items only covered README and
  `publish/*` → `tools/net10.0/any`.

### Deployment that replaced packaging

1. Build the fixer project (CopyAnalyzerDll stages
   `tools/src/redmuffin.Tools.ConfigureAwaitFixer/publish/`).
2. Copy that staging directory to `~/.local/bin/ConfigureAwaitFixer/`.
3. Wire harness hooks to `ConfigureAwaitFixer.exe --fix {{file}}` on `.cs`
   writes (Grok: `~/.grok/hooks/bin/code-formatters.json` or equivalent).

## Related

- `docs/solutions/performance-issues/configureawait-daemon-job-object-detached-spawn.md`
  — why the warm daemon must detach from the Job Object and use WinExe; why
  hooks + published binary can host it and NuGet/`.targets` cannot.
- `docs/solutions/tooling-decisions/configureawait-msbuild-hook-incompatibility.md`
  — deadlock analysis for `.targets` + MSBuildWorkspace (resolution banner
  notes removal of the nominal secondary net).
- `docs/solutions/tooling-decisions/configureawait-auto-fix-research.md` —
  research that preferred a separate script outside MSBuild; Current banner
  matches hooks-only delivery.
- `docs/solutions/developer-experience/automated-configureawait-fixer.md` —
  journey log; architecture table and secondary-net claims marked historical.
- `docs/configureawait-fixer-status-guide-2026-06-08.md` §9.3 — historical
  PackageReference / restore-risk snapshot; not a template to resurrect.
- `tools/src/redmuffin.Tools.ConfigureAwaitFixer/README.md` — deploy via
  publish + hooks (PackageReference install removed).
- `tools/nuget.config` — `local-tools` maps QualityGates only (ConfigureAwaitFixer
  pattern removed).
- `docs/configureawait-fixer-status-guide-2026-06-08.md` — status snapshot;
  §4 / §9.3 refreshed for hooks-only delivery.
- `CONCEPTS.md` — ConfigureAwaitFixer daemon, detached spawn, headless
  observability, and hook-owned fixer delivery.
