#!/usr/bin/env pwsh
<#
.SYNOPSIS
    List-Sidenotes.ps1 — Fast listing of pending sidenotes.

.DESCRIPTION
    Replaces the slow agent-tool-based sidenotes list flow (glob + read each file)
    with a single-pass PowerShell script. Reads frontmatter from all SN-*.md files,
    filters to pending status, and outputs a numbered list.

    Completes in milliseconds regardless of sidenote count.

.PARAMETER SidenotesPath
    Path to the sidenotes directory. Defaults to docs/sidenotes relative to the
    repository root (parent of the scripts directory).

.EXAMPLE
    pwsh scripts/List-Sidenotes.ps1

.EXAMPLE
    pwsh scripts/List-Sidenotes.ps1 -SidenotesPath "C:\other-repo\docs\sidenotes"
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$SidenotesPath
)

# Let genuine errors surface; use -ErrorAction SilentlyContinue only where
# individual file read failures are expected and acceptable.
$ErrorActionPreference = 'Continue'

# Resolve sidenotes path: default to docs/sidenotes relative to repo root
if (-not $SidenotesPath) {
    $RepoRoot = (Get-Item $PSScriptRoot).Parent.FullName
    $SidenotesPath = Join-Path $RepoRoot 'docs\sidenotes'
}

if (-not (Test-Path $SidenotesPath)) {
    Write-Host "Error: Sidenotes directory not found: $SidenotesPath" -ForegroundColor Red
    exit 1
}

# Find all sidenote files in one call
$files = @(Get-ChildItem -Path $SidenotesPath -Filter 'SN-*.md' -File -ErrorAction SilentlyContinue)

if ($files.Count -eq 0) {
    Write-Host "No pending sidenotes."
    exit 0
}

# Parse each file: extract id, date, status, title
$pending = @()
$longTitles = @()

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw -ErrorAction SilentlyContinue
    if (-not $content) {
        continue
    }

    # Extract frontmatter block (between --- markers)
    $fmMatch = [regex]::Match($content, '^---\s*\r?\n(.*?)\r?\n---(?:\r?\n|$)', [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if (-not $fmMatch.Success) {
        # No frontmatter — malformed
        $id = $file.BaseName
        Write-Host "[malformed sidenote] $id" -ForegroundColor Yellow
        continue
    }

    $frontmatter = $fmMatch.Groups[1].Value

    # Extract status
    $statusMatch = [regex]::Match($frontmatter, '^status:\s*(.+)$', [System.Text.RegularExpressions.RegexOptions]::Multiline)
    if (-not $statusMatch.Success) {
        $id = $file.BaseName
        Write-Host "[malformed sidenote] $id" -ForegroundColor Yellow
        continue
    }
    $status = $statusMatch.Groups[1].Value.Trim().Trim('"').Trim("'")

    # Only include pending sidenotes
    if ($status -ne 'pending') {
        continue
    }

    # Extract id
    $idMatch = [regex]::Match($frontmatter, '^id:\s*(.+)$', [System.Text.RegularExpressions.RegexOptions]::Multiline)
    $id = if ($idMatch.Success) { $idMatch.Groups[1].Value.Trim() } else { $file.BaseName }

    # Extract date
    $dateMatch = [regex]::Match($frontmatter, '^date:\s*(.+)$', [System.Text.RegularExpressions.RegexOptions]::Multiline)
    $date = if ($dateMatch.Success) { $dateMatch.Groups[1].Value.Trim() } else { 'unknown' }

    # Extract title
    $titleMatch = [regex]::Match($frontmatter, '^title:\s*(.+)$', [System.Text.RegularExpressions.RegexOptions]::Multiline)
    $title = if ($titleMatch.Success) {
        $titleMatch.Groups[1].Value.Trim().Trim('"').Trim("'")
    } else {
        # Fallback: use first 110 chars of body (matches the capture hard cap)
        $bodyStart = $fmMatch.Index + $fmMatch.Length
        $body = $content.Substring($bodyStart).Trim()
        if ($body.Length -gt 110) { $body.Substring(0, 110) + '...' } else { $body }
    }

    # Track long titles for soft warning
    if ($title.Length -gt 110) {
        $longTitles += [PSCustomObject]@{
            Id    = $id
            Length = $title.Length
        }
    }

    $pending += [PSCustomObject]@{
        Id    = $id
        Date  = $date
        Title = $title
    }
}

if ($pending.Count -eq 0) {
    Write-Host "No pending sidenotes."
    exit 0
}

# Sort by ID (natural sort on SN-NNNN)
$pending = $pending | Sort-Object { [int]($_.Id -replace '^SN-', '') }

# Output numbered list
Write-Host "Pending sidenotes:"
for ($i = 0; $i -lt $pending.Count; $i++) {
    $item = $pending[$i]
    Write-Host "$($i + 1). $($item.Id) ($($item.Date)) - $($item.Title)"
}

# Soft warnings for long titles (target ~100 chars, max 110)
if ($longTitles.Count -gt 0) {
    Write-Host ""
    foreach ($lt in $longTitles) {
        Write-Host "⚠ $($lt.Id) title is $($lt.Length) chars (target ~100, max 110)" -ForegroundColor Yellow
    }
}

exit 0
