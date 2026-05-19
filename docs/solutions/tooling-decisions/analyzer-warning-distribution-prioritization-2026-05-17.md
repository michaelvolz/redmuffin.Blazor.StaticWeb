---
title: "Analyzer Violation Baseline: TreatWarningsAsErrors Enablement Distribution"
date: 2026-05-17
module: build-tooling
problem_type: tooling_decision
component: tooling
severity: high
applies_when:
  - "Prioritizing auto-fix tool development across multiple analyzer categories"
  - "Evaluating which diagnostics justify custom fixer investment vs. deferral"
  - "Planning phased cleanup cycles for a large diagnostic backlog"
  - "Adding new auto-fix capabilities to the MSBuildWorkspace-based fixer pipeline"
tags:
  [
    analyzers,
    treatwarningsaserrors,
    ca2007,
    ca1849,
    prioritization,
    msbuildworkspace,
    quality-gates,
    configureawait,
    code-fixing,
    roslyn,
    fixer-strategy,
  ]
---

# Analyzer Violation Baseline: TreatWarningsAsErrors Enablement Distribution

## Context

When `TreatWarningsAsErrors` was enabled on May 12 2026 across a .NET 10 SDK
Blazor WASM project (net9.0 target), 248 accumulated analyzer violations became
hard build errors. Nine analyzers are active: Microsoft.NetAnalyzers,
Meziantou.Analyzer, Microsoft.VisualStudio.Threading.Analyzers,
Roslynator.Analyzers, Roslynator.CodeAnalysis.Analyzers,
Roslynator.Formatting.Analyzers, StyleCop.Analyzers,
Microsoft.AspNetCore.Components.Analyzers, and AsyncFixer.

StyleCop formatting rules are mostly suppressed (22 rules); IDE rules are
handled by `dotnet format` on save via the opencode.jsonc save hook.

**The full distribution at enablement:**

| #   | Diagnostic            | Description                   | Count | % of Total |
| --- | --------------------- | ----------------------------- | ----- | ---------- |
| 1   | CA2007                | Missing ConfigureAwait(false) | 124   | 50.0%      |
| 2   | CA1849                | Use async I/O methods         | 64    | 25.8%      |
| 3   | SA\*                  | Formatting violations         | 48    | 19.4%      |
| 4   | MA0016                | Use collection abstractions   | 42    | 16.9%      |
| 5   | CA1305                | Culture-invariant ToString    | 28    | 11.3%      |
| 6   | MA0002                | StringComparer.Ordinal        | 16    | 6.5%       |
| 7   | CA1869/1854/1812/1859 | Performance/API               | 14    | 5.6%       |
| 8   | MA0051                | Method too long               | 14    | 5.6%       |
| 9   | MA0048                | Nested type in own file       | 8     | 3.2%       |

Three findings emerged that shape all subsequent tooling work:

1. **CA2007 is the only diagnostic LLMs NEVER produce correctly.** Session
   history confirms 7 fix cycles across sessions, zero correct first tries
   (session history). Every session with new async code produces CA2007
   violations that the LLM cannot preempt. Peak was 124 CA2007 errors in a
   single TreatWarningsAsErrors enablement build.

2. **SA\* formatting rules are already handled** by the `dotnet format` pipeline
   triggered on save via opencode.jsonc. They require no custom fixer
   investment.

3. **MA0051 and MA0048 cannot be auto-fixed.** They require structural
   refactoring (method extraction, type extraction to own files). Any tool
   that touches them must produce a human-readable report, not an automated
   code change.

4. **Roslynator CLI is the ideal fixer framework** but is blocked on .NET 10
   SDK (issue #1748: `System.Composition.AttributedModel` removed from the
   shared framework). The custom `MSBuildWorkspace` tool is an
   architecture-identical bridge, to be discarded when Roslynator ships a
   `net10.0` target.

## Guidance

### Prioritization Framework: Three-Phase Rollout

The distribution data dictates a phased approach ordered by ROI, not by
diagnostic count alone.

#### Phase 1: CA2007 (current)

**Why this first:** 50% of all violations. Zero LLM preemption. High manual-fix
token cost (~2 turns per violation). Architecture research complete.

**Approach:** Custom MSBuildWorkspace tool that loads the official CA2007
analyzer, applies only CA2007 fixes. Architecture documented in
`configureawait-auto-fix-research-2026-05-17.md`.

#### Phase 2: CA1849 + Remaining Auto-Fixable Categories

**Why this comes second:** 164 additional auto-fixable violations across 6
diagnostic categories (66.1% of the baseline).

| Diagnostic   | Auto-Fixable? | Fix Pattern                                               |
| ------------ | ------------- | --------------------------------------------------------- |
| CA1849       | Yes           | `stream.Read(...)` → `stream.ReadAsync(...)`              |
| MA0016       | Yes           | `List<T>` → `IList<T>` in signatures                      |
| CA1305       | Yes           | `.ToString()` → `.ToString(CultureInfo.InvariantCulture)` |
| MA0002       | Yes           | Add `StringComparer.Ordinal` argument                     |
| CA18XX group | Mixed         | Conditional syntax rewrites                               |

**Infrastructure pattern:** All share the identical `MSBuildWorkspace` pattern
validated on CA2007. A single `ApplyAnalyzerFixes` tool loads the project,
runs all official analyzers, filters to the diagnostic IDs in scope, and
applies syntax rewrites.

**Risk note:** CA1849 async I/O replacement can change semantics in iterator
methods, `finally` blocks, or code that relies on synchronous I/O ordering.
Always gate with `CI=true` skip.

#### Phase 3: Structural-Only Diagnostics

MA0051 (5.6%) and MA0048 (3.2%) cannot be fixed by pattern replacement. They
require understanding code responsibility boundaries.

**Tool output:** A diagnostic report (JSON), not an auto-fix. Consumed by
cleanup-planning sessions. The cleanup skill (`rm-gates-cleanup`) already
handles these as part of quality gates workflows.

### Operationalizing Phases via .editorconfig Severity

`TreatWarningsAsErrors=true` is a blunt instrument — every `warning`-
severity diagnostic becomes a hard error. This prevents the Phase 2/3
distinction from working: diagnostics the fixer does not yet handle
should remain warnings, not block the build.

The correct approach is per-diagnostic severity in `.editorconfig`:

```ini
# Phase 1 — fixer handles this, no reason to ever build with it
dotnet_diagnostic.CA2007.severity = error

# Phase 2 — not yet in fixer, visible but does not block build
dotnet_diagnostic.CA1849.severity = warning
dotnet_diagnostic.MA0016.severity = warning
dotnet_diagnostic.CA1305.severity = warning
dotnet_diagnostic.MA0002.severity = warning

# Phase 3 — structural, cannot be auto-fixed, requires manual triage
dotnet_diagnostic.MA0051.severity = warning
dotnet_diagnostic.MA0048.severity = warning
```

When a diagnostic moves from Phase 2 to Phase 1 (fixer handles it),
change its severity from `warning` to `error`. Any diagnostic not yet
covered by the fixer remains `warning` — visible in build output but
not blocking.

This replaces `TreatWarningsAsErrors=true` entirely. The difference:

| Approach                     | CA2007 (fixed) | CA1849 (unfixed) | MA0051 (structural) |
| ---------------------------- | -------------- | ---------------- | ------------------- |
| `TreatWarningsAsErrors=true` | Blocks build   | Blocks build     | Blocks build        |
| Per-diagnostic severity      | Blocks build   | Shows warning    | Shows warning       |

CI still sees the full picture — diagnostics at `error` severity are
hard failures regardless of warnings-as-errors settings.

The order of rollout: convert to per-diagnostic severity → Phase 1
diagnostics at `error` → add Phase 2 diagnostics to fixer → promote
to `error` one at a time.

### Architectural Principles (All Phases)

1. **Always load the official analyzer DLL**, never re-implement detection
   logic. Official analyzers are maintained by the .NET/Meziantou/Roslynator
   teams and guaranteed correct for all edge cases.

2. **Use `MSBuildWorkspace`**, not `CSharpSyntaxTree.ParseText`. The workspace
   reuses the cached compilation from the last `dotnet build` — no second
   compilation.

3. **Run as a separate script**, never embed in MSBuild `.targets`. MSBuild
   targets cannot access Roslyn diagnostics as items, and pre-compilation
   hooks race with the diagnostic pipeline.

4. **CI skip via environment variable.** `CI=true` → no-op. CI treats all
   diagnostics as hard errors — auto-fix runs only on dev boxes.

5. **Descending position order per file.** Syntax spans are byte offsets.
   Fixing position 100 breaks span calculations for position 200. Process
   diagnostics in descending position order per file.

## Why This Matters

### Data-Driven Decisions, Not Intuition

Without the distribution data, a fixer team might invest first in MA0016
(collection abstractions, 16.9%) because it's "clean code." The data shows
CA2007 is 3× larger AND uniquely LLM-resistant — investing anywhere else
first would leave 50% of violations accumulating on every async edit.

### LLM Failure Mode is Quantified

"LLMs forget ConfigureAwait" is a known phenomenon. What the distribution
data adds is magnitude: 124 occurrences, 0% first-try correctness across
7 sessions, ~2 turn overhead per occurrence. This transforms ConfigureAwait
from "annoying" to "highest-ROI automation target in the project."

### Infrastructure Reuse is Proven

The `MSBuildWorkspace` pattern validated on CA2007 is not CA2007-specific.
Every Phase 2 diagnostic follows the identical pattern: load project → run
official analyzer → filter diagnostic IDs → apply syntax rewrites. The
per-diagnostic cost is only the syntax rewrite logic.

### Structural Diagnostics are Explicitly Deferred

MA0051 and MA0048 appear in the distribution at 8.8% combined. The data
makes clear that building an auto-fixer for these is not viable — method
extraction and type extraction are semantic refactoring, not pattern
replacement.

### The Roslynator CLI Block is Documented

The Roslynator CLI (`roslynator fix CA2007`) would be the ideal solution —
it already supports diagnostic-specific fixing. The .NET 10 SDK block
(issue #1748) is documented here so future readers understand why a custom
tool exists. When Roslynator ships `net10.0`, the custom `MSBuildWorkspace`
tool can be archived.

## When to Apply

Reference this distribution data and prioritization framework whenever:

- **Adding a new auto-fix capability** to the `MSBuildWorkspace` tool. The
  Phase 2/3 classification determines whether the diagnostic justifies a
  syntax rewriter (Phase 2) or should only produce a report (Phase 3).

- **A new analyzer is enabled** and produces a spike of violations. Run the
  same distribution analysis (count by diagnostic ID) before deciding where
  to invest.

- **Planning a cleanup session.** The Phase 1/2/3 framework tells the
  cleanup agent which violations will be auto-fixed (no human review needed),
  which require verification (async I/O order changes), and which must be
  handled manually (method extraction).

- **The Roslynator CLI becomes available** on .NET 10. At that point, compare
  the custom tool's diagnostic coverage against Roslynator's built-in fixers.

## Examples

### Prioritization Table (from Baseline Data)

```
┌──────────────────────────────────────────────────────────────────┐
│                    PHASE 1 — Ship Immediately                     │
│  CA2007 (124, 50.0%): ConfigureAwait(false) — LLM-proof         │
│  7 fix cycles, zero correct first tries (session history)        │
├──────────────────────────────────────────────────────────────────┤
│                    PHASE 2 — Next Investment                     │
│  CA1849  (64,  25.8%): Async I/O replacement                    │
│  MA0016  (42,  16.9%): Collection abstraction                   │
│  CA1305  (28,  11.3%): Culture-invariant ToString               │
│  MA0002  (16,   6.5%): StringComparer.Ordinal                   │
│  CA18XX  (14,   5.6%): Performance/API group                    │
│  Total:  164 violations (66.1%) fixable via one pipeline        │
├──────────────────────────────────────────────────────────────────┤
│                    DEFERRED                                       │
│  SA*     (48,  19.4%): Handled by dotnet format on save         │
│  MA0051  (14,   5.6%): Method too long (structural)             │
│  MA0048  ( 8,   3.2%): Nested type in own file (structural)     │
│  Total:  70 violations (28.2%) deferred or handled elsewhere    │
└──────────────────────────────────────────────────────────────────┘
```

### MSBuildWorkspace Pattern Scaffold (Phase 2 Template)

```csharp
// tools/ApplyAnalyzerFixes/Program.cs
// Generalized pattern — add diagnostic IDs to extend coverage

var diagnosticFixers = new Dictionary<string, Func<SyntaxNode, Diagnostic, SyntaxNode>>
{
    ["CA2007"] = (node, diag) => /* ConfigureAwait rewrite */,
    ["CA1849"] = (node, diag) => /* Stream.Read → ReadAsync */,
    ["MA0016"] = (node, diag) => /* List<T> → IList<T> */,
    ["CA1305"] = (node, diag) => /* .ToString(CultureInfo.InvariantCulture) */,
    ["MA0002"] = (node, diag) => /* StringComparer.Ordinal argument */,
};

var workspace = MSBuildWorkspace.Create();
var project = await workspace.OpenProjectAsync(projectPath);

var analyzers = LoadOfficialAnalyzers();
var compilation = await project.GetCompilationAsync();

var fixableDiagnostics = (await compilation!
    .WithAnalyzers(analyzers)
    .GetAnalyzerDiagnosticsAsync(token))
    .Where(d => diagnosticFixers.ContainsKey(d.Id));

// Descending position order per file
foreach (var fileGroup in fixableDiagnostics
    .GroupBy(d => d.Location.SourceTree?.FilePath))
{
    var tree = fileGroup.First().Location.SourceTree!;
    var root = await tree.GetRootAsync();

    foreach (var diag in fileGroup
        .OrderByDescending(d => d.Location.SourceSpan.Start))
    {
        var node = root.FindNode(diag.Location.SourceSpan);
        root = diagnosticFixers[diag.Id](node, diag);
    }

    await File.WriteAllTextAsync(tree.FilePath,
        root.ToFullString(), token);
}
```

## Related

- `configureawait-auto-fix-research-2026-05-17.md` — Definitive research on the
  official-analyzer + `MSBuildWorkspace` architecture
- `automated-configureawait-fixer-2026-05-16.md` — Full journey log of the
  custom Roslyn fixer build (6 dead-end approaches documented)
- `csharp-standards-final-2026-04-06.md` — Authoritative `ConfigureAwait(false)`
  policy and analyzer configuration
- `rm-guide-async` — Project ConfigureAwait conventions
- `rm-gates-cleanup` — Phase 3 structural violation remediation workflow
