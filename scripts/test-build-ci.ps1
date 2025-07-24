#!/usr/bin/env pwsh
# CI/CD Simulation Test Build (AoT Enabled)
# This script simulates CI/CD environment conditions for local testing
# Useful for debugging CI/CD issues locally

param(
    [switch]$Clean = $true,  # Default to clean in CI simulation
    [switch]$Verbose = $false,
    [string]$Filter = "",
    [switch]$Coverage = $true  # Default to coverage in CI simulation
)

# Import shared functions
. "$PSScriptRoot/test-build-common.ps1"

try {
    Write-BuildStatus "Starting CI/CD Simulation Test Build"

    # Simulate CI/CD environment variables
    $env:CI = "true"
    $env:GITHUB_ACTIONS = "true"
    $env:AOT_TESTS = "true"

    Write-BuildStatus "Environment: CI/CD SIMULATION"
    Write-Host "  CI=true, GITHUB_ACTIONS=true, AOT_TESTS=true" -ForegroundColor Yellow
    Write-Host "  This matches GitHub Actions environment" -ForegroundColor Yellow

    if ($Clean) {
        Write-BuildStatus "Cleaning solution (CI/CD always cleans)..."
        dotnet clean --verbosity minimal
    }

    # Build and test with CI/CD settings
    $testProject = "tests/redmuffin.Blazor.StaticWeb.Tests/redmuffin.Blazor.StaticWeb.Tests.csproj"

    Write-BuildStatus "Building test project (CI/CD mode with AoT)..."
    $buildArgs = @("build", $testProject)
    if ($Verbose) { $buildArgs += "--verbosity", "normal" }
    else { $buildArgs += "--verbosity", "minimal" }

    $buildStart = Get-Date
    & dotnet @buildArgs
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }
    $buildEnd = Get-Date
    $buildTime = ($buildEnd - $buildStart).TotalSeconds

    Write-BuildStatus "CI/CD build completed in $([math]::Round($buildTime, 1)) seconds"

    Write-BuildStatus "Running tests (CI/CD mode)..."
    $testArgs = @("test", $testProject, "--no-build")
    if ($Filter) { $testArgs += "--filter", $Filter }
    if ($Verbose) { $testArgs += "--verbosity", "normal" }
    if ($Coverage) {
        $testArgs += "/p:CollectCoverage=true"
        Write-BuildStatus "Code coverage enabled (CI/CD mode)"
    }

    $testStart = Get-Date
    & dotnet @testArgs
    if ($LASTEXITCODE -ne 0) { throw "Tests failed" }
    $testEnd = Get-Date
    $testTime = ($testEnd - $testStart).TotalSeconds

    Write-BuildStatus "Tests completed in $([math]::Round($testTime, 1)) seconds"
    Write-BuildStatus "✅ CI/CD Simulation Successful!"
    Write-Host "Total time: $([math]::Round(($buildEnd - $buildStart).TotalSeconds + $testTime, 1)) seconds" -ForegroundColor Green
    Write-Host "🤖 Ready for CI/CD deployment!" -ForegroundColor Magenta

} catch {
    Write-BuildError "CI/CD simulation failed: $($_.Exception.Message)"
    exit 1
} finally {
    # Clean up environment variables
    $env:CI = $null
    $env:GITHUB_ACTIONS = $null
    $env:AOT_TESTS = $null
}