---
title: TUnit and Microsoft.Testing.Platform Version Incompatibility
date: 2026-04-07
category: test-failures
module: testing
problem_type: test_failure
component: testing_framework
severity: high
tags:
  - tuni
  - nuget
  - version-incompatibility
  - ci-cd
  - deployment-failure
symptoms:
  - MissingMethodException during test execution
  - Method 'PlatformResources.get_UnexpectedExceptionDuringByteConversionErrorMessage' not found
  - GitHub Actions deployment pipeline blocked at test stage
root_cause: config_error
resolution_type: dependency_update
---

# TUnit and Microsoft.Testing.Platform Version Incompatibility

## Problem

Deployment pipeline failed due to unintended upgrade of Microsoft.Testing.Platform from 2.1.0 to 2.2.1. TUnit 1.28.7 is incompatible with Microsoft.Testing.Platform 2.2.1, causing a MissingMethodException during test execution that blocked all deployments.

## Symptoms

- Tests fail with `MissingMethodException: PlatformResources.get_UnexpectedExceptionDuringByteConversionErrorMessage`
- GitHub Actions workflow blocked at test stage
- All subsequent deployments blocked until resolution
- Error occurs in both test projects: `redmuffin.Blazor.StaticWeb.Api.Tests` and `redmuffin.Blazor.StaticWeb.Tests`

## What Didn't Work

- Initial assumption that TUnit upgrade resolved the issue (it didn't - the incompatibility was with Microsoft.Testing.Platform version)
- Attempting to upgrade TUnit as a fix (wrong dependency identified initially)

## Solution

**Directory.Packages.props** (reverted version):

```xml
- <MicrosoftTestingPlatformVersion>2.2.1</MicrosoftTestingPlatformVersion>
+ <MicrosoftTestingPlatformVersion>2.1.0</MicrosoftTestingPlatformVersion>
```

**scripts/Update-PackageVersions.ps1** (added prevention constraint):

```powershell
function Get-IncompatiblePackageConstraints {
    return @{
        'Microsoft.ApplicationInsights.WorkerService' = @{
            MaxVersion = '2.23.0'
            Reason = 'Version 3.x+ is incompatible with Microsoft.Azure.Functions.Worker.ApplicationInsights 2.x (ITelemetryInitializer breaking change)'
        }
        'Microsoft.Testing.Platform' = @{
            MaxVersion = '2.1.0'
            Reason = 'Version 2.2+ is incompatible with TUnit 1.28.7 (MissingMethodException: PlatformResources.get_UnexpectedExceptionDuringByteConversionErrorMessage)'
        }
    }
}
```

**Verification commands**:

```bash
dotnet restore
dotnet build --verbosity quiet  # 0 warnings, 0 errors
dotnet test --verbosity quiet    # all tests pass
```

## Why This Works

TUnit 1.28.7 depends on Microsoft.Testing.Platform APIs that changed between versions 2.1.0 and 2.2.1. Specifically, `PlatformResources.get_UnexpectedExceptionDuringByteConversionErrorMessage` was removed or relocated in 2.2.1, causing a runtime MissingMethodException. Reverting to the compatible version 2.1.0 restores the expected API surface.

The root cause was a configuration gap: the automated package update script (`Update-PackageVersions.ps1`) lacked constraints to prevent incompatible package upgrades. The fix adds a constraint entry to `Get-IncompatiblePackageConstraints` that blocks Microsoft.Testing.Platform upgrades past 2.1.0 with a documented reason.

## Prevention

1. **Constraint in Update-PackageVersions.ps1**: Added `Get-IncompatiblePackageConstraints` entry blocking Microsoft.Testing.Platform upgrades past 2.1.0 with documented reason
2. **Documentation of incompatibility**: The constraint includes the specific error message for future reference
3. **Pattern established**: Similar constraint already existed for Microsoft.ApplicationInsights.WorkerService, establishing a pattern for handling incompatible package upgrades
4. **Centralized version management**: Using `Directory.Packages.props` for centralized package versioning makes version changes visible and reviewable (see `docs/solutions/best-practices/csharp-standards-final-2026-04-06.md`)

## Related Issues

- No related GitHub issues found
- Related documentation: `docs/solutions/best-practices/csharp-standards-final-2026-04-06.md` (TUnit standards and centralized package versioning)
- Related file: `Directory.Packages.props` (centralized version management)
