---
title: C# Standards Consolidation
date: 2026-04-06
tags: [csharp, standards, consolidation, dotnet9]
module: redmuffin-standards
problem_type: documentation
---

# C# Standards Consolidation

This document consolidates all C# coding standards from multiple sources with clear separation. The goal is to create a single source of truth for C# standards in this repository.

**Status**: Consolidation complete. Contradictions resolved. Ready for final optimization.

**Resolved Contradictions** (see `csharp-standards-contradictions-2026-04-06.md` for details):

- Testing Framework: TUnit with built-in fluent assertions (NOT FluentAssertions package)
- Blazor DI: `required` modifier (C# 11+), `default!` pattern is invalid for new code
- Indentation: 4 spaces for C# files
- .editorconfig: Conflicting rules documented

---

## Source 1: rm-csharp-standards Skill

**File**: `.opencode/skills/redmuffin-standards/rm-csharp-standards/SKILL.md`

### Analyzer Rules (Zero Tolerance)

#### StyleCop

- SA1402: One type per file
- SA1208/1210: Order usings (System first, alphabetical)
- SA1201-1214: Enforce member order
- SA1413: Trailing commas in multi-line initializers
- SA1028: No trailing whitespace
- SA1500: Opening brace on new line
- SA1507: No multiple blank lines
- SA1508: No blank line before closing brace

#### Meziantou

- MA0016: Use `IEnumerable<T>`, `IList<T>` abstractions
- MA0002/0006/0074: Specify `StringComparison.OrdinalIgnoreCase`
- MA0048: File name must match type name
- MA0051: Methods <60 lines
- MA0053: Make class sealed when possible

#### Microsoft

- CA1845: Use `AsSpan()` instead of `Substring()`
- CA1854: Use `TryGetValue` for Dictionary
- CA1869: Cache `JsonSerializerOptions` instances
- CA1848: Use `LoggerMessage` delegates
- CA2016: Forward `CancellationToken` parameters
- CA1805: Remove explicit default initialization
- CA1822: Mark members static when possible

#### Permitted Warning

- IL2111 (Blazor WebAssembly `App_razor.g.cs` trimming) is safe to ignore

#### Documentation Warnings

- Fix SA1623/SA1615 in Visual Studio if documented; skip if undocumented

### Logging

**CRITICAL**: LoggerMessage declarations MUST be in `*.Logging.cs` files, NEVER in the main file.

- Use `LoggerMessage` delegates, NEVER `Logger.LogError()`
- Main file: ONLY contains function calls like `LogEvent(logger, exception)`
- Logging file: ONLY contains delegate declarations

```csharp
// Main file - only calls, NO declarations
LogEvent(Logger, null);

// Logging file - declarations only
private static readonly Action<ILogger, Exception?> LogEvent = LoggerMessage.Define(...);
```

### Partial Class Organization

#### Verification Checklist (check BEFORE implementing)

- [ ] Does the main file contain LoggerMessage declarations? → Move them to `*.Logging.cs`
- [ ] Does `*.Logging.cs` exist? → Create it if not
- [ ] Are function calls in `*.Logging.cs`? → Move them to main file

#### Blazor Components

Split into `ComponentName.razor.cs` (logic, lifecycle, properties, events) and `ComponentName.Logging.cs` (`LoggerMessage` declarations)

```csharp
// ComponentName.razor.cs
public partial class Home : ComponentBase
{
    [Inject] public required ILogger<Home> Logger { get; set; }
    private async Task HandleClickAsync()
    {
        LogButtonClicked(Logger, null);
    }
}

// ComponentName.Logging.cs
public partial class Home
{
    private static readonly Action<ILogger, Exception?> LogButtonClicked = LoggerMessage.Define(LogLevel.Information, new EventId(5, "ButtonClicked"), "Button clicked");
}
```

#### Services (includes Azure Functions)

Split into `ServiceName.cs` (logic, methods, properties, function calls) and `ServiceName.Logging.cs` (`LoggerMessage` declarations only)

#### Tests

Split into `TestClassName.cs` (`[Test]` methods) and `TestClassName.Helpers.cs` (TestScope, mocks, utilities in same partial class). NEVER create separate helper files.

#### File Naming (MUST follow)

- Components: `Home.razor.cs` (calls), `Home.Logging.cs` (declarations)
- Services: `UserService.cs` (calls), `UserService.Logging.cs` (declarations)
- Azure Functions: `FunctionName.cs` (calls), `FunctionName.Logging.cs` (declarations)
- Tests: `HomeTests.cs`, `HomeTests.Helpers.cs`
- Test files: Use `Component_Tests.cs` for new test files (e.g., `Home_Tests.cs`)

### Naming Conventions

- Types/Namespaces: PascalCase
- Methods/Properties: PascalCase
- Private fields: camelCase
- Static readonly fields: UpperCamelCase_underscore
- Interfaces: Prefix with "I" (e.g., IUserService)

### Formatting

- Space indentation (4 spaces)
- Max line length: 160 characters
- Opening brace on new line
- Prefer file-scoped namespace declarations and single-line using directives
- Use pattern matching and switch expressions wherever possible
- Use `nameof` instead of string literals

### Nullable Reference Types

- Declare variables non-nullable, and check for `null` at entry points
- Always use `is null` or `is not null` instead of `== null` or `!= null`

### LightMock.Generator with Optional Parameters

**CRITICAL**: Always specify ALL parameters explicitly for interfaces with optional parameters:

```csharp
// FAILS: _mock.Arrange(f => f.GetAsync("key"))
// WORKS: _mock.Arrange(f => f.GetAsync("key", CancellationToken.None))
```

Use `CancellationToken.None`, `null`, `The<T>.IsAnyValue` for optional params in both Arrange() and Assert() calls.

### C# 12/13 Features

- Primary constructors
- Collection expressions (`[1, 2, 3]`)
- `ref readonly` parameters
- Use `nameof` instead of string literals

### Async Programming

#### Naming

- Use `Async` suffix for all async methods
- Match sync counterparts: `GetDataAsync()` for `GetData()`

#### Return Types

- Return `Task<T>` when returning a value
- Return `Task` when no value
- Consider `ValueTask<T>` for high-performance scenarios to reduce allocations
- Avoid `void` except for event handlers

#### Exception Handling

- Use try/catch around await expressions
- Use `ConfigureAwait(false)` to prevent deadlocks in library code
- NEVER swallow exceptions silently

#### Performance

- Use `Task.WhenAll()` for parallel execution
- Use `Task.WhenAny()` for timeouts/first-completed
- Consider cancellation tokens for long-running operations

#### Common Pitfalls (NEVER DO)

- Never use `.Wait()`, `.Result`, or `.GetAwaiter().GetResult()`
- Avoid mixing blocking and async code
- Don't create async void methods (except event handlers)
- Always await Task-returning methods

#### Patterns

- Async command pattern for long-running operations
- `IAsyncEnumerable<T>` for async streams
- Task-based asynchronous pattern (TAP) for public APIs

### Design Patterns

#### Required Patterns

- **Command Pattern**: Generic base classes, `ICommandHandler<TOptions>` interface, `CommandHandlerOptions` inheritance
- **Factory Pattern**: Complex object creation, service provider integration
- **Dependency Injection**: Primary constructors, `ArgumentNullException` null checks, interface abstractions
- **Repository Pattern**: Async data access, provider abstractions
- **Provider Pattern**: External service abstractions, clear contracts, configuration handling

#### Review Checklist

- Design Patterns: Command Handler, Factory, Provider, Repository correctly implemented?
- Architecture: Namespace conventions? Proper separation of concerns?
- .NET Best Practices: Primary constructors, async/await, ResourceManager, structured logging?
- GoF Patterns: Command, Factory, Template Method, Strategy patterns?
- SOLID Principles: Any violations?
- Performance: Async/await, resource disposal, `ConfigureAwait(false)`?
- Testability: Mockable components, async testability, AAA pattern?
- Security: Input validation, secure credential handling, parameterized queries?
- Documentation: XML docs for public APIs?

#### Key Focus Areas

- Command Handlers: Validation in base class, consistent error handling
- Factories: Dependency configuration, service provider integration
- Providers: Connection management, async patterns, exception handling
- Configuration: Data annotations, validation attributes

---

## Source 2: rm-output-style Skill

**File**: `.opencode/skills/redmuffin-standards/rm-output-style/SKILL.md`

### Formatting

| File Type       | Indentation   | Notes             |
| --------------- | ------------- | ----------------- |
| C#              | 4 spaces      | -                 |
| .razor, .cshtml | 4 spaces      | -                 |
| .csproj         | 2 spaces      | -                 |
| All             | Max 160 chars | Brace on new line |

### Naming Conventions

| Type               | Convention       | Example                   |
| ------------------ | ---------------- | ------------------------- |
| Types/Namespaces   | PascalCase       | `HomePage`, `UserService` |
| Methods/Properties | PascalCase       | `GetUser()`               |
| Private fields     | camelCase        | `_userService`            |
| Static readonly    | UpperCamelCase\_ | `LogEvent`                |
| Interfaces         | Prefix "I"       | `IUserService`            |
| Test doubles       | `[Class]_[Type]` | `NavigationManager_Mock`  |

### C# 12/13 Features

- Primary constructors
- Collection expressions: `[1, 2, 3]`
- `ref readonly` parameters
- Pattern matching in switch expressions
- `nameof` not string literals

### File-Scoped Namespaces

```csharp
namespace MyNamespace;
// Single-line using directives
// System.* first, then alphabetical
```

**Priority:** Tables > bullets > single-line > prose. ALL info preserved, verbosity removed.

---

## Source 3: rm-strict-coding-standards Skill

**File**: `.opencode/skills/redmuffin-standards/rm-strict-coding-standards/SKILL.md`

### ARCHITECTURE & DESIGN (ALWAYS)

- Prefer composition over inheritance in 100% of cases unless true "is-a" specialization with no viable composition (use interfaces + delegation/Strategy/Decorator).
- Model "has-a" via interfaces; never inherit for reuse. Keep hierarchies flat (<2 levels max).
- Follow Dependency Rule: outer layers depend inward only (Clean Architecture layers: Domain → Application → Infrastructure).
- Single Responsibility Principle (SRP): one reason to change per class/service.
- Interface Segregation: client-specific interfaces (small, focused).
- Open/Closed: extend via composition, never modify core.
- Liskov Substitution: subtypes replaceable without behavior change.
- No god classes, no anemic domain models.

### DEPENDENCY INJECTION (STRICT .NET BUILT-IN)

- Never new dependencies inside methods/constructors (except primitives, DTOs, or pure value objects).
- Always constructor injection for required deps; use IServiceProvider only for optional/runtime factories.
- Register via extension methods in Infrastructure layer (AddMyFeature(this IServiceCollection)).
- Use Microsoft.Extensions.DependencyInjection only (no third-party containers unless explicitly approved).
- Lifetimes (strict):
  - Transient: lightweight, no state.
  - Scoped: per-request (e.g., DbContext, repositories).
  - Singleton: thread-safe, expensive, global state only.
- NEVER inject Scoped into Singleton (captive dependency). Use IServiceScopeFactory in singletons when needed.
- Validate scopes in dev (validateScopes: true).
- Configuration: Options pattern only (IOptions<T>, never raw IConfiguration).
- No Service Locator (GetService<T>() inside business code = anti-pattern).
- Keyed services for multiple impls of same interface.
- All services small, testable, no statics/stateful globals.

### TDD (RED-GREEN-REFACTOR - NON-NEGOTIABLE)

- Write failing test first (Red) → minimal code to pass (Green) → refactor.
- Three laws: (1) No prod code without failing test. (2) Only enough test to fail. (3) Only enough prod to pass.
- Tests first for all business logic, use cases, domain rules.
- Use TUnit with built-in fluent assertions (`Assert.That()`). **Do NOT use the separate FluentAssertions package.**
- Use LightMock.Generator for external dependencies, custom mocks for internal components.
- Unit tests: isolate via interfaces/DI (mocks for deps).
- Integration tests for external boundaries only.
- 80%+ coverage on domain/application; 100% on critical paths.
- Tests independent, fast (<100ms), descriptive names (Should_When_Then).
- Never change passing tests except for requirement change.
- Refactor only after Green; keep tests green at all times.

### TRUNK-BASED DEVELOPMENT (TBD)

- All work commits to trunk (main) multiple times/day.
- Changes small (hours max); no long-lived branches.
- Short-lived PR branches (<1 day) only for review/CI; delete after merge.
- Pre-commit: full local build + all tests pass.
- CI must run on every commit; trunk always green/releasable.
- Hide WIP with feature flags (Config or LaunchDarkly-style) or branch-by-abstraction.
- No feature branches for release artifacts.
- Use TDD + feature flags to keep trunk stable.

### CODE STYLE & QUALITY (ENFORCED)

- C# latest (nullable enabled, records, primary constructors where clean).
- Blazor: component composition > inheritance; inject services; use @inject.
- PowerShell: same DI/composition mindset when applicable.
- No comments explaining code; code must be self-documenting.
- Pure functions where possible; immutable by default.
- Domain events for side effects.
- CQRS when beneficial (MediatR or minimal APIs).
- No direct EF/DbContext in application layer (repositories only if needed; use use-case services).
- Error handling: Result<T> or exceptions with global filters (never silent fails).
- Logging: structured, injected ILogger<T>.
- Performance: async/await everywhere possible; no .Result/.Wait.
- Security: validate inputs, least privilege, no secrets in code.

### FILE/PROJECT STRUCTURE (STANDARD)

```
Solution
├── Domain/ (entities, value objects, interfaces, exceptions)
├── Application/ (use cases, services, DTOs)
├── Infrastructure/ (impls, EF, external clients, DI extensions)
├── Presentation/ (API, Blazor, controllers/components)
├── Tests/ (Unit, Integration)
```

### AGENT WORKFLOW RULES

- For new feature: (1) Write tests first. (2) Implement via TDD. (3) Inject all deps. (4) Compose, never inherit. (5) Feature flag if not complete. (6) Small PR to trunk.
- Refactor existing: preserve tests, apply rules above.
- Review own output: check every rule before finalizing.
- Ask for clarification only on ambiguous requirements; never guess architecture.

**Activation**: This skill applies to new services/classes, feature architecture, structural refactoring, and code reviews. It does NOT apply to trivial bug fixes, config edits, CSS/SCSS changes, documentation, or running commands. If a bug fix requires structural changes, load the skill.

---

## Source 4: rm-dotnet Skill

**File**: `.opencode/skills/redmuffin-standards/rm-dotnet/SKILL.md`

### Project Configuration

- **Frontend**: Blazor WebAssembly (.NET 9), feature-based structure
- **Backend**: Azure Functions (.NET 9), isolated worker
- **Build Settings**: `WasmStripILAfterAOT=true`, `InvariantGlobalization=true`, `PublishTrimmed=true`
- **Deployment**: Azure Static Web Apps with CSP and caching in `staticwebapp.config.json`

### Dependencies

- Blazored.LocalStorage
- Markdig
- Microsoft.Azure.Functions.Worker
- TUnit
- Zurb Foundation (CDN)
- FontAwesome (CDN)
- BuildWebCompiler2022 (SCSS)
- Coverlet
- Analyzers (Roslynator, StyleCop, Meziantou, VSThreading)

### Dependency Injection

- **Blazor Components**: Use `[Inject]` with `required` modifier (C# 11+)
- **Services**: Use primary constructor syntax: `public class MyClass(IDependency dependency)`
- **Null checks**: `ArgumentNullException.ThrowIfNull(dependency)`
- **Service lifetimes**: Register with appropriate lifetimes — Singleton, Scoped, Transient
- **Framework**: Use `Microsoft.Extensions.DependencyInjection`

```csharp
// Blazor components - use required (C# 11+)
public partial class Home : ComponentBase
{
    [Inject] public required NavigationManager Navigation { get; set; }
    [Inject] public required ILogger<Home> Logger { get; set; }
}

// Services - use primary constructor
public class UserService(ILogger<UserService> logger, IUserRepository repository)
{
    public User GetUser(int id) => repository.GetById(id);
}
```

### Build Commands

```bash
# Build entire solution
dotnet build

# Fast build (after restore)
dotnet build --no-restore

# Build with warnings only
dotnet build --verbosity quiet

# Clean build
dotnet clean --verbosity minimal
```

### Test Commands

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~TestClassName"

# Run single test
dotnet test --filter "FullyQualifiedName~TestMethodName"

# List all tests
dotnet test --list-tests

# Run by category (treenode-filter)
dotnet test -- --treenode-filter "/*/*/*/*[Category=Smoke]"                 # Smoke (27, ~0.8s)
dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Home]"          # Home (52)
dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Videos]"        # Videos (10)
dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Articles]"      # Articles (17)
dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Cache]"         # Cache (31)
dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Raindrop]"      # Raindrop (24)
dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:RaindropItems]" # RaindropItems (17)
dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Core]"          # Core (13)
dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:ApiExample]"    # ApiExample (5)
```

**AOT Testing**: CI runs with AOT (`CI=true`/`GITHUB_ACTIONS=true`). Locally AOT is disabled for speed.

### Dev Modes

| Mode       | Port | Use Case             | Command                                               |
| ---------- | ---- | -------------------- | ----------------------------------------------------- |
| Normal     | 5233 | UI, mock data (99%)  | `dotnet run --project src/redmuffin.Blazor.StaticWeb` |
| Full Stack | 4280 | Real API, OAuth, E2E | `pwsh Start.ps1 -Auto`                                |

### Coverage

```powershell
# Generate coverage report
pwsh scripts/Generate-CoverageReport.ps1

# View coverage report
pwsh scripts/View-CoverageReport.ps1
```

### Development Build Scripts

- `scripts/test-build-fast.ps1` - Fast dev build (~9s, AoT disabled)
- `scripts/test-build-aot.ps1` - Production parity testing
- `scripts/DisplayWarnings.ps1` - Show all build warnings

### Zero Warnings Policy

After any C# file change, run build and fix all warnings except:

- IL2111 (Blazor WebAssembly `App_razor.g.cs` trimming)

```bash
# Check for warnings
dotnet build --verbosity quiet
```

### Azure Functions (Isolated Worker)

- Use `Program.cs` with `AddFunctions worker` configuration
- Bindings use input/output attributes
- Use `FunctionContext` for logging and dependency injection

### Best Practices

#### Architecture & Patterns

- Use primary constructor syntax for DI: `public class MyClass(IDependency dependency)`
- Prefix interfaces with 'I' (e.g., `IUserService`)
- Use Command Handler pattern with generic base classes
- Follow namespace structure: `{Core|Console|App|Service}.{Feature}`

#### Async/Await

- Return `Task<T>` for value, `Task` for void
- Use `ConfigureAwait(false)` where appropriate
- NEVER use `.Wait()`, `.Result`, or `.GetAwaiter().GetResult()`
- Use `Task.WhenAll()` for parallel execution

#### Resource Management

- Use `ResourceManager` for localized messages
- Separate `LogMessages` and `ErrorMessages` `.resx` files
- Implement proper disposal patterns

#### Code Quality

- Ensure SOLID principles
- Comprehensive XML documentation for public APIs
- C# 12+ features
- Meaningful names reflecting domain concepts
- See `rm-csharp-standards` skill for detailed coding standards

---

## Source 5: .editorconfig

**File**: `.editorconfig`

### Roslynator Analyzer Configuration

```ini
# Global Roslynator Settings
dotnet_analyzer_diagnostic.category-roslynator.severity = default
roslynator_analyzers.enabled_by_default = true
roslynator_refactorings.enabled = true
roslynator_compiler_diagnostic_fixes.enabled = true

# Specific Roslynator Rules
dotnet_diagnostic.ros0003.severity = suggestion
dotnet_diagnostic.rcs1205.severity = none
```

### Microsoft Analyzer Configuration

```ini
# Design Category Rules
dotnet_analyzer_diagnostic.category-design.severity = warning

# Meziantou.Analyzer Rules
dotnet_diagnostic.ma0038.severity = none
```

### StyleCop.Analyzers Configuration

```ini
# Documentation and XML Rules
dotnet_diagnostic.sa0001.severity = none # XML documentation comments must be valid
dotnet_diagnostic.sa1600.severity = none # Elements must be documented
dotnet_diagnostic.sa1601.severity = none # Partial elements must be documented
dotnet_diagnostic.sa1629.severity = none # Documentation text must end with a period
dotnet_diagnostic.sa1633.severity = none # File must have a header
dotnet_diagnostic.sa1649.severity = none # File name must match first type name

# Formatting and Layout Rules
dotnet_diagnostic.sa1005.severity = none # Single-line comments must not be preceded by blank lines
dotnet_diagnostic.sa1009.severity = none # Closing parenthesis must be on the same line
dotnet_diagnostic.sa1027.severity = none # Tabs must not be used
dotnet_diagnostic.sa1111.severity = none # Closing parenthesis must be on the same line as the last parameter
dotnet_diagnostic.sa1117.severity = none # Parameters must be on separate lines
dotnet_diagnostic.sa1127.severity = none # Generic type constraints must be on separate lines
dotnet_diagnostic.sa1133.severity = none # Do not combine attributes
dotnet_diagnostic.sa1134.severity = none # Attributes must be on separate lines
dotnet_diagnostic.sa1200.severity = none # Using directives must be placed correctly
dotnet_diagnostic.sa1202.severity = none # members must be ordered by access
dotnet_diagnostic.sa1204.severity = none # Static members must appear before instance members
dotnet_diagnostic.sa1402.severity = warning # File may only contain a single type
dotnet_diagnostic.sa1407.severity = none # Arithmetic expressions must declare precedence
dotnet_diagnostic.sa1413.severity = none # Use trailing commas in multi-line initializers
dotnet_diagnostic.sa1500.severity = none # Braces must not be omitted
dotnet_diagnostic.sa1512.severity = none # Single-line comments must not be followed by blank lines
dotnet_diagnostic.sa1515.severity = none # Single-line comments must be preceded by blank lines
dotnet_diagnostic.sa1516.severity = none # Elements must be separated by blank lines
dotnet_diagnostic.sa1519.severity = none # Braces must not be preceded or followed by a blank line

# Naming Rules
dotnet_diagnostic.sa1300.severity = none # Element names must begin with an uppercase letter
dotnet_diagnostic.sa1309.severity = none # Field names must not begin with an underscore
dotnet_diagnostic.sa1310.severity = none # Field names must not contain underscores

# Code Style Rules
dotnet_diagnostic.sa1101.severity = none # Prefix local calls with 'this.'
```

### C# Style and Formatting Preferences

```ini
# Code Style Preferences
csharp_new_line_before_members_in_object_initializers = false
csharp_preferred_modifier_order = public, private, protected, internal, file, new, static, abstract, virtual, sealed, readonly, override, extern, unsafe, volatile, async, required:suggestion
csharp_style_prefer_utf8_string_literals = true:suggestion
csharp_style_var_elsewhere = true:suggestion
csharp_style_var_for_built_in_types = true:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion

# Control Flow Statements
csharp_style_allow_single_line_control_flow_statements = false:warning

# Custom Underscore Conventions
csharp_style_namespace_underscore = true:suggestion
csharp_style_method_name_underscore = true:suggestion
csharp_style_static_variable_underscore = true:suggestion
csharp_style_static_field_underscore = true:suggestion

# .NET Style Preferences
dotnet_style_parentheses_in_arithmetic_binary_operators = never_if_unnecessary:none
dotnet_style_parentheses_in_other_binary_operators = always_for_clarity:none
dotnet_style_parentheses_in_relational_binary_operators = never_if_unnecessary:none
dotnet_style_predefined_type_for_locals_parameters_members = true:suggestion
dotnet_style_predefined_type_for_member_access = true:suggestion
dotnet_style_qualification_for_event = false:suggestion
dotnet_style_qualification_for_field = false:suggestion
dotnet_style_qualification_for_method = false:suggestion
dotnet_style_qualification_for_property = false:suggestion
dotnet_style_require_accessibility_modifiers = for_non_interface_members:suggestion
```

### Naming Rules Configuration

```ini
# Private Static Readonly Fields Rule
dotnet_naming_rule.private_static_readonly_rule.severity = warning
dotnet_naming_rule.private_static_readonly_rule.style = upper_camel_case_underscore_tolerant_style
dotnet_naming_rule.private_static_readonly_rule.symbols = private_static_readonly_symbols

# Types and Namespaces Rule
dotnet_naming_rule.types_and_namespaces_rule.severity = warning
dotnet_naming_rule.types_and_namespaces_rule.style = upper_camel_case_underscore_tolerant_style
dotnet_naming_rule.types_and_namespaces_rule.symbols = types_and_namespaces_symbols

# Naming Styles
dotnet_naming_style.upper_camel_case_underscore_tolerant_style.capitalization = pascal_case
dotnet_naming_style.upper_camel_case_underscore_tolerant_style.word_separator = _

# Naming Symbols
dotnet_naming_symbols.private_static_readonly_symbols.applicable_accessibilities = private
dotnet_naming_symbols.private_static_readonly_symbols.applicable_kinds = field
dotnet_naming_symbols.private_static_readonly_symbols.required_modifiers = readonly,static

dotnet_naming_symbols.types_and_namespaces_symbols.applicable_accessibilities = *
dotnet_naming_symbols.types_and_namespaces_symbols.applicable_kinds = class,delegate,enum,namespace,struct
```

### ReSharper Configuration

```ini
# General ReSharper Settings
resharper_apply_auto_detected_rules = false
resharper_csharp_max_line_length = 160
resharper_keep_existing_attribute_arrangement = true
resharper_show_autodetect_configure_formatting_tip = false

# ReSharper Inspection Severities
resharper_arrange_redundant_parentheses_highlighting = hint
resharper_arrange_this_qualifier_highlighting = hint
resharper_arrange_type_member_modifiers_highlighting = hint
resharper_arrange_type_modifiers_highlighting = hint
resharper_built_in_type_reference_style_for_member_access_highlighting = hint
resharper_built_in_type_reference_style_highlighting = hint
resharper_redundant_base_qualifier_highlighting = warning
resharper_suggest_var_or_type_built_in_types_highlighting = hint
resharper_suggest_var_or_type_elsewhere_highlighting = hint
resharper_suggest_var_or_type_simple_types_highlighting = hint

# ReSharper Underscore Tolerance
resharper_csharp_types_underscore_tolerant = true
resharper_csharp_namespaces_underscore_tolerant = true
resharper_csharp_upper_camel_case_underscore_tolerant = true
```

### File-Specific Indentation Rules

```ini
# C/C++ and Related Languages (Tab Indentation)
[*.{c,c++,cc,cginc,compute,cp,cpp,cppm,cs,cshtml,cu,cuh,cxx,fx,fxh,h,hh,hlsl,hlsli,hlslinc,hpp,htm,html,hxx,inc,inl,ino,ipp,ixx,mpp,mq4,mq5,mqh,razor,tpp,usf,ush}]
indent_style = tab
indent_size = tab
tab_width = 4

# Web and UI Files (4-Space Indentation)
[*.{asax,ascx,aspx,axaml,cs,cshtml,htm,html,master,paml,razor,skin,vb,xaml,xamlx,xoml}]
indent_style = space
indent_size = 4
tab_width = 4

# Project and Configuration Files (2-Space Indentation)
[*.{appxmanifest,axml,build,config,csproj,dbml,discomap,dtd,jsproj,lsproj,njsproj,nuspec,proj,props,resw,resx,StyleCop,targets,tasks,uxml,vbproj,xml,xsd}]
indent_style = space
indent_size = 2
tab_width = 2
```

**Note**: The original `.editorconfig` has `.cs` and `.cshtml` in both rules, creating a conflict. The effective setting is space indentation (4 spaces) for C# files.

---

## Source 6: docs/TestingGuidelines.md

**File**: `docs/TestingGuidelines.md`

### Test Double Standards

#### Naming Convention

All test doubles must follow the pattern: `[ClassName]_[Type]` where `[Type]` is one of:

- `Mock` - For behavior verification
- `Stub` - For state verification with predefined responses
- `Spy` - For recording interactions while maintaining real functionality
- `Fake` - For simple working implementations
- `Dummy` - For placeholders that satisfy parameter requirements

#### Strategic Approach

##### LightMock.Generator (External Dependencies)

Use for 3rd party and external dependencies:

- `IHttpClientFactory`
- `ILocalStorageService`
- `ILogger<T>`
- External APIs
- Azure services

##### Custom Mocks (Internal Components)

Use for internal components and services:

- `NavigationManager`
- Internal services
- Blazor components
- Project-specific abstractions

#### Examples

```csharp
// ✅ CORRECT: Custom mock for internal component
public sealed class NavigationManager_Mock : NavigationManager
{
    public string? NavigatedTo { get; private set; }
    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
        NavigatedTo = uri;
    }
}

// ✅ CORRECT: LightMock for external dependency
var httpClientMock = new Mock<IHttpClientFactory>();
httpClientMock.Arrange(f => f.CreateClient(The<string>.IsAnyValue))
    .Returns(new HttpClient());

// ✅ CORRECT: Test double with proper suffix
public sealed class DelayProvider_Stub : IDelayProvider
{
    public Task DelayAsync(int milliseconds) => Task.CompletedTask;
}
```

#### Disposable Pattern for Test Doubles

All test doubles that own disposable resources (e.g., `MemoryStream`, `HttpClient`, `IDisposable` fields) **must implement `IDisposable`**:

```csharp
// ✅ CORRECT: Mock with disposable resources implements IDisposable
public sealed class HttpRequestData_Mock : HttpRequestData, IDisposable
{
    private readonly MemoryStream _bodyStream = new();

    public override Stream Body => _bodyStream;

    public void Dispose()
    {
        _bodyStream.Dispose();
    }
}

// ❌ INCORRECT: Missing IDisposable causes CA2001/CA1001 warnings
public sealed class HttpRequestData_Mock : HttpRequestData
{
    private readonly MemoryStream _bodyStream = new(); // Warning: disposable field
}
```

**Why this matters:**

- Prevents CA2001 ("Call System.IDisposable.Dispose on object") warnings
- Prevents CA1001 ("Type owns disposable field(s) but is not disposable") warnings
- Ensures zero-warning builds as required by project policy
- Tests using `using var request = ...` pattern dispose correctly

**Pattern for test usage:**

```csharp
// ✅ CORRECT: Using statement ensures disposal
using var request = TestScope.CreateHttpRequestData(functionContext, requestBody);
```

### Organizational Standards

#### Partial Class Structure

- **Main test file**: `[TestClass].cs` - Contains only `[Test]` methods
- **Helper file**: `[TestClass].Helpers.cs` - Contains TestScope, mocks, and utilities
- **CRITICAL**: All test helpers must be in corresponding partial files, never separate helper files

#### TestScope Architecture

All test classes must use TestScope pattern:

```csharp
public sealed class TestScope(string baseUri = "http://localhost:5000/") : IDisposable
{
    public BunitContext BUnitContext { get; } = new();
    public NavigationManager_Mock NavigationManager { get; } = new(baseUri);
    public Logger_Spy<T> Logger { get; } = new();

    public TestScope WithStandardServices() { /* setup */ return this; }
    public void Dispose() => BUnitContext?.Dispose();
}
```

### Framework Standards

#### TUnit Usage

- Use `[Test]` attribute for test methods
- Use `[Category]` attribute for test organization
- Use `[Arguments]` for data-driven tests
- Use TUnit's built-in fluent assertions: `Assert.That(actual).IsNotNull()`, `Assert.That(value).IsEqualTo(expected)`
- Use `Assert.Multiple()` for grouping related assertions
- **Note**: TUnit has built-in fluent assertions - do NOT use the separate FluentAssertions package
- Prefer custom mocks over LightMock.Generator for internal components
- Use LightMock.Generator for external dependencies only

### Code Quality

- **Zero build warnings policy** (except IL2111)
- `ConfigureAwait(false)` on all awaits (except at end of assert statements)
- Follow StyleCop/Meziantou analyzer rules
- Use C# 13 patterns and modern syntax

### Compliance Checklist

Before committing any test changes:

- [ ] Test double naming follows `[Class]_[Type]` convention
- [ ] Strategic approach used (LightMock vs Custom)
- [ ] TestScope architecture implemented
- [ ] Partial class organization followed
- [ ] Zero build warnings achieved
- [ ] All helpers placed in corresponding partial files
- [ ] TUnit standards followed
- [ ] ConfigureAwait(false) properly applied
- [ ] Disposable test doubles implement `IDisposable` (CA2001/CA1001 compliance)

---

## Source 7: .github/guides/blazor.md

**File**: `.github/guides/blazor.md`

### Blazor Code Style and Structure

- Write idiomatic and efficient Blazor and C# code.
- Follow .NET and Blazor conventions.
- Use Razor Components appropriately for component-based UI development.
- Prefer inline functions for smaller components but separate complex logic into code-behind or service classes.
- Async/await should be used where applicable to ensure non-blocking UI operations.

### Naming Conventions

- Follow PascalCase for component names, method names, and public members.
- Use camelCase for private fields and local variables.
- Prefix interface names with "I" (e.g., IUserService).

### Blazor and .NET Specific Guidelines

- Utilize Blazor's built-in features for component lifecycle (e.g., OnInitializedAsync, OnParametersSetAsync).
- Use data binding effectively with @bind.
- Leverage Dependency Injection for services in Blazor.
- Structure Blazor components and services following Separation of Concerns.
- Always use the latest version C#, currently C# 13 features like record types, pattern matching, and global usings.

### Error Handling and Validation

- Implement proper error handling for Blazor pages and API calls.
- Use logging for error tracking in the backend and consider capturing UI-level errors in Blazor with tools like ErrorBoundary.
- Implement validation using FluentValidation or DataAnnotations in forms.

### Blazor API and Performance Optimization

- Utilize Blazor server-side or WebAssembly optimally based on the project requirements.
- Use asynchronous methods (async/await) for API calls or UI actions that could block the main thread.
- Optimize Razor components by reducing unnecessary renders and using StateHasChanged() efficiently.
- Minimize the component render tree by avoiding re-renders unless necessary, using ShouldRender() where appropriate.
- Use EventCallbacks for handling user interactions efficiently, passing only minimal data when triggering events.

### Caching Strategies

- Implement in-memory caching for frequently used data, especially for Blazor Server apps. Use IMemoryCache for lightweight caching solutions.
- For Blazor WebAssembly, utilize localStorage or sessionStorage to cache application state between user sessions.
- Consider Distributed Cache strategies (like Redis or SQL Server Cache) for larger applications that need shared state across multiple users or clients.
- Cache API calls by storing responses to avoid redundant calls when data is unlikely to change, thus improving the user experience.

### State Management Libraries

- Use Blazor's built-in Cascading Parameters and EventCallbacks for basic state sharing across components.
- Implement advanced state management solutions using libraries like Fluxor or BlazorState when the application grows in complexity.
- For client-side state persistence in Blazor WebAssembly, consider using Blazored.LocalStorage or Blazored.SessionStorage to maintain state between page reloads.
- For server-side Blazor, use Scoped Services and the StateContainer pattern to manage state within user sessions while minimizing re-renders.

### API Design and Integration

- Use HttpClient or other appropriate services to communicate with external APIs or your own backend.
- Implement error handling for API calls using try-catch and provide proper user feedback in the UI.

### Testing and Debugging in Visual Studio

- All unit testing and integration testing should be done in Visual Studio Enterprise.
- Test Blazor components and services using TUnit.
- Use custom mocks for internal components, LightMock.Generator for external dependencies.
- Debug Blazor UI issues using browser developer tools and Visual Studio's debugging tools for backend and server-side issues.
- For performance profiling and optimization, rely on Visual Studio's diagnostics tools.

### Security and Authentication

- Implement Authentication and Authorization in the Blazor app where necessary using ASP.NET Identity or JWT tokens for API authentication.
- Use HTTPS for all web communication and ensure proper CORS policies are implemented.

### API Documentation and Swagger

- Use Swagger/OpenAPI for API documentation for your backend API services.
- Ensure XML documentation for models and API methods for enhancing Swagger documentation.

---

## Source 8: .github/guides/azure-functions.md

**File**: `.github/guides/azure-functions.md`

### Azure Functions Programming Best Practices

- **Dependency Injection**: Use `Startup.cs` to register services (`ILogger`, `IHttpClientFactory`) in C# Azure Functions for testability and maintainability. Example: `builder.Services.AddSingleton<IMyService, MyService>();`.
- **Cold Start Optimization**: Minimize assembly size by reducing dependencies in C# projects. Use .NET Isolated Worker for better control over startup logic and avoid heavy initialization in function code.
- **Error Handling**: Implement retry policies with Polly for transient failures. Use try-catch blocks to handle exceptions gracefully and return meaningful HTTP status codes (e.g., `400` for bad requests) for HTTP triggers.
- **Input Validation**: Validate HTTP trigger inputs using C# model validation (e.g., `System.ComponentModel.DataAnnotations`) or custom checks to ensure security and prevent errors.
- **Structured Logging**: Use `ILogger` for structured logging in C#, capturing only essential data (e.g., request IDs, errors) to avoid performance overhead. Example: `logger.LogInformation("Processing {RequestId}", requestId);`.
- **Asynchronous Programming**: Use `async`/`await` in C# functions for I/O-bound operations (e.g., HTTP calls, database queries) to improve scalability. Avoid blocking calls like `.Result` or `.Wait()`.
- **Function Granularity**: Write single-responsibility functions. Split complex logic into smaller, focused functions to improve maintainability and reusability. Example: Separate data retrieval and processing into distinct functions.
- **Configuration Management**: Access settings via environment variables using `Environment.GetEnvironmentVariable` in C#. Avoid hardcoding values to ensure flexibility across environments.
- **Unit Testing**: Write unit tests for function logic using frameworks like xUnit or MSTest. Mock dependencies (e.g., `ILogger`, `IHttpClientFactory`) with Moq to isolate function behavior.
- **Idempotency**: Ensure functions are idempotent, especially for event-driven triggers (e.g., Queue, Event Hub). Handle duplicate messages gracefully using unique identifiers or state checks.
- **Parameter Optimization**: Use strongly-typed bindings (e.g., `QueueTrigger`, `BlobInput`) in C# to reduce parsing logic and improve type safety. Avoid overusing dynamic `JObject` inputs.
- **Resource Cleanup**: Dispose of resources (e.g., database connections, HTTP clients) properly using `IDisposable` or `using` statements to prevent memory leaks in long-running functions.
- **Code Reusability**: Extract shared logic into class libraries or static methods in C#. Use NuGet packages for cross-function utilities to maintain DRY principles.
- **Performance Monitoring**: Instrument code with custom metrics via Application Insights SDK in C# (e.g., `TelemetryClient.TrackMetric`) to track function-specific performance indicators.
- **Versioning**: For HTTP-triggered functions, implement API versioning (e.g., via query parameters or headers) to support backward compatibility as function logic evolves.
- **Secure Coding**: Sanitize inputs and outputs to prevent injection attacks (e.g., SQL, XSS). Use libraries like `AntiXssEncoder` for output encoding in HTTP responses.

---

## Source 9: AGENTS.md

**File**: `AGENTS.md`

### Build Commands

- ALWAYS `dotnet build --verbosity quiet` after C# changes
- ALWAYS `dotnet build -c Debug-Sass` after SCSS/JS changes
- ALWAYS `dotnet test` before commit

### Zero Warnings Policy

- **Pragma warnings are deliberate choices** — `#pragma warning disable` directives suppress warnings we've consciously decided to keep. Never remove or modify pragma directives without explicit user approval. Goal: zero errors, zero warnings before commit. Pragmas enable this by documenting intentional deviations from analyzer rules.

### Package Management

- Use `scripts/Update-PackageVersions.ps1` for NuGet package updates according to our Central Package Management (CPM) setup, then finish with `dotnet clean && dotnet build --verbosity quiet && dotnet test` as the final verification step.
- ALWAYS use `pwsh -NoProfile` for all PowerShell commands to optimize performance. The profile is only useful for manual work.
- Keep every package version value centralized in the top property section of `Directory.Packages.props`; item groups should reference properties instead of hard-coded version literals.

### Stack

| Technology      | Version     | Purpose        |
| --------------- | ----------- | -------------- |
| .NET            | 9.0         | Core framework |
| Blazor          | WebAssembly | Frontend       |
| Azure Functions | .NET 9      | Backend        |
| TUnit           | Latest      | Testing        |
| SCSS/Sass       | -           | Styling        |

---

## Source 10: .NET 9 / C# 13 Latest Features (2024-2025)

**Reference**: Microsoft Learn, .NET Blog announcements

### C# 13 New Features

#### params Collections

The `params` modifier now supports collection expressions, not just arrays:

```csharp
// Before C# 13: Only arrays
void Concat(params string[] items) { }

// C# 13+: Any collection type
void Concat<T>(params List<T> items) { }
void Concat<T>(params IEnumerable<T> items) { }
void Concat<T>(params ReadOnlySpan<T> items) { }
```

#### New Lock Type and Semantics

C# 13 introduces `System.Threading.Lock` for better thread safety:

```csharp
Lock myLock = new();

void Process()
{
    lock (myLock)
    {
        // Thread-safe operation
    }
}
```

#### New Escape Sequence - `\e`

The escape sequence `\e` represents the ESC character (U+001B):

```csharp
Console.WriteLine("\e[1mBold text\e[0m"); // ANSI escape codes
```

#### Method Group Natural Type Improvements

Method group conversions have improved type inference:

```csharp
// Better inference for method groups
var result = items.Select(Console.WriteLine); // Improved type inference
```

#### Implicit Indexer Access in Object Initializers

Indexers can now be used in object initializers:

```csharp
var matrix = new Matrix
{
    [0, 0] = 1,
    [1, 1] = 1
};
```

#### ref struct Interface Support

`ref struct` types can now implement interfaces:

```csharp
public ref struct MyRefStruct : IDisposable
{
    public void Dispose() { }
}
```

#### ref struct Generic Type Parameters

`ref struct` types can now be used as generic type arguments:

```csharp
void Process<T>(T value) where T : allows ref struct
{
    // Can now accept ref struct types
}
```

#### Partial Properties and Indexers

Properties and indexers can now be partial:

```csharp
public partial class MyClass
{
    public partial string Name { get; set; }
    public partial int this[int index] { get; set; }
}
```

#### Overload Resolution Priority

Library authors can designate one overload as better than others:

```csharp
[OverloadResolutionPriority(1)]
public void Process(ReadOnlySpan<byte> data) { }

public void Process(byte[] data) { }
```

#### field Backed Properties (Preview)

New `field` keyword for auto-implemented property backing fields:

```csharp
public string Name
{
    get => field;
    set => field = value ?? throw new ArgumentNullException(nameof(value));
}
```

### .NET 9 Runtime Improvements

#### Dynamic Adaptation for Server GC

Server GC now adapts to application memory requirements instead of machine resources:

- Better memory management for cloud apps
- Reduced memory footprint in high-core environments
- Can configure legacy Server GC if needed

#### Performance Improvements

- **LINQ optimizations**: `Take`, `DefaultIfEmpty` up to 10x faster for empty collections
- **System.Text.Json**: >50% improvements for various operations
- **Exception handling**: 50% faster (adopted Native AOT model)
- **Dynamic PGO**: 70% faster execution for optimized code patterns

#### New LINQ Methods

```csharp
// CountBy - aggregate counts by key
var counts = items.CountBy(x => x.Category);

// AggregateBy - aggregate state by key without intermediate allocations
var aggregated = items.AggregateBy(
    x => x.Category,
    seed: 0,
    (acc, item) => acc + item.Value
);
```

#### TimeSpan From Methods

New `From*` methods that accept `int` instead of `double`:

```csharp
var timeout = TimeSpan.FromSeconds(30);  // int overload
var delay = TimeSpan.FromMilliseconds(100);  // int overload
```

### .NET 9 Library Improvements

#### System.Text.Json Enhancements

- Nullable reference type annotations
- JSON schema export from types
- Customizable indentation
- Multiple root-level JSON values from single stream
- `JsonMarshal.GetRawUtf8Value()` for UTF8 bytes without allocation

#### PriorityQueue Updates

New `Remove` method to update priority:

```csharp
var queue = new PriorityQueue<string, int>();
queue.Enqueue("item1", 1);
queue.Remove("item1", out var element, out var priority);
queue.Enqueue("item1", 5); // Updated priority
```

#### Cryptography Additions

- One-shot hash methods on `CryptographicOperations`
- KMAC algorithm support

#### PersistedAssemblyBuilder

New type to save emitted assemblies:

```csharp
var assemblyBuilder = new PersistedAssemblyBuilder(...);
// Can now save the assembly to disk
assemblyBuilder.Save("MyAssembly.dll");
```

### ASP.NET Core 9 Improvements

#### Static File Optimization

- Automatic fingerprinted versioning at build time
- Pre-compression with Brotli at publish time
- Content-based hash for aggressive caching

#### Blazor Enhancements

- `RendererInfo.IsInteractive` for runtime render mode detection
- Improved reconnection experience for Blazor Server
- New Hybrid and Web app templates

#### OpenAPI Built-in Support

```csharp
// Native AOT-friendly OpenAPI document generation
builder.Services.AddOpenApi();
```

#### Security Improvements

- Easier HTTPS development certificate setup on Linux
- Built-in authentication state flow to client in Blazor
- OAuth/OIDC extensibility for additional parameters
- Pushed Authorization Requests (PAR) support

### Best Practices for .NET 9

#### Use Collection Expressions

```csharp
// Prefer collection expressions
List<int> numbers = [1, 2, 3, 4, 5];
int[] array = [1, 2, 3];
Span<int> span = [1, 2, 3];
```

#### Leverage Primary Constructors

```csharp
// Use primary constructors for DI
public class UserService(ILogger<UserService> logger, IUserRepository repository)
{
    public User GetUser(int id) => repository.GetById(id);
}
```

#### Use Span-Based APIs

```csharp
// Prefer AsSpan() over Substring()
ReadOnlySpan<char> span = text.AsSpan(start, length);

// Use span-based LINQ operations
var count = text.AsSpan().Count(c => char.IsDigit(c));
```

#### Cache JsonSerializerOptions

```csharp
// CA1869: Cache JsonSerializerOptions instances
private static readonly JsonSerializerOptions s_options = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};
```

#### Use LoggerMessage Delegates

```csharp
// CA1848: Use LoggerMessage delegates for performance
private static readonly Action<ILogger, string, Exception?> LogProcessing
    = LoggerMessage.Define<string>(LogLevel.Information, new EventId(1), "Processing {Item}");
```

---

## Source 11: Codebase Analysis (Undocumented Patterns)

**Method**: Grep analysis of `src/` and `tests/` directories

### File-Scoped Namespaces (116 occurrences)

All C# files use file-scoped namespaces (C# 10+ feature):

```csharp
// CORRECT: File-scoped namespace
namespace redmuffin.Blazor.StaticWeb.Features.RaindropItems.Models;

// AVOID: Block-scoped namespace
namespace redmuffin.Blazor.StaticWeb.Features.RaindropItems.Models
{
    // ...
}
```

### LoggerMessage Pattern (126 occurrences)

All logging uses `LoggerMessage.Define` with separate `.Logging.cs` files:

```csharp
// Main file: Service.cs - ONLY calls, NO declarations
public async Task ProcessAsync()
{
    LogProcessing(Logger, "item123");
}

// Logging file: Service.Logging.cs - ONLY declarations
public sealed partial class Service
{
    private static readonly Action<ILogger, string, Exception?> LogProcessing
        = LoggerMessage.Define<string>(LogLevel.Information, new EventId(1, "Processing"), "Processing {Item}");
}
```

**Key Rules:**

- Main file contains ONLY function calls like `LogEvent(logger, exception)`
- Logging file contains ONLY delegate declarations
- NEVER use `Logger.LogError()` - always use `LoggerMessage.Define`
- File naming: `ComponentName.Logging.cs` for Blazor, `ServiceName.Logging.cs` for services

### Expression-Bodied Members

Used for computed properties and simple methods:

```csharp
// Computed properties
public bool IsSuccess => Status == RaindropCacheStatus.Hit;
public bool IsExpired => Status == RaindropCacheStatus.Expired;
public double CompressionRatio => OriginalSize > 0 ? (double)CompressedSize / OriginalSize * 100 : 0;

// Simple methods
public override string ToString() => $"Result: {Status}";
```

### Record Types (14 occurrences)

Used for immutable data transfer objects:

```csharp
// Immutable record
public record RaindropItem(
    string Id,
    string Title,
    string? Excerpt
);

// Readonly record struct for value types
public readonly record struct PerformanceMetrics(
    long TotalItems,
    double AverageAccessCount
);
```

### Init-Only Properties

Used for result objects and configuration:

```csharp
public sealed class RaindropCacheResult<T>
{
    public RaindropCacheStatus Status { get; init; }
    public T? Data { get; init; }
    public RaindropCacheMetadata? Metadata { get; init; }
    public string? ErrorMessage { get; init; }
}
```

### Required Properties with DI

Used in Blazor components for dependency injection (C# 11+):

```csharp
public partial class Home : ComponentBase
{
    [Inject] public required NavigationManager Navigation { get; set; }
    [Inject] public required IHttpClientFactory HttpClientFactory { get; set; }
    [Inject] public required IDelayProvider DelayProvider { get; set; }
}
```

**Note**: The `required` modifier provides compile-time null safety, eliminating the need for `default!` and runtime validation.

### Sealed Partial Classes in Tests (64 occurrences)

All test classes use `sealed partial class` pattern:

```csharp
// Main test file: HomeTests.cs
[Category("Feature:Home")]
[Category("Unit")]
public sealed partial class HomeTests
{
    [Test]
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
}

// Helper file: HomeTests.Helpers.cs
public sealed partial class HomeTests
{
    public TestScope CreateTestScope() => new();
}

// Infrastructure file: HomeTests.Infrastructure.cs
public sealed partial class HomeTests
{
    // Setup, teardown, shared utilities
}
```

### ConfigureAwait(false) (257 occurrences)

Used consistently throughout async code:

```csharp
// All async calls use ConfigureAwait(false)
var response = await httpClient.GetAsync(apiUrl, token).ConfigureAwait(false);
var json = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
await InvokeAsync(StateHasChanged).ConfigureAwait(false);
```

**Rule:** Apply `ConfigureAwait(false)` to ALL async calls except at the end of assert statements in tests.

### Null Pattern Matching (19 occurrences)

Use `is null` and `is not null` instead of `== null` and `!= null`:

```csharp
// CORRECT
if (request is null) return await CreateBadRequestResponseAsync(req, "Missing code.", token).ConfigureAwait(false);
if (redirectUri is not null) ProcessRedirect(redirectUri);

// AVOID
if (request == null) return ...;
if (redirectUri != null) ProcessRedirect(redirectUri);
```

### TUnit Test Patterns

```csharp
// Test method attributes
[Test]
[Category("Feature:Home")]
[Category("Unit")]
public async Task TestName()

// Multiple assertions - TUnit's built-in fluent API
using (Assert.Multiple())
{
    await Assert.That(actual).IsNotNull();
    await Assert.That(actual.Value).IsEqualTo(expected);
}

// Fluent assertions - TUnit built-in (NOT FluentAssertions package)
await Assert.That(component.Find("h1")).IsNotNull();
await Assert.That(result.IsSuccess).IsTrue();
await Assert.That(items.Count).IsGreaterThan(0);

// Note: TUnit has built-in fluent assertions - do NOT use the separate FluentAssertions package
```

### Using Declarations

Used for resource management:

```csharp
// CORRECT: Using declaration
using var scope = CreateTestScope();
var component = scope.BUnitContext.Render<HomePage>();

// AVOID: Using statement (older pattern)
using (var scope = CreateTestScope())
{
    var component = scope.BUnitContext.Render<HomePage>();
}
```

### Fire-and-Forget Pattern

For background operations that shouldn't block:

```csharp
// Fire-and-forget with ConfigureAwait(false)
_ = Task.Run(async () => await RefreshDataInBackgroundAsync().ConfigureAwait(false));
```

### XML Documentation

All public APIs have XML documentation:

```csharp
/// <summary>
///     Represents the result of a raindrop cache operation with success/failure states and optional data.
/// </summary>
/// <typeparam name="T">The type of raindrop data being cached.</typeparam>
public sealed class RaindropCacheResult<T>
{
    /// <summary>
    ///     Gets the status of the cache operation.
    /// </summary>
    public RaindropCacheStatus Status { get; init; }
}
```

---

## Next Steps

1. **User Review**: Review this consolidation document for accuracy and completeness
2. **Identify Duplicates**: Mark sections that appear in multiple sources
3. **Decide What to Delete**: User determines which duplicate sections to remove
4. **Create Final Version**: Rewrite into a single optimized C# standards document
5. **Create New Skill**: Create `rm-c-sharp-standards` skill with the consolidated content
