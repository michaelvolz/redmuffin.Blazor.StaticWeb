# Define variables for ports and paths
$frontendProject = "redmuffin.Blazor.StaticWeb.csproj"
$backendProject = "redmuffin.Blazor.StaticWeb.Api.csproj"
$frontendPort = 5233
$backendPort = 7071

# Start the Blazor WebAssembly frontend
Start-Job -ScriptBlock {
    Push-Location "src/redmuffin.Blazor.StaticWeb/"
        Start-Process pwsh -ArgumentList '-NoExit', '-Command', "dotnet watch run --project $using:frontendProject"
    Pop-Location
}

# Start the Azure Functions API backend
Start-Job -ScriptBlock {
    Push-Location "src/redmuffin.Blazor.StaticWeb.Api/"
        Start-Process pwsh -ArgumentList '-NoExit', '-Command', "dotnet run --project $using:backendProject"
    Pop-Location
}

# Wait for processes to start
Start-Sleep -Seconds 3

# Start Azure Static Web Apps CLI
Start-Process pwsh -ArgumentList '-NoExit', '-Command', "swa start 'http://localhost:$frontendPort' --api-location 'http://localhost:$backendPort/api'"

# Add logging
Write-Host "Frontend running at http://localhost:$frontendPort"
Write-Host "Backend running at http://localhost:$backendPort"
