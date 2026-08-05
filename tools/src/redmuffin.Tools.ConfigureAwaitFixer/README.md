# redmuffin.Tools.ConfigureAwaitFixer

Roslyn-based CA2007 **fixer** (not a whitespace formatter). On each `.cs`
post-edit hook it rewrites awaits that need `.ConfigureAwait(false)`, using
the official Microsoft CA2007 analyzer and `MSBuildWorkspace`. TUnit
`Assert.*` chains are left alone.

Delivery is **hooks + published WinExe only**. There is no NuGet
`PackageReference`, no local-feed install, and no MSBuild `.targets` safety
net (removed in `c3c141b1`). See
`docs/solutions/tooling-decisions/configureawait-fixer-nuget-targets-removal.md`.

## Requirements

- .NET 10 SDK
- Deployed binary under `~/.local/bin/ConfigureAwaitFixer/` (see Deploy)
- Harness post-edit hook wiring for `.cs` files

## Deploy

1. Build the project (the `CopyAnalyzerDll` target stages analyzer DLLs,
   BuildHost, and outputs into `publish/`):

   ```powershell
   dotnet build tools/src/redmuffin.Tools.ConfigureAwaitFixer/ConfigureAwaitFixer.csproj
   ```

2. Copy the staging directory to the user-local bin (create the destination
   if needed):

   ```powershell
   Copy-Item -Recurse -Force `
     tools/src/redmuffin.Tools.ConfigureAwaitFixer/publish/* `
     $HOME/.local/bin/ConfigureAwaitFixer/
   ```

3. Wire the harness to run the **exe** with `--fix` on `.cs` writes, for
   example Grok `~/.grok/hooks/bin/code-formatters.json`:

   ```text
   ConfigureAwaitFixer.exe --fix {{file}}
   ```

   Then csharpier (or your style tool). CAF is first; it is still a **fixer**.

The production PE is **WinExe** (subsystem 2) so a warm daemon can detach
without opening a console. After deploy, confirm subsystem 2 and that a
`--daemon` process can survive the hook Job Object (Task Scheduler path).
Details: `docs/solutions/performance-issues/configureawait-daemon-job-object-detached-spawn.md`.

## Manual run

```powershell
& "$HOME/.local/bin/ConfigureAwaitFixer/ConfigureAwaitFixer.exe" --fix path\to\File.cs
```

Cold open is multi-second; warm (daemon up) is ~sub-second. Logs:
`~/.grok/logs/configureawait-daemon.log`.

## What it does not do

- Does **not** run during `dotnet build` (MSBuildWorkspace + `.targets` deadlocks).
- Does **not** replace NetAnalyzers or `TreatWarningsAsErrors` — those still
  enforce CA2007 at commit/build time.
- Does **not** run on `.razor` (hooks use `dotnet format` there only).
- Is **not** a PackageReference consumer dependency — do not re-add one.

## Project layout

| Path                         | Role                                               |
| ---------------------------- | -------------------------------------------------- |
| `Program.cs` / fix pipeline  | CLI modes (`--fix`, daemon, one-shot)              |
| `publish/`                   | Deploy staging (committed or regenerated on build) |
| `ConfigureAwaitFixer.csproj` | `IsPackable=false`, `OutputType=WinExe`            |

## Related

- `docs/solutions/tooling-decisions/configureawait-fixer-nuget-targets-removal.md`
- `docs/solutions/conventions/fixer-vs-formatter-terminology.md`
- `docs/solutions/performance-issues/configureawait-daemon-job-object-detached-spawn.md`
- `docs/solutions/tooling-decisions/configureawait-msbuild-hook-incompatibility.md`
- `CONCEPTS.md` — ConfigureAwaitFixer (fixer), hook-owned fixer delivery
