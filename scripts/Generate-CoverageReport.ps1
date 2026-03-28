# Script: Generate-CoverageReport.ps1

<##
.SYNOPSIS
Generates code coverage reports using TUnit's native coverage.

.DESCRIPTION
This script runs the test suite and generates code coverage reports using TUnit's
built-in coverage support (Microsoft.Testing.Extensions.CodeCoverage - included
automatically with TUnit).

.NOTES
- Uses TUnit native coverage via dotnet run --coverage
- Output format: Cobertura XML
- No extra packages needed (included in TUnit)

.EXAMPLE
# Run all tests and generate coverage reports
.\scripts\Generate-CoverageReport.ps1

# Run without building first
.\scripts\Generate-CoverageReport.ps1 -NoBuild

# View coverage summary in terminal
.\scripts\Generate-CoverageReport.ps1 -View

# AUTHOR: Michael Volz
# LAST UPDATED: 2026-03-28
##>

param(
    [switch]$NoBuild,
    [switch]$View
)

$ErrorActionPreference = 'Continue'

function Get-CoverageSummary {
    param([string]$XmlPath)
    
    if (-not (Test-Path $XmlPath)) {
        Write-Host "Coverage file not found: $XmlPath" -ForegroundColor Red
        return
    }

    [xml]$xml = Get-Content $XmlPath
    $lineRate = [math]::Round([double]$xml.coverage.'line-rate' * 100, 1)
    $branchRate = [math]::Round([double]$xml.coverage.'branch-rate' * 100, 1)
    
    $lineColor = if ($lineRate -ge 70) { 'Green' } elseif ($lineRate -ge 50) { 'Yellow' } else { 'Red' }
    $branchColor = if ($branchRate -ge 70) { 'Green' } elseif ($branchRate -ge 50) { 'Yellow' } else { 'Red' }
    
    Write-Host ""
    Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "        CODE COVERAGE SUMMARY" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "  Line Coverage:    $lineRate%" -ForegroundColor $lineColor
    Write-Host "  Branch Coverage:  $branchRate%" -ForegroundColor $branchColor
    Write-Host "═══════════════════════════════════════════`n" -ForegroundColor Cyan
}

function Invoke-TUnitCoverage {
    param(
        [string]$ProjectPath,
        [string]$OutputPrefix
    )

    Write-Host "Running tests with coverage: $ProjectPath" -ForegroundColor Blue

    $testArgs = @(
        'run',
        '--project', $ProjectPath,
        '--configuration', 'Release',
        '--no-restore',
        '--coverage',
        '--coverage-output-format', 'cobertura',
        '--coverage-output', "./coverage/$OutputPrefix-cobertura.xml"
    )

    if ($NoBuild) {
        $testArgs += '--no-build'
    }

    Write-Host "Command: dotnet $($testArgs -join ' ')" -ForegroundColor Gray
    
    & dotnet @testArgs 2>&1 | ForEach-Object {
        if ($_ -match "error|failed|succeeded|duration") {
            Write-Host "  $_" -ForegroundColor Yellow
        }
    }

    $outputPath = "coverage/$OutputPrefix-cobertura.xml"
    if ((Test-Path $outputPath) -and (Get-Item $outputPath).Length -gt 0) {
        Write-Host "Coverage generated: $outputPath" -ForegroundColor Green
        return $true
    }

    Write-Host "  Coverage generation failed for $ProjectPath" -ForegroundColor Red
    return $false
}

if ($View) {
    $blazorXml = "coverage/blazor-cobertura.xml"
    $apiXml = "coverage/api-cobertura.xml"
    
    if (Test-Path $blazorXml) {
        Get-CoverageSummary -XmlPath $blazorXml
    }
    elseif (Test-Path $apiXml) {
        Get-CoverageSummary -XmlPath $apiXml
    }
    else {
        Write-Host "No coverage reports found. Run .\scripts\Generate-CoverageReport.ps1 first." -ForegroundColor Red
    }
    exit
}

if (-not $NoBuild) {
    Write-Host "Building solution..." -ForegroundColor Cyan
    dotnet build --configuration Release --no-restore -v q
}

Write-Host ""
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "      Running tests with TUnit Coverage" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════`n" -ForegroundColor Cyan

$blazorSuccess = Invoke-TUnitCoverage -ProjectPath 'tests/redmuffin.Blazor.StaticWeb.Tests' -OutputPrefix 'blazor'
$apiSuccess = Invoke-TUnitCoverage -ProjectPath 'tests/redmuffin.Blazor.StaticWeb.Api.Tests' -OutputPrefix 'api'

Write-Host ""
Write-Host "═══════════════════════════════════════════" -ForegroundColor Green
Write-Host "      Coverage report generation complete!" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════" -ForegroundColor Green

if ($blazorSuccess) {
    Get-CoverageSummary -XmlPath "coverage/blazor-cobertura.xml"
}

Write-Host "View again: .\scripts\Generate-CoverageReport.ps1 -View`n" -ForegroundColor Yellow