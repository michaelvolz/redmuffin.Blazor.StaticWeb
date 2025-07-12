# Script: View-CoverageReport.ps1

<##
.SYNOPSIS
Opens code coverage reports in the default web browser.

.DESCRIPTION
This script provides a convenient way to view code coverage reports by opening
the HTML reports in the default web browser. Supports multiple report types.

.PARAMETER ReportType
The type of coverage report to view.
Options: 'Unified', 'Branded', 'Html'
Default: 'Unified'

.NOTES
- Requires coverage reports to have been generated first.
- Opens reports in the default web browser.

.EXAMPLE
# View the unified coverage report
.\scripts\View-CoverageReport.ps1

.EXAMPLE
# View the branded coverage report
.\scripts\View-CoverageReport.ps1 -ReportType Branded

.EXAMPLE
# View the basic HTML coverage report
.\scripts\View-CoverageReport.ps1 -ReportType Html

# AUTHOR: Michael Volz
# LAST UPDATED: 2025-07-12
##>

[CmdletBinding()]
param(
	[Parameter(Mandatory = $false)]
	[ValidateSet('Unified', 'Branded', 'Html')]
	[string]$ReportType = 'Unified'
)

function Open-CoverageReport {
	[CmdletBinding()]
	param(
		[Parameter(Mandatory = $true)]
		[string]$Type
	)

	# Define report paths based on type
	$reportPaths = @{
		'Unified' = 'coverage/unified/index.html'
		'Branded' = 'coverage/branded/index.html'
		'Html'    = 'coverage/html/index.html'
	}

	$reportPath = $reportPaths[$Type]

	if (-not $reportPath) {
		Write-Error "Invalid report type: $Type"
		return
	}

	$fullPath = Join-Path -Path (Get-Location) -ChildPath $reportPath

	if (-not (Test-Path -Path $fullPath)) {
		Write-Warning "Coverage report not found at: $fullPath"
		Write-Host "Please run .\scripts\Generate-CoverageReport.ps1 first to generate reports." -ForegroundColor Yellow
		return
	}

	try {
		Write-Host "Opening $Type coverage report..." -ForegroundColor Green
		Start-Process -FilePath $fullPath -ErrorAction Stop
		Write-Host "Coverage report opened in default browser." -ForegroundColor Green
	}
	catch {
		Write-Error "Failed to open coverage report: $($_.Exception.Message)"
	}
}

# Main execution
try {
	Open-CoverageReport -Type $ReportType
}
catch {
	Write-Error "An error occurred: $($_.Exception.Message)"
	exit 1
}
