---
name: rm-guide-architecture
description: "Shortcut: rm:guide-architecture. Use when designing services, boundaries, patterns, or cross-layer C# changes."
---

# rm-guide-architecture

See also: `rm-gates-cleanup` Gate 3 for the Architecture gate (`--arch-config`
flag, `arch-rules.yml`), `rm-guide-cleanup` §1 for SLAP and method quality.

## CRITICAL

- Prefer composition over inheritance.
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

## NEVER

- Do not add architecture for hypothetical future use.
- Do not use Service Locator in business code.
