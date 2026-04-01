# Script constants
$frontendProject = "redmuffin.Blazor.StaticWeb.csproj"
$backendProject = "redmuffin.Blazor.StaticWeb.Api.csproj"
$frontendPort = 5233
$backendPort = 7071
$frontendPath = "src/redmuffin.Blazor.StaticWeb"
$backendPath = "src/redmuffin.Blazor.StaticWeb.Api"
$pidFile = ".dev-session.pids"

# Parse command line arguments
$autoMode = $false
foreach ($arg in $args) {
    if ($arg -eq "-Auto") {
        $autoMode = $true
    }
}

# Determine if we should use -NoExit
$useNoExit = -not $autoMode

# Cleanup function
function Stop-DevEnvironment {
    Write-Host "Stopping development environment..." -ForegroundColor Yellow
    
    if (Test-Path $pidFile) {
        $lines = Get-Content $pidFile
        foreach ($line in $lines) {
            $parts = $line -split '=', 2
            if ($parts.Count -eq 2) {
                $name = $parts[0].Trim()
                $processIdStr = $parts[1].Trim()
                
                if (-not [string]::IsNullOrWhiteSpace($processIdStr)) {
                    try {
                        $processId = [int]$processIdStr
                        $proc = Get-Process -Id $processId -ErrorAction SilentlyContinue
                        if ($proc) {
                            Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
                            Write-Host "  Stopped $name (PID: $processId)" -ForegroundColor Gray
                        }
                    } catch {
                        # Process already gone
                    }
                }
            }
        }
        Remove-Item $pidFile -ErrorAction SilentlyContinue
    }
    
    # Also clean up any orphaned dotnet processes on the ports
    foreach ($port in @($frontendPort, $backendPort, 4280)) {
        Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue | ForEach-Object {
            try {
                $proc = Get-Process -Id $_.OwningProcess -ErrorAction SilentlyContinue
                if ($proc -and $proc.ProcessName -match 'dotnet|swa') {
                    Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue
                    Write-Host "  Cleaned up orphaned process on port $port (PID: $($_.OwningProcess))" -ForegroundColor Gray
                }
            } catch {
                # Process already gone
            }
        }
    }
    
    Write-Host "Development environment stopped." -ForegroundColor Green
}

# Verify prerequisites
function Test-Prerequisites {
    $tools = @('dotnet', 'swa')
    foreach ($tool in $tools) {
        if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) {
            throw "Required tool '$tool' is not installed."
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

# Kill any existing processes on required ports
function Clear-ExistingProcesses {
    Write-Host "Checking for existing processes..." -ForegroundColor Gray
    foreach ($port in @($frontendPort, $backendPort, 4280)) {
        Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue | ForEach-Object {
            try {
                Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue
                Write-Host "  Killed process on port $port (PID: $($_.OwningProcess))" -ForegroundColor Gray
            } catch {
                # Ignore errors
            }
        }
    }
    Start-Sleep -Seconds 1
}

# Main execution
try {
    if ($autoMode) {
        if (Test-Path $pidFile) {
            Write-Host "Development environment already running. Run Stop.ps1 first or use -Auto flag." -ForegroundColor Yellow
            exit 0
        }
        Write-Host "Starting in AUTO mode (PID tracking enabled)" -ForegroundColor Cyan
    } else {
        Write-Host "Starting in INTERACTIVE mode (use -Auto for automated tracking)" -ForegroundColor Cyan
    }
    
    Clear-ExistingProcesses
    Test-Prerequisites
    Test-ProjectStructure
    
    # Build arguments
    $noExitArg = if ($useNoExit) { '-NoExit' } else { $null }
    
    # Start Frontend
    Write-Host "Starting Frontend..." -ForegroundColor Cyan
    $frontendArgs = @($noExitArg, '-Command', "dotnet watch run --project $frontendProject") | Where-Object { $_ }
    $frontendProcess = Start-Process -FilePath "pwsh" -ArgumentList $frontendArgs -WorkingDirectory $frontendPath -PassThru
    
    # Start Backend
    Write-Host "Starting Backend..." -ForegroundColor Cyan
    $backendArgs = @($noExitArg, '-Command', "dotnet watch run --project $backendProject") | Where-Object { $_ }
    $backendProcess = Start-Process -FilePath "pwsh" -ArgumentList $backendArgs -WorkingDirectory $backendPath -PassThru
    
    # Wait for dotnet processes to start
    Start-Sleep -Seconds 3
    
    # Start SWA
    Write-Host "Starting Azure Static Web Apps CLI..." -ForegroundColor Cyan
    $swaArgs = @($noExitArg, '-Command', "swa start 'http://localhost:$frontendPort' --api-location 'http://localhost:$backendPort/api'") | Where-Object { $_ }
    $swaProcess = Start-Process -FilePath "pwsh" -ArgumentList $swaArgs -PassThru
    
    Write-Host ""
    Write-Host "Development environment started!" -ForegroundColor Green
    Write-Host "  Frontend:  http://localhost:$frontendPort" -ForegroundColor Yellow
    Write-Host "  Backend:   http://localhost:$backendPort" -ForegroundColor Yellow
    Write-Host "  SWA Proxy: http://localhost:4280" -ForegroundColor Yellow
    Write-Host ""
    
    if ($autoMode) {
        # Save PIDs for automated cleanup
        @(
            "Frontend=$($frontendProcess.Id)"
            "Backend=$($backendProcess.Id)"
            "SWA=$($swaProcess.Id)"
        ) | Out-File -FilePath $pidFile -Encoding utf8
        
        Write-Host "PID file created: $pidFile" -ForegroundColor Gray
        Write-Host "To stop, run: pwsh Stop.ps1" -ForegroundColor Cyan
    } else {
        Write-Host "Running in INTERACTIVE mode. Close the windows manually when done." -ForegroundColor Cyan
    }
}
catch {
    Write-Host "Error: $_" -ForegroundColor Red
    Stop-DevEnvironment
    exit 1
}
