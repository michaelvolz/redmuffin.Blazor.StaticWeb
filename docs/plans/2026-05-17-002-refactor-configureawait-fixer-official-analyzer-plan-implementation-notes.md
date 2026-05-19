# Implementation Notes — ConfigureAwaitFixer v2

**Plan:** `2026-05-17-002-refactor-configureawait-fixer-official-analyzer-plan.md`
**Date:** 2026-05-17

---

## Decisions Not In Plan

### 1. Analyzer DLL resolution: NuGet SFS problem

**Decision:** Use `unzip` shell command to extract analyzer DLLs from the `.nupkg` during build.

**Why:** The `Microsoft.CodeAnalysis.NetAnalyzers` package stores DLLs under `analyzers/` inside the `.nupkg`. NuGet does not extract them to disk by default (Single File Storage). `GeneratePathProperty` resolves the package root, but the files at `analyzers/Microsoft.CodeAnalysis.*.dll` are not materialized on disk during restore — they only exist inside the compressed `.nupkg`. MSBuild's `<Copy>` task fails with "file not found" even though the path is correct. The `.nupkg` IS always on disk at `$(NuGetPackageRoot)/microsoft.codeanalysis.netanalyzers/VERSION/`.

**Tradeoffs:**

- `unzip -o -j` extracts only the two needed DLLs, not the entire 2.2 MB nupkg
- Requires `unzip` on the build machine (available on Linux/macOS by default; Windows has PowerShell Expand-Archive as fallback)
- MSBuild `<Unzip>` task was considered but doesn't support file-level include filtering

**Alternative rejected:** Committing DLLs to repo. Would require updating them on every NetAnalyzers version bump.

### 2. BuildHost DLL extraction

**Decision:** Extract `BuildHost-netcore/` from the `Microsoft.CodeAnalysis.Workspaces.MSBuild` nupkg alongside the analyzer DLLs.

**Why:** MSBuildWorkspace spawns a separate `BuildHost` process to evaluate MSBuild projects. The BuildHost DLL (`Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost.dll`) and its dependencies must be present relative to the executing assembly's directory. Without them, `OpenProjectAsync` crashes with `System.Exception: The build host could not be found`.

**Packaging:** Extracted to `$(OutputPath)BuildHost-netcore/` and `publish/BuildHost-netcore/`. Packaged as `tools/net10.0/any/BuildHost-netcore/` in the nupkg. MSBuildWorkspace discovers BuildHost at `<AppContext.BaseDirectory>/BuildHost-netcore/`.

### 3. `.targets` hook removed — deadlock discovered

**Decision:** Remove the `.targets` file from the NuGet package. The fixer runs on file save via OpenCode's formatter pipeline instead.

**Why:** MSBuildWorkspace spawns a child MSBuild process (BuildHost) to evaluate projects. When called from a `.targets` hook during `dotnet build`, the parent MSBuild and child BuildHost both evaluate the same project → **deadlock.** Two MSBuild processes cannot simultaneously evaluate the same project file. MSBuildWorkspace is designed for IDE tooling (between-build analysis), not for in-build hooks.

Full analysis: `docs/solutions/tooling-decisions/configureawait-msbuild-hook-incompatibility-2026-05-17.md`

### 4. Save-time architecture via OpenCode formatter

**Decision:** The fixer runs on `.cs` file save via OpenCode's formatter pipeline (`opencode.jsonc`). No `.targets` hook.

**Why:**

- On save, no MSBuild build is active — MSBuildWorkspace can open the project safely
- Zero CA2007 errors visible in `dotnet build` (files are clean before compilation)
- LLM never needs to remember an extra command
- Pairs with existing `dotnet format` formatter entry (same save pipeline)

### 5. Single-file mode (`--file`)

**Decision:** `--file <path>` flag limits analysis and fixing to one file.

**Why:** The formatter pipeline saves one file at a time. The fixer should only fix that file. Implementation: walk up from file path to find `.csproj`, load MSBuildWorkspace for the project, run the analyzer, filter diagnostics to `d.Location.SourceTree.FilePath == resolvedFile`.

### 6. Wall-clock timer

**Decision:** `Stopwatch` timer prints `[ConfigureAwaitFixer] Completed in X.XXXs` to stderr on every run.

**Why:** Provides visibility into the overhead added to the save → format → fix pipeline. Even on 0-fix runs (common case after initial cleanup), the duration is reported.

### 2. test project path resolution

**Decision:** `AppContext.BaseDirectory` requires 5 `..` levels to reach tools root from test project output directory.

**Why:** Test project outputs to `tools/tests/redmuffin.Tools.QualityGates.Tests/bin/Debug/net10.0/`. Five `..` levels reach the tools root, then `src/redmuffin.Tools.ConfigureAwaitFixer`.

### 3. MSBuildWorkspace project loading: requires .csproj

**Decision:** Fixer now requires a `.csproj` in the target directory (scans `*.csproj` in TopDirectoryOnly).

**Why:** v1 fixer worked on raw directories of `.cs` files (syntax-only). v2 fixer needs `MSBuildWorkspace.OpenProjectAsync()` which requires a real project file to produce a full compilation (needed for the analyzer's `WithAnalyzers` API). Tests were updated to create a minimal `.csproj`.

**Tradeoffs:**

- Fixer no longer works on loose `.cs` files — requires a project context
- This is correct for the `.targets` hook usage (`$(MSBuildProjectDirectory)` always has a `.csproj`)
- The `.targets` hook `AfterTargets="CoreCompile"` means the project was just compiled — compilation is fresh in cache

### 4. `HasConfigureAwait` guard kept even with official analyzer

**Decision:** The `HasConfigureAwait` check (line ~100) is preserved as additional defense.

**Why:** The official analyzer may re-flag already-fixed awaits if the file has multiple issues and one fix invalidates other diagnostic locations. It's cheap insurance (string comparison on the syntax tree).

### 5. Nested `FindNode` fixed with ReplaceNode overload

**Decision:** Replaced `root.FindNode(d.Location.SourceSpan)` with `newRoot.FindNode(d.Location.SourceSpan)` and used the `ReplaceNode(SyntaxNode, SyntaxNode)` overload (takes old+new node, not old+lambda).

**Why:** After `newRoot = newRoot.ReplaceNode(...)`, the old `root.FindNode()` returns a node from the stale tree — the span position is correct but the node identity is wrong for ReplaceNode. `newRoot.FindNode()` returns the node from the updated tree. The `ReplaceNode(oldNode, newNode)` overload is simpler and fixes the CS1660 lambda-type error.

### 6. Post-fix syntax validation

**Decision:** After applying fixes to a file, parse the result with `CSharpSyntaxTree.ParseText()` and check for `DiagnosticSeverity.Error`. Skip the write if parse errors exist.

**Why:** If CA2007's diagnostic source span maps to a node that doesn't accept `.ConfigureAwait(false)` wrapping cleanly, the corrupted file is caught before writing to disk. This was planned. Added.

### 7. MA0076/MA0075 culture-sensitive ToString in diagnostic logging

**Decision:** Used `Diagnostic.Id` + `Diagnostic.GetMessage()` instead of `Diagnostic.ToString()` for error messages.

**Why:** `Diagnostic.ToString()` triggers MA0075 (implicit culture-sensitive ToString). `GetMessage()` returns a `LocalizableString` which is already culture-aware. For int values in interpolated strings, used `.ToString(CultureInfo.InvariantCulture)`.

---

## File Changes Beyond Plan

| File                          | Change                                                               | Reason                                                        |
| ----------------------------- | -------------------------------------------------------------------- | ------------------------------------------------------------- |
| `ConfigureAwaitFixer.csproj`  | Added `GeneratePathProperty="true"` on NetAnalyzers PackageReference | Needed to resolve `$(PkgMicrosoft_CodeAnalysis_NetAnalyzers)` |
| `ConfigureAwaitFixer.csproj`  | Added `unzip` Exec target for analyzer DLLs                          | NuGet SFS — DLLs not on disk (see Decision 1)                 |
| `ConfigureAwaitFixerTests.cs` | Added `CreateTestProjectAsync` helper                                | v2 fixer needs .csproj (see Decision 3)                       |
| `ConfigureAwaitFixerTests.cs` | Fixed path: 5 `..` levels                                            | See Decision 2                                                |
| `ConfigureAwaitFixerTests.cs` | `file static class` for fixture (not `[Before(TestSession)]`)        | TUnit0042: global hooks must be in separate class             |
| `Program.cs`                  | `CultureInfo` using added                                            | MA0076 fix                                                    |
| `Program.cs`                  | `GroupBy` with `StringComparer.Ordinal`                              | MA0002 fix                                                    |
| `Program.cs`                  | `ReplaceNode(oldNode, newNode)` not lambda                           | CS1660 fix                                                    |
| `Program.cs`                  | `newRoot.FindNode()` not `root.FindNode()`                           | Stale tree fix (see Decision 5)                               |

---

## Pending Issues

1. **MSBuild target `CopyAnalyzerDll` uses `unzip` — not Windows-compatible.** Will need `Expand-Archive` fallback for Windows CI. Low priority (CI doesn't build the fixer).

2. **`ContinueOnError="true"` on unzip Exec.** If `unzip` is missing on the build machine (unlikely), the build proceeds but without analyzer DLLs — fixer will exit 1 with a clear error message. Acceptable failure mode.

3. **Save-time fixer adds overhead to `.cs` save.** First save per file: 2-5s (MSBuildWorkspace loads project). Subsequent saves: instant (0 CA2007 to find). Timer provides visibility. Accepted tradeoff.

## Final Verification (2026-05-17)

- **Tools solution:** 426 tests, 425 passed, 1 skipped, 0 failed. 0 errors, 0 warnings.
- **Main solution:** 365 tests, 365 passed. 0 errors, 0 warnings.
- **`.targets` hook:** Removed from packaging. Fixer runs on file save via OpenCode formatter.
- **Nupkg:** `redmuffin.Tools.ConfigureAwaitFixer.1.0.2.nupkg` contains both analyzer DLLs + BuildHost-netcore/. No `.targets` file.

## Key Discoveries

1. **CA2007 analyzer lives in `Microsoft.CodeAnalysis.NetAnalyzers.dll`, not `CSharp.NetAnalyzers.dll`.** The CSharp DLL has language-specific analyzers; the base DLL has language-agnostic ones including CA2007.

2. **CA2007 is DISABLED by default in .NET 10.** Requires explicit `.editorconfig` `dotnet_diagnostic.CA2007.severity = warning` or `AnalysisMode=AllEnabledByDefault` to fire. Our project's `Directory.Build.props` already enables it. Test fixtures need their own `.editorconfig`.

3. **Diagnostic SourceSpan points to inner expression, not outer AwaitExpressionSyntax.** CA2007's diagnostic location covers `Task.Delay(100)` not `await Task.Delay(100)`. Fix uses `AncestorsAndSelf().OfType<AwaitExpressionSyntax>().FirstOrDefault()` to find the parent.

4. **`MSBuildWorkspace.OpenProjectAsync` needs `Microsoft.CodeAnalysis.CSharp.Workspaces` PackageReference.** Without it, the error is "language 'C#' is not supported" — the C# language is not registered.

5. **NuGet SFS (Single File Storage) for analyzer packages.** The `.nupkg` contains DLLs under `analyzers/dotnet/` and `analyzers/dotnet/cs/`, but NuGet does not extract them to disk during restore. Must use `unzip` to extract at build time.

6. **`.targets` hook deadlocks with MSBuildWorkspace.** The fixer works perfectly outside of `dotnet build`. When called from a `.targets` hook during an active build, the parent MSBuild and child MSBuild (BuildHost) deadlock evaluating the same project. MSBuildWorkspace is designed for IDE tooling, not in-build hooks.

7. **Save-time formatter pipeline is the correct integration point.** OpenCode runs formatters on `.cs` save. Adding the fixer as a second formatter entry runs it before any build — no deadlock, zero visible errors.

## Changes to Plan

| Plan said                                                  | What actually happened                                                                 | Why                                                                |
| ---------------------------------------------------------- | -------------------------------------------------------------------------------------- | ------------------------------------------------------------------ |
| Load from `Microsoft.CodeAnalysis.CSharp.NetAnalyzers.dll` | Load from BOTH `Microsoft.CodeAnalysis.NetAnalyzers.dll` AND `CSharp.NetAnalyzers.dll` | CA2007 is in the base assembly (Discovery 1)                       |
| `IsTestAssertion` and `StartsWithAssertChain` removed      | Removed as planned                                                                     | Official analyzer never flags test chains                          |
| Node found via `FindNode(diag.Location.SourceSpan)`        | Added `AncestorsAndSelf().OfType<AwaitExpressionSyntax>()`                             | Diagnostic span covers inner expression (Discovery 3)              |
| No `.editorconfig` needed                                  | Test fixtures need `dotnet_diagnostic.CA2007.severity = warning`                       | CA2007 disabled by default in .NET 10 (Discovery 2)                |
| `CSharp.Workspaces` not in plan                            | Added PackageReference                                                                 | Required for MSBuildWorkspace language registration (Discovery 4)  |
| Analyzer DLL via `PrivateAssets=none` + build output       | Extracted from `.nupkg` via `unzip` Exec target                                        | NuGet SFS prevents file materialization (Decision 1, Discovery 5)  |
| Global.json copy in test fixture                           | Removed — not needed                                                                   | CSharp.Workspaces resolved the language registration, not SDK path |
| `.targets` hook enabled, `AfterTargets="CoreCompile"`      | **`.targets` hook removed**                                                            | Deadlock with MSBuildWorkspace (Discovery 6)                       |
| Fixer runs on every `dotnet build`                         | Fixer runs on `.cs` file save via OpenCode formatter pipeline                          | No deadlock, zero visible errors (Discovery 7)                     |
