# Script: Generate-CoverageReport.ps1

<##
.SYNOPSIS
Generates code coverage reports for the redmuffin.Blazor.StaticWeb project.

.DESCRIPTION
This script runs the test suite and generates code coverage reports in HTML, XML,
and JSON formats utilizing Coverlet and ReportGenerator tools.

.NOTES
- Requires Coverlet & ReportGenerator.
- Run from the project's root directory.
- Coverage reports are placed in the 'coverage' directory.

.EXAMPLE
# Run all tests and generate coverage reports
.\scripts\Generate-CoverageReport.ps1

# AUTHOR: Michael Volz
# LAST UPDATED: 2025-07-12
##>

param(
    [switch]$NoBuild
)

# Ensure the required tools are installed
function Ensure-Tools {
    Write-Host "Ensuring tools are installed..." -ForegroundColor Green
    dotnet tool list -g | Select-String 'reportgenerator' -quiet
    if ($?) { Write-Host "ReportGenerator already installed." }
    else { Write-Host "Installing ReportGenerator..."; dotnet tool install --global dotnet-reportgenerator-globaltool }
}

# Execute tests with code coverage
function Run-Tests {
    [CmdletBinding()]
    param(
        [switch]$NoBuild
    )

    Write-Host "Running tests with coverage..." -ForegroundColor Cyan

    # Build parameters for dotnet test
    $testParams = @()
    if ($NoBuild) {
        $testParams += '--no-build'
    }

    # Run Blazor tests
    Write-Host "Running Blazor tests..." -ForegroundColor Blue
    & dotnet test 'tests/redmuffin.Blazor.StaticWeb.Tests' @testParams

    # Run API tests
    Write-Host "Running API tests..." -ForegroundColor Blue
    & dotnet test 'tests/redmuffin.Blazor.StaticWeb.Api.Tests' @testParams
}

# Generate coverage reports
function Generate-Reports {
    Write-Host "Generating HTML and unified coverage reports..." -ForegroundColor Yellow
    reportgenerator "-reports:coverage/*.opencover.xml" -targetdir:"coverage/unified" -reporttypes:"Html" -title:"Unified Coverage Report"
    reportgenerator -reports:"coverage/*.opencover.xml" -targetdir:"coverage" -reporttypes:"Xml"
    reportgenerator -reports:"coverage/*.opencover.xml" -targetdir:"coverage/branded" -reporttypes:"Html" -tag:"v1.0.0" -historydir:"coverage/history"
}

# Main execution
Ensure-Tools
Run-Tests -NoBuild:$NoBuild
Generate-Reports

Write-Host "Coverage report generation complete." -ForegroundColor Green

