---
title: Consolidate C# Coding Standards and Resolve Documentation Contradictions
date: 2026-04-06
last_updated: 2026-04-06
category: best-practices
module: csharp
problem_type: documentation_gap
component: documentation
severity: medium
applies_when:
  - Writing or reviewing C# code in the repository
  - Configuring code analyzers or .editorconfig
  - Updating coding standards documentation or skills
  - Onboarding new developers who reference coding standards
tags:
  - csharp
  - coding-standards
  - documentation
  - consolidation
  - tunit
  - blazor
  - dependency-injection
  - editorconfig
---

> **Historical (2026-06-08):** Process documentation for the April 2026 consolidation. References several skills that were since deleted (m-csharp-standards, m-output-style, m-strict-coding-standards, m-dotnet). The actual standards live in csharp-standards-final.md. Retained as process archaeology.

# Consolidate C# Coding Standards and Resolve Documentation Contradictions

## Context

C# coding standards were scattered across 11 sources within the project (skills, guides, .editorconfig, AGENTS.md, codebase analysis). This caused friction when:

- Contributors followed outdated skill guidance that contradicted actual codebase patterns
- Code reviews raised false positives based on stale documentation
- AI agents produced code matching documented standards but violating actual codebase conventions
- .editorconfig had conflicting indentation rules that confused formatters

The core issue: **documentation drift**. Skills and guides had evolved separately from the codebase, creating a "documentation vs reality" gap.

## Guidance

### Testing Framework

```csharp
// ✅ CORRECT: TUnit with built-in assertions
[Test]
[Category("Feature:Home")]
public async Task MyTest()
{
    using (Assert.Multiple())
    {
        await Assert.That(actual).IsNotNull();
        await Assert.That(actual.Value).IsEqualTo(expected);
    }
}

// ❌ WRONG: FluentAssertions package (not in project)
actual.Should().Be(expected);

// ✅ CORRECT: Custom mocks for internal components
public sealed class NavigationManager_Mock : NavigationManager
{
    public string? NavigatedTo { get; private set; }
    protected override void NavigateToCore(string uri, NavigationOptions options)
        => NavigatedTo = uri;
}

// ✅ CORRECT: LightMock.Generator for external dependencies
var httpClientMock = new Mock<IHttpClientFactory>();
httpClientMock.Arrange(f => f.CreateClient(The<string>.IsAnyValue))
    .Returns(new HttpClient());
```

**Key Decision**: TUnit has built-in fluent assertions (`Assert.That()`). Do NOT use the separate FluentAssertions package.

### Blazor Dependency Injection

```csharp
// ✅ CORRECT: C# 11+ required modifier for new code
[Inject]
public required NavigationManager Navigation { get; set; }
[Inject]
public required ILogger<Home> Logger { get; set; }

// ❌ WRONG: default! for new code
[Inject]
public NavigationManager Navigation { get; set; } = default!;

// ⚠️ ACCEPTABLE: default! in legacy code (migrate gradually)
// Only for existing code that hasn't been updated yet
```

**Key Decision**: The `required` modifier provides compile-time null safety, eliminating the need for `default!` and runtime validation.

### Indentation

```csharp
// ✅ CORRECT: 4 spaces for C# files
// .editorconfig effective rule: indent_size = 4, indent_style = space

// ❌ WRONG: Tabs (skills documentation was outdated)
```

**Key Decision**: Codebase uses 4 spaces. The .editorconfig had conflicting rules (both tab and space for .cs files), but the effective setting is space.

### .editorconfig Conflict

The original .editorconfig had `.cs` files in both tab and space indentation rules:

```ini
# CONFLICT: .cs appears in both rules
[*.{c,...,cs,...}]  # Line 191: indent_style = tab
[*.{...,cs,...}]   # Line 203: indent_style = space

# RESOLUTION: Document the conflict, effective setting is space (4)
```

## Why This Matters

| Impact                   | Description                                                                                                                                    |
| ------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------- |
| **Contributor Friction** | When documentation contradicts codebase reality, developers waste time resolving false conflicts or produce non-standard code requiring rework |
| **AI Agent Accuracy**    | AI assistants rely heavily on documented standards. Stale documentation causes agents to generate code that fails code review                  |
| **Review Efficiency**    | Reviewers may raise issues based on outdated guidance, creating noise and slowing PR throughput                                                |
| **Onboarding Clarity**   | New contributors need a single source of truth, not 11 contradictory sources                                                                   |
| **Compile-Time Safety**  | The `required` vs `default!` choice matters—`required` catches null issues at compile time rather than runtime                                 |

## When to Apply

**Apply this guidance when:**

- Creating new Blazor components with `[Inject]` properties
- Writing new test files or test methods
- Formatting C# code or configuring IDE/editor settings
- Reviewing PRs that touch test code or Blazor components
- Onboarding new contributors to the codebase
- Updating or consolidating documentation files
- Configuring .editorconfig or analyzer rules

**Priority rules:**

1. **Codebase reality wins** — actual patterns in code take precedence over documented ones
2. **Compile-time safety wins** — prefer `required` over `default!` for new code
3. **Built-in wins** — TUnit's built-in assertions over external assertion libraries
4. **Common practice wins** — spaces (4) over tabs for C# in this codebase

**Do NOT apply:**

- Do not block PRs for `default!` in existing files (gradual migration)
- Do not require FluentAssertions migration (project doesn't use it)

## Examples

### Example 1: New Blazor Component

```csharp
// BEFORE (following outdated skill):
public partial class MyComponent : ComponentBase
{
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    protected override async Task OnInitializedAsync()
        => ArgumentNullException.ThrowIfNull(Navigation);
}

// AFTER (correct pattern):
public partial class MyComponent : ComponentBase
{
    [Inject]
    public required NavigationManager Navigation { get; set; }

    // No runtime null check needed - compile-time safety
}
```

### Example 2: New Test File

```csharp
// BEFORE (following skill with xUnit + FluentAssertions):
using Xunit;
using FluentAssertions;

public class MyTests
{
    [Fact]
    public void Test()
    {
        actual.Should().Be(expected);
    }
}

// AFTER (correct TUnit pattern):
public sealed partial class MyTests
{
    [Test]
    [Category("Unit")]
    public async Task Test()
    {
        await Assert.That(actual).IsEqualTo(expected);
    }
}
```

### Example 3: .editorconfig Fix

```ini
# BEFORE (conflicting):
[*.{c,c++,cs,...}]
indent_style = tab

[*.{cs,cshtml,...}]
indent_style = space
indent_size = 4

# AFTER (resolved):
# C/C++ files use tabs, C# files use spaces
# .cs removed from tab rule to eliminate conflict
```

## Notes to Investigate

Two items require further research to finalize:

1. **`ArgumentNullException.ThrowIfNull`** — May trigger analyzer warnings; need ONE approved pattern
2. **`LoggerMessage.Define` syntax** — May have updated patterns in .NET 9; verify current best practice

## Next Steps

### Phase 1: Investigate Skill Loading Triggers

- Analyze how OpenCode triggers skill loading
- Document trigger patterns (file type, task type, keywords)
- Identify optimal granularity for skill guides

### Phase 2: Create Separate Guide Skills

Each section becomes a separate skill with prefix `rm-guide-`:

| Section                   | Proposed Skill Name        | Trigger When...                                                   |
| ------------------------- | -------------------------- | ----------------------------------------------------------------- |
| 1. Naming Conventions     | `rm-guide-naming`          | Creating/renaming types, methods, fields, test doubles            |
| 2. C# 12/13 Features      | `rm-guide-csharp-features` | Using new C# features, modernizing code                           |
| 3. Async Programming      | `rm-guide-async`           | Writing async code, ConfigureAwait, Task patterns                 |
| 4. File-Scoped Namespaces | `rm-guide-namespaces`      | Creating new files, namespace organization                        |
| 5. Logging                | `rm-guide-logging`         | Adding logging, LoggerMessage pattern, partial class organization |
| 6. Dependency Injection   | `rm-guide-di`              | Injecting services, Blazor components, constructor injection      |
| 7. Testing Standards      | `rm-guide-testing`         | Writing tests, test doubles, TUnit, TestScope                     |
| 8. Zero Warnings Policy   | `rm-guide-warnings`        | Fixing build warnings, pragma warnings, analyzer rules            |
| 9. Blazor Components      | `rm-guide-blazor`          | Creating Blazor components, lifecycle, state management           |
| 10. Azure Functions       | `rm-guide-azure-functions` | Creating Azure Functions, isolated worker, DI                     |
| 11. Architecture & Design | `rm-guide-architecture`    | Designing services, SOLID, Clean Architecture, patterns           |
| 12. Project Configuration | `rm-guide-config`          | Build commands, test commands, dev modes, package management      |
| 13. .NET 9 Best Practices | `rm-guide-dotnet9`         | Using .NET 9 features, performance optimizations                  |
| 14. Code Quality          | `rm-guide-code-quality`    | Expression-bodied members, records, null pattern matching         |

### Phase 3: Reference in AGENTS.md

- Add all `rm-guide-*` skills to AGENTS.md
- Document trigger conditions for each skill
- Ensure AI knows what exists and when to self-trigger

### Phase 4: Reality Check

- Analyze each section for content that is 100% superfluous to ANY model
- Identify content that models already know (common knowledge)
- Create foolproof system to identify trimmable content
- Document criteria for "superfluous" vs "essential"

### Success Criteria

1. Each skill loads ONLY when relevant task is performed
2. Token usage is minimized (no massive file loading)
3. All essential information is preserved
4. No duplication across skills
5. AGENTS.md provides complete map of available guides

## Related

### Final Document

- **Single Source of Truth**: `docs/solutions/best-practices/csharp-standards-final-2026-04-06.md` — Consolidated C# standards (14 sections, ~1,600 lines)

### Research Documents

The consolidation process analyzed 11 source documents. Intermediate work products (raw consolidation, contradictions analysis, duplicates analysis) were removed during the 2026-05-09 refresh — the key findings are preserved in this resolution document and the final standards reference below.

### Related Solutions

- **Test Double Pattern**: `docs/solutions/best-practices/test-double-disposable-pattern-2026-04-06.md` — TUnit testing patterns for mock disposal

### Source Files (Archived)

> **2026-05-24 update:** The four archived skill files have been deleted from the codebase:
> `rm-csharp-standards`, `rm-output-style`, `rm-strict-coding-standards`, and `rm-dotnet`.
> Their content was consolidated into the final standards doc and the `redmuffin-standards/` sub-skills.
> The `.editorconfig` indentation conflict (.cs in both tab and space rules) is still unresolved
> as of this update — the fix was never applied.

The following source files were analyzed during consolidation (deleted status as noted):

- ~~`.opencode/skills/redmuffin-standards/rm-csharp-standards/SKILL.md`~~ — deleted, content moved to `rm-guide-csharp-features`
- ~~`.opencode/skills/redmuffin-standards/rm-output-style/SKILL.md`~~ — deleted, content absorbed by editorconfig
- ~~`.opencode/skills/redmuffin-standards/rm-strict-coding-standards/SKILL.md`~~ — deleted, decomposed into 22 sub-skills
- ~~`.opencode/skills/redmuffin-standards/rm-dotnet/SKILL.md`~~ — deleted, content moved to `rm-guide-csharp-features`
- `.editorconfig` — still active, indentation conflict unresolved
- `docs/TestingGuidelines.md` — still active
- `.github/guides/blazor.md` — deleted
- `.github/guides/azure-functions.md` — deleted
- `AGENTS.md`
