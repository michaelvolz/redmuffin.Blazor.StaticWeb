---
title: Start Script Documentation
date: 2025-07-19
---

## Overview

`Start.ps1` is a PowerShell helper for final integration-style testing and full
environment validation. It is not the normal day-to-day development startup path.

For routine development, start the frontend directly with:

```powershell
dotnet run --project src/redmuffin.Blazor.StaticWeb/redmuffin.Blazor.StaticWeb.csproj --launch-profile https
```

## Components

### 1. Blazor WebAssembly Frontend

- **Project**: `redmuffin.Blazor.StaticWeb.csproj`
- **Port**: 5233 (fixed, must not be changed)
- **Command**: `dotnet watch run`
- **Purpose**: Runs the Blazor WebAssembly frontend application with hot reload capabilities
- **Location**: `src/redmuffin.Blazor.StaticWeb/`

### 2. Azure Functions Backend

- **Project**: `redmuffin.Blazor.StaticWeb.Api.csproj`
- **Port**: 7071 (fixed, must not be changed)
- **Command**: `dotnet watch run`
- **Purpose**: Runs the Azure Functions API backend with hot reload capabilities
- **Location**: `src/redmuffin.Blazor.StaticWeb.Api/`

### 3. Azure Static Web Apps CLI

- **Command**: `swa start`
- **Default Port**: 4280
- **Purpose**: Creates a proxy that merges the Blazor frontend and Azure Functions API into a single URL
- **Configuration**:
  - Frontend URL: `http://localhost:5233`
  - API Location: `http://localhost:7071/api`

## Important Considerations

### Port Configuration

- The ports used in this script (5233 for frontend, 7071 for backend) are fixed and must not be modified
- These ports are essential for the proper functioning of the local development environment
- The SWA CLI automatically uses port 4280 for the merged endpoint

### Development Workflow

- Each component runs in its own console window for clear visibility of logs and errors
- The `dotnet watch run` command enables hot reload for both frontend and backend
- All console outputs must remain visible to the developer at all times
- The SWA CLI automatically waits for both frontend and backend to be available before starting

### Production Deployment

- This script is for local development only
- Production deployment is handled via GitHub workflows to Azure Static Web Apps

## Script Operation

1. The script starts the Blazor WebAssembly frontend in a new PowerShell window using `Start-Job`
2. It then starts the Azure Functions backend in another PowerShell window using `Start-Job`
3. Finally, it launches the Azure Static Web Apps CLI in a third window to create a unified local development environment
4. The SWA CLI automatically handles waiting for the other services to be ready

## Example URLs

- Frontend (Direct): `http://localhost:5233`
- Backend API (Direct): `http://localhost:7071`
- SWA Proxy (Combined): `http://localhost:4280`

## Usage

Use this script only when you want the full multi-process test environment:

```powershell
.\start.ps1
```

## Notes

- The script uses PowerShell jobs to manage multiple processes
- Each component runs in a separate window for clear log visibility
- The SWA CLI provides a development environment that closely matches production
- All components include hot reload functionality for rapid development

## Optimization Report

### Current Issues and Risks

1. **No Error Handling**
   - Project file existence is not verified
   - Directory existence is not checked
   - No validation of required tools (dotnet, swa CLI)
   - Port availability is not checked
   - No error handling for failed job starts

2. **Process Management**
   - No cleanup of processes on script termination
   - No way to gracefully stop all processes
   - Jobs are started but not tracked or monitored

3. **Configuration**
   - No verbose/debug mode for troubleshooting

4. **Feedback and Monitoring**
   - Limited status information during startup
   - No feedback if services fail after starting

### Optimization Plan

#### Phase 1: Basic Error Handling and Validation

1. Add prerequisite checks:

```powershell
# Function to verify required tools
function Test-Prerequisites {
    $tools = @('dotnet', 'swa')
    foreach ($tool in $tools) {
        if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) {
            throw "Required tool '$tool' is not installed"
        }
    }
}
```

2. Add project validation:

```powershell
# Function to verify project files and directories
function Test-ProjectStructure {
    $requiredPaths = @(
        "src/redmuffin.Blazor.StaticWeb/$frontendProject",
        "src/redmuffin.Blazor.StaticWeb.Api/$backendProject"
    )
    foreach ($path in $requiredPaths) {
        if (-not (Test-Path $path)) {
            throw "Required path not found: $path"
        }
    }
}
```

#### Phase 2: Process Management

1. Add job tracking and cleanup:

```powershell
# Store job information
$jobs = @()

# Cleanup function
function Stop-DevEnvironment {
    foreach ($job in $jobs) {
        Stop-Job -Job $job
        Remove-Job -Job $job
    }
    # Find and stop any remaining processes
    Get-Process | Where-Object { $_.CommandLine -match 'dotnet watch|swa start' } | Stop-Process -Force
}
```

2. Add error handling for job starts:

```powershell
try {
    $job = Start-Job -ScriptBlock { ... }
    $jobs += $job
    # Wait briefly and check job status
    Start-Sleep -Seconds 1
    if ($job.State -eq 'Failed') {
        throw "Job failed to start: $($job.ChildJobs[0].Error)"
    }
} catch {
    Stop-DevEnvironment
    throw
}
```

#### Phase 3: Configuration and Flexibility

1. Add verbose logging:

```powershell
function Write-VerboseLog {
    param([string]$Message)
    if ($config.verbose) {
        Write-Host "[DEBUG] $Message" -ForegroundColor Cyan
    }
}
```

#### Phase 4: Health Monitoring

1. Add monitoring loop:

```powershell
function Start-HealthMonitoring {
    Start-Job -ScriptBlock {
        while ($true) {
            Test-ServiceHealth -Url "http://localhost:$using:frontendPort" -Name "Frontend"
            Test-ServiceHealth -Url "http://localhost:$using:backendPort" -Name "Backend"
            Start-Sleep -Seconds 30
        }
    }
}
```

### Implementation Priority

1. Phase 1: Critical for basic reliability
2. Phase 2: Important for proper process management
3. Phase 4: Useful for development experience
4. Phase 3: Nice-to-have flexibility improvements

### Expected Benefits

- More reliable startup process
- Better error handling and feedback
- Proper process cleanup
- Improved development experience
- Easier troubleshooting

### Notes

- All improvements maintain existing functionality
- Fixed ports are still respected
- No changes to the core workflow
- Added features are optional/configurable
