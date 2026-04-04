#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Fast dev environment cleanup — closes MCP Brave, orphan dotnet, and verifies.
.DESCRIPTION
    Replaces the slow multi-agent rm:cleanup skill with a single fast script.
    Completes in ~1 second.
#>

$ErrorActionPreference = 'SilentlyContinue'

# --- Single process snapshot ---
$procs = Get-CimInstance Win32_Process -Property ProcessId, Name, ParentProcessId, CommandLine -ErrorAction SilentlyContinue

if (-not $procs) {
    Write-Host "Cleanup: no process data available."
    exit 1
}

$devenvPids = @($procs | Where-Object { $_.Name -eq 'devenv.exe' } | Select-Object -ExpandProperty ProcessId)

# --- Brave cleanup ---
$brave = @($procs | Where-Object { $_.Name -eq 'brave.exe' -and $_.CommandLine -like '*chrome-devtools-mcp*' })
$braveCount = $brave.Count
if ($braveCount -gt 0) {
    $brave | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }
}

# --- Orphan dotnet cleanup ---
$dotnet = @($procs | Where-Object { $_.Name -eq 'dotnet.exe' -and $_.ParentProcessId -notin $devenvPids })
$dotnetCount = $dotnet.Count
if ($dotnetCount -gt 0) {
    $dotnet | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }
}

# --- Nul file cleanup ---
$nulPath = Join-Path $PSScriptRoot '..' 'nul'
$nulDeleted = $false
if (Test-Path $nulPath) {
    Remove-Item $nulPath -Force
    $nulDeleted = $true
}

# --- Verification ---
Start-Sleep -Milliseconds 300

$residualDotnet = @(Get-Process dotnet -ErrorAction SilentlyContinue)
$residualCount = $residualDotnet.Count

$portCount = 0
if ($residualCount -gt 0) {
    $residualPids = $residualDotnet | ForEach-Object { $_.Id }
    $portMatches = netstat -ano | Select-String 'LISTENING' | Where-Object {
        $pid = $_.Line.TrimEnd() -replace '.*\s+'
        $residualPids -contains $pid
    }
    $portCount = @($portMatches).Count
}

# --- Summary ---
Write-Host "Cleanup: stopped $dotnetCount dotnet, closed $braveCount Brave, nul deleted: $nulDeleted. Residual dotnet: $residualCount, listening ports: $portCount"
