---
title: Secrets Management Guide
date: 2026-03-30
---

This document describes the secure secrets management system for the redmuffin.Blazor.StaticWeb devcontainer.

## Overview

We use a **secure, multi-layered approach** to manage API keys and secrets:

1. **Encrypted Storage**: Secrets stored in PowerShell SecretStore (Windows Credential Manager)
2. **Memory-Only Exposure**: Secrets temporarily loaded as environment variables
3. **Automatic Injection**: DevContainer reads secrets via `${localEnv:VARIABLE_NAME}` syntax
4. **Clean Lifecycle**: Secrets exist only when devcontainer is running

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│  Windows PowerShell SecretStore (Encrypted at Rest)          │
│  - Uses Windows Credential Manager                           │
│  - User-authenticated access only                            │
│  - No plain text files                                       │
└─────────────────────────────────────────────────────────────┘
                           │
                           │ (setup-secrets.ps1 to store)
                           │ (opencode-secure.ps1 to read)
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  PowerShell Session (Memory Only)                            │
│  - Temporary environment variables                           │
│  - Current process only                                      │
│  - Never written to disk                                     │
└─────────────────────────────────────────────────────────────┘
                           │
                           │ (devcontainer up)
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  DevContainer (Environment Variables)                        │
│  - Reads ${localEnv:VARIABLE_NAME}                           │
│  - Available to MCP servers and app                          │
│  - Memory only, never persisted                              │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  Container Stops → Secrets Gone                              │
└─────────────────────────────────────────────────────────────┘
```

## Quick Start

### 1. Install Required PowerShell Modules

Open PowerShell as Administrator and run:

```powershell
# Install SecretManagement modules
Install-Module Microsoft.PowerShell.SecretManagement -Force
Install-Module Microsoft.PowerShell.SecretStore -Force
```

### 2. Store Your Secrets

Navigate to the project directory and run:

```powershell
.\scripts\setup-secrets.ps1
```

This will guide you through storing:

- `BRAVE_API_KEY` - Brave Search API Key
- `CONTEXT7_API_KEY` - Context7 API Key (optional)
- `RAINDROP_CLIENT_ID` - Raindrop.io OAuth Client ID
- `RAINDROP_CLIENT_SECRET` - Raindrop.io OAuth Client Secret

### 3. Start the DevContainer

```powershell
.\scripts\opencode-secure.ps1
```

The script will:

1. Load secrets from PowerShell SecretStore
2. Set them as temporary environment variables
3. Start the devcontainer
4. Inject secrets automatically

## How It Works

### Secret Storage

Secrets are stored encrypted using Windows Credential Manager:

```powershell
# Store a secret (done by setup-secrets.ps1)
Set-Secret -Name BRAVE_API_KEY -Secret "your-api-key" -Vault DevContainerSecrets

# Verify storage
Get-SecretInfo -Vault DevContainerSecrets
```

### Secret Loading

When you run `opencode-secure.ps1`:

```powershell
# Read secret from encrypted store
$secret = Get-Secret -Name BRAVE_API_KEY -Vault DevContainerSecrets

# Convert to plain text (temporary)
$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secret)
$plainText = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
[System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($BSTR)

# Set as environment variable (current process only)
[System.Environment]::SetEnvironmentVariable("BRAVE_API_KEY", $plainText, 'Process')
```

### Container Injection

The devcontainer reads secrets from host environment:

```json
{
  "remoteEnv": {
    "BRAVE_API_KEY": "${localEnv:BRAVE_API_KEY}",
    "CONTEXT7_API_KEY": "${localEnv:CONTEXT7_API_KEY}",
    "RAINDROP_CLIENT_ID": "${localEnv:RAINDROP_CLIENT_ID}",
    "RAINDROP_CLIENT_SECRET": "${localEnv:RAINDROP_CLIENT_SECRET}"
  }
}
```

## Security Features

### At Rest

- Secrets stored encrypted using Windows Credential Manager
- OS-level encryption tied to user account
- No plain text files anywhere

### In Transit

- Memory-only exposure (never written to disk)
- Current PowerShell process only
- Automatic cleanup after use

### In Container

- Environment variables (memory only)
- Never persisted to container filesystem
- Available only while container runs

### After Use

- Complete cleanup when container stops
- No trace of secrets in logs or history
- Fresh start on each run

## Managing Secrets

### View Stored Secrets

```powershell
# List all stored secrets (names only, not values)
Get-SecretInfo -Vault DevContainerSecrets
```

### Update a Secret

```powershell
# Update an existing secret
Set-Secret -Name BRAVE_API_KEY -Secret "new-api-key" -Vault DevContainerSecrets
```

### Remove a Secret

```powershell
# Remove a specific secret
Remove-Secret -Name BRAVE_API_KEY -Vault DevContainerSecrets
```

### Reset All Secrets

```powershell
# Remove and reconfigure all secrets
.\scripts\setup-secrets.ps1 -Reset
```

### Troubleshooting

#### Module Not Found

If you get an error about missing modules:

```powershell
# Install required modules
Install-Module Microsoft.PowerShell.SecretManagement -Force
Install-Module Microsoft.PowerShell.SecretStore -Force

# Import modules
Import-Module Microsoft.PowerShell.SecretManagement
Import-Module Microsoft.PowerShell.SecretStore
```

#### Vault Locked

If the vault is locked:

```powershell
# Unlock the vault
Unlock-SecretStore -Password (Read-Host -AsSecureString "Enter vault password")
```

#### Secrets Not Loading

1. Verify secrets are stored:

   ```powershell
   Get-SecretInfo -Vault DevContainerSecrets
   ```

2. Run setup script again:

   ```powershell
   .\scripts\setup-secrets.ps1
   ```

3. Check for errors in output

## Comparison with Other Approaches

| Approach                     | Security  | Convenience | VS Code Required |
| ---------------------------- | --------- | ----------- | ---------------- |
| **PowerShell SecretStore**   | Excellent | High        | No               |
| VS Code Secrets              | Excellent | High        | Yes              |
| Environment Variables (host) | Good      | High        | No               |
| .env files                   | Poor      | High        | No               |
| Hardcoded in files           | Terrible  | High        | No               |

## Best Practices

1. **Never commit secrets** - The .gitignore already protects against this
2. **Rotate keys periodically** - Use `setup-secrets.ps1 -Reset` to update
3. **Use least privilege** - Only store secrets you need
4. **Lock your workstation** - Secrets are tied to your user account
5. **Monitor access** - Check Windows Credential Manager periodically

## Migration from Other Methods

### From Environment Variables

If you currently have secrets in environment variables:

```powershell
# Migrate to SecretStore
$env:BRAVE_API_KEY | Set-Secret -Name BRAVE_API_KEY -Vault DevContainerSecrets
```

### From .env Files

If you have a .env file:

```powershell
# Read .env file and store in SecretStore
Get-Content .env | ForEach-Object {
    if ($_ -match '^([^=]+)=(.+)$') {
        $name = $matches[1]
        $value = $matches[2]
        Set-Secret -Name $name -Secret $value -Vault DevContainerSecrets
    }
}
```

Then delete the .env file.

## References

- [PowerShell SecretManagement](https://learn.microsoft.com/powershell/utility-modules/secretmanagement/get-started/using-secretstore)
- [Windows Credential Manager](https://support.microsoft.com/windows/credential-manager)
- [DevContainer Environment Variables](https://code.visualstudio.com/remote/advancedcontainers/environment-variables)
