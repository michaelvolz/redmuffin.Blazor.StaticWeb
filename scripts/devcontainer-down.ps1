#Requires -Version 5.1

<#
.SYNOPSIS
    Stops the devcontainer.

.DESCRIPTION
    Stops the running devcontainer. Use this when you're done developing
    to free up system resources.

.EXAMPLE
    .\devcontainer-down.ps1
    Stops the devcontainer.

.LINK
    See .devcontainer/SECURITY.md for security architecture details.
#>

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

Write-Host "=== Stopping DevContainer ===" -ForegroundColor Cyan

# Check if Docker is running
try {
    $null = docker ps 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Docker not running"
    }
} catch {
    Write-Host "Docker is not running. Nothing to stop." -ForegroundColor Yellow
    exit 0
}

# Get project root
$projectRoot = Split-Path -Parent $PSScriptRoot

# Find container by devcontainer label
$containerRunning = docker ps --filter "label=devcontainer.local_folder=$projectRoot" --format "{{.Names}}"

if (-not $containerRunning) {
    Write-Host "Devcontainer is not running. Nothing to stop." -ForegroundColor Green
    exit 0
}

Write-Host "Stopping devcontainer: $containerRunning..." -ForegroundColor Yellow

# Stop and remove the container
docker stop $containerRunning
docker rm $containerRunning

if ($LASTEXITCODE -eq 0) {
    Write-Host "Devcontainer stopped and removed successfully." -ForegroundColor Green
} else {
    Write-Host "Error: Failed to stop devcontainer." -ForegroundColor Red
    Write-Host "You may need to stop it manually." -ForegroundColor Yellow
    exit 1
}
