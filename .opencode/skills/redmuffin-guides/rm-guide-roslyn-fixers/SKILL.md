---
name: rm-guide-roslyn-fixers
description: Guides development of auto-fix tools for Roslyn analyzer diagnostics in this .NET solution. Covers the MSBuildWorkspace + official analyzer architecture pattern, diagnostic prioritization (Phase 1/2/3 distribution data), and the extensible fixer scaffold. USE FOR: adding a new auto-fix diagnostic, extending the ConfigureAwait fixer, building any syntax-rewrite-based code fixer, deciding which analyzer warnings to auto-fix next. DO NOT USE FOR: quality gates toolchain (see rm-guide-gates-development), general MSBuild configuration (see rm-guide-build-config), or analyzer warning policy (see rm-guide-warnings).
---

# rm-guide-roslyn-fixers

## Purpose

Guide for building and extending auto-fix tools that apply Roslyn analyzer
diagnostic fixes. Covers the architectural pattern proven on CA2007
(ConfigureAwait) and extensible to any diagnostic with a mechanical syntax
rewrite.

## When This Skill Triggers

- Creating a new auto-fix console tool for an analyzer diagnostic
- Adding a new diagnostic ID to an existing fixer
- Deciding which diagnostic to tackle next based on distribution data
- Researching whether a diagnostic can be auto-fixed vs. needs structural
  refactoring
- Any work in `tools/src/redmuffin.Tools.ConfigureAwaitFixer/` or a future
  `tools/src/ApplyAnalyzerFixes/`

## Architecture Principles

### 1. Never re-implement detection logic

Load the official analyzer DLL and run it against the project's cached
compilation. Every diagnostic is detected by the official analyzer
shipped by Microsoft, Meziantou, Roslynator, or AsyncFixer:

```csharp
var analyzerAssembly = Assembly.LoadFrom(pathToDll);
var analyzers = analyzerAssembly.GetTypes()
    .Where(t => typeof(DiagnosticAnalyzer).IsAssignableFrom(t) && !t.IsAbstract)
    .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t)!)
    .ToImmutableArray();

var diagnostics = (await compilation!
    .WithAnalyzers(analyzers)
    .GetAnalyzerDiagnosticsAsync(token))
    .Where(d => targetDiagnosticIds.Contains(d.Id));
```

Official analyzers are maintained by their respective teams and
guaranteed correct for all edge cases. Heuristics ("all awaits", "all
awaits except Assert.\*") break on TUnit assertion chains,
expression-bodied members, and future C# patterns.

### 2. Never parse files as standalone syntax trees

`CSharpSyntaxTree.ParseText` creates a syntax tree with zero type
information — no cross-file resolution, no NuGet references, no
project context. Use `MSBuildWorkspace` to load the cached
compilation from the last build:

```csharp
var workspace = MSBuildWorkspace.Create();
var project = await workspace.OpenProjectAsync(projectPath);
var compilation = await project.GetCompilationAsync();
```

The fixer runs analyzers on the cached compilation (~0.5-1s per
project), then applies syntax rewrites. No second compilation.

### 3. Never fix diagnostics in ascending position order

Syntax spans are byte offsets. Fixing position 100 invalidates span
calculations for position 200. Process in descending order per file:

```csharp
foreach (var fileGroup in diagnostics
    .GroupBy(d => d.Location.SourceTree?.FilePath))
{
    var root = await fileGroup.First().Location.SourceTree!.GetRootAsync();

    foreach (var diag in fileGroup
        .OrderByDescending(d => d.Location.SourceSpan.Start))
    {
        var node = root.FindNode(diag.Location.SourceSpan);
        root = root.ReplaceNode(node, Transform(node, diag));
    }

    await File.WriteAllTextAsync(tree.FilePath, root.ToFullString(), token);
}
```

### 4. Never embed the fixer in MSBuild `.targets`

`BeforeTargets="CoreCompile"` runs before diagnostics exist.
`AfterTargets="CoreCompile"` cannot read them (MSBuild does not
expose Roslyn diagnostics as items). Nested `dotnet format` inside
MSBuild creates recursive build loops.

Run the fixer as a standalone console tool between build cycles:

```bash
dotnet build && dotnet run --project tools/ApplyAnalyzerFixes -- solution.slnx && dotnet build
```

### 5. Never run the fixer in CI

Guard with an environment variable:

```csharp
if (Environment.GetEnvironmentVariable("CI") == "true") return 0;
```

CI treats all analyzer diagnostics as hard errors. Auto-fix runs
only on dev boxes. If a diagnostic reaches CI, the dev box fixer
did not run or could not fix it — correct signal, never suppress it.

### 6. Never modify source-generated files

```csharp
if (filePath.EndsWith(".Designer.cs") ||
    filePath.EndsWith(".g.cs") ||
    filePath.EndsWith("_AssemblyInfo.cs"))
    continue;
```

Generated files are regenerated on next build — modifying them is
pointless or harmful.

### 7. Never use an MSBuild `.targets` hook — deadlock

MSBuildWorkspace spawns a child MSBuild process (BuildHost) to
evaluate projects. When called from a `.targets` hook during
`dotnet build`, the parent MSBuild and child BuildHost both
evaluate the same project → **deadlock.**

Instead, integrate fixers via OpenCode's formatter pipeline on
file save. The fixer runs when no MSBuild build is active — no
deadlock. See `opencode.jsonc` `formatter` section for the
`configureawait-fixer` entry as the canonical pattern.

Documented: `docs/solutions/tooling-decisions/configureawait-msbuild-hook-incompatibility-2026-05-17.md`

## Gotchas — NuGet Analyzer Package Behavior

### Analyzer DLLs are in the .nupkg, not on disk

NuGet analyzer packages store DLLs under `analyzers/dotnet/` inside
the `.nupkg`. NuGet does **not** materialize these files to disk during
restore (Single File Storage convention). `PrivateAssets=none` makes
the package resolvable as a dependency, but the DLL path (e.g.,
`$(NuGetPackageRoot)/.../analyzers/dotnet/Microsoft.CodeAnalysis.NetAnalyzers.dll`)
points into uncompressed space — `<Copy>` fails with "file not found."

Do not use `<Copy>` or `CopyToOutputDirectory` on analyzer DLL paths.
Extract from the `.nupkg` at build time instead:

```xml
<Exec Command="unzip -o -j &quot;$(NuGetPackageRoot)path/to/package.nupkg&quot;
       &quot;analyzers/dotnet/TargetAnalyzer.dll&quot; -d &quot;$(OutputPath)&quot;"
      ContinueOnError="true" />
```

The `.nupkg` is always on disk after restore. The internal path (e.g.,
`analyzers/dotnet/Microsoft.CodeAnalysis.NetAnalyzers.dll`) matches
the NuGet analyzer convention — use `unzip -l` to verify.

### Base + language-specific assemblies

Analyzer packages ship separate DLLs for language-agnostic rules and
C#-specific rules. Example: `Microsoft.CodeAnalysis.NetAnalyzers`:

- `Microsoft.CodeAnalysis.NetAnalyzers.dll` — 228 rules including CA2007
- `Microsoft.CodeAnalysis.CSharp.NetAnalyzers.dll` — 43 C#-specific rules

Load **both** when building a general-purpose analyzer tool. A single
`Assembly.LoadFrom` on the CSharp DLL misses the majority of diagnostics
including CA2007.

### MSBuildWorkspace needs language registration

`MSBuildWorkspace.OpenProjectAsync` loads project files but does not
register language support automatically. Without
`Microsoft.CodeAnalysis.CSharp.Workspaces` PackageReference, calling
`OpenProjectAsync` on a `.csproj` throws:
`"Cannot open project because the language 'C#' is not supported."`

`Workspaces.MSBuild` handles MSBuild project loading. `CSharp.Workspaces`
handles C# syntax tree and compilation support. Both are required.

### Some diagnostics are disabled by default

Diagnostic default severity varies by .NET SDK version and `AnalysisMode`.
CA2007 is `severity=none` by default in .NET 10. It fires during
`dotnet build` only when enabled via `AnalysisMode=AllEnabledByDefault`
or explicit `.editorconfig` `dotnet_diagnostic.CA2007.severity = warning`.

Test fixtures for analyzer-based tools must include an `.editorconfig`
enabling the target diagnostic. Without it, `WithAnalyzers` produces
zero results even though the analyzer is loaded and `SupportedDiagnostics`
lists the ID.

### `.targets` hook deadlocks with MSBuildWorkspace

Never add a `<Target>` that calls an MSBuildWorkspace-based fixer
via `<Exec>`. The fixer spawns a child MSBuild process (BuildHost)
that evaluates the project, deadlocking with the parent MSBuild
already evaluating the same project. MSBuildWorkspace is designed
for IDE tooling (between-build analysis), not in-build hooks.

### Integration via OpenCode formatter pipeline

Fixers run on `.cs` file save via the `formatter` section in
`opencode.jsonc`. The formatter pipeline invokes the fixer when
no MSBuild build is active — MSBuildWorkspace can safely open
the project. Pattern:

```json
"configureawait-fixer": {
  "command": ["dotnet", "<path-to-fixer.dll>", "--file", "$FILE"],
  "extensions": [".cs"]
}
```

The fixer must accept `--file <path>` for single-file mode and
walk up to find the `.csproj`.

## Diagnostic Prioritization Framework

Based on the TreatWarningsAsErrors enablement data (May 12 2026, 248
total violations across 9 diagnostic categories). Full analysis:
`docs/solutions/tooling-decisions/analyzer-warning-distribution-prioritization-2026-05-17.md`.

### Distribution Data

| #   | Diagnostic                          | Count | %     | Auto-Fixable?            |
| --- | ----------------------------------- | ----- | ----- | ------------------------ |
| 1   | CA2007 — ConfigureAwait(false)      | 124   | 50.0% | Yes                      |
| 2   | CA1849 — async I/O methods          | 64    | 25.8% | Yes (verify ordering)    |
| 3   | SA\* — formatting                   | 48    | 19.4% | Handled by dotnet format |
| 4   | MA0016 — collection abstractions    | 42    | 16.9% | Yes                      |
| 5   | CA1305 — culture-invariant ToString | 28    | 11.3% | Yes                      |
| 6   | MA0002 — StringComparer.Ordinal     | 16    | 6.5%  | Yes                      |
| 7   | CA18XX — performance/API            | 14    | 5.6%  | Mixed                    |
| 8   | MA0051 — method too long            | 14    | 5.6%  | No (structural)          |
| 9   | MA0048 — nested type in own file    | 8     | 3.2%  | No (structural)          |

### Phase Assignment

**Phase 1 (current)**: CA2007. 50% of violations. LLM-proof — 7 fix
cycles, zero correct first tries across all sessions (session history).

**Phase 2 (next)**: CA1849, MA0016, CA1305, MA0002, CA18XX group.
164 violations, all fixable via the same MSBuildWorkspace pipeline.
Per-diagnostic cost is only the syntax rewrite function.

**Phase 3 (report only)**: MA0051, MA0048. Structural refactoring
required — cannot be pattern-replaced. Produce a JSON report for
cleanup sessions. See `rm-quality-gates` for the remediation workflow.

### Operationalizing Phases via .editorconfig

When `TreatWarningsAsErrors=true` is active, every warning blocks the
build — making Phase 2/3 distinction impossible. Instead, use
per-diagnostic severity in `.editorconfig`:

```ini
dotnet_diagnostic.CA2007.severity = error   # Phase 1 — fixer handles
dotnet_diagnostic.CA1849.severity = warning  # Phase 2 — not yet in fixer
dotnet_diagnostic.MA0051.severity = warning  # Phase 3 — structural
```

When a diagnostic moves from Phase 2 to Phase 1 (fixer handles it),
promote its severity from `warning` to `error`.

See `docs/solutions/tooling-decisions/analyzer-warning-distribution-prioritization-2026-05-17.md`
§"Operationalizing Phases via .editorconfig Severity" for the full
analysis.

## Adding a New Diagnostic to the Fixer

### Invariant scaffold (never change these)

```csharp
var diagnosticFixers = new Dictionary<string, Func<SyntaxNode, Diagnostic, SyntaxNode>>
{
    ["CA2007"] = FixConfigureAwait,
    // Add new entries here
};
```

The fix loop, analyzer loading, workspace creation, CI guard, and
source-generated file skip never change. Only the dictionary entries
and their fixer methods are diagnostic-specific.

### Per-diagnostic pattern

```csharp
// Example: CA1305 — add CultureInfo.InvariantCulture to .ToString()
static SyntaxNode FixCultureInvariantToString(SyntaxNode node, Diagnostic diag)
{
    var invocation = (InvocationExpressionSyntax)node;
    return invocation.WithArgumentList(
        ArgumentList(SingletonSeparatedList(
            Argument(MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("CultureInfo"),
                    IdentifierName("InvariantCulture")),
                IdentifierName("ToString"))))));
}
```

### Steps for each new diagnostic

1. Add the diagnostic ID to the dictionary
2. Write the `static SyntaxNode Fix<X>(SyntaxNode, Diagnostic)` method
3. Write a syntax-tree-input → syntax-tree-output test
4. Verify on a test project with an intentional violation
5. Update the distribution table above if this is a newly tracked diagnostic

## Related Skills

- `rm-guide-gates-development` — Quality gates toolchain conventions. Separate scope.
- `rm-guide-warnings` — Analyzer warning policy, pragma decision tree.
- `rm-guide-build-config` — Build commands, MSBuild conventions.
- `rm-guide-async` — ConfigureAwait conventions and async patterns.

## Reference Docs

- `docs/solutions/tooling-decisions/analyzer-warning-distribution-prioritization-2026-05-17.md`
  — Full distribution data, Phase 1/2/3 framework
- `docs/solutions/tooling-decisions/configureawait-auto-fix-research-2026-05-17.md`
  — Official-analyzer + MSBuildWorkspace architecture research
- `docs/solutions/developer-experience/automated-configureawait-fixer-2026-05-16.md`
  — Journey log of the custom fixer build (6 dead-end approaches)
- `docs/solutions/tooling-decisions/configureawait-msbuild-hook-incompatibility-2026-05-17.md`
  — `.targets` hook deadlock analysis and save-time architecture decision

## Roslynator CLI Status

Roslynator CLI (`roslynator fix`) is the ideal framework — supports any
analyzer DLL and any CodeFixProvider. Blocked on .NET 10 SDK:
`System.Composition.AttributedModel` removed from shared framework
(dotnet/roslynator#1748). When Roslynator ships `net10.0`, archive
the custom tool.
