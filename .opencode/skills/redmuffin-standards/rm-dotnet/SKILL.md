---
name: rm-dotnet
description: "Shortcut: rm:dotnet. .NET 9 project config, DI, build commands, Azure Functions, and repo best practices. Use when working with .csproj files, dependency injection, build/test commands, Azure Functions, or coverage reports."
---

# .NET Development

## Project Configuration

- **Frontend**: Blazor WebAssembly (.NET 9), feature-based structure
- **Backend**: Azure Functions (.NET 9), isolated worker
- **Build Settings**: `WasmStripILAfterAOT=true`, `InvariantGlobalization=true`, `PublishTrimmed=true`
- **Deployment**: Azure Static Web Apps with CSP and caching in `staticwebapp.config.json`

## Dependencies

- Blazored.LocalStorage
- Markdig
- Microsoft.Azure.Functions.Worker
- TUnit
- Zurb Foundation (CDN)
- FontAwesome (CDN)
- BuildWebCompiler2022 (SCSS)
- Coverlet
- Analyzers (Roslynator, StyleCop, Meziantou, VSThreading)

## Dependency Injection

- **Blazor Components**: Use `[Inject]` with `default!`, validate in `OnInitializedAsync`
- **Services**: Use primary constructor syntax: `public class MyClass(IDependency dependency)`
- **Null checks**: `ArgumentNullException.ThrowIfNull(dependency)`
- **Service lifetimes**: Register with appropriate lifetimes — Singleton, Scoped, Transient
- **Framework**: Use `Microsoft.Extensions.DependencyInjection`

```csharp
public partial class UserProfile : ComponentBase
{
    [Inject] private IUserService UserService { get; set; } = default!;
    protected override async Task OnInitializedAsync() => ArgumentNullException.ThrowIfNull(UserService);
}
```

## Build Commands

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

## Test Commands

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

## Dev Modes

| Mode       | Port | Use Case             | Command                                               |
| ---------- | ---- | -------------------- | ----------------------------------------------------- |
| Normal     | 5233 | UI, mock data (99%)  | `dotnet run --project src/redmuffin.Blazor.StaticWeb` |
| Full Stack | 4280 | Real API, OAuth, E2E | `pwsh Start.ps1 -Auto`                                |

## Coverage

```powershell
# Generate coverage report
pwsh scripts/Generate-CoverageReport.ps1

# View coverage report
pwsh scripts/View-CoverageReport.ps1
```

## Development Build Scripts

- `scripts/test-build-fast.ps1` - Fast dev build (~9s, AoT disabled)
- `scripts/test-build-aot.ps1` - Production parity testing
- `scripts/DisplayWarnings.ps1` - Show all build warnings

## Zero Warnings Policy

After any C# file change, run build and fix all warnings except:

- IL2111 (Blazor WebAssembly `App_razor.g.cs` trimming)

```bash
# Check for warnings
dotnet build --verbosity quiet
```

## Azure Functions (Isolated Worker)

- Use `Program.cs` with `AddFunctions worker` configuration
- Bindings use input/output attributes
- Use `FunctionContext` for logging and dependency injection

## Best Practices

### Architecture & Patterns

- Use primary constructor syntax for DI: `public class MyClass(IDependency dependency)`
- Prefix interfaces with 'I' (e.g., `IUserService`)
- Use Command Handler pattern with generic base classes
- Follow namespace structure: `{Core|Console|App|Service}.{Feature}`

### Async/Await

- Return `Task<T>` for value, `Task` for void
- Use `ConfigureAwait(false)` where appropriate
- NEVER use `.Wait()`, `.Result`, or `.GetAwaiter().GetResult()`
- Use `Task.WhenAll()` for parallel execution

### Resource Management

- Use `ResourceManager` for localized messages
- Separate `LogMessages` and `ErrorMessages` `.resx` files
- Implement proper disposal patterns

### Code Quality

- Ensure SOLID principles
- Comprehensive XML documentation for public APIs
- C# 12+ features
- Meaningful names reflecting domain concepts
- See `rm-csharp-standards` skill for detailed coding standards
