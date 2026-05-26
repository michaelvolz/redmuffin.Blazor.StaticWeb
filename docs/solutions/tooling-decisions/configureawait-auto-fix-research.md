---
title: "Build-Time CA2007 Auto-Fix — Official Analyzer + MSBuildWorkspace"
date: 2026-05-17
module: build-tooling
problem_type: tooling_decision
component: tooling
severity: high
applies_when:
  - "Building any auto-fix tool for Roslyn analyzer diagnostics in a .NET project"
  - "Need to auto-apply code fixes triggered by official C# analyzers"
  - "MSBuild .targets approaches have been ruled out"
tags:
  [
    configureawait,
    ca2007,
    roslyn,
    msbuildworkspace,
    code-fixing,
    netanalyzers,
    msbuild,
    analyzer-integration,
  ]
---

# Build-Time CA2007 Auto-Fix: Official Analyzer + MSBuildWorkspace

## Context

A Blazor WASM project needed `.ConfigureAwait(false)` auto-applied to all
non-assert awaits during local development — LLMs consistently forget
`ConfigureAwait(false)` (avg 6 fix cycles per session, peak of 124 CA2007
errors in one session). After five failed approaches — semantic-model detection
in MSBuild `.targets`, syntax-only heuristics with broken Assert-chain
detection, `dotnet format analyzers` (reports but never applies CodeFixProviders),
Roslynator CLI (broken on .NET 10 SDK), and MSBuild diagnostic extraction
(MSBuild does not expose Roslyn diagnostics as items) — the sole viable path
is a custom tool that loads the **official Microsoft CA2007 analyzer** via
`MSBuildWorkspace`, reuses the cached compilation, and applies fixes as syntax
rewrites.

The existing custom fixer (`redmuffin.Tools.ConfigureAwaitFixer`, documented in
`automated-configureawait-fixer-2026-05-16.md`) uses syntax-only detection
(all awaits minus `Assert.*` chains). This works but re-implements detection
logic that the official analyzer already handles correctly — TUnit assertion
types, `IAsyncDisposable`, `ValueTask`, expression-bodied members, and future
C# async patterns. The correct approach is to delegate detection entirely to
the .NET team's analyzer.

## Guidance

**Architecture**: A standalone console tool, invoked outside MSBuild, that:

1. Opens the project via `MSBuildWorkspace.OpenProjectAsync()` — this reuses
   the MSBuild evaluation cache from the last `dotnet build`, avoiding a second
   compilation (critical for performance).

2. Loads the official analyzer assembly from the NuGet package cache:

```csharp
var packagesDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".nuget", "packages");

var analyzerPath = Path.Combine(packagesDir,
    "microsoft.codeanalysis.netanalyzers",
    "10.0.0-preview.*", // version matches SDK
    "analyzers", "dotnet", "cs",
    "Microsoft.CodeAnalysis.CSharp.NetAnalyzers.dll");

var analyzerAssembly = Assembly.LoadFrom(analyzerPath);
var analyzers = analyzerAssembly.GetTypes()
    .Where(t => typeof(DiagnosticAnalyzer).IsAssignableFrom(t)
                && !t.IsAbstract)
    .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t)!)
    .ToImmutableArray();
```

3. Retrieves the cached `Compilation` and runs analyzers:

```csharp
var compilation = await project.GetCompilationAsync();
var diagnostics = (await compilation!
    .WithAnalyzers(analyzers)
    .GetAnalyzerDiagnosticsAsync(cancellationToken))
    .Where(d => d.Id == "CA2007");
```

4. Applies fixes in **descending position order per file** to preserve syntax
   spans:

```csharp
foreach (var fileGroup in diagnostics
    .GroupBy(d => d.Location.SourceTree?.FilePath))
{
    var tree = fileGroup.First().Location.SourceTree!;
    var root = await tree.GetRootAsync();

    foreach (var diag in fileGroup
        .OrderByDescending(d => d.Location.SourceSpan.Start))
    {
        var awaitExpr = (AwaitExpressionSyntax)
            root.FindNode(diag.Location.SourceSpan);
        root = root.ReplaceNode(awaitExpr,
            awaitExpr.WithConfigureAwait(false));
    }

    await File.WriteAllTextAsync(tree.FilePath,
        root.ToFullString(), cancellationToken);
}
```

5. Guards CI safety: `if (Environment.GetEnvironmentVariable("CI") == "true")
return 0;`

**`AwaitExpressionSyntax.WithConfigureAwait(bool)` extension:**

```csharp
public static AwaitExpressionSyntax WithConfigureAwait(
    this AwaitExpressionSyntax awaitExpr, bool continueOnCapturedContext)
{
    return awaitExpr.WithExpression(
        SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                awaitExpr.Expression,
                SyntaxFactory.IdentifierName("ConfigureAwait")),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(
                        SyntaxFactory.LiteralExpression(
                            continueOnCapturedContext
                                ? SyntaxKind.TrueLiteralExpression
                                : SyntaxKind.FalseLiteralExpression))))));
}
```

**Key design decisions:**

| Decision                                                 | Rationale                                                                                                                     |
| -------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------- |
| Tool runs as **separate script**, not MSBuild target     | Avoids recursion (build → fix → source changed → rebuild), avoids timing problem (diagnostics don't exist before compilation) |
| **`MSBuildWorkspace`**, not `CSharpSyntaxTree.ParseText` | Reuses cached compilation from prior `dotnet build` — no second compilation needed (crucial for performance)                  |
| **Descending position order** per file                   | Syntax spans are byte offsets; fixing position 100 invalidates position 200. Descending order keeps all earlier spans stable  |
| **`Assembly.LoadFrom`** for analyzer loading             | `AnalyzerAssemblyLoader` is `internal` — can't use `AnalyzerFileReference` in standalone tools                                |
| **Skip source-generated files**                          | `*.Designer.cs`, `*.g.cs`, `*_AssemblyInfo.cs` are not fix candidates                                                         |
| **CI skip via environment variable**                     | `CI=true` → no-op. CI treats CA2007 as a hard error — dev boxes auto-fix                                                      |

## Why This Matters

1. **Official analyzer is the only correct detector.** Heuristics ("all awaits",
   "all awaits except `Assert.*`") break on TUnit assertion chains
   (`Assert.That(x).Throws()` — returns `ThrowsAssertion<T>`, not `Task`),
   expression-bodied members, and delegate assignments. CA2007 is maintained by
   the .NET team and guaranteed to match the rule specification.

2. **`dotnet format analyzers` CANNOT be relied on.** It reports diagnostics but
   never invokes `RegisterCodeFixesAsync` — confirmed experimentally for both
   CA2007 and `IDE0005` (unused using). This is a CLI toolchain limitation, not
   a project configuration issue.

3. **MSBuild `.targets` are the wrong vehicle for code fixing.** Before-targets
   run before compilation (diagnostics don't exist). After-targets cannot read
   diagnostics (MSBuild doesn't expose Roslyn diagnostics as items). A
   separate tool invocation avoids both problems.

4. **`MSBuildWorkspace` avoids recompilation.** The fixing phase is a pure
   syntax rewrite — opening the project via `MSBuildWorkspace` loads the cached
   compilation without triggering a second build. On the next `dotnet build`,
   only the modified files recompile (~2-5 seconds).

5. **Extensible to any diagnostic ID.** Adding support for future analyzers
   (e.g., `RCS1090`, `MA0004`) means adding their diagnostic IDs to the filter.
   The loader loads ALL analyzers from the assembly; the filter selects which
   to fix.

## When to Apply

Use this pattern whenever building an auto-fix tool for Roslyn analyzer
diagnostics:

- Load the **official analyzer DLL** via `Assembly.LoadFrom` — never
  re-implement detection logic.
- Use **`MSBuildWorkspace`** to reuse the cached compilation (no second build).
- Run the fixer as a **separate script**, never embed in MSBuild `.targets`.
- Sort diagnostics by **file + descending position** to keep syntax spans stable.
- Guard with an **environment variable** for CI/opt-out.

## Examples

### Before: semantic-model approach (broken)

The original fixer used `SemanticModel.GetTypeInfo()` with only 3 reference
assemblies (`object`, `Task`, `ValueTask`). It could not resolve user-defined
method return types:

```csharp
// Only 3 references — can't resolve MyMethodAsync's return type
var references = new List<MetadataReference>
{
    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
    MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
    MetadataReference.CreateFromFile(typeof(ValueTask).Assembly.Location),
};

var compilation = CSharpCompilation.Create("Fix", syntaxTrees, references);
var model = compilation.GetSemanticModel(tree);

// typeInfo.Type is null for user methods → IsAwaitableTask returns false
// → await is silently skipped → 0 fixes applied
var typeInfo = model.GetTypeInfo(awaitExpr.Expression);
```

**Result**: 0 awaits fixed. Broken for all real-world code.

### Before: syntax-only with broken Assert-chain detection

```csharp
static bool StartsWithAssertChain(ExpressionSyntax expression)
{
    // BUG: Only walks MemberAccessExpressionSyntax.
    // Assert.That(x).IsEmpty() → IsEmpty() is an InvocationExpressionSyntax,
    // not a MemberAccessExpressionSyntax — never reaches "Assert"
    while (expression is MemberAccessExpressionSyntax memberAccess)
        expression = memberAccess.Expression;
    return expression is IdentifierNameSyntax { Identifier.Text: "Assert" };
}
```

**Result**: Added `.ConfigureAwait(false)` to 765 TUnit assertion chains
(`Assert.That(x).IsEmpty().ConfigureAwait(false)` → CS1929 build errors).

### After: MSBuildWorkspace + official analyzer (recommended)

The tool loads the project's cached compilation, runs only the official CA2007
analyzer, and fixes exactly what it flags — nothing more, nothing less:

```csharp
// tools/ApplyConfigureAwait/Program.cs
var workspace = MSBuildWorkspace.Create();
var project = await workspace.OpenProjectAsync(projectPath);

var analyzers = LoadOfficialAnalyzers();
var compilation = await project.GetCompilationAsync();

var ca2007Diagnostics = (await compilation!
    .WithAnalyzers(analyzers)
    .GetAnalyzerDiagnosticsAsync(token))
    .Where(d => d.Id == "CA2007");

ApplyFixes(project, ca2007Diagnostics);
```

**Result**: Only CA2007-flagged awaits fixed. TUnit `Assert.*` chains untouched
(CA2007 correctly identifies `ThrowsAssertion<T>` as non-Task). Build passes
on next compilation. ~3-9 seconds per project.

## Related

- `automated-configureawait-fixer-2026-05-16.md` — Full journey log of the
  custom Roslyn fixer build (MSBuild targets, NuGet packaging, semantic type
  checking, `dotnet format` dead end)
- `csharp-standards-final-2026-04-06.md` — Authoritative `ConfigureAwait(false)`
  policy and `.editorconfig` analyzer configuration
- `quality-gates-tool-operational-gotchas-2026-05-09.md` — Roslyn patterns
  (`CSharpSyntaxTree` vs `MSBuildWorkspace`), SDK coexistence
- SN-0032 — Original research task that launched the ConfigureAwait fixer work
  ("Research ways to make ConfigureAwait less painful for LLMs")
- `rm-guide-async` — Project ConfigureAwait conventions
