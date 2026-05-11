#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Run-QualityGates.ps1 — single-command survey of all quality gates against
    the tools project. Generates coverage, runs CRAP/SCRAP/Architecture/Dupes,
    and saves full output to /tmp/gates-output.txt.

.DESCRIPTION
    For use in the optimized single-pass cleanup workflow. Run before starting
    cleanup to survey violations, and once after finishing to verify fixes.

.EXAMPLE
    ./scripts/Run-QualityGates.ps1

    Output saved to /tmp/gates-output.txt.
#>

$ErrorActionPreference = 'Stop'

$RepoRoot = Join-Path $PSScriptRoot '..' -Resolve
$ToolsDir = Join-Path $RepoRoot 'tools'
$SrcProject = 'src/redmuffin.Tools.QualityGates'
$TestProject = 'tests/redmuffin.Tools.QualityGates.Tests'
$CoverageFile = '/tmp/quality-gates-coverage.xml'
$OutputFile = '/tmp/gates-output.txt'
$ArchConfig = "$SrcProject/arch-rules.yml"

Write-Host '=== Quality Gates Survey ==='
Write-Host 'Step 1/2: Generating coverage...'

Push-Location $ToolsDir
try {
    dotnet run --project $TestProject `
        --coverage `
        --coverage-output-format cobertura `
        --coverage-output $CoverageFile `
        > $null 2>&1

    if (-not (Test-Path $CoverageFile)) {
        Write-Error "Coverage file not generated: $CoverageFile"
        exit 1
    }

    $coverageSize = (Get-Item $CoverageFile).Length
    Write-Host "Coverage generated: $coverageSize bytes"
    Write-Host ''
    Write-Host 'Step 2/2: Running all gates...'

    dotnet run --project $SrcProject -- all `
        --project $SrcProject `
        --test-project $TestProject `
        --coverage-file $CoverageFile `
        --arch-config $ArchConfig `
        --dupes `
        > $OutputFile 2>&1

    Write-Host ''
    Write-Host "=== Results saved to $OutputFile ==="
    Write-Host ''

    Select-String -Path $OutputFile -Pattern '^(===|CRAP:|SCRAP:|ARCH:|MUTATE:|DUPES:|DUPLICATE|Overall:)'

    Write-Host ''
    $crapLines = Select-String -Path $OutputFile -Pattern '^\s+\d+\.\d+\s+\d+\s+\d+\s*\%' | ForEach-Object { $_.Line }
    $violations = 0
    $gaps = 0
    foreach ($line in $crapLines) {
        if ($line -match '^\s+(\d+\.\d+)\s+(\d+)\s+(\d+)\s*\%') {
            if ([double]$Matches[1] -gt 8) {
                if ($line -match 'COVERAGE GAP') { $gaps++ }
                else { $violations++ }
            }
        }
    }
    Write-Host "Real CRAP violations >8: $violations"
    Write-Host "COVERAGE GAPs (algorithmic): $gaps"
    Write-Host 'Survey complete.'
}
finally {
    Pop-Location
}
