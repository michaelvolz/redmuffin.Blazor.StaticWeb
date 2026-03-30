#!/usr/bin/env pwsh
#Requires -Version 7.0
#Requires -Modules Microsoft.PowerShell.SecretManagement, Microsoft.PowerShell.SecretStore

<#
.SYNOPSIS
    Securely stores API keys in PowerShell SecretStore for use with devcontainer.

.DESCRIPTION
    This script helps you store API keys securely in the PowerShell SecretStore.
    These secrets are encrypted at rest and only accessible to you.
    They are temporarily exposed as environment variables when starting the devcontainer.

.NOTES
    - Secrets are stored encrypted using Windows Credential Manager
    - No secrets are ever written to disk in plain text
    - Secrets are only in memory when the devcontainer is running
    - This is the recommended secure approach for local development

.EXAMPLE
    .\setup-secrets.ps1

    This will guide you through setting up all required secrets.

.EXAMPLE
    .\setup-secrets.ps1 -Reset

    This will reset all secrets (useful for rotating API keys).
#>

[CmdletBinding()]
param(
    [switch]$Reset,
    [switch]$Silent
)

$ErrorActionPreference = 'Stop'

# Configuration
$SecretStoreName = 'DevContainerSecrets'
$RequiredSecrets = @(
    @{ Name = 'BRAVE_API_KEY'; Description = 'Brave Search API Key for MCP web search'; Url = 'https://brave.com/search/api/' }
    @{ Name = 'CONTEXT7_API_KEY'; Description = 'Context7 API Key (optional) for up-to-date library documentation'; Url = 'https://context7.com'; Optional = $true }
    @{ Name = 'RainDropClientID'; Description = 'Raindrop.io OAuth Client ID'; Url = 'https://app.raindrop.io/settings/integrations/developer' }
    @{ Name = 'RainDropClientSecret'; Description = 'Raindrop.io OAuth Client Secret'; Url = 'https://app.raindrop.io/settings/integrations/developer' }
    @{ Name = 'RainDropTestToken'; Description = 'Raindrop.io Test Token for API testing'; Url = 'https://app.raindrop.io/settings/integrations/developer' }
)

Write-Host "=== Secure DevContainer Secrets Setup ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "This script will store your API keys securely in PowerShell SecretStore." -ForegroundColor White
Write-Host "Secrets are encrypted at rest and only in memory when the devcontainer runs." -ForegroundColor White
Write-Host ""

# Check if SecretManagement modules are available
try {
    $secretStoreVault = Get-SecretVault -Name $SecretStoreName -ErrorAction SilentlyContinue
    if (-not $secretStoreVault) {
        Write-Host "Initializing SecretStore vault..." -ForegroundColor Yellow
        Register-SecretVault -Name $SecretStoreName -ModuleName Microsoft.PowerShell.SecretStore -DefaultVault
        Write-Host "SecretStore vault created successfully." -ForegroundColor Green
    } else {
        Write-Host "SecretStore vault already configured." -ForegroundColor Green
    }
} catch {
    Write-Error "Failed to initialize SecretStore: $_"
    Write-Host ""
    Write-Host "Please install required modules:" -ForegroundColor Yellow
    Write-Host "  Install-Module Microsoft.PowerShell.SecretManagement -Force" -ForegroundColor Cyan
    Write-Host "  Install-Module Microsoft.PowerShell.SecretStore -Force" -ForegroundColor Cyan
    exit 1
}

# Check if we need to unlock the vault
try {
    $testSecret = Get-Secret -Name "test" -Vault $SecretStoreName -ErrorAction SilentlyContinue
} catch {
    Write-Host ""
    Write-Host "SecretStore is locked. Please unlock it." -ForegroundColor Yellow
    Unlock-SecretStore -Password (Read-Host -AsSecureString -Prompt "Enter SecretStore password")
}

# Remove existing secrets if resetting
if ($Reset) {
    Write-Host ""
    Write-Host "Reset mode: Removing all existing secrets..." -ForegroundColor Yellow
    foreach ($secret in $RequiredSecrets) {
        $existing = Get-Secret -Name $secret.Name -Vault $SecretStoreName -ErrorAction SilentlyContinue
        if ($existing) {
            Remove-Secret -Name $secret.Name -Vault $SecretStoreName
            Write-Host "  Removed: $($secret.Name)" -ForegroundColor Gray
        }
    }
    Write-Host "All secrets removed." -ForegroundColor Green
    Write-Host ""
}

# Configure each secret
$configuredSecrets = 0

foreach ($secret in $RequiredSecrets) {
    $existing = Get-Secret -Name $secret.Name -Vault $SecretStoreName -ErrorAction SilentlyContinue
    
    if ($existing -and -not $Reset) {
        if (-not $Silent) {
            Write-Host "[$($secret.Name)]" -ForegroundColor Cyan -NoNewline
            Write-Host " already configured." -ForegroundColor Green
        }
        $configuredSecrets++
        continue
    }
    
    Write-Host ""
    Write-Host "[$($secret.Name)]" -ForegroundColor Cyan
    Write-Host "  Description: $($secret.Description)" -ForegroundColor White
    Write-Host "  Documentation: $($secret.Url)" -ForegroundColor Blue
    
    if ($secret.Optional) {
        Write-Host "  Note: This secret is optional" -ForegroundColor Yellow
        $skip = Read-Host "  Skip this secret? (y/N)"
        if ($skip -eq 'y') {
            continue
        }
    }
    
    $value = Read-Host "  Enter value (or press Enter to skip)" -AsSecureString
    
    if ($value.Length -gt 0) {
        Set-Secret -Name $secret.Name -SecureStringSecret $value -Vault $SecretStoreName
        Write-Host "  Saved successfully." -ForegroundColor Green
        $configuredSecrets++
    } else {
        Write-Host "  Skipped." -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "=== Setup Complete ===" -ForegroundColor Cyan
Write-Host "Configured $configuredSecrets/$($RequiredSecrets.Count) secrets." -ForegroundColor White
Write-Host ""
Write-Host "To start the devcontainer with these secrets:" -ForegroundColor White
Write-Host "  .\scripts\opencode-secure.ps1" -ForegroundColor Cyan
Write-Host ""
Write-Host "To verify your secrets are stored:" -ForegroundColor White
Write-Host "  Get-SecretInfo -Vault DevContainerSecrets" -ForegroundColor Cyan
Write-Host ""
Write-Host "Security Notes:" -ForegroundColor Yellow
Write-Host "  - Secrets are encrypted at rest using Windows Credential Manager" -ForegroundColor Gray
Write-Host "  - Secrets are only in memory when the devcontainer is running" -ForegroundColor Gray
Write-Host "  - No secrets are ever written to disk in plain text" -ForegroundColor Gray
Write-Host "  - The devcontainer will read secrets automatically on startup" -ForegroundColor Gray
