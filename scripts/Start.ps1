# Kill any existing processes on required ports
Get-NetTCPConnection -LocalPort 5233,7071,4280 -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue }
Get-Process | Where-Object { $_.ProcessName -match 'dotnet|swa' } | Stop-Process -Force -ErrorAction SilentlyContinue

# Script constants
$frontendProject = "redmuffin.Blazor.StaticWeb.csproj"
$backendProject = "redmuffin.Blazor.StaticWeb.Api.csproj"
$frontendPort = 5233  # Fixed port - do not change
$backendPort = 7071   # Fixed port - do not change
$frontendPath = "src/redmuffin.Blazor.StaticWeb"
$backendPath = "src/redmuffin.Blazor.StaticWeb.Api"

# Store all jobs for cleanup
$script:jobs = @()

# Cleanup function for graceful shutdown
function Stop-DevEnvironment {
    Write-Host "Stopping development environment..." -ForegroundColor Yellow
    foreach ($job in $script:jobs) {
        Stop-Job -Job $job -ErrorAction SilentlyContinue
        Remove-Job -Job $job -ErrorAction SilentlyContinue
    }
    Get-Process | Where-Object { $_.CommandLine -match 'dotnet watch|swa start' } | Stop-Process -Force -ErrorAction SilentlyContinue
    Write-Host "Development environment stopped." -ForegroundColor Green
}

# Verify prerequisites
function Test-Prerequisites {
    $tools = @('dotnet', 'swa')
    foreach ($tool in $tools) {
        if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) {
            throw "Required tool '$tool' is not installed. Please install it before running this script."
        }
    }
}

# Verify project structure
function Test-ProjectStructure {
    $paths = @(
        "$frontendPath/$frontendProject",
        "$backendPath/$backendProject"
    )
    foreach ($path in $paths) {
        if (-not (Test-Path $path)) {
            throw "Required project not found: $path"
        }
    }
}

# Start a job and handle errors
function Start-ProjectJob {
    param(
        [string]$Path,
        [string]$Project,
        [string]$Name
    )

    try {
        Write-Host "Starting $Name..." -ForegroundColor Cyan
        $job = Start-Job -ScriptBlock {
            Set-Location $using:Path
            Start-Process pwsh -ArgumentList '-NoExit', '-Command', "dotnet watch run --project $using:Project"
        }
        $script:jobs += $job

        # Quick check if job started successfully
        Start-Sleep -Seconds 1
        if ($job.State -eq 'Failed') {
            throw "Failed to start ${Name}: $($job.ChildJobs[0].Error)"
        }
    }
    catch {
        Write-Host "Error starting ${Name}: $_" -ForegroundColor Red
        Stop-DevEnvironment
        throw
    }
}

# Main execution block
try {
    # Register cleanup on script exit
    $null = Register-EngineEvent -SourceIdentifier PowerShell.Exiting -Action { Stop-DevEnvironment }

    # Verify environment
    Test-Prerequisites
    Test-ProjectStructure

    # Start frontend and backend
    Start-ProjectJob -Path $frontendPath -Project $frontendProject -Name "Frontend"
    Start-ProjectJob -Path $backendPath -Project $backendProject -Name "Backend"

    # Start SWA CLI
    Write-Host "Starting Azure Static Web Apps CLI..." -ForegroundColor Cyan
    Start-Process pwsh -ArgumentList '-NoExit', '-Command', "swa start 'http://localhost:$frontendPort' --api-location 'http://localhost:$backendPort/api'"

    # Display status
    Write-Host "
Development environment started successfully!" -ForegroundColor Green
    Write-Host "Frontend: http://localhost:$frontendPort" -ForegroundColor Yellow
    Write-Host "Backend: http://localhost:$backendPort" -ForegroundColor Yellow
    Write-Host "SWA Proxy: http://localhost:4280" -ForegroundColor Yellow
}
catch {
    Write-Host "Error: $_" -ForegroundColor Red
    Stop-DevEnvironment
    exit 1
}
