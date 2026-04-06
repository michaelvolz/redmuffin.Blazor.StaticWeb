---
name: strict-coding-standards
description: "Enforces composition over inheritance, constructor DI only, TDD (Red-Green-Refactor), trunk-based development, and SOLID/Clean Architecture rules. Use ONLY when creating new services/classes, designing feature architecture, performing structural refactoring, or reviewing code for design-pattern violations. Do NOT load for trivial bug fixes, config edits, CSS/SCSS changes, documentation, or running commands. If a bug fix requires structural changes, load the skill."
---

# STRICT CODING RULES (MANDATORY - VIOLATION = REGENERATE)

1. ARCHITECTURE & DESIGN (ALWAYS)

Prefer composition over inheritance in 100% of cases unless true "is-a" specialization with no viable composition (use interfaces + delegation/Strategy/Decorator).
Model "has-a" via interfaces; never inherit for reuse. Keep hierarchies flat (<2 levels max).
Follow Dependency Rule: outer layers depend inward only (Clean Architecture layers: Domain → Application → Infrastructure).
Single Responsibility Principle (SRP): one reason to change per class/service.
Interface Segregation: client-specific interfaces (small, focused).
Open/Closed: extend via composition, never modify core.
Liskov Substitution: subtypes replaceable without behavior change.
No god classes, no anemic domain models.

2. DEPENDENCY INJECTION (STRICT .NET BUILT-IN)

Nevernew dependencies inside methods/constructors (except primitives, DTOs, or pure value objects).
Always constructor injection for required deps; use IServiceProvider only for optional/runtime factories.
Register via extension methods in Infrastructure layer (AddMyFeature(this IServiceCollection)).
Use Microsoft.Extensions.DependencyInjection only (no third-party containers unless explicitly approved).
Lifetimes (strict):
Transient: lightweight, no state.
Scoped: per-request (e.g., DbContext, repositories).
Singleton: thread-safe, expensive, global state only.

NEVER inject Scoped into Singleton (captive dependency). Use IServiceScopeFactory in singletons when needed.
Validate scopes in dev (validateScopes: true).
Configuration: Options pattern only (IOptions<T>, never raw IConfiguration).
No Service Locator (GetService<T>() inside business code = anti-pattern).
Keyed services for multiple impls of same interface.
All services small, testable, no statics/stateful globals.

3. TDD (RED-GREEN-REFACTOR - NON-NEGOTIABLE)

Write failing test first (Red) → minimal code to pass (Green) → refactor.
Three laws: (1) No prod code without failing test. (2) Only enough test to fail. (3) Only enough prod to pass.
Tests first for all business logic, use cases, domain rules.
Use xUnit/NUnit/MSTest + Moq/AutoFixture + FluentAssertions.
Unit tests: isolate via interfaces/DI (mocks for deps).
Integration tests for external boundaries only.
80%+ coverage on domain/application; 100% on critical paths.
Tests independent, fast (<100ms), descriptive names (Should_When_Then).
Never change passing tests except for requirement change.
Refactor only after Green; keep tests green at all times.

4. TRUNK-BASED DEVELOPMENT (TBD)

All work commits to trunk (main) multiple times/day.
Changes small (hours max); no long-lived branches.
Short-lived PR branches (<1 day) only for review/CI; delete after merge.
Pre-commit: full local build + all tests pass.
CI must run on every commit; trunk always green/releasable.
Hide WIP with feature flags (Config or LaunchDarkly-style) or branch-by-abstraction.
No feature branches for release artifacts.
Use TDD + feature flags to keep trunk stable.

5. CODE STYLE & QUALITY (ENFORCED)

C# latest (nullable enabled, records, primary constructors where clean).
Blazor: component composition > inheritance; inject services; use @inject.
PowerShell: same DI/composition mindset when applicable.
No comments explaining code; code must be self-documenting.
Pure functions where possible; immutable by default.
Domain events for side effects.
CQRS when beneficial (MediatR or minimal APIs).
No direct EF/DbContext in application layer (repositories only if needed; use use-case services).
Error handling: Result<T> or exceptions with global filters (never silent fails).
Logging: structured, injected ILogger<T>.
Performance: async/await everywhere possible; no .Result/.Wait.
Security: validate inputs, least privilege, no secrets in code.

6. FILE/PROJECT STRUCTURE (STANDARD)
   textSolution
   ├── Domain/ (entities, value objects, interfaces, exceptions)
   ├── Application/ (use cases, services, DTOs)
   ├── Infrastructure/ (impls, EF, external clients, DI extensions)
   ├── Presentation/ (API, Blazor, controllers/components)
   ├── Tests/ (Unit, Integration)
7. AGENT WORKFLOW RULES

For new feature: (1) Write tests first. (2) Implement via TDD. (3) Inject all deps. (4) Compose, never inherit. (5) Feature flag if not complete. (6) Small PR to trunk.
Refactor existing: preserve tests, apply rules above.
Review own output: check every rule before finalizing.
Ask for clarification only on ambiguous requirements; never guess architecture.

VIOLATIONS TRIGGER AUTO-REVIEW + REGENERATION. THESE RULES ARE NON-NEGOTIABLE FOR ALL OUTPUT.

**Activation**: This skill applies to new services/classes, feature architecture, structural refactoring, and code reviews. It does NOT apply to trivial bug fixes, config edits, CSS/SCSS changes, documentation, or running commands. If a bug fix requires structural changes, load the skill.
