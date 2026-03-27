---
name: dotnet
description: .NET project configuration, dependency injection, build settings, and deployment for Blazor WebAssembly and Azure Functions.
invocable: false
---

# .NET Development

## Project Configuration

- **Frontend**: Blazor WebAssembly (.NET 9), feature-based structure
- **Backend**: Azure Functions (.NET 8), isolated worker
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

```csharp
public partial class UserProfile : ComponentBase
{
    [Inject] private IUserService UserService { get; set; } = default!;
    protected override async Task OnInitializedAsync() => ArgumentNullException.ThrowIfNull(UserService);
}
```
