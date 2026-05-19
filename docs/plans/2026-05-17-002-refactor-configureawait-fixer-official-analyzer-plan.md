---
title: "refactor: Replace ConfigureAwaitFixer detection with official CA2007 analyzer"
type: refactor
status: active
date: 2026-05-17
---

# refactor: Replace ConfigureAwaitFixer detection with official CA2007 analyzer

## Summary

Replace the syntax-only heuristic detection in `Program.cs` with the official
`Microsoft.CodeAnalysis.NetAnalyzers` CA2007 analyzer loaded via
`Assembly.LoadFrom`. Every other part of the fixer — `.targets` hook,
`Directory.Build.props` wiring, NuGet packaging, CI guard, CLI arguments —
stays unchanged. The change is **one method**: load the official analyzer,
run it, fix exactly what it flags. Nothing else.

---

## Problem Frame

v1.0.1 walks syntax trees guessing which `await` expressions need
`.ConfigureAwait(false)`. The official CA2007 analyzer ships in the .NET SDK
and is maintained by Microsoft. The fixer must delegate detection 100% to it.

---

## Requirements

- R1. Load the official CA2007 analyzer from `Microsoft.CodeAnalysis.NetAnalyzers`
  and run it against the target project's compilation
- R2. Apply `.ConfigureAwait(false)` ONLY to `await` expressions flagged by the
  official analyzer — zero heuristic detection
- R3. **Auto-fix on file save via OpenCode formatter pipeline.** The fixer runs
  on every `.cs` file save — before any build. The build's analyzer finds
  zero CA2007 violations because files are already clean.
  `.targets` hook removed (deadlocks — see `configureawait-msbuild-hook-incompatibility-2026-05-17.md`).
- R4. CI-skips via existing `$(CI)` / `$(GITHUB_ACTIONS)` guard (in fixer code, not .targets)
- R5. Bundle the analyzer DLL and MSBuildWorkspace BuildHost in the NuGet package
- R6. All production logic covered by tests
- R7. **Timer:** Fixer reports wall-clock duration on stderr for performance visibility
- R8. **Single-file mode:** `--file` flag limits analysis to one file (used by formatter pipeline)

**What does NOT change:** Root `Directory.Build.props` wiring, NuGet package identity,
local feed, `IsConfigureAwaitFixerProject` self-exclusion, CI guard mechanism,
`AddConfigureAwait` syntax factory. **What IS removed:** the `.targets` hook
(deadlocks during `dotnet build` — MSBuildWorkspace cannot open a project from
within its own MSBuild build).

---

## Scope Boundaries

- Only detection is replaced. Heuristic `AwaitExpressionSyntax` walker out,
  `Assembly.LoadFrom` + `WithAnalyzers` in.
- The `.targets` hook is **removed** from the NuGet package. The fixer runs
  on file save via OpenCode's formatter pipeline (`opencode.jsonc` formatter
  entry). No per-build overhead.
- The fixer accepts `--file <path>` in addition to the existing project-directory
  argument. In `--file` mode, it walks up to find the `.csproj`, loads
  MSBuildWorkspace, and fixes only the specified file.
- A precise wall-clock timer reports duration on stderr (format: `[ConfigureAwaitFixer] Completed in 1.234s`).
- Root wiring, package identity, CI guard, and all other infrastructure unchanged.

### Deferred to Follow-Up Work

- Adding CA1849, MA0016, CA1305, MA0002 diagnostic fixers
- Replacing the custom tool with Roslynator CLI when it ships `net10.0` support

---

## Context & Research

### Architecture research: `configureawait-auto-fix-research-2026-05-17.md`

After 5 dead-end approaches (MSBuild diagnostic extraction, syntax-only,
`dotnet format`, Roslynator CLI, CodeFixProvider discovery), the only viable
path is `Assembly.LoadFrom` on the official analyzer DLL loaded into an
`MSBuildWorkspace` compilation. Key findings:

- `dotnet format analyzers` reports CA2007 but NEVER invokes CodeFixProviders
- Roslynator CLI is broken on .NET 10 SDK (`System.Composition.AttributedModel`
  assembly removed)
- `Assembly.LoadFrom` is required because `AnalyzerAssemblyLoader` is `internal`
- `MSBuildWorkspace.OpenProjectAsync` reuses the compiled project — no second build

### Existing fixer: `tools/src/redmuffin.Tools.ConfigureAwaitFixer/`

| Component              | v1.0.1                                     | v1.0.2                                                                                                                           |
| ---------------------- | ------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------- |
| Detection              | Syntax-only `AwaitExpressionSyntax` walker | `Assembly.LoadFrom(analyzerDll)` → `WithAnalyzers()` → filter `CA2007`                                                           |
| Fix                    | `AddConfigureAwait` syntax factory         | Same factory, node found via `d.Location.SourceSpan`                                                                             |
| `IsTestAssertion`      | Fragile chain walker                       | **Removed** — official CA2007 never flags TUnit Assert chains                                                                    |
| `.targets`             | `BeforeTargets="CoreCompile"`              | **Removed** — fixer runs on file save via OpenCode formatter. MSBuildWorkspace deadlocks inside MSBuild builds.                  |
| `CoreCompileDependsOn` | Fixer runs before compilation              | Removed — no .targets hook                                                                                                       |
| CLI args               | `args[0]` = project directory              | `args[0]` = project directory, `--file <path>` for single-file mode. Timer on stderr.                                            |
| CI guard               | `.targets` Condition + env var             | Unchanged                                                                                                                        |
| Root wiring            | `Directory.Build.props` PackageReference   | Unchanged                                                                                                                        |
| NuGet packaging        | `IncludeBuildOutput=false`, `publish/*`    | Unchanged                                                                                                                        |
| New dependencies       | `Microsoft.CodeAnalysis.CSharp` only       | + `Microsoft.CodeAnalysis.NetAnalyzers`, `Microsoft.CodeAnalysis.CSharp.Workspaces`, `Microsoft.CodeAnalysis.Workspaces.MSBuild` |

### Key Technology

| Item                                | Version      | Source               |
| ----------------------------------- | ------------ | -------------------- |
| .NET SDK                            | 10.0.100+    | `tools/global.json`  |
| Roslyn (CodeAnalysis.CSharp)        | 5.3.0        | CPM                  |
| Roslyn CSharp Workspaces            | 5.3.0        | New PackageReference |
| Roslyn MSBuild Workspaces           | 5.3.0        | New PackageReference |
| Microsoft.CodeAnalysis.NetAnalyzers | TBD (latest) | New PackageReference |

---

## Key Technical Decisions

- **Replace detection, change nothing else.** The fixer already works — NuGet
  packaging, CI guard, root wiring. The only broken piece was detection (heuristic).
  Replace it and stop.
- **Fixer runs on file save, not on build.** The `.targets` hook is removed
  because MSBuildWorkspace deadlocks when called from within an active MSBuild
  build of the same project (see `configureawait-msbuild-hook-incompatibility-2026-05-17.md`).
  Instead, OpenCode's formatter pipeline invokes the fixer on `.cs` save.
- **Single-file mode (`--file`).** When the formatter pipeline saves a file,
  the fixer loads the project via MSBuildWorkspace (no active build — no deadlock),
  runs the official analyzer, and fixes only the saved file.
- **`PrivateAssets=none` on NetAnalyzers** so the package resolves as a
  dependency. At build time, an `unzip` Exec target extracts the two
  analyzer DLLs from the `.nupkg` (NuGet does not materialize files under
  `analyzers/dotnet/` to disk — SFS convention). Extracted to both
  build output and publish directory.
- **Tests in existing test project** — `tools/tests/redmuffin.Tools.QualityGates.Tests/`.
  No new test project. The fixer's public API (a directory → files modified) is
  testable through its existing CLI entry point.
- **`.targets` `ContinueOnError=true`** preserved so a fixer crash doesn't
  break the build on dev machines.

---

## Implementation Units

- U1. **Add analyzer dependencies and remove `.targets` hook packaging**

**Goal:** Add `Microsoft.CodeAnalysis.NetAnalyzers` and
`Microsoft.CodeAnalysis.Workspaces.MSBuild` PackageReferences, the
corresponding CPM version entries, and remove the `.targets` file
from the NuGet package.

**Requirements:** R5

**Dependencies:** None

**Files:**

- Modify: `tools/src/redmuffin.Tools.ConfigureAwaitFixer/ConfigureAwaitFixer.csproj`
- Modify: `tools/Directory.Packages.props`
- Modify: `tools/src/redmuffin.Tools.ConfigureAwaitFixer/build/redmuffin.Tools.ConfigureAwaitFixer.targets`
  (kept as reference but excluded from packaging)

**Approach:**

- Add `PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers"`
  with `PrivateAssets="none"` and `GeneratePathProperty="true"` to fixer csproj
- Add `PackageReference Include="Microsoft.CodeAnalysis.Workspaces.MSBuild"`
  with `GeneratePathProperty="true"` for BuildHost extraction
- Add `PackageVersion` entries for both to `tools/Directory.Packages.props`
- Remove `None Include="build/*.targets"` ItemGroup from packaging
- Add `unzip` Exec target to extract analyzer DLLs from NetAnalyzers nupkg
- Add `unzip` Exec target to extract BuildHost-netcore/\* from Workspaces nupkg

**Test scenarios:**

- Happy path: `dotnet publish` succeeds and `dotnet pack` produces a nupkg
  containing both NetAnalyzers DLLs and BuildHost-netcore/ in
  `tools/net10.0/any/`
- Happy path: `.targets` file is NOT in the NuGet package

**Verification:**

- `dotnet publish` + `dotnet pack` succeeds
- `unzip -l tools/nupkgs/redmuffin.Tools.ConfigureAwaitFixer.1.0.2.nupkg`
  shows analyzer DLLs and BuildHost in the package, no .targets file

---

- U2. **Replace detection in Program.cs with official analyzer + add --file and timer**

**Goal:** Delete the syntax-only heuristic detection. Add `Assembly.LoadFrom`
on the bundled analyzer DLL. Run `compilation.WithAnalyzers(analyzers)`,
filter to `d.Id == "CA2007"`, apply `.ConfigureAwait(false)` at each
diagnostic location. Add `--file` flag for single-file mode and
wall-clock timer on stderr.

**Requirements:** R1, R2, R6, R7, R8

**Dependencies:** U1

**Files:**

- Modify: `tools/src/redmuffin.Tools.ConfigureAwaitFixer/Program.cs`
- Create: `tools/tests/redmuffin.Tools.QualityGates.Tests/ConfigureAwaitFixerTests.cs`

**Approach:**

- Load analyzer from both NetAnalyzers assemblies: `Assembly.LoadFrom` on
  `Microsoft.CodeAnalysis.NetAnalyzers.dll` (base, contains CA2007) and
  `Microsoft.CodeAnalysis.CSharp.NetAnalyzers.dll` (C#-specific).
  CA2007 is in the base assembly, not the CSharp one.
- Instantiate: filter types to `DiagnosticAnalyzer` subtypes, `Activator.CreateInstance`
- Load project: `MSBuildWorkspace.Create().OpenProjectAsync(projectPath)`
- Run: `compilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync(token)`
- Filter: `d.Id == "CA2007"`
- Fix per file: group by path, `OrderByDescending(d => d.Location.SourceSpan.Start)`,
  `root.FindNode(diag.Location.SourceSpan)` — the diagnostic span covers
  the inner expression (e.g., `Task.Delay(100)`), not the outer
  `AwaitExpressionSyntax`. Walk up via
  `found.AncestorsAndSelf().OfType<AwaitExpressionSyntax>().FirstOrDefault()`
  to find the parent await node. Apply `.ConfigureAwait(false)` transform,
  verify result parses via `CSharpSyntaxTree.ParseText(fixedSource).GetDiagnostics()`,
  write via `File.WriteAllTextAsync`
- **Remove**: `IsTestAssertion` and `StartsWithAssertChain` methods entirely
  (official CA2007 never flags TUnit assertion chains — no heuristic needed)
- **Remove**: the `AwaitExpressionSyntax` walker that guesses which awaits
  need fixing (replaced by `WithAnalyzers` diagnostic filter)
- **Add**: `--file <path>` flag. When provided, fixer walks up from the file
  path to find the `.csproj`, loads MSBuildWorkspace with that project,
  and only fixes `d.Location.SourceTree.FilePath == resolvedFile`.
  When absent, fixes all source files in the project (existing behavior).
- **Add**: `Stopwatch` timer. Print `[ConfigureAwaitFixer] Completed in X.XXXs`
  to stderr on every run (even if 0 fixes).
- **Keep**: `AddConfigureAwait` syntax factory (reused — still the same transform)
- **Keep**: `IsSourceFile` filter
- **Keep**: `HasConfigureAwait` guard (the official analyzer may or may not
  double-flag already-fixed awaits; this is cheap insurance)
- C# type-check in `GetEnvironmentVariable` uses
  `string.Equals(..., StringComparison.OrdinalIgnoreCase)` for case-insensitive
  CI variable matching

**Execution note:** Write a characterization test FIRST against v1.0.1:
create a test project with a known CA2007 violation, run the fixer, verify
the file is modified at the correct location. Then replace detection, re-run
the same test, verify identical result — but now driven by the official analyzer.

**Test scenarios:**

- Happy path: a single `await FooAsync()` without `.ConfigureAwait(false)`
  is fixed by the official analyzer's diagnostic. Test fixture must include
  an `.editorconfig` with `dotnet_diagnostic.CA2007.severity = warning` —
  CA2007 is disabled by default in .NET 10.
- Happy path: a file where every `await` already has `.ConfigureAwait(false)`
  produces 0 fixes (no false positives)
- Edge case: `await using var x = FooAsync()` — official analyzer's behavior
  is accepted as-is (the fixer does not second-guess)
- Edge case: two CA2007 diagnostics in the same file — both fixed, descending
  order preserves span positions
- Error path: fixer runs with `CI=true` → returns 0 without modifying files
- Error path: analyzer DLL missing → clear error message, non-zero exit

**Verification:**

- Create a `.csproj` with an intentional CA2007 violation, run the fixer,
  verify `.ConfigureAwait(false)` is added at the exact node the official
  analyzer flagged
- All QualityGates tests still pass (`dotnet run --project
tools/tests/redmuffin.Tools.QualityGates.Tests`)
- Fixer tests pass

---

- U3. **Pack v1.0.2, wire into OpenCode formatter, rebuild both solutions**

**Goal:** Pack the v1.0.2 NuGet package (minus `.targets`), add formatter
entry to `opencode.jsonc`, clear caches, rebuild, run all tests.

**Requirements:** R3, R4, R6

**Dependencies:** U2

**Files:**

- Modify: `tools/src/redmuffin.Tools.ConfigureAwaitFixer/ConfigureAwaitFixer.csproj`
  (bump `<Version>` to `1.0.2`)
- Modify: `.opencode/opencode.jsonc` (add `configureawait-fixer` formatter entry)

**Approach:**

- Bump version to `1.0.2`
- Run `dotnet publish` (regenerates publish/ with analyzer DLLs + BuildHost)
- Run `dotnet pack --output tools/nupkgs/`
- Add to `opencode.jsonc` formatter section:
  ```json
  "configureawait-fixer": {
    "command": ["dotnet", "/home/flynn/.nuget/packages/redmuffin.tools.configureawaitfixer/1.0.2/tools/net10.0/any/ConfigureAwaitFixer.dll", "--file", "$FILE"],
    "extensions": [".cs"]
  }
  ```
- Clear NuGet caches: `dotnet nuget locals all --clear`
- Rebuild both solutions
- Run all tests
- Smoke test: save a `.cs` file with an intentional CA2007 violation —
  verify the formatter pipeline fixes it before the next build

**Verification:**

- Both solutions: 0 errors, 0 warnings
- All tests pass
- `.cs` file saved with `await FooAsync()` → file has `.ConfigureAwait(false)`
  added before `dotnet build` runs
- Timer output visible on stderr

---

## System-Wide Impact

- **Interaction graph:** The fixer no longer has a `.targets` hook. It runs
  on `.cs` file save via OpenCode's formatter pipeline. The fixer opens the
  project via MSBuildWorkspace (no active build — no deadlock), runs the
  official CA2007 analyzer, and fixes the saved file. `dotnet build` then
  runs on clean files — zero CA2007 errors.
- **Unchanged invariants:** Root `Directory.Build.props` wiring unchanged.
  NuGet package identity, local feed, CI guard, `IsConfigureAwaitFixerProject`
  self-exclusion all unchanged.
- **Removed code:** `IsTestAssertion`, `StartsWithAssertChain`, and the
  `AwaitExpressionSyntax` walker are deleted. The official analyzer replaces
  all three with zero heuristic code.

---

## Risks & Dependencies

| Risk                                                                         | Mitigation                                                                                                                                       |
| ---------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------ |
| `Microsoft.CodeAnalysis.NetAnalyzers` version changes internal DLL structure | `PrivateAssets=none` + `dotnet publish` auto-copies whatever the resolved version ships. Tested at pack time.                                    |
| `MSBuildWorkspace.OpenProjectAsync` deadlocks during `dotnet build`          | Fixer is removed from `.targets` hook. Runs on file save where no MSBuild build is active — no conflict.                                         |
| Analyzer flags nodes that aren't `AwaitExpressionSyntax`                     | The diagnostic span covers the inner expression. Walk up via `AncestorsAndSelf().OfType<AwaitExpressionSyntax>()` to find the parent await node. |
| NuGet cache staleness after re-packing                                       | `dotnet nuget locals all --clear` is documented in U3.                                                                                           |
| Fixer adds 2-5s to file save via MSBuildWorkspace                            | Accepted tradeoff. First save per file pays the cost; subsequent saves exit instantly (0 CA2007). Timer provides visibility.                     |
| Formatter pipeline modifies file mid-edit                                    | Same behavior as existing `dotnet format` entry. Editor reloads modified file.                                                                   |

---

## Sources & References

- **Architecture research:** `docs/solutions/tooling-decisions/configureawait-auto-fix-research-2026-05-17.md`
- **Deadlock analysis:** `docs/solutions/tooling-decisions/configureawait-msbuild-hook-incompatibility-2026-05-17.md`
- **Existing fixer:** `tools/src/redmuffin.Tools.ConfigureAwaitFixer/Program.cs`
- **TDD discipline:** `rm-tdd` (`.opencode/skills/rm-tdd/SKILL.md`)
