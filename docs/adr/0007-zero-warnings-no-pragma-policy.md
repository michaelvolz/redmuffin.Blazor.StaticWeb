---
date: 2026-05-30
status: accepted
---

# Zero Warnings Policy — TreatWarningsAsErrors, No Pragmas

Every project has `TreatWarningsAsErrors=true`. Zero diagnostics are tolerated
in any build configuration. `#pragma warning disable` is banned except for one
narrowly defined case: when two analyzers enforce genuinely contradictory
patterns on the same code location, a file-level suppression with documented
reasoning is permitted and must be added to the known-conflicts table.

## Considered Options

**Keep warnings as warnings.**
Rejected. Warnings accumulate and become ambient noise. A team with a "warnings
are OK" policy never reaches zero — the metric becomes "acceptable number of
warnings," which drifts upward over time. Zero is a crisp, unambiguous target.

**Selective suppression via `NoWarn` in `.csproj` or `.editorconfig`.**
Rejected. Project-wide suppression hides problems globally — future code
in the same project silently inherits the exemption without scrutiny. Every
suppression must be location-specific and intentional.

**Suppression via `.editorconfig` `severity = none`.**
Rejected. Same blanket problem as `NoWarn`. The `editorconfig` is the top
authority for code quality rules; muting diagnostics there subverts its role.

**Liberal `#pragma warning disable`.**
Restricted to one specific exemption. Genuine analyzer contradictions (two
analyzers enforcing incompatible rules on the same code location) are the
only acceptable use. Each instance requires file-level suppression (not
line-level), a `// Reason:` comment explaining the conflict, and an entry in
the known-conflicts table in the `rm-warnings` skill.

## Consequences

- Initial enforcement fixed 248 warnings to zero across 9 fix batches.
- `dotnet format` auto-fixes approximately 75% of StyleCop and Roslyn analyzer
  violations — remaining warnings must be fixed manually.
- `dotnet build` literally means zero diagnostics. Every new analyzer version
  that adds warnings triggers a mandatory fix cycle before merging.
- The `rm-warnings` skill documents the pragma decision tree and the
  known-conflicts table.
- During development editing, warnings are informational (not build-breaking)
  to keep iteration fast. `TreatWarningsAsErrors` is enforced in CI and at
  commit time.
- When two analyzers conflict on the same code pattern, the fix is to align
  the code with the stricter rule, never to suppress one analyzer.
