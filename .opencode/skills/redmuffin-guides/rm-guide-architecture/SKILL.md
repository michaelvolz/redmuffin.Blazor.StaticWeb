---
name: rm-guide-architecture
description: "Use when designing services, boundaries, patterns, or cross-layer C# changes."
---

# rm-guide-architecture

See also: `rm-quality-gates` Gate 3 for the Architecture gate (`--arch-config`
flag, `arch-rules.yml`), `rm-guide-code-quality` §1 for SLAP and method quality.

## Author Sub-Skills

Load both during architecture work for author-specific principles:

```
skill({ name: "rm-architecture-ousterhout" })
skill({ name: "rm-architecture-uncle-bob-martin" })
```

rm-architecture-ousterhout covers deep modules, complexity
elimination, and information hiding (John Ousterhout).
rm-architecture-uncle-bob-martin covers SOLID principles, Clean
Architecture, and metrics-driven quality standards (Robert C. Martin).

Never load one without the other — they are complementary lenses on the
same architecture domain.

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

## Feature Folder Structure

The project follows the Blazor feature-folder pattern (Giesel 2022,
Hilton 2021). Every feature lives as a top-level folder under `Features/`
with all its code co-located:

```
Features/
  Common/components/     ← shared by 2+ features
  Common/PageLoadSpeed/   ← cross-cutting domain
  Raindrop/               ← domain feature
  HomePage/               ← single-page feature
  DebugPage/              ← multi-page feature
  ...
```

### Rules

- **Feature isolation:** A component in one feature folder must never
  reference a component in a sibling feature. Pull shared components
  up to `Features/Common/Components/`.
- **Locality before reuse:** Do not extract a shared component after
  only 2 consumers. Wait for 3+ distinct features to prove the
  abstraction is real (Metz: Rule of Three).
- **No root `Services/`:** Services belong with their consumers:
  feature-specific in `Features/{Domain}/Services/`, cross-cutting in
  `Core/Services/`.
- **Dead code has no home:** If a model has zero consumers, delete it.
  Do not keep it in a generic bucket "in case we need it later."

### Reference structure (Hilton)

```
Features/Components/ (most abstract)  ← Features/ can reference
Features/Common/     (shared domain)
Features/Raindrop/   (domain feature)
Features/HomePage/   (leaf feature)    ← cannot reference siblings
Core/                (infrastructure)  ← everything can reference
```

`rm-guide-naming` §Directory & Namespace Structure has the full folder-to-namespace mapping.
