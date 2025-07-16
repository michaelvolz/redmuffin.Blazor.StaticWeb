# Trimming Warnings Documentation

## Current Status

This Blazor WebAssembly project currently has **2 expected IL2111 warnings** that are safe to ignore.

## Known Warnings

### IL2111 - LayoutView.Layout.set Warnings (2 instances)

**Location**: `App_razor.g.cs` (auto-generated file)
**Method**: `Microsoft.AspNetCore.Components.LayoutView.Layout.set`
**Reason**: The Blazor framework uses reflection to access the Layout property of LayoutView components

**Why it's safe to ignore**:
- This is auto-generated code from the Blazor compiler
- The LayoutView component is part of the official ASP.NET Core framework
- The reflection access is expected behavior for the framework's routing system
- The types being accessed are preserved by the framework's trimming configuration

**Source**: 
```razor
<!-- From App.razor -->
<LayoutView Layout="@typeof(MainLayout)">
    <p role="alert">Sorry, there's nothing at this address.</p>
</LayoutView>
```

## Suppression Strategy

**Decision**: We do NOT suppress these warnings because:

1. **Targeted suppression is not possible** - The warnings come from auto-generated code that cannot be specifically targeted without suppressing ALL IL2111 warnings
2. **Future warning visibility** - We want to see any new IL2111 warnings that might indicate real problems in user code
3. **Transparency** - The warnings are visible as a reminder of the framework limitation

## Monitoring

### Expected Warnings (Safe to Ignore)
- ✅ 2x IL2111 warnings for `LayoutView.Layout.set` in `App_razor.g.cs`

### Warnings to Investigate
- ❌ Any IL2111 warnings NOT related to `LayoutView.Layout.set`
- ❌ Any IL2111 warnings from user code (non-auto-generated files)
- ❌ Any new IL2026, IL2070, IL2072, IL2075, IL2077 warnings

## Future Improvements

Microsoft may provide better targeting mechanisms for suppressing auto-generated code warnings in future versions. When available, we can:

1. Update to use more targeted suppression
2. Remove this documentation
3. Ensure clean builds without losing important warning visibility

## References

- [ASP.NET Core Blazor WebAssembly trimming](https://docs.microsoft.com/aspnet/core/blazor/host-and-deploy/webassembly#trimming)
- [.NET IL Linker warnings](https://docs.microsoft.com/dotnet/core/deploying/trimming-warnings)
- [Issue tracking for LayoutView trimming warnings](https://github.com/dotnet/aspnetcore/issues)
