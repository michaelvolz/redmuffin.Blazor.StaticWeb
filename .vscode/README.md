# VS Code Development Setup

## Overview

This project is configured for a smooth development experience in VS Code, similar to Visual Studio 2022. The setup allows you to start all three required components with a single click:

1. The Blazor WebAssembly app (http://localhost:5233)
2. The Azure Functions API (http://localhost:7071/api)
3. The Static Web Apps CLI proxy (http://localhost:4280)

## Launch Configurations

### Full Stack Development

This is the recommended launch configuration for development. It starts all three components in the correct order and opens the SWA CLI proxy in your browser.

1. In VS Code, go to the Run and Debug view (Ctrl+Shift+D)
2. Select "Full Stack Development" from the dropdown
3. Click the green play button or press F5

This will:
- Start the Blazor WebAssembly app
- Start the Azure Functions API
- Start the SWA CLI proxy
- Open http://localhost:4280 in your browser

### Other Launch Configurations

- **Launch and Debug Standalone Blazor WebAssembly App**: Starts only the Blazor app
- **Debug Functions (.NET Isolated)**: Starts only the Azure Functions API
- **Attach to .NET Functions**: Attaches to an already running Functions host
- **Blazor WebAssembly + Functions API**: Starts both the Blazor app and Functions API
- **Full Stack (Blazor + Functions + SWA)**: Starts all three components (alternative to "Full Stack Development")

## Tasks

The following tasks are available:

- **start-full-stack**: Starts all three components in sequence
- **start-blazor-app**: Starts the Blazor WebAssembly app
- **func: host start**: Starts the Azure Functions API
- **run swa-launcher**: Starts the SWA CLI proxy using the SwaLauncher project
- **run swa cli**: Starts the SWA CLI proxy directly

## Troubleshooting

If you encounter issues:

1. Make sure all required tools are installed:
   - .NET SDK (versions 8.0 and 9.0)
   - Azure Functions Core Tools
   - Static Web Apps CLI (`npm install -g @azure/static-web-apps-cli`)

2. Check that the ports are not in use:
   - 5233 (Blazor app)
   - 7071 (Functions API)
   - 4280 (SWA CLI proxy)

3. If the SWA CLI proxy fails to connect, make sure both the Blazor app and Functions API are running first.