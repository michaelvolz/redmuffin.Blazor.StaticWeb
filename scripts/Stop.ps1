# Script constants
$pidFile = ".dev-session.pids"
$frontendPort = 5233
$backendPort = 7071

Write-Host "Stopping development environment..." -ForegroundColor Yellow

$stoppedCount = 0

# First, try to stop processes from PID file
if (Test-Path $pidFile) {
    Write-Host "Reading PID file..." -ForegroundColor Gray
    $lines = Get-Content $pidFile
    
    foreach ($line in $lines) {
        $line = $line.Trim()
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        
        $parts = $line -split '=', 2
        if ($parts.Count -eq 2) {
            $name = $parts[0].Trim()
            $processIdStr = $parts[1].Trim()
            
            if ([string]::IsNullOrWhiteSpace($processIdStr)) { continue }
            
            try {
                $processId = [int]$processIdStr
                $proc = Get-Process -Id $processId -ErrorAction SilentlyContinue
                
                if ($proc) {
                    # Kill the pwsh process (parent)
                    Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
                    Write-Host "  Stopped $name parent (PID: $processId)" -ForegroundColor Gray
                    $stoppedCount++
                    
                    # Wait a moment for child processes to die
                    Start-Sleep -Milliseconds 500
                }
            } catch {
                Write-Host "  Note: $name parent (PID: $processIdStr) already stopped" -ForegroundColor Gray
            }
        }
    }
    
    Remove-Item $pidFile -ErrorAction SilentlyContinue
}

# Clean up any remaining processes on the known ports
Write-Host "Cleaning up remaining processes..." -ForegroundColor Gray

foreach ($port in @($frontendPort, $backendPort, 4280)) {
    $connections = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
    foreach ($conn in $connections) {
        try {
            $proc = Get-Process -Id $conn.OwningProcess -ErrorAction SilentlyContinue
            if ($proc) {
                $procName = $proc.ProcessName
                Stop-Process -Id $conn.OwningProcess -Force -ErrorAction SilentlyContinue
                Write-Host "  Stopped $procName on port $port (PID: $($conn.OwningProcess))" -ForegroundColor Gray
                $stoppedCount++
            }
        } catch {
            # Ignore errors
        }
    }
}

# Also check for any dotnet or swa processes that might be orphaned
$orphanedProcesses = Get-Process | Where-Object { 
    ($_.ProcessName -eq 'dotnet' -or $_.ProcessName -eq 'swa') -and
    ($_.CommandLine -match 'watch run|swa start' -or $_.CommandLine -match 'redmuffin')
} | Select-Object -First 10

foreach ($proc in $orphanedProcesses) {
    try {
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
        Write-Host "  Stopped orphaned $($proc.ProcessName) (PID: $($proc.Id))" -ForegroundColor Gray
        $stoppedCount++
    } catch {
        # Ignore errors
    }
}

Write-Host ""
if ($stoppedCount -gt 0) {
    Write-Host "Development environment stopped ($stoppedCount processes killed)." -ForegroundColor Green
} else {
    Write-Host "Development environment stopped (no active processes found)." -ForegroundColor Green
}
