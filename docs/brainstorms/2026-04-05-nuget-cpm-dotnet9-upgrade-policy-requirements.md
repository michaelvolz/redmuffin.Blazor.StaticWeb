---
date: 2026-04-05
topic: nuget-cpm-dotnet9-upgrade-policy
---

# NuGet CPM Upgrade Policy for .NET 9

## Problem Frame

The repository needs a clear, low-risk way to update NuGet packages when using
Central Package Management. The goal is to keep dependencies current without
accidentally pulling in .NET 10 requirements or breaking the existing .NET 9 /
Blazor / Azure Static Web Apps baseline.

## Requirements

**Package update policy**

- R1. Package updates must preserve .NET 9 compatibility for the repo.
- R2. Updates must avoid introducing any dependency that requires .NET 10.
- R3. The update process should work well with Central Package Management.

**Contributor guidance**

- R4. The repo should document the package-update workflow clearly enough for
  contributors to follow it consistently.
- R5. The guidance should call out the .NET 9 baseline and the no-.NET-10
  constraint.

## Success Criteria

- Contributors can update packages without guessing which version line is safe.
- Package updates do not introduce a .NET 10 dependency by accident.
- The guidance is clear enough to support a later implementation plan.

## Scope Boundaries

- Not the actual package version selection or upgrade execution.
- Not a full dependency audit of every package in the repo.
- Not a broader platform migration beyond keeping the current .NET 9 baseline.

## Key Decisions

- Stay on .NET 9 for this work.
- Treat .NET 10 dependencies as out of bounds for the immediate update effort.
- Document the workflow instead of relying on tribal knowledge.

## Dependencies / Assumptions

- Assumes the repository remains on the current .NET 9 / Blazor / Azure Static
  Web Apps stack.
- Assumes package updates will be handled through CPM rather than per-project
  package pinning.

## Outstanding Questions

### Deferred to Planning

- [Affects R1-R3][Needs research] What is the safest command-line and review
  workflow for package updates in this repo?
- [Affects R4-R5][User decision] Should the workflow guidance live in
  `AGENTS.md`, `README.md`, or both?

## Next Steps

→ /ce:plan for structured implementation planning
