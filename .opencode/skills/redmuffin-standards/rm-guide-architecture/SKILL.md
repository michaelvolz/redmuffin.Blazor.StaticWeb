---
name: rm-guide-architecture
description: "Use when designing services, boundaries, patterns, or cross-layer C# changes."
---

# rm-guide-architecture

See also: `rm-gates-cleanup` Gate 3 for the Architecture gate (`--arch-config`
flag, `arch-rules.yml`), `rm-guide-cleanup` §1 for SLAP and method quality.

## CRITICAL

- Never use inheritance where composition suffices.
- Keep dependencies flowing inward.
- Give each type one reason to change.

## WHEN TO LOAD

- Designing a new feature slice or service.
- Refactoring boundaries across components, services, and APIs.

## GUIDANCE

- Use small, explicit abstractions.
- Keep domain, application, infrastructure, and presentation concerns separate.
- Introduce patterns only when they reduce complexity.
- After structural changes, run `dotnet run -- arch --arch-config arch-rules.yml`
  to verify no dependency violations.
- When multiple components share a workflow, prefer a composed orchestrator
  (context record + static methods with `Func<>` callbacks) over a base class.
  Example: `docs/solutions/architecture-patterns/composition-over-inheritance-orchestrator-pattern-2026-05-23.md`

## NEVER

- Do not add architecture for hypothetical future use.
- Do not use Service Locator in business code.
