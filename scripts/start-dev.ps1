# Define variables for ports and paths
$frontendProject = "src/redmuffin.Blazor.StaticWeb"
$backendProject = "src/redmuffin.Blazor.StaticWeb.Api"
$frontendPort = 5233
$backendPort = 7184

# Start the Blazor WebAssembly frontend
Start-Job -ScriptBlock {
    Start-Process pwsh -ArgumentList '-NoExit', '-Command', "dotnet run --project $using:frontendProject"
}

# Start the Azure Functions API backend
Start-Job -ScriptBlock {
    Start-Process pwsh -ArgumentList '-NoExit', '-Command', "dotnet run --project $using:backendProject"
}

# Wait for processes to start
Start-Sleep -Seconds 5

# Start Azure Static Web Apps CLI
swa start "http://localhost:$frontendPort" --api-location "http://localhost:$backendPort/api"

# Add logging
Write-Host "Frontend running at http://localhost:$frontendPort"
Write-Host "Backend running at http://localhost:$backendPort"
