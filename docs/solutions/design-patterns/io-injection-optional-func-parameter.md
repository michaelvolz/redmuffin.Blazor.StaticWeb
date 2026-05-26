---
title: "I/O Injection Pattern — Optional Func<T> Parameters for Process/File Testability"
date: 2026-05-16
category: design-patterns
module: QualityGates
problem_type: design_pattern
component: tooling
severity: medium
applies_when:
  - "A method calls Process.Start or file I/O and cannot be unit tested without a real process or filesystem"
  - "A CRAP violation is caused by I/O coupling rather than pure logic complexity"
  - "You need to inject a fake implementation without changing the public API contract"
tags:
  [
    ioc,
    testability,
    dotnet,
    crap,
    process-injection,
    optional-func,
    refactoring,
  ]
---

# I/O Injection Pattern — Optional Func\<T\> Parameters

## Context

Methods like `MutateHandler.ResolveCoverageLinesAsync` and `CrapCommand.GenerateCoverageForAllProjects`
spawned real processes (`dotnet run --coverage`), making them untestable without the real toolchain.
CRAP scores hit 30.0 and 12.1 — the combination of high cyclomatic complexity and zero test coverage
on the process-spawning paths. Coverage tools cannot instrument `Process.Start` calls.

## Guidance

Add an optional `Func<string, Task<string?>> generateCoverage = null` parameter.
When null, delegate to the real implementation. In tests, provide a fake.
Extract pure logic (classification, filtering, validation) into separate methods
so the I/O boundary is a thin shell around pure core logic.

**Pattern checklist:**

1. Identify the I/O call (Process.Start, File.ReadAllText, HttpClient, etc.)
2. Add optional `Func<TInput, Task<TOutput>>` parameter defaulting to null
3. In the method body: `func ??= RealImplementation` as fallback
4. Split out pure helper methods (ShouldSkip, WasGenerated, ClassifyAndFilter, Merge) — each with CC=1
5. Test the pure helpers directly; test the I/O method by injecting fakes

## Why This Matters

- CRAP score drops from 30.0 to 7.7 (PASS) — a 75% reduction
- No interface bloat, no DI container registration, no mocking framework needed
- Same pattern works for any I/O boundary: Process.Start, File.ReadAllText, HttpClient,
  Directory.EnumerateFiles
- Fakes are trivial lambdas, making tests self-contained and readable

## When to Apply

Any method that directly calls Process.Start, File._, Directory._, HttpClient, or any
other external I/O. The pattern is especially valuable when:

- The I/O call is the only reason the method is untested
- You want to avoid adding a whole interface + DI registration for a single dependency
- The fake behavior in tests is dead simple (return a fixed string, return null, return empty)

Do NOT apply when the method has 5+ I/O dependencies — at that point, DI with proper
interfaces is clearer.

## Examples

**Before (CRAP 30.0):**

```csharp
private static async Task<HashSet<int>> ResolveCoverageLinesAsync(
    string testProjectPath, MutateOptions options,
    HashSet<int> currentCoverage, TextWriter output)
{
    if (currentCoverage.Count > 0 || !options.AutoCoverage)
        return currentCoverage;

    var process = Process.Start(new ProcessStartInfo("dotnet", "run --coverage ...")
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    });
    await process!.WaitForExitAsync().ConfigureAwait(false);
    // ... parse output, find coverage file, load lines ...
}
```

**After (CRAP 7.7 — PASS):**

```csharp
public static async Task<IReadOnlySet<int>> ResolveCoverageLinesAsync(
    string testProjectPath, MutateOptions options,
    IReadOnlySet<int> currentCoverage, TextWriter output,
    Func<string, Task<string?>>? generateCoverage = null)
{
    if (ShouldSkipCoverageGeneration(currentCoverage, options))
        return currentCoverage;

    generateCoverage ??= GenerateCoverageAsync;
    var generatedPath = await generateCoverage(testProjectPath)
        .ConfigureAwait(false);
    if (!WasCoverageGenerated(generatedPath))
        return currentCoverage;

    // copy generated file to expected path, load coverage
    var destPath = CoverageFilePath(testProjectPath);
    Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
    File.Copy(generatedPath!, destPath, overwrite: true);
    return CoverageReader.LoadCoverage(destPath).ToHashSet();
}

// Pure helpers — each CC=1, trivially testable
public static bool ShouldSkipCoverageGeneration(
    IReadOnlySet<int> currentCoverage, MutateOptions options)
    => currentCoverage.Count > 0 || !options.AutoCoverage;

public static bool WasCoverageGenerated(string? generatedPath)
    => generatedPath is not null;
```

**Test with fake generator:**

```csharp
[Test]
public async Task ResolveCoverageLinesAsync_handles_null_from_generator()
{
    using var writer = new StringWriter();
    var result = await MutateHandler.ResolveCoverageLinesAsync(
        "/fake/project", new MutateOptions { AutoCoverage = true },
        new HashSet<int>(), writer,
        generateCoverage: _ => Task.FromResult<string?>(null))
        .ConfigureAwait(false);
    await Assert.That(result.Count).IsEqualTo(0);
}

[Test]
public async Task ResolveCoverageLinesAsync_skips_when_already_have_coverage()
{
    var result = await MutateHandler.ResolveCoverageLinesAsync(
        "/fake/project", new MutateOptions { AutoCoverage = true },
        new HashSet<int> { 1, 2, 3 }, new StringWriter())
        .ConfigureAwait(false);
    await Assert.That(result.Count).IsEqualTo(3);
}
```

## Related

- [Raindrop Presentation Helper Extraction](/docs/solutions/design-patterns/raindrop-presentation-helper-extraction-2026-05-14.md)
  — Sibling seam pattern: static-method extraction for pure functions
- [CRAP-Driven Functional Refactoring](/docs/solutions/best-practices/crap-driven-functional-refactoring-2026-05-12.md)
  — Methodology that drove this pattern discovery
