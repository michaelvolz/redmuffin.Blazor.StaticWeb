---
title: C# Standards Duplicates Analysis
date: 2026-04-06
tags: [csharp, standards, consolidation, analysis]
module: redmuffin-standards
problem_type: documentation
---

# C# Standards Duplicates Analysis

This document identifies sections that appear in multiple sources in the consolidation document. **Nothing is deleted yet** - this is a thorough analysis to inform the final consolidation.

---

## Duplicate Analysis Summary

| Topic                     | Sources     | Recommendation                                      |
| ------------------------- | ----------- | --------------------------------------------------- |
| Naming Conventions        | 1, 2, 6     | Consolidate into single section                     |
| C# 12/13 Features         | 1, 2, 10    | Consolidate into single section                     |
| Async/Await Patterns      | 1, 4, 8, 11 | Consolidate into single section                     |
| Nullable Reference Types  | 1, 2        | Consolidate into single section                     |
| File-Scoped Namespaces    | 1, 2, 11    | Consolidate into single section                     |
| ConfigureAwait(false)     | 1, 4, 6, 11 | Consolidate into single section                     |
| LoggerMessage Pattern     | 1, 11       | Keep Source 1 as primary, Source 11 as verification |
| Dependency Injection      | 3, 4, 7     | Consolidate into single section                     |
| Testing Standards         | 6, 11       | Keep Source 6 as primary, Source 11 as verification |
| Zero Warnings Policy      | 1, 4, 6, 9  | Consolidate into single section                     |
| Blazor Component Patterns | 1, 7        | Consolidate into single section                     |
| Azure Functions           | 4, 8        | Consolidate into single section                     |

---

## Detailed Duplicate Analysis

### 1. Naming Conventions

**Appears in:**

- **Source 1 (rm-csharp-standards)**: Lines 122-128
- **Source 2 (rm-output-style)**: Lines 246-254
- **Source 6 (TestingGuidelines)**: Lines 716-724

**Content Comparison:**

| Aspect             | Source 1                  | Source 2                  | Source 6         |
| ------------------ | ------------------------- | ------------------------- | ---------------- |
| Types/Namespaces   | PascalCase                | PascalCase                | -                |
| Methods/Properties | PascalCase                | PascalCase                | -                |
| Private fields     | camelCase                 | camelCase                 | -                |
| Static readonly    | UpperCamelCase_underscore | UpperCamelCase_underscore | -                |
| Interfaces         | Prefix "I"                | Prefix "I"                | -                |
| Test doubles       | -                         | `[Class]_[Type]`          | `[Class]_[Type]` |

**Recommendation:** Create single "Naming Conventions" section combining:

- General naming (from Source 1/2)
- Test double naming (from Source 2/6)
- Table format for clarity

---

### 2. C# 12/13 Features

**Appears in:**

- **Source 1 (rm-csharp-standards)**: Lines 155-160
- **Source 2 (rm-output-style)**: Lines 256-262
- **Source 10 (.NET 9 / C# 13)**: Lines 1007-1123

**Content Comparison:**

| Feature                      | Source 1 | Source 2 | Source 10        |
| ---------------------------- | -------- | -------- | ---------------- |
| Primary constructors         | ✓        | ✓        | ✓ (detailed)     |
| Collection expressions       | ✓        | ✓        | ✓ (detailed)     |
| `ref readonly` parameters    | ✓        | ✓        | -                |
| Pattern matching             | -        | ✓        | -                |
| `nameof`                     | ✓        | ✓        | -                |
| `params` collections         | -        | -        | ✓ (new in C# 13) |
| `Lock` type                  | -        | -        | ✓ (new in C# 13) |
| `\e` escape                  | -        | -        | ✓ (new in C# 13) |
| `ref struct` interfaces      | -        | -        | ✓ (new in C# 13) |
| Partial properties           | -        | -        | ✓ (new in C# 13) |
| Overload resolution priority | -        | -        | ✓ (new in C# 13) |
| `field` backed properties    | -        | -        | ✓ (preview)      |

**Recommendation:** Create single "C# Language Features" section:

- C# 12 features (primary constructors, collection expressions, ref readonly)
- C# 13 features (params collections, Lock, \e, ref struct interfaces, partial properties)
- Keep Source 10 as the authoritative reference for new features

---

### 3. Async/Await Patterns

**Appears in:**

- **Source 1 (rm-csharp-standards)**: Lines 162-199
- **Source 4 (rm-dotnet)**: Lines 508-513
- **Source 8 (azure-functions.md)**: Lines 956-957
- **Source 11 (Codebase Analysis)**: Lines 1422-1433

**Content Comparison:**

| Aspect                  | Source 1 | Source 4 | Source 8 | Source 11    |
| ----------------------- | -------- | -------- | -------- | ------------ |
| Naming (Async suffix)   | ✓        | -        | -        | -            |
| Return types            | ✓        | ✓        | -        | -            |
| Exception handling      | ✓        | -        | -        | -            |
| Performance             | ✓        | -        | -        | -            |
| Common pitfalls         | ✓        | ✓        | -        | -            |
| Patterns                | ✓        | -        | -        | -            |
| ConfigureAwait(false)   | ✓        | ✓        | -        | ✓ (verified) |
| Never use .Wait/.Result | ✓        | ✓        | ✓        | -            |

**Recommendation:** Create single "Async Programming" section:

- Naming conventions
- Return types
- Exception handling
- Performance patterns
- Common pitfalls (NEVER DO)
- ConfigureAwait(false) usage
- Codebase verification note

---

### 4. Nullable Reference Types

**Appears in:**

- **Source 1 (rm-csharp-standards)**: Lines 139-142
- **Source 2 (rm-output-style)**: Lines 264-268

**Content Comparison:**

| Aspect                     | Source 1 | Source 2 |
| -------------------------- | -------- | -------- |
| Declare non-nullable       | ✓        | ✓        |
| Check null at entry points | ✓        | ✓        |
| `is null` / `is not null`  | ✓        | ✓        |

**Recommendation:** Single section, identical content. Keep Source 1 version.

---

### 5. File-Scoped Namespaces

**Appears in:**

- **Source 1 (rm-csharp-standards)**: Line 135 (mentioned)
- **Source 2 (rm-output-style)**: Lines 270-276
- **Source 11 (Codebase Analysis)**: Lines 1284-1297

**Content Comparison:**

| Aspect             | Source 1 | Source 2 | Source 11       |
| ------------------ | -------- | -------- | --------------- |
| Preference stated  | ✓        | ✓        | ✓               |
| Code example       | -        | ✓        | ✓               |
| Verification count | -        | -        | 116 occurrences |

**Recommendation:** Create single section with:

- Preference statement
- Code example (correct vs avoid)
- Codebase verification note

---

### 6. ConfigureAwait(false)

**Appears in:**

- **Source 1 (rm-csharp-standards)**: Line 179
- **Source 4 (rm-dotnet)**: Line 511
- **Source 6 (TestingGuidelines)**: Line 847
- **Source 11 (Codebase Analysis)**: Lines 1422-1433

**Content Comparison:**

| Aspect              | Source 1 | Source 4 | Source 6 | Source 11       |
| ------------------- | -------- | -------- | -------- | --------------- |
| Use in library code | ✓        | ✓        | -        | -               |
| Prevent deadlocks   | ✓        | -        | -        | -               |
| Except in tests     | -        | -        | ✓        | ✓               |
| Code example        | -        | -        | -        | ✓               |
| Verification count  | -        | -        | -        | 257 occurrences |

**Recommendation:** Create single section:

- General rule: Use ConfigureAwait(false) in library/production code
- Exception: Not needed at end of assert statements in tests
- Code example
- Codebase verification note

---

### 7. LoggerMessage Pattern

**Appears in:**

- **Source 1 (rm-csharp-standards)**: Lines 60-74, 76-104
- **Source 11 (Codebase Analysis)**: Lines 1299-1323

**Content Comparison:**

| Aspect                 | Source 1 | Source 11       |
| ---------------------- | -------- | --------------- |
| CRITICAL warning       | ✓        | ✓               |
| File separation        | ✓        | ✓               |
| Main file content      | ✓        | ✓               |
| Logging file content   | ✓        | ✓               |
| File naming convention | ✓        | ✓               |
| Verification count     | -        | 126 occurrences |

**Recommendation:** Keep Source 1 as primary, add Source 11 verification note.

---

### 8. Dependency Injection

**Appears in:**

- **Source 3 (rm-strict-coding-standards)**: Lines 297-312
- **Source 4 (rm-dotnet)**: Lines 398-412
- **Source 7 (blazor.md)**: Lines 888-889

**Content Comparison:**

| Aspect                 | Source 3 | Source 4 | Source 7 |
| ---------------------- | -------- | -------- | -------- |
| Never new dependencies | ✓        | -        | -        |
| Constructor injection  | ✓        | ✓        | -        |
| Service lifetimes      | ✓        | ✓        | -        |
| Captive dependencies   | ✓        | -        | -        |
| Options pattern        | ✓        | -        | -        |
| Blazor [Inject]        | -        | ✓        | ✓        |
| Null checks            | -        | ✓        | -        |

**Recommendation:** Create single "Dependency Injection" section:

- General DI principles (from Source 3)
- Blazor-specific patterns (from Source 4/7)
- Service lifetimes
- Captive dependency warning
- Options pattern

---

### 9. Testing Standards

**Appears in:**

- **Source 6 (TestingGuidelines)**: Lines 710-863
- **Source 11 (Codebase Analysis)**: Lines 1385-1469

**Content Comparison:**

| Aspect                  | Source 6 | Source 11 |
| ----------------------- | -------- | --------- |
| Test double naming      | ✓        | -         |
| Strategic approach      | ✓        | -         |
| Disposable pattern      | ✓        | -         |
| Partial class structure | ✓        | ✓         |
| TestScope architecture  | ✓        | -         |
| TUnit usage             | ✓        | ✓         |
| Code quality checklist  | ✓        | -         |
| Sealed partial classes  | -        | ✓         |
| Using declarations      | -        | ✓         |
| Fire-and-forget         | -        | ✓         |

**Recommendation:** Keep Source 6 as primary, add Source 11 patterns:

- Sealed partial classes
- Using declarations
- Fire-and-forget pattern

---

### 10. Zero Warnings Policy

**Appears in:**

- **Source 1 (rm-csharp-standards)**: Lines 52-58 (Permitted Warning)
- **Source 4 (rm-dotnet)**: Lines 482-490
- **Source 6 (TestingGuidelines)**: Line 846
- **Source 9 (AGENTS.md)**: Lines 981-983

**Content Comparison:**

| Aspect             | Source 1 | Source 4 | Source 6 | Source 9 |
| ------------------ | -------- | -------- | -------- | -------- |
| IL2111 permitted   | ✓        | ✓        | ✓        | -        |
| Zero warnings goal | -        | ✓        | ✓        | ✓        |
| Pragma warnings    | -        | -        | -        | ✓        |
| Build command      | -        | ✓        | -        | ✓        |

**Recommendation:** Create single "Zero Warnings Policy" section:

- Goal: zero errors, zero warnings
- Permitted warning: IL2111 (Blazor WebAssembly trimming)
- Pragma warnings are deliberate choices
- Build command to check

---

### 11. Blazor Component Patterns

**Appears in:**

- **Source 1 (rm-csharp-standards)**: Lines 84-104
- **Source 7 (blazor.md)**: Lines 871-942

**Content Comparison:**

| Aspect              | Source 1 | Source 7 |
| ------------------- | -------- | -------- |
| Partial class split | ✓        | -        |
| Logging separation  | ✓        | -        |
| File naming         | ✓        | -        |
| Code style          | -        | ✓        |
| Lifecycle           | -        | ✓        |
| Data binding        | -        | ✓        |
| DI                  | -        | ✓        |
| Error handling      | -        | ✓        |
| Performance         | -        | ✓        |
| Caching             | -        | ✓        |
| State management    | -        | ✓        |

**Recommendation:** Create single "Blazor Components" section:

- Partial class organization (from Source 1)
- Logging pattern (from Source 1)
- Code style and structure (from Source 7)
- Lifecycle, binding, DI (from Source 7)
- Performance and caching (from Source 7)

---

### 12. Azure Functions

**Appears in:**

- **Source 4 (rm-dotnet)**: Lines 493-497
- **Source 8 (azure-functions.md)**: Lines 950-967

**Content Comparison:**

| Aspect           | Source 4 | Source 8 |
| ---------------- | -------- | -------- |
| Isolated worker  | ✓        | ✓        |
| Program.cs setup | ✓        | -        |
| Bindings         | ✓        | -        |
| FunctionContext  | ✓        | -        |
| DI               | -        | ✓        |
| Cold start       | -        | ✓        |
| Error handling   | -        | ✓        |
| Input validation | -        | ✓        |
| Logging          | -        | ✓        |
| Async            | -        | ✓        |
| Testing          | -        | ✓        |
| Idempotency      | -        | ✓        |
| Resource cleanup | -        | ✓        |

**Recommendation:** Create single "Azure Functions" section combining both sources.

---

## Consolidation Strategy

### Phase 1: Merge Exact Duplicates

These sections are nearly identical and can be merged:

1. **Naming Conventions** → Single section
2. **Nullable Reference Types** → Single section
3. **File-Scoped Namespaces** → Single section with verification
4. **LoggerMessage Pattern** → Keep Source 1, add verification note

### Phase 2: Consolidate Overlapping Content

These sections have overlapping content that needs careful merging:

1. **C# 12/13 Features** → Merge with Source 10 as authoritative
2. **Async/Await Patterns** → Merge all 4 sources
3. **ConfigureAwait(false)** → Merge all 4 sources
4. **Dependency Injection** → Merge all 3 sources
5. **Testing Standards** → Keep Source 6, add Source 11 patterns
6. **Zero Warnings Policy** → Merge all 4 sources
7. **Blazor Components** → Merge both sources
8. **Azure Functions** → Merge both sources

### Phase 3: Unique Content (No Duplicates)

These sections appear in only one source and should be preserved:

1. **Analyzer Rules** (Source 1) - StyleCop, Meziantou, Microsoft
2. **LightMock.Generator** (Source 1) - Optional parameters
3. **Design Patterns** (Source 1) - Command, Factory, Repository, Provider
4. **Architecture & Design** (Source 3) - SOLID, Clean Architecture
5. **TDD** (Source 3) - Red-Green-Refactor
6. **Trunk-Based Development** (Source 3) - TBD rules
7. **Project Configuration** (Source 4) - Build settings, dependencies
8. **Build/Test Commands** (Source 4) - Specific commands
9. **Dev Modes** (Source 4) - Port configuration
10. **.editorconfig Settings** (Source 5) - All analyzer configs
11. **Test Double Standards** (Source 6) - Naming, strategic approach
12. **Caching Strategies** (Source 7) - Blazor-specific
13. **State Management** (Source 7) - Blazor-specific
14. **.NET 9 Runtime** (Source 10) - GC, performance
15. **.NET 9 Library** (Source 10) - LINQ, JSON, PriorityQueue
16. **ASP.NET Core 9** (Source 10) - Static files, OpenAPI
17. **Expression-Bodied Members** (Source 11) - Codebase pattern
18. **Record Types** (Source 11) - Codebase pattern
19. **Init-Only Properties** (Source 11) - Codebase pattern
20. **Required Properties** (Source 11) - Codebase pattern
21. **XML Documentation** (Source 11) - Codebase pattern

---

## Proposed Final Structure

```
# C# Standards (Final Version)

## 1. Analyzer Rules
   - StyleCop (from Source 1)
   - Meziantou (from Source 1)
   - Microsoft (from Source 1)
   - .editorconfig settings (from Source 5)
   - Permitted warnings

## 2. Naming Conventions
   - General naming (merged from Sources 1, 2, 6)
   - Test double naming

## 3. C# Language Features
   - C# 12 features (merged from Sources 1, 2)
   - C# 13 features (from Source 10)
   - File-scoped namespaces (merged from Sources 1, 2, 11)
   - Nullable reference types (merged from Sources 1, 2)
   - Expression-bodied members (from Source 11)
   - Record types (from Source 11)
   - Init-only properties (from Source 11)
   - Required properties (from Source 11)

## 4. Async Programming
   - Naming conventions
   - Return types
   - Exception handling
   - Performance patterns
   - Common pitfalls
   - ConfigureAwait(false) (merged from Sources 1, 4, 6, 11)

## 5. Logging
   - LoggerMessage pattern (from Source 1, verified by Source 11)
   - Partial class organization
   - File naming conventions

## 6. Dependency Injection
   - General principles (from Source 3)
   - Blazor patterns (from Sources 4, 7)
   - Service lifetimes
   - Captive dependencies
   - Options pattern

## 7. Architecture & Design
   - SOLID principles (from Source 3)
   - Clean Architecture (from Source 3)
   - Design patterns (from Source 1)
   - Trunk-based development (from Source 3)

## 8. Testing Standards
   - Test double naming (from Source 6)
   - Strategic approach (from Source 6)
   - Partial class structure (merged from Sources 6, 11)
   - TestScope architecture (from Source 6)
   - TUnit usage (merged from Sources 6, 11)
   - Sealed partial classes (from Source 11)
   - Using declarations (from Source 11)
   - Fire-and-forget pattern (from Source 11)

## 9. Blazor Components
   - Partial class organization (from Source 1)
   - Logging separation (from Source 1)
   - Code style (from Source 7)
   - Lifecycle (from Source 7)
   - Performance (from Source 7)
   - Caching (from Source 7)
   - State management (from Source 7)

## 10. Azure Functions
   - Isolated worker (merged from Sources 4, 8)
   - DI (from Source 8)
   - Error handling (from Source 8)
   - Testing (from Source 8)
   - Idempotency (from Source 8)

## 11. Project Configuration
   - Build settings (from Source 4)
   - Dependencies (from Source 4)
   - Build commands (from Source 4)
   - Test commands (from Source 4)
   - Dev modes (from Source 4)

## 12. Zero Warnings Policy
   - Goal (merged from Sources 4, 6, 9)
   - Permitted warnings (from Source 1)
   - Pragma warnings (from Source 9)

## 13. Code Quality
   - XML documentation (from Source 11)
   - Code style (from Source 3)
   - Formatting (from Source 2)

## 14. .NET 9 Best Practices
   - Runtime improvements (from Source 10)
   - Library improvements (from Source 10)
   - ASP.NET Core improvements (from Source 10)
   - Best practices (from Source 10)
```

---

## Next Steps

1. ✅ **Backup created** - `csharp-standards-consolidation-2026-04-06.backup.md`
2. ✅ **Duplicates identified** - This document
3. ⏳ **User decision needed** - Review this analysis and approve consolidation strategy
4. ⏳ **Create final version** - After approval
5. ⏳ **Create new skill** - `rm-c-sharp-standards`

---

## Questions for User

Before proceeding with the final consolidation:

1. **Do you agree with the proposed final structure?**
2. **Are there any sections you want to keep separate that I've proposed merging?**
3. **Are there any sections I've marked as unique that should be merged?**
4. **Should the .editorconfig settings (Source 5) be kept as a separate reference section or integrated into the Analyzer Rules section?**
5. **Should the .NET 9 features (Source 10) be a separate section or integrated into the relevant C# Language Features section?**
