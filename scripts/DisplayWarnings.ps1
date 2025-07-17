<#
.SYNOPSIS
Displays all warnings from the build process with fancy formatting and emojis.

.DESCRIPTION
Runs `dotnet clean` and `dotnet build --no-restore --verbosity quiet`, captures warnings, and presents them in a human-readable format sorted by frequency,
with IL* warnings in softened color. Uses emojis for enhanced readability.

.PARAMETER None

.EXAMPLE
DisplayWarnings.ps1

.NOTES
Ensure compliance with project guidelines by reviewing `.github/copilot-instructions.md` before executing.
#>

[CmdletBinding()]
param()

function Invoke-Build {
    $buildOutput = &{
        $ErrorActionPreference = 'Stop'
        dotnet clean 2>&1 | Out-Null
        dotnet build --no-restore --verbosity quiet 2>&1
    }
    return $buildOutput
}

function Get-WarningsSummary {
    param(
        [string[]]$BuildOutput
    )

    $warnings = @{}
    $warningDetails = @{}

    foreach ($line in $BuildOutput) {
        # Match various warning patterns: CS, IL, CA, SA, MA, etc.
        if ($line -match 'warning\s+([A-Z]{2}[0-9]+):\s*(.+?)\s*\[') {
            $warningCode = $matches[1]
            $warningMessage = $matches[2].Trim()

            if (-not $warnings.ContainsKey($warningCode)) {
                $warnings[$warningCode] = 0
                $warningDetails[$warningCode] = $warningMessage
            }
            $warnings[$warningCode]++
        }
        # Alternative pattern for warnings without brackets
        elseif ($line -match 'warning\s+([A-Z]{2}[0-9]+):\s*(.+)$') {
            $warningCode = $matches[1]
            $warningMessage = $matches[2].Trim()

            if (-not $warnings.ContainsKey($warningCode)) {
                $warnings[$warningCode] = 0
                $warningDetails[$warningCode] = $warningMessage
            }
            $warnings[$warningCode]++
        }
    }

    return @{
        Warnings = $warnings.GetEnumerator() | Sort-Object -Property Value -Descending
        Details = $warningDetails
    }
}

function Get-WarningCategory {
    param(
        [string]$WarningCode
    )

    switch -Regex ($WarningCode) {
        '^CS' { return '🔧 C# Compiler' }
        '^CA' { return '🔍 Code Analysis' }
        '^SA' { return '📝 StyleCop' }
        '^MA' { return '⚡ Meziantou' }
        '^IL' { return '🔗 IL Linker' }
        '^MSB' { return '🔨 MSBuild' }
        default { return '❓ Other' }
    }
}

function Display-Warnings {
    param(
        [hashtable]$WarningsSummary
    )

    $warnings = $WarningsSummary.Warnings
    $details = $WarningsSummary.Details

    if ($warnings.Count -eq 0) {
        Write-Host "✅ No warnings found! Build is clean." -ForegroundColor Green
        return
    }

    # Separate IL warnings from others
    $ilWarnings = $warnings | Where-Object { $_.Key -like 'IL*' }
    $otherWarnings = $warnings | Where-Object { $_.Key -notlike 'IL*' }

    # Summary statistics
    $totalWarnings = ($warnings | Measure-Object -Property Value -Sum).Sum
    $totalILWarnings = ($ilWarnings | Measure-Object -Property Value -Sum).Sum
    $totalOtherWarnings = $totalWarnings - $totalILWarnings

    Write-Host "🔍 Warnings: $totalWarnings total ($totalOtherWarnings standard, $totalILWarnings IL)" -ForegroundColor Yellow

    # Show top 5 warnings only
    $topWarnings = @($otherWarnings | Select-Object -First 5)

    foreach ($warning in $topWarnings) {
        $emoji = if ($warning.Value -gt 10) { '🔥' } elseif ($warning.Value -gt 5) { '⚠️' } else { '💡' }
        Write-Host "$emoji $($warning.Key): $($warning.Value)" -ForegroundColor White
    }

    if ($totalILWarnings -gt 0) {
        Write-Host "🔗 IL: $totalILWarnings (expected)" -ForegroundColor DarkGray
    }
}

# Main execution
Write-Host "🚀 Starting build process..." -ForegroundColor Green
$buildOutput = Invoke-Build
Write-Host "✅ Build completed. Analyzing warnings..." -ForegroundColor Green
$warningsSummary = Get-WarningsSummary -BuildOutput $buildOutput
Display-Warnings -WarningsSummary $warningsSummary
