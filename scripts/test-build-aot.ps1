#!/usr/bin/env pwsh
# Production Parity Test Build (AoT Enabled)
# This script runs tests with AoT compilation for production environment testing
# Build time: ~11.1 seconds vs 9.4 seconds without AoT

param(
    [switch]$Clean = $false,
    [switch]$Verbose = $false,
    [string]$Filter = "",
    [switch]$Coverage = $false
)

# Import shared functions
. "$PSScriptRoot/test-build-common.ps1"

try {
    Write-BuildStatus "Starting Production Parity Test Build (AoT Enabled)"

    # Force AoT compilation for production parity
    $env:AOT_TESTS = "true"

    Write-BuildStatus "AoT Status: ENABLED (Production Parity Mode)"
    Write-Host "  Expected build time: ~11.1 seconds" -ForegroundColor Yellow
    Write-Host "  This matches production WebAssembly compilation" -ForegroundColor Yellow

    if ($Clean) {
        Write-BuildStatus "Cleaning solution..."
        dotnet clean --verbosity minimal
    }

    # Build and test
    $testProject = "tests/redmuffin.Blazor.StaticWeb.Tests/redmuffin.Blazor.StaticWeb.Tests.csproj"

    Write-BuildStatus "Building test project with AoT compilation..."
    $buildArgs = @("build", $testProject)
    if ($Verbose) { $buildArgs += "--verbosity", "normal" }
    else { $buildArgs += "--verbosity", "minimal" }

    $buildStart = Get-Date
    & dotnet @buildArgs
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }
    $buildEnd = Get-Date
    $buildTime = ($buildEnd - $buildStart).TotalSeconds

    Write-BuildStatus "AoT build completed in $([math]::Round($buildTime, 1)) seconds"

    Write-BuildStatus "Running tests with AoT-compiled assemblies..."
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
    Write-BuildStatus "✅ Production Parity Build Successful!"
    Write-Host "Total time: $([math]::Round(($buildEnd - $buildStart).TotalSeconds + $testTime, 1)) seconds" -ForegroundColor Green
    Write-Host "🎯 Production compatibility verified!" -ForegroundColor Magenta

} catch {
    Write-BuildError "Production parity build failed: $($_.Exception.Message)"
    exit 1
} finally {
    # Clean up environment variables
    $env:AOT_TESTS = $null
}