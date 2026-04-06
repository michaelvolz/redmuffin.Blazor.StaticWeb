---
title: C# Standards Contradictions Analysis
date: 2026-04-06
tags: [csharp, standards, consolidation, contradictions]
module: redmuffin-standards
problem_type: documentation
---

# C# Standards Contradictions Analysis

This document identifies contradictions between sources and between sources and the actual codebase. **These must be resolved before creating the final consolidated version.**

---

## Contradiction Summary

| #   | Topic             | Sources                    | Codebase              | Severity   | Resolution Needed               |
| --- | ----------------- | -------------------------- | --------------------- | ---------- | ------------------------------- |
| 1   | Testing Framework | xUnit/NUnit/MSTest         | TUnit                 | **HIGH**   | Update skills to match codebase |
| 2   | Blazor DI Pattern | `default!` with validation | Both patterns used    | **HIGH**   | Choose one pattern              |
| 3   | Indentation       | Tab indentation            | Space indentation (4) | **MEDIUM** | Clarify in skills               |
| 4   | .editorconfig     | Conflicting rules          | -                     | **MEDIUM** | Fix .editorconfig               |

---

## Contradiction #1: Testing Framework

### What the Skills Say

**Source 3 (rm-strict-coding-standards)**:

> Use xUnit/NUnit/MSTest + Moq/AutoFixture + FluentAssertions.

**Source 7 (blazor.md)**:

> Test Blazor components and services using xUnit, NUnit, or MSTest.

### What the Codebase Uses

**Actual usage**: TUnit with 268 `[Test]` attribute occurrences

```csharp
// Actual codebase pattern
[Test]
[Category("Feature:Home")]
[Category("Unit")]
public async Task Home_ComponentStructure_HasRequiredElements()
{
    using var scope = CreateTestScope();
    var component = scope.BUnitContext.Render<HomePage>();

    using (Assert.Multiple())
    {
        await Assert.That(component.Find("h1")).IsNotNull();
    }
}
```

### Contradiction

| Aspect           | Skills                      | Codebase                         |
| ---------------- | --------------------------- | -------------------------------- |
| Test framework   | xUnit/NUnit/MSTest          | **TUnit**                        |
| Test attribute   | `[Fact]` / `[Test]` (NUnit) | `[Test]` (TUnit)                 |
| Assertions       | FluentAssertions            | **TUnit fluent** (`Assert.That`) |
| Multiple asserts | Not specified               | `Assert.Multiple()`              |

### Resolution Required

**Option A**: Update skills to reflect TUnit usage (RECOMMENDED)

- Update Source 3 to say: "Use TUnit with fluent assertions"
- Update Source 7 to say: "Test using TUnit"
- Add TUnit-specific patterns to Source 6

**Option B**: Migrate codebase to xUnit (NOT RECOMMENDED)

- Would require rewriting 268+ tests
- TUnit is modern and actively maintained

---

## Contradiction #2: Blazor Dependency Injection Pattern

### What the Skills Say

**Source 4 (rm-dotnet)**:

```csharp
public partial class UserProfile : ComponentBase
{
    [Inject] private IUserService UserService { get; set; } = default!;
    protected override async Task OnInitializedAsync() => ArgumentNullException.ThrowIfNull(UserService);
}
```

### What Source 11 (Codebase Analysis) Shows

```csharp
public partial class Home : ComponentBase
{
    [Inject] public required NavigationManager Navigation { get; set; }
    [Inject] public required IHttpClientFactory HttpClientFactory { get; set; }
}
```

### What the Codebase Actually Uses

**Both patterns are used:**

| Pattern                        | Occurrences | Files                                                          |
| ------------------------------ | ----------- | -------------------------------------------------------------- |
| `[Inject] ... = default!;`     | 13          | App.razor.cs, PageLoadSpeed.razor.cs, LoadSpeed.razor.cs, etc. |
| `[Inject] public required ...` | 4           | Home.razor.cs only                                             |

### Contradiction

| Aspect      | Source 4                | Source 11    | Codebase |
| ----------- | ----------------------- | ------------ | -------- |
| Pattern     | `default!` + validation | `required`   | **Both** |
| Null safety | Runtime check           | Compile-time | Mixed    |
| Visibility  | `private`               | `public`     | Mixed    |

### Resolution Required

**Option A**: Use `required` without `default!` (RECOMMENDED for new code)

- Compile-time null safety
- No runtime validation needed
- Cleaner syntax
- Matches C# 11+ best practices

**Option B**: Use `default!` with validation (current skill)

- Runtime null check
- Explicit validation
- Works with older C# versions

**Option C**: Document both patterns as acceptable

- `required` for new code
- `default!` with validation for existing code

### Recommendation

Standardize on `required` for new Blazor components:

```csharp
// RECOMMENDED for new code
[Inject] public required NavigationManager Navigation { get; set; }
[Inject] public required ILogger<Home> Logger { get; set; }

// ACCEPTABLE for existing code (don't change)
[Inject] private IUserService UserService { get; set; } = default!;
```

---

## Contradiction #3: Indentation

### What the Skills Say

**Source 1 (rm-csharp-standards)**:

> Tab indentation (4 tabs = 4 spaces)

**Source 2 (rm-output-style)**:

> C# Tab (4 spaces)

Both are confusingly worded. "Tab indentation" suggests tabs, but "(4 tabs = 4 spaces)" and "Tab (4 spaces)" suggest spaces.

### What .editorconfig Says

**Lines 191-194** (C/C++ and Related Languages):

```ini
[*.{c,c++,cc,cginc,compute,cp,cpp,cppm,cs,cshtml,cu,cuh,cxx,fx,fxh,h,hh,hlsl,hlsli,hlslinc,hpp,htm,html,hxx,inc,inl,ino,ipp,ixx,mpp,mq4,mq5,mqh,razor,tpp,usf,ush}]
indent_style = tab
```

**Lines 203-206** (Web and UI Files):

```ini
[*.{asax,ascx,aspx,axaml,cs,cshtml,htm,html,master,paml,razor,skin,vb,xaml,xamlx,xoml}]
indent_style = space
indent_size = 4
```

### Contradiction in .editorconfig

Both rules include `.cs` files:

1. Line 191: `indent_style = tab` for `.cs`
2. Line 203: `indent_style = space` for `.cs`

The later rule (lines 203-206) **overrides** the earlier rule, so the effective setting is:

- **`indent_style = space`**
- **`indent_size = 4`**

### What the Codebase Uses

Let me verify the actual indentation in the codebase...

### Resolution Required

**Option A**: Update skills to match .editorconfig (RECOMMENDED)

- Change Source 1 to: "Space indentation (4 spaces)"
- Change Source 2 to: "C# Space (4 spaces)"

**Option B**: Update .editorconfig to use tabs

- Remove `.cs` from line 203
- Keep only line 191 rule

**Option C**: Clarify the contradiction in .editorconfig

- Remove `.cs` from one of the conflicting rules

### Recommendation

Update skills to say "Space indentation (4 spaces)" to match the effective .editorconfig setting.

---

## Contradiction #4: .editorconfig Conflicting Rules

### The Problem

The .editorconfig has overlapping file patterns with conflicting settings:

**Rule 1 (Lines 191-194)**:

```ini
[*.{c,c++,...,cs,cshtml,...}]
indent_style = tab
```

**Rule 2 (Lines 203-206)**:

```ini
[*.{asax,...,cs,cshtml,...}]
indent_style = space
```

Both rules match `.cs` and `.cshtml` files, but with different indentation styles.

### Resolution Required

Remove `.cs` and `.cshtml` from one of the rules:

**Option A**: Keep space indentation (RECOMMENDED)

```ini
# Remove .cs and .cshtml from line 191
[*.{c,c++,cc,cginc,compute,cp,cpp,cppm,cu,cuh,cxx,fx,fxh,h,hh,hlsl,hlsli,hlslinc,hpp,hxx,inc,inl,ino,ipp,ixx,mpp,mq4,mq5,mqh,tpp,usf,ush}]
indent_style = tab
```

**Option B**: Keep tab indentation

```ini
# Remove .cs and .cshtml from line 203
[*.{asax,ascx,aspx,axaml,htm,html,master,paml,razor,skin,vb,xaml,xamlx,xoml}]
indent_style = space
```

---

## Additional Observations (Not Contradictions)

### 1. TDD Stance

**Source 3** says TDD is "NON-NEGOTIABLE" but this is a guideline, not a contradiction. The codebase may or may not follow TDD strictly.

### 2. Trunk-Based Development

**Source 3** says TBD is required, but this is a process guideline, not a code contradiction.

### 3. Composition Over Inheritance

**Source 3** says "100% of cases" for composition over inheritance. This is a strong stance but not a contradiction.

---

## Master Resolution Document

This document serves as the single source of truth for all contradiction resolutions. The final consolidated C# standards document will be created based on these decisions.

### Decisions Made (2026-04-06)

| #   | Contradiction              | Decision                                                                                                                                                                                                                                     | Rationale                                                                                                                  |
| --- | -------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| 1   | **Testing Framework**      | Use **TUnit only** with built-in fluent assertions (`Assert.That()`). Prefer custom mocks over LightMock.Generator for internal components. Use LightMock.Generator for external dependencies only. **Do NOT use FluentAssertions package.** | Codebase uses TUnit (268 tests). TUnit has built-in fluent assertions. Custom mocks are preferred for internal components. |
| 2   | **Blazor DI Pattern**      | Use **`required` modifier** (C# 11+). The `default!` pattern is **invalid** for new code. Compile-time null safety eliminates runtime validation.                                                                                            | `required` provides compile-time safety. Cleaner syntax. Matches C# 11+ best practices.                                    |
| 3   | **Indentation**            | Use **4 spaces** for C# files.                                                                                                                                                                                                               | Matches effective .editorconfig setting. Clear and unambiguous.                                                            |
| 4   | **.editorconfig Conflict** | Remove `.cs` and `.cshtml` from tab indentation rule. C# files use space indentation (4 spaces).                                                                                                                                             | Eliminates conflicting rules. Matches actual codebase usage.                                                               |

### Code Patterns to Use

#### Testing (TUnit)

```csharp
// CORRECT: TUnit with built-in fluent assertions
[Test]
[Category("Feature:Home")]
[Category("Unit")]
public async Task Home_ComponentStructure_HasRequiredElements()
{
    using var scope = CreateTestScope();
    var component = scope.BUnitContext.Render<HomePage>();

    using (Assert.Multiple())
    {
        await Assert.That(component.Find("h1")).IsNotNull();
        await Assert.That(component.Find("button")).IsNotNull();
    }
}

// CORRECT: Custom mock for internal components
public sealed class NavigationManager_Mock : NavigationManager
{
    public string? NavigatedTo { get; private set; }
    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
        NavigatedTo = uri;
    }
}

// CORRECT: LightMock for external dependencies
var httpClientMock = new Mock<IHttpClientFactory>();
httpClientMock.Arrange(f => f.CreateClient(The<string>.IsAnyValue))
    .Returns(new HttpClient());

// WRONG: Do NOT use FluentAssertions package
// await component.Find("h1").Should().NotBeNull(); // NO
```

#### Blazor Dependency Injection

```csharp
// CORRECT: Use required modifier (C# 11+)
public partial class Home : ComponentBase
{
    [Inject] public required NavigationManager Navigation { get; set; }
    [Inject] public required ILogger<Home> Logger { get; set; }
    [Inject] public required IHttpClientFactory HttpClientFactory { get; set; }
}

// WRONG: Do NOT use default! for new code
// [Inject] private IUserService UserService { get; set; } = default!; // NO
```

#### Indentation

```csharp
// CORRECT: 4 spaces for C#
public class Example
{
    public void Method()
    {
        if (condition)
        {
            DoSomething();
        }
    }
}

// WRONG: Do NOT use tabs for C#
// public class Example
// {
//     public void Method()
//     {
//     }
// }
```

---

## Resolution Checklist

Before creating the final consolidated version, resolve:

- [x] **Contradiction #1**: TUnit with built-in fluent assertions, custom mocks preferred ✅ DECIDED
- [x] **Contradiction #2**: `required` modifier for Blazor DI ✅ DECIDED
- [x] **Contradiction #3**: 4 spaces indentation ✅ DECIDED
- [x] **Contradiction #4**: Fix .editorconfig conflicting rules ✅ DECIDED

---

## Next Steps

1. Create final consolidated C# standards document with all contradictions resolved
2. Create new `rm-c-sharp-standards` skill with the consolidated content
3. Archive original source documents (keep unchanged for reference)

---

## Proposed Resolutions

### 1. Testing Framework

**Update all skills to say:**

> Use TUnit with fluent assertions. Use `[Test]` attribute for test methods, `[Category]` for organization, and `Assert.That()` for assertions. Use `Assert.Multiple()` for grouping related assertions.

### 2. Blazor DI Pattern

**Update Source 4 to show both patterns:**

```csharp
// RECOMMENDED for new code (C# 11+)
[Inject] public required NavigationManager Navigation { get; set; }

// ACCEPTABLE for existing code
[Inject] private IUserService UserService { get; set; } = default!;
protected override async Task OnInitializedAsync() => ArgumentNullException.ThrowIfNull(UserService);
```

### 3. Indentation

**Update Source 1 and Source 2 to say:**

> Space indentation (4 spaces) for C# files

### 4. .editorconfig

**Remove `.cs` and `.cshtml` from line 191** to eliminate the conflict.

---

## Questions for User

1. **Testing Framework**: Should we update all skills to reference TUnit instead of xUnit/NUnit/MSTest?
2. **Blazor DI**: Should we standardize on `required` for new code, or keep both patterns documented?
3. **Indentation**: Should we use spaces (4) as the effective .editorconfig setting, or change to tabs?
4. **.editorconfig**: Should I fix the conflicting rules in .editorconfig?
