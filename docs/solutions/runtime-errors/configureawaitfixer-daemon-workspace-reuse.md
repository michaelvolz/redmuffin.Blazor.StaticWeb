---
title: "ConfigureAwaitFixer daemon: 'already part of the workspace' crash on referenced-project files"
date: 2026-08-05
category: runtime-errors/
module: tools/src/redmuffin.Tools.ConfigureAwaitFixer
problem_type: runtime_error
component: tooling
severity: high
symptoms:
  - "Fix requests for a file in a referenced library project exit with code 1 when a tests project that references it was opened first"
  - "PostToolUse formatter hook logs FORMATTER FAIL [exit] in %TEMP%\morpheus-hook-failures.jsonl"
  - "Daemon log records FATAL: System.ArgumentException: '<project>' is already part of the workspace"
  - "Failures recur deterministically until the daemon is killed; re-running the edit or the hook does not help"
root_cause: wrong_api
resolution_type: code_fix
tags: [configureawaitfixer, roslyn, msbuildworkspace, daemon, formatter-hook, project-reference, workspace-reuse, project-graph]
related_components: [development_workflow]
---

# ConfigureAwaitFixer daemon: 'already part of the workspace' crash on referenced-project files

## Problem

ConfigureAwaitFixer (CAF), the first `.cs` formatter in the Grok/Morpheus PostToolUse hook chain (harness host config, outside this repo), crashed with `ArgumentException: '<project>' is already part of the workspace` when a fix request arrived for a file in a project the workspace had already loaded as a _reference_ (for example, a library pulled in by an opened tests project). Because the client converts any daemon failure into `FATAL:` + exit code 1, every such request surfaced as a failed formatter hook, which the Morpheus host treated as an edit failure.

## Symptoms

- **Exit-code-1 formatter hook failures** in Morpheus sessions: the hook chain runs `ConfigureAwaitFixer.exe --fix <file>` (`tools/src/redmuffin.Tools.ConfigureAwaitFixer/Program.cs:12-13`), and `FixClient.RunAsync` writes `FATAL: <message>` to stderr and returns 1 on any failed response (`tools/src/redmuffin.Tools.ConfigureAwaitFixer/FixClient.cs:44-49`) or any exception (`FixClient.cs:50-56`). The host records these as `FORMATTER FAIL [exit]` lines in `%TEMP%\morpheus-hook-failures.jsonl` (`CodeFormatters.FailureLogPath` — harness host config, verified against the host rather than this repo).
- **`FATAL: System.ArgumentException: '<project>' is already part of the workspace`** in the daemon log, written by the per-request catch in `HandleClientAsync` (`Daemon.cs:198`).
- **Which requests trigger it:** only files in a project the daemon has never explicitly opened but that the workspace already owns — precisely the referenced projects. The exact production shape was: the daemon opens a **tests** project (fixing a file in it), MSBuildWorkspace pulls every `ProjectReference` target library into the workspace solution, and a later request for a **library** file cache-misses and hits the duplicate open.

## What Didn't Work

- **Killing the daemon** unblocked the immediate session (a fresh process starts with an empty workspace), but it recurs: the next time a tests project is opened, its referenced libraries re-enter the workspace graph and the next library-file request crashes again. The daemon is spawned detached on the first `--fix` of a session, so a kill buys exactly one clean session before the next tests-project open rebuilds the graph (session history).
- **Re-running the edit** did not help; the daemon process keeps its workspace across requests, so the second run hits the identical cache miss and exception. The failure was deterministic once the workspace contained the referenced project.
- No other remediation attempt was made; the durable fix is the workspace reuse described below.

## Solution

The fix is a **reuse-before-open** lookup at the top of the project-open path. `EnsureProjectOpenAsync` (`Daemon.cs:336-367`) previously went straight from a `_projectIds` cache miss to `OpenProjectAsync`. It now first asks the workspace solution whether it already owns a project with that csproj path, reuses that `ProjectId`, and records it in the cache:

```csharp
// tools/src/redmuffin.Tools.ConfigureAwaitFixer/Daemon.cs:341-366
if (
    _projectIds.TryGetValue(projectPath, out var cached)
    && _workspace.CurrentSolution.GetProject(cached) is not null
)
{
    return cached;
}

// MSBuildWorkspace opens the whole project graph, so a project we have
// only seen as a reference (e.g. a library pulled in by an opened tests
// project) is already part of the workspace solution. Re-opening it
// throws "'<project>' is already part of the workspace" — reuse the
// already-open project instead.
var existing = FindOpenProject(projectPath);
if (existing is not null)
{
    _projectIds[projectPath] = existing.Id;
    return existing.Id;
}

DaemonLog.Info($"Opening project {projectPath}");
var project = await _workspace
    .OpenProjectAsync(projectPath, cancellationToken: cancellationToken)
    .ConfigureAwait(false);
_projectIds[projectPath] = project.Id;
return project.Id;
```

The lookup was extracted as a helper, `FindOpenProject` (`Daemon.cs:377-382`), which scans the live workspace solution for a project whose `FilePath` matches the requested csproj, using `StringComparison.OrdinalIgnoreCase`:

```csharp
// tools/src/redmuffin.Tools.ConfigureAwaitFixer/Daemon.cs:378-382
private Project? FindOpenProject(string projectPath) =>
    _workspace.CurrentSolution.Projects.FirstOrDefault(p =>
        !string.IsNullOrEmpty(p.FilePath)
        && string.Equals(p.FilePath, projectPath, StringComparison.OrdinalIgnoreCase)
    );
```

The cache itself is the case-insensitive `Dictionary<string, ProjectId> _projectIds` (`Daemon.cs:34-36`), and the whole open+sync sequence already runs under the single `_workspaceLock` semaphore (`Daemon.cs:290-309`), so the lookup-and-store pair is race-free.

A second, behavior-preserving change was needed to keep the method-length analyzer (MA0051) happy: the reload fallback that used to live inline at the end of `SyncDocumentAsync` (`Daemon.cs:383-436`) was extracted into `ReloadProjectAsync` (`Daemon.cs:445-471`). It does exactly what the inline block did — when `TryApplyChanges` rejects the in-memory document sync (typically a brand-new file not yet part of the evaluated project), it removes the project from the workspace, clears the `_projectIds` entry, reopens the csproj, and returns the text the workspace actually holds. No behavior changed; the extraction only shortened `SyncDocumentAsync`.

### Regression test

`should_reuse_referenced_project_already_in_workspace` (`tools/tests/redmuffin.Tools.ConfigureAwaitFixer.Tests/DaemonTests.cs:51-79`) reproduces the exact production shape:

1. Creates a tests project whose csproj carries a `<ProjectReference Include=".../lib/Lib.csproj">` (via `CreateTestProjectAsync`'s `projectReference` parameter, `DaemonTests.cs:301-336`), with a bare-await file in each project.
2. Runs `--fix` on the app file first — opening the tests project pulls the library into the MSBuildWorkspace solution.
3. Runs `--fix` on the library file — the path that used to crash.

It asserts the second call exits 0, its stderr does **not** contain `"already part of the workspace"`, and the library file was actually fixed (contains `.ConfigureAwait(false)`). The suite is 37/37 green.

### Deployment

The fix was built and deployed to the hook binary location (`~/.local/bin/ConfigureAwaitFixer/`, SHA-256 verified), and the old daemon process was killed so the next `--fix` spawns a fresh daemon running the fixed binary.

## Why This Works

The root cause is an API misuse driven by MSBuildWorkspace's project-graph semantics. `OpenProjectAsync` loads the _entire_ project graph reachable from the requested csproj — opening a tests project also opens every referenced library into the workspace solution (`Daemon.cs:349-352`), while the daemon only cached the one csproj it explicitly opened. The resulting state (project present in `CurrentSolution` but absent from `_projectIds`) made the cache lookup at `Daemon.cs:341-347` miss, and the second `OpenProjectAsync` on a project Roslyn already owns throws `ArgumentException: '<project>' is already part of the workspace`.

Reusing the already-open project is the correct API use: `CurrentSolution.Projects` is the authoritative list of projects the workspace owns, so checking it before opening closes the exact gap the cache could not see. The `OrdinalIgnoreCase` comparison (`Daemon.cs:380`) matches how the cache keys are already compared (`Daemon.cs:34-36`) and absorbs drive-letter/case drift between the csproj path discovered by `Arguments.FindCsproj` and the `FilePath` MSBuild recorded. Once found, storing the id in `_projectIds` also makes every later request for that project a plain cache hit.

## Prevention

- **Regression coverage:** the referenced-project test (`DaemonTests.cs:51-79`) pins the contract that a fix request for a file in a project the workspace already loaded must succeed — exit 0, no "already part of the workspace" in stderr, file actually fixed.
- **General rule for this daemon:** before calling `OpenProjectAsync`, check `CurrentSolution.Projects` for an already-open project with the same csproj path; the cache alone is not sufficient because MSBuildWorkspace loads whole project graphs that the cache never sees. `FindOpenProject` is the single place implementing that rule, so any future open path should route through it.
- **Open limitation (documented, not fixed):** `ReloadProjectAsync` (`Daemon.cs:445-471`) still calls `OpenProjectAsync` directly after `RemoveProject`. `RemoveProject` removes only the named project — its references remain in the workspace solution — so in a multi-project solution where references survive, a reload could in theory hit the same duplicate-open. The trigger is narrow (a brand-new file not yet in the evaluated project, whose project was loaded as a reference), it has not been observed in production, and fixing it was not authorized; it is recorded here so the next change to this path knows to route the reopen through the same reuse-before-open check.

## Related Issues

- `docs/solutions/performance-issues/configureawait-daemon-job-object-detached-spawn.md` — sister doc on the same daemon and the same hook-failure observable (`%TEMP%\morpheus-hook-failures.jsonl`); this fix adds a second crash root cause to that symptom surface (moderate overlap — consolidation review recommended).
- `docs/solutions/developer-experience/automated-configureawait-fixer.md` — canonical fixer doc describing the MSBuildWorkspace engine this fix guards.
- `docs/solutions/tooling-decisions/configureawait-auto-fix-research.md` — documents the `OpenProjectAsync` engine choice whose project-graph behavior caused the crash.
- `docs/solutions/tooling-decisions/configureawait-msbuild-hook-incompatibility.md` — why MSBuildWorkspace project-graph evaluation and hook (not `.targets`) delivery shaped this design.
- `docs/solutions/tooling-decisions/configureawait-fixer-nuget-targets-removal.md` — the delivery model (harness post-edit hooks + published WinExe client/daemon) under which the daemon runs.
- `docs/solutions/conventions/fixer-vs-formatter-terminology.md` — PostToolUse formatter-pipeline terminology behind the `FORMATTER FAIL [exit]` symptom.
- `docs/configureawait-fixer-status-guide-2026-06-08.md` — dated snapshot of the fixer's MSBuildWorkspace architecture; predates the workspace-reuse guard.
