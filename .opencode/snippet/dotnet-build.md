---
aliases: [build]
description: .NET build commands for this project
---
Run these dotnet commands as needed:

**Build:**
- `dotnet build` - Build entire solution
- `dotnet build --no-restore` - Fast build (after restore)

**Test:**
- `dotnet test` - Run all tests
- `dotnet test --filter "FullyQualifiedName~TestClassName"` - Run specific test class

**AOT Testing:**
- Use `CI=true` to run tests with AOT (disabled locally for speed)

**Coverage:**
- `.\scripts\Generate-CoverageReport.ps1` - Generate coverage report
- `.\scripts\View-CoverageReport.ps1` - View unified report