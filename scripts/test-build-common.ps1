# Test Build Scripts for AoT Control
# Usage Examples:
#   .\test-build-fast.ps1     # Development mode (AoT disabled) - Fast builds
#   .\test-build-aot.ps1      # Production mode (AoT enabled) - Production parity
#   .\test-build-ci.ps1       # CI/CD mode (simulates CI environment)

# Common parameters
param(
    [switch]$Clean = $false,
    [switch]$Verbose = $false,
    [string]$Filter = "",
    [switch]$Coverage = $false
)

# Set error handling
$ErrorActionPreference = "Stop"

function Write-BuildStatus {
    param([string]$Message, [string]$Color = "Green")
    Write-Host "🚀 $Message" -ForegroundColor $Color
}

function Write-BuildError {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

# Export functions for other scripts to use
if ($MyInvocation.InvocationName -ne '&') {
    Export-ModuleMember -Function Write-BuildStatus, Write-BuildError
}