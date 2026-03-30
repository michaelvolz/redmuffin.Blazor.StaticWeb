#Requires -Version 7.0
#Requires -Modules Microsoft.PowerShell.SecretManagement

<#
.SYNOPSIS
    Starts devcontainer and runs opencode inside it with full security boundary.

.DESCRIPTION
    This script ensures the devcontainer is running and then executes opencode
    inside the container. Secrets are securely read from PowerShell SecretStore
    and temporarily exposed as environment variables.

    Security Flow:
    1. Secrets are stored encrypted in PowerShell SecretStore
    2. This script reads them and sets as temporary env vars (memory only)
    3. DevContainer reads env vars via ${localEnv:VARIABLE_NAME} syntax
    4. Secrets are available in container for MCP servers and app
    5. When container stops, secrets are gone from memory

    The container is started automatically if not running.

.EXAMPLE
    .\opencode-secure.ps1
    Starts opencode in the devcontainer with secrets injected.

.EXAMPLE
    .\opencode-secure.ps1 "create component Button"
    Starts opencode with arguments.

.LINK
    See .devcontainer/SECURITY.md for security architecture details.
    See docs/SECRETS-SETUP.md for secret configuration guide.
#>

[CmdletBinding()]
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Arguments
)

$ErrorActionPreference = 'Stop'

Write-Host "=== OpenCode Secure Development ===" -ForegroundColor Cyan

# Check if devcontainer CLI is installed
try {
    $null = Get-Command devcontainer -ErrorAction Stop
} catch {
    Write-Host "Error: DevContainer CLI not found." -ForegroundColor Red
    Write-Host "Install it with: npm install -g @devcontainers/cli" -ForegroundColor Yellow
    exit 1
}

# Load secrets from PowerShell SecretStore
Write-Host "`nLoading secrets from secure storage..." -ForegroundColor Cyan

$SecretStoreName = 'DevContainerSecrets'
$RequiredSecrets = @('BRAVE_API_KEY', 'CONTEXT7_API_KEY', 'RainDropClientID', 'RainDropClientSecret', 'RainDropTestToken')
$secretsLoaded = 0
$secretsMissing = @()

try {
    # Check if SecretStore vault exists
    $vault = Get-SecretVault -Name $SecretStoreName -ErrorAction SilentlyContinue
    
    if (-not $vault) {
        Write-Host "`nWarning: SecretStore vault '$SecretStoreName' not found." -ForegroundColor Yellow
        Write-Host "Run the setup script first:" -ForegroundColor Cyan
        Write-Host "  .\scripts\setup-secrets.ps1" -ForegroundColor White
        Write-Host ""
        
        $continue = Read-Host "Continue without secrets? (y/N)"
        if ($continue -notmatch '^[Yy]$') {
            exit 0
        }
    } else {
        # Load each secret
        foreach ($secretName in $RequiredSecrets) {
            try {
                $secret = Get-Secret -Name $secretName -Vault $SecretStoreName -ErrorAction Stop
                if ($secret) {
                    # Convert SecureString to plain text (temporary, in memory only)
                    $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secret)
                    $plainText = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
                    [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($BSTR)
                    
                    # Set as environment variable (current process only)
                    [System.Environment]::SetEnvironmentVariable($secretName, $plainText, 'Process')
                    
                    # Clear plain text from memory
                    $plainText = $null
                    
                    $secretsLoaded++
                    Write-Host "  Loaded: $secretName" -ForegroundColor Green
                }
            } catch {
                $secretsMissing += $secretName
            }
        }
        
        if ($secretsMissing.Count -gt 0) {
            Write-Host "`nWarning: Some secrets not found:" -ForegroundColor Yellow
            $secretsMissing | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
            Write-Host "Run the setup script to configure missing secrets:" -ForegroundColor Cyan
            Write-Host "  .\scripts\setup-secrets.ps1" -ForegroundColor White
            Write-Host ""
            
            $continue = Read-Host "Continue with partial secrets? (y/N)"
            if ($continue -notmatch '^[Yy]$') {
                exit 0
            }
        }
    }
} catch {
    Write-Host "`nError accessing secrets: $_" -ForegroundColor Red
    Write-Host "Run the setup script:" -ForegroundColor Cyan
    Write-Host "  .\scripts\setup-secrets.ps1" -ForegroundColor White
    Write-Host ""
    
    $continue = Read-Host "Continue without secrets? (y/N)"
    if ($continue -notmatch '^[Yy]$') {
        exit 0
    }
}

Write-Host "Loaded $secretsLoaded/$($RequiredSecrets.Count) secrets." -ForegroundColor Green

# Check if Docker is running
try {
    $null = docker ps 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Docker not running"
    }
} catch {
    Write-Host "Error: Docker is not running or not accessible." -ForegroundColor Red
    Write-Host "Please start Docker Desktop and ensure WSL2 backend is enabled." -ForegroundColor Yellow
    exit 1
}

# Check if SSH agent is running (for GitHub access from container)
$sshAgentRunning = $env:SSH_AUTH_SOCK -or (Get-Process ssh-agent -ErrorAction SilentlyContinue)
if (-not $sshAgentRunning) {
    Write-Host "`nWarning: SSH agent not detected." -ForegroundColor Yellow
    Write-Host "Git push/pull from the container will not work without SSH agent forwarding." -ForegroundColor Yellow
    Write-Host "To enable:" -ForegroundColor Cyan
    Write-Host "  1. Start SSH agent: Get-Service ssh-agent | Set-Service -StartupType Automatic; Start-Service ssh-agent" -ForegroundColor White
    Write-Host "  2. Add your key: ssh-add `$HOME\.ssh\id_ed25519" -ForegroundColor White
    Write-Host "  3. Restart the devcontainer" -ForegroundColor White
    Write-Host ""
    
    $continue = Read-Host "Continue without SSH agent? (y/N)"
    if ($continue -notmatch '^[Yy]$') {
        exit 0
    }
}

# Get the project root (one level up from scripts directory)
$projectRoot = Split-Path -Parent $PSScriptRoot

# Check if Dockerfile has changed since last container was built
$dockerfilePath = Join-Path $projectRoot ".devcontainer\Dockerfile"
$dockerfileHash = (Get-FileHash $dockerfilePath -Algorithm MD5).Hash

# Check if devcontainer is already running
$containerRunning = docker ps --filter "label=devcontainer.local_folder=$projectRoot" --format "{{.Names}}"

# Check if we need to rebuild (container running but Dockerfile changed)
$needsRebuild = $false
if ($containerRunning) {
    # Check if container has the expected label with hash
    $existingHash = docker inspect $containerRunning --format '{{index .Config.Labels "devcontainer.dockerfile_hash"}}' 2>$null
    if ($existingHash -ne $dockerfileHash) {
        Write-Host "Dockerfile has changed. Rebuilding container..." -ForegroundColor Yellow
        $needsRebuild = $true
    }
}

if (-not $containerRunning -or $needsRebuild) {
    Write-Host "`nStarting devcontainer..." -ForegroundColor Cyan
    
    if ($needsRebuild) {
        # Stop and remove old container
        docker stop $containerRunning 2>$null
        docker rm $containerRunning 2>$null
    }
    
    # Start the devcontainer with build flag
    devcontainer up --workspace-folder $projectRoot --build-no-cache
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error: Failed to start devcontainer." -ForegroundColor Red
        Write-Host "First run may require VS Code to configure secrets." -ForegroundColor Yellow
        Write-Host "Alternatively, use: code .  (then Reopen in Container)" -ForegroundColor Yellow
        exit 1
    }
    
    Write-Host "Devcontainer started successfully." -ForegroundColor Green
    Write-Host "Secrets injected securely from PowerShell SecretStore." -ForegroundColor Green
} else {
    Write-Host "Devcontainer is already running (Dockerfile unchanged)." -ForegroundColor Green
}

# Run opencode inside the container
Write-Host "`nStarting opencode in devcontainer..." -ForegroundColor Cyan

devcontainer exec opencode $Arguments

exit $LASTEXITCODE
