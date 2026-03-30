#Requires -Version 5.1

<#
.SYNOPSIS
    Configures SSH agent on Windows for devcontainer SSH forwarding.

.DESCRIPTION
    This script sets up the SSH agent service on Windows and adds your SSH key.
    This enables GitHub access from within the devcontainer through SSH agent forwarding.

.EXAMPLE
    .\setup-ssh-agent.ps1
    Configures SSH agent and adds the default key.

.EXAMPLE
    .\setup-ssh-agent.ps1 -KeyPath "$HOME\.ssh\id_ed25519"
    Configures SSH agent with a specific key.

.LINK
    See .devcontainer/SECURITY.md for security architecture details.
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$KeyPath = "$HOME\.ssh\id_ed25519"
)

$ErrorActionPreference = 'Stop'

Write-Host "=== SSH Agent Setup for DevContainer ===" -ForegroundColor Cyan

# Check if running on Windows
if (-not $IsWindows -and -not ($env:OS -eq "Windows_NT")) {
    Write-Host "Error: This script is designed for Windows." -ForegroundColor Red
    exit 1
}

# Step 1: Enable and start SSH agent service
Write-Host "`n[1/3] Configuring SSH agent service..." -ForegroundColor Cyan

try {
    $service = Get-Service ssh-agent -ErrorAction Stop
    
    if ($service.StartType -eq 'Disabled') {
        Write-Host "Enabling SSH agent service..." -ForegroundColor Yellow
        Set-Service -Name ssh-agent -StartupType Automatic
    }
    
    if ($service.Status -ne 'Running') {
        Write-Host "Starting SSH agent service..." -ForegroundColor Yellow
        Start-Service ssh-agent
    }
    
    Write-Host "SSH agent service is running." -ForegroundColor Green
} catch {
    Write-Host "Error: Could not configure SSH agent service." -ForegroundColor Red
    Write-Host "Make sure OpenSSH is installed (Windows 10/11 feature)." -ForegroundColor Yellow
    exit 1
}

# Step 2: Check for SSH key
Write-Host "`n[2/3] Checking for SSH key..." -ForegroundColor Cyan

if (-not (Test-Path $KeyPath)) {
    Write-Host "Error: SSH key not found at $KeyPath" -ForegroundColor Red
    Write-Host "`nTo generate a new SSH key:" -ForegroundColor Cyan
    Write-Host "  ssh-keygen -t ed25519 -C 'your-email@example.com'" -ForegroundColor White
    Write-Host "`nThen add it to GitHub: https://github.com/settings/keys" -ForegroundColor White
    exit 1
}

Write-Host "Found SSH key: $KeyPath" -ForegroundColor Green

# Step 3: Add key to SSH agent
Write-Host "`n[3/3] Adding key to SSH agent..." -ForegroundColor Cyan

try {
    $output = ssh-add $KeyPath 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "SSH key added successfully!" -ForegroundColor Green
    } else {
        Write-Host "Warning: Could not add SSH key. Output: $output" -ForegroundColor Yellow
    }
} catch {
    Write-Host "Error: Failed to add SSH key to agent." -ForegroundColor Red
    Write-Host "Error details: $_" -ForegroundColor Yellow
    exit 1
}

# Step 4: Verify
Write-Host "`n[Verification]" -ForegroundColor Cyan
$keys = ssh-add -l 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "SSH agent has the following keys loaded:" -ForegroundColor Green
    Write-Host $keys -ForegroundColor White
} else {
    Write-Host "Warning: Could not verify SSH keys." -ForegroundColor Yellow
}

Write-Host "`n=== Setup Complete ===" -ForegroundColor Cyan
Write-Host "SSH agent is now configured. You can use Git from the devcontainer." -ForegroundColor Green
Write-Host "Run '.\scripts\opencode-secure.ps1' to start the devcontainer." -ForegroundColor White
