---
title: "Automated ConfigureAwait(false) Fixer via MSBuild CoreCompileDependsOn"
date: 2026-05-16
category: developer-experience
module: tools
problem_type: developer_experience
component: tooling
severity: medium
symptoms:
  - "LLMs and developers consistently forget .ConfigureAwait(false) on async calls"
  - "CA2007 and MA0004 analyzer errors appear on every build with new async code"
  - "Manual ConfigureAwait annotation is repetitive and consumes LLM token budget"
root_cause: missing_tooling
resolution_type: tooling_addition
applies_when:
  - "Building .NET projects that require ConfigureAwait(false) on all Task/ValueTask awaits"
  - "Using LLM-based code generation that does not auto-add ConfigureAwait"
  - "A zero-warning build policy catches missing ConfigureAwait annotations"
  - "dotnet format has been ruled out as incapable of applying CodeFixProviders"
related_components:
  - build-pipeline
  - redmuffin.Blazor.StaticWeb
tags:
  - configureawait
  - roslyn
  - msbuild
  - nuget
  - ca2007
  - code-fixer
  - analyzer
  - build-pipeline
---

# Automated ConfigureAwait(false) Fixer via MSBuild CoreCompileDependsOn

## Context

LLMs (and developers) consistently forget to add `.ConfigureAwait(false)` to async calls. Analysis of 7 prior development sessions showed an average of **6 fix cycles per session**, with a peak of **124 CA2007 errors** in a single build batch after `TreatWarningsAsErrors` was enabled. Every cycle: write code → `dotnet build` → CA2007/MA0004 error → manually add `.ConfigureAwait(false)` → rebuild. The LLM incurred fixed overhead of ~2 extra turns and hundreds of tokens per cycle.

`dotnet format` was researched exhaustively — all subcommands (base, style, analyzers), all severity levels, with and without `TreatWarningsAsErrors`, with and without `.editorconfig` entries — and proved it **cannot apply CodeFixProviders**. It only formats whitespace and detects diagnostics; it never invokes `RegisterCodeFixesAsync`.

The `AutoCodeFix` package (kzu, MIT license) proved the concept of build-time code fix application in 2019 but targeted .NET Core 3.x and is no longer maintained. No existing analyzer NuGet package (Meziantou, Roslynator, StyleCop) auto-applies code fixes during build — their `.targets` files only handle analyzer DLL setup and editorconfig defaults.

The gap was clear: no standard mechanism existed to auto-apply `ConfigureAwait(false)` during `dotnet build`. A custom Roslyn-based tool was the only viable path.

## Guidance

### 1. Architecture overview

Four components work together:

| Component       | File                                                                                                    | Purpose                                              |
| --------------- | ------------------------------------------------------------------------------------------------------- | ---------------------------------------------------- |
| Roslyn fixer    | `tools/src/redmuffin.Tools.ConfigureAwaitAnalyzer/Program.cs`                                           | Finds missing `.ConfigureAwait(false)` and adds them |
| MSBuild targets | `tools/src/redmuffin.Tools.ConfigureAwaitAnalyzer/build/redmuffin.Tools.ConfigureAwaitAnalyzer.targets` | Wires fixer into `CoreCompileDependsOn`              |
| NuGet package   | `tools/nupkgs/redmuffin.Tools.ConfigureAwaitAnalyzer.1.0.0.nupkg`                                       | Distributes fixer + targets to consuming projects    |
| Local feed      | `tools/nupkgs/` + `tools/nuget.config`                                                                  | Serves the package during restore                    |

### 2. The semantic check (critical — never use pure syntax)

Pure syntax analysis wraps ALL `await` expressions, including TUnit assertion chains like `await Assert.That(result).IsTrue()` — these return `Bool_IsTrue_Assertion` (namespace `TUnit.Assertions`), NOT a task type. Result: CS1929 errors across 41 test files.

Always use `SemanticModel.GetTypeInfo()`:

```csharp
static bool IsAwaitableTask(
    SemanticModel model,
    AwaitExpressionSyntax awaitExpr,
    HashSet<string> asyncAwaitableTypes)
{
    var typeInfo = model.GetTypeInfo(awaitExpr.Expression);
    var type = typeInfo.Type;

    if (type is null)
        return false;

    if (!string.Equals(type.ContainingNamespace?.ToDisplayString(),
        "System.Threading.Tasks", StringComparison.Ordinal))
        return false;

    return asyncAwaitableTypes.Contains(type.MetadataName);
}
```

The valid metadata names are `Task`, ``Task`1``, `ValueTask`, ``ValueTask`1``.

### 3. Building the compilation for semantic access

The fixer loads its own reference assemblies:

```csharp
var references = new List<MetadataReference>
{
    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
    MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
    MetadataReference.CreateFromFile(typeof(ValueTask).Assembly.Location),
};

var compilation = CSharpCompilation.Create(
    "ConfigureAwaitFix",
    syntaxTrees,
    references,
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
```

Only three assemblies are needed: `System.Runtime` (for `object`), `System.Threading.Tasks` (for `Task`/`ValueTask`). The compilation is created once per build, parsing all `.cs` files in the project directory (excluding `obj/` and `bin/`).

### 4. Applying the fix via Roslyn syntax rewriting

```csharp
static AwaitExpressionSyntax AddConfigureAwait(AwaitExpressionSyntax awaitExpr)
{
    return awaitExpr.WithExpression(
        SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                awaitExpr.Expression,
                SyntaxFactory.IdentifierName("ConfigureAwait")))
            .WithArgumentList(SyntaxFactory.ArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(
                        SyntaxFactory.LiteralExpression(
                            SyntaxKind.FalseLiteralExpression))))));
}
```

The rewrite uses `awaitExpr.WithExpression()` to replace the existing awaited expression with a new invocation chain: `expr.ConfigureAwait(false)`. The `SyntaxAnnotation`-based node replacement pattern from the mutation gate was not needed here — simple `ReplaceNodes` on the compilation root is sufficient.

### 5. MSBuild integration — CoreCompileDependsOn, not AfterTargets

`AfterTargets="CoreCompile"` and `AfterTargets="Build"` were both tested and silently fail: when the parent target is skipped during incremental builds, `AfterTargets` followers do not fire.

The reliable hook is `CoreCompileDependsOn` property injection:

```xml
<Project>
    <PropertyGroup>
        <CoreCompileDependsOn>$(CoreCompileDependsOn);ConfigureAwaitFix</CoreCompileDependsOn>
    </PropertyGroup>

    <Target Name="ConfigureAwaitFix" BeforeTargets="CoreCompile">
        <Exec Command="dotnet &quot;$(_ConfigureAwaitFixerPath)&quot; &quot;$(MSBuildProjectDirectory)&quot;"
              StandardErrorImportance="low"
              ContinueOnError="true" />
    </Target>
</Project>
```

Key design decisions:

- **`BeforeTargets="CoreCompile"`**: Fixer runs BEFORE the compiler inspects the code. By the time compilation starts, all `await` expressions already have `.ConfigureAwait(false)`. CA2007 never fires. The LLM never sees an error.
- **`CoreCompileDependsOn`**: Appends the fixer target to the compilation dependency chain, guaranteeing execution on every build regardless of incremental compilation status.
- **`ContinueOnError="true"`**: The fixer is non-fatal — if it fails (e.g., corrupted file), the build continues with the existing code.
- **`StandardErrorImportance="low"`**: Suppresses fixer output from normal build output. Only visible with `-v:normal` or higher.

### 6. NuGet packaging for a tool-only package

The `.csproj` must declare:

```xml
<IncludeBuildOutput>false</IncludeBuildOutput>
```

Without this, the compiled DLL is added as a project reference — the consuming project tries to load `ConfigureAwaitFixer.dll` as a library, causing CS5001 errors about missing `Main`.

The `.targets` file MUST match `{PackageId}.targets`:

```
build/redmuffin.Tools.ConfigureAwaitAnalyzer.targets
```

NuGet auto-imports `build/{PackageId}.targets` only — any other filename is silently ignored.

Published output goes in `tools/net10.0/any/` via static `<None Pack="true" PackagePath="...">` items. Pre-build the publish output and commit to `publish/` — do NOT generate during `dotnet pack` via `<Exec>`. The publish-during-pack approach is fragile: the publish step times out on Roslyn dependencies, and `--no-restore` inside `<Exec>` creates race conditions.

### 7. NuGet operational gotchas

- **Package source mapping** in `tools/nuget.config` is required for the `local-tools` feed. Without it, NuGet may resolve from `nuget.org` instead.
- **Clear the NuGet cache** between pack iterations: `dotnet nuget locals all --clear`. Stale cached packages cause silent failures — the `.targets` import appears correct but the target never fires.
- **`NU5128` warning** (missing lib/ref assemblies) is harmless for tool-only packages. Cannot be suppressed without adding dummy lib assemblies.
- **`packages.lock.json`** changes when package source mapping changes — commit it alongside the config change.

### 8. Performance characteristics

The fixer adds approximately **1 second per project** per build (type checking + file I/O for all `.cs` files). It runs on EVERY build regardless of whether new `await` expressions exist. This overhead is acceptable for the tools solution (~4 projects, ~4s total). For larger solutions, an incremental optimization (compare file modification times against last fix timestamp) would be warranted but is not yet implemented.

## Why This Matters

### Before the fixer

```
LLM writes async code → dotnet build → CA2007 error (line 9, col 15)
  → LLM reads error → adds .ConfigureAwait(false) → dotnet build → clean
```

Cost: 2 extra turns, ~8 seconds wall time, ~200 tokens per error. At 124 errors in a single batch, the LLM needed ~50 turns just to fix ConfigureAwait violations.

### After the fixer

```
LLM writes async code → dotnet build → 0 errors, 0 warnings
```

Cost: zero. The fixer added `.ConfigureAwait(false)` before the compiler inspected the file.

### TUnit corruption prevented

Without semantic analysis, the fixer corrupts test files. `await Assert.That(result).IsTrue()` becomes `await Assert.That(result).IsTrue().ConfigureAwait(false)`, producing CS1929 errors on every assertion in the test suite. Semantic type checking via `GetTypeInfo()` eliminates 100% of false positives.

### Build chain reliability

`CoreCompileDependsOn` is the only reliable hook for pre-compilation steps in incremental builds. `AfterTargets` silently stops firing, leaving unfixed `await` expressions accumulating in the codebase with no warning.

## When to Apply

- When LLMs (or developers) repeatedly produce CA2007/MA0004 violations in a codebase enforcing `ConfigureAwait(false)`
- When building any Roslyn-based automated code fixer that must run during `dotnet build`
- When the fixer must modify source files BEFORE the compiler runs (pre-compilation, not post-build)
- When `dotnet format` has been confirmed incapable of applying the desired CodeFixProvider
- When the overhead of running the fixer on every build (~1s per project) is acceptable relative to the cost of manual fixes
- When the consumer project can reference a NuGet package from a local feed (not yet published to nuget.org)

## Examples

### Zero-error build cycle

LLM writes:

```csharp
public async Task<WeatherForecast[]> GetForecastsAsync()
{
    var response = await Http.GetAsync("api/weather");
    var json = await response.Content.ReadAsStringAsync();
    return JsonSerializer.Deserialize<WeatherForecast[]>(json);
}
```

`dotnet build` output:

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

The file on disk now contains:

```csharp
public async Task<WeatherForecast[]> GetForecastsAsync()
{
    var response = await Http.GetAsync("api/weather").ConfigureAwait(false);
    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    return JsonSerializer.Deserialize<WeatherForecast[]>(json);
}
```

### Semantic check prevents false positives

```csharp
// TUnit assertion — NOT a Task/ValueTask, correctly skipped
await Assert.That(result).IsTrue();

// Task.WhenAll — IS a Task, correctly fixed
await Task.WhenAll(tasks);
```

### Project integration

```xml
<!-- tools/src/redmuffin.Tools.QualityGates/redmuffin.Tools.QualityGates.csproj -->
<ItemGroup>
    <PackageReference Include="redmuffin.Tools.ConfigureAwaitAnalyzer" Version="1.0.0" />
</ItemGroup>
```

No other configuration needed. The `.targets` auto-import hooks the fixer into every build.

## Related

- `docs/solutions/best-practices/csharp-standards-final-2026-04-06.md` — authoritative ConfigureAwait(false) policy documentation
- `docs/solutions/tooling-decisions/crap-quality-gates-pipeline-2026-05-09.md` — separate-solution + local NuGet feed architecture pattern
- `docs/adr/0002-quality-gates-toolchain.md` — architectural decision record for the tools solution structure
- `docs/solutions/developer-experience/quality-gates-tool-operational-gotchas-2026-05-09.md` — Roslyn patterns, `dotnet pack` workflow, SDK coexistence
- `tools/src/redmuffin.Tools.ConfigureAwaitAnalyzer/Program.cs` — fixer implementation (138 lines)
- `tools/src/redmuffin.Tools.ConfigureAwaitAnalyzer/build/redmuffin.Tools.ConfigureAwaitAnalyzer.targets` — MSBuild integration target

## Investigation Dead Ends (session history)

- **`dotnet format`**: All subcommands, all severity levels — never invokes `RegisterCodeFixesAsync`. Confirmed by 6 independent tests.
- **Pure syntax await detection**: Corrupted 41 test files with CS1929 errors before semantic analysis was added.
- **`AfterTargets="Build"`**: Target silently stops firing during incremental builds when project is up-to-date.
- **`AfterTargets="CoreCompile"`**: Same problem — follows parent skip semantics.
- **Publish-during-pack via `<Exec>`**: `dotnet publish` timed out on Roslyn dependencies inside the MSBuild pack step.
- **`<Analyzer>` MSBuild item hack**: Points to Debug build output path, breaks on `dotnet clean`, not portable across machines.
