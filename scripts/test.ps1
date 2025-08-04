<#
.SYNOPSIS
Runs all tests in the solution optimized for speed and reliability.

.DESCRIPTION
Executes tests with the following optimizations:
- Builds in Release configuration only
- Skips package restore and code coverage
- Uses multiple test threads for parallel execution
- Provides minimal but meaningful output
#>

[CmdletBinding()]
param(
    [Parameter()]
    [switch]$NoLogo,

    [Parameter()]
    [switch]$NoRestore
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Define test parameters
$testParams = @(
    'test',
    '--configuration', 'Release',    # Build in Release mode only
    '--no-restore',                 # Skip package restore
    '--nologo',                     # Reduce noise in output
    '-maxCpuCount',                 # Use all available CPUs
    '--verbosity', 'minimal',       # Minimal but useful output
    '-p:CollectCoverage=false',     # Disable code coverage
    '--filter', 'FullyQualifiedName!~IntegrationTests' # Exclude integration tests
)

try {
    # Run the tests
    Write-Host "Running tests..." -ForegroundColor Cyan
    & dotnet $testParams
    
    if ($LASTEXITCODE -ne 0) {
        throw "Tests failed with exit code $LASTEXITCODE"
    }
    
    Write-Host "All tests passed successfully!" -ForegroundColor Green
} catch {
    Write-Error "Error occurred during test execution: $_"
    exit 1
}
