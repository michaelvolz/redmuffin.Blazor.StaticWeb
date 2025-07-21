#!/usr/bin/env pwsh
# Fast Development Test Build (AoT Disabled)
# This script runs tests without AoT compilation for faster local development cycles
# Build time: ~9.4 seconds vs 11.1 seconds with AoT

param(
    [switch]$Clean = $false,
    [switch]$Verbose = $false,
    [string]$Filter = "",
    [switch]$Coverage = $false
)

# Import shared functions
. "$PSScriptRoot/test-build-common.ps1"

try {
    Write-BuildStatus "Starting Fast Development Test Build (AoT Disabled)"

    # Ensure AoT is disabled for development speed
    $env:AOT_TESTS = "false"
    $env:CI = $null
    $env:GITHUB_ACTIONS = $null

    Write-BuildStatus "AoT Status: DISABLED (Development Mode - Faster Builds)"
    Write-Host "  Expected build time: ~9.4 seconds" -ForegroundColor Yellow
    Write-Host "  Use test-build-aot.ps1 for production parity testing" -ForegroundColor Yellow

    if ($Clean) {
        Write-BuildStatus "Cleaning solution..."
        dotnet clean --verbosity minimal
    }

    # Build and test
    $testProject = "tests/redmuffin.Blazor.StaticWeb.Tests/redmuffin.Blazor.StaticWeb.Tests.csproj"

    Write-BuildStatus "Building test project..."
    $buildArgs = @("build", $testProject)
    if ($Verbose) { $buildArgs += "--verbosity", "normal" }
    else { $buildArgs += "--verbosity", "minimal" }

    $buildStart = Get-Date
    & dotnet @buildArgs
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }
    $buildEnd = Get-Date
    $buildTime = ($buildEnd - $buildStart).TotalSeconds

    Write-BuildStatus "Build completed in $([math]::Round($buildTime, 1)) seconds"

    Write-BuildStatus "Running tests..."
    $testArgs = @("test", $testProject, "--no-build")
    if ($Filter) { $testArgs += "--filter", $Filter }
    if ($Verbose) { $testArgs += "--verbosity", "normal" }
    if ($Coverage) { $testArgs += "/p:CollectCoverage=true" }

    $testStart = Get-Date
    & dotnet @testArgs
    if ($LASTEXITCODE -ne 0) { throw "Tests failed" }
    $testEnd = Get-Date
    $testTime = ($testEnd - $testStart).TotalSeconds

    Write-BuildStatus "Tests completed in $([math]::Round($testTime, 1)) seconds"
    Write-BuildStatus "✅ Fast Development Build Successful!"
    Write-Host "Total time: $([math]::Round(($buildEnd - $buildStart).TotalSeconds + $testTime, 1)) seconds" -ForegroundColor Green

} catch {
    Write-BuildError "Fast development build failed: $($_.Exception.Message)"
    exit 1
} finally {
    # Clean up environment variables
    $env:AOT_TESTS = $null
}