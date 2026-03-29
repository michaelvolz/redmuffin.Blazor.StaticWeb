# DevContainer for redmuffin.Blazor.StaticWeb

A fully configured development container with .NET 9, Node.js, Docker-in-Docker, and all required tools.

## Features

- **.NET 9 SDK** - All projects use .NET 9
- **Node.js LTS** - For Azure Static Web Apps CLI, Prettier, commitlint
- **Docker-in-Docker** - MCP servers and containerized tools
- **Azure Functions Core Tools** - Local API development
- **Git hooks** - Commit message validation

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) or Docker Engine
- [VS Code](https://code.visualstudio.com/) with [Remote - Containers extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers)

## Quick Start

### Option 1: Open in Container (Recommended)

1. Open the project in VS Code
2. Install the **Remote - Containers** extension
3. Click the green button in the bottom-left corner
4. Select "Reopen in Container"

The container will build automatically on first open.

### Option 2: Using Docker Compose

```bash
# Build and start the container
docker-compose -f .devcontainer/docker-compose.yml up -d

# Attach to the running container
docker exec -it redmuffin-devcontainer bash
```

## Configuration

### Environment Variables

Copy `.devcontainer/.env.example` to `.devcontainer/.env` and fill in your values:

```bash
cp .devcontainer/.env.example .devcontainer/.env
```

Available variables:

- `BRAVE_API_KEY` - Brave Search API Key (for Brave Search MCP Server)
- `CONTEXT7_API_KEY` - Context7 API Key (for up-to-date documentation)

### Ports

| Port | Service   | Description                           |
| ---- | --------- | ------------------------------------- |
| 4280 | SWA       | Full stack development (Blazor + API) |
| 5233 | Blazor    | Frontend-only development             |
| 7071 | Functions | Azure Functions API                   |

## Included Tools

### .NET Tools

- .NET 9 SDK
- Azure Functions Core Tools v4
- ReportGenerator (global tool)

### Node.js Tools

- `@azure/static-web-apps-cli` - Azure Static Web Apps CLI
- `prettier` - Code formatter
- `@commitlint/cli` - Commit message validation
- `chrome-devtools-mcp` - Chrome DevTools MCP server

### Docker Images (MCP Servers)

- `mcp/puppeteer` - Browser automation
- `mcp/fetch` - Web content fetching
- `mcp/time` - Time/date information
- `mcp/sequentialthinking` - Reasoning assistance
- `mcp/brave-search` - Web search

## Development Workflow

### Build and Test

```bash
# Build the solution
dotnet build

# Run tests
dotnet test

# Watch mode (auto-rebuild on changes)
dotnet watch
```

### Start Development Servers

```bash
# Full stack (SWA emulator)
dotnet run --project src/SwaLauncher/SwaLauncher.csproj

# Frontend only
dotnet run --project src/redmuffin.Blazor.StaticWeb/redmuffin.Blazor.StaticWeb.csproj
```

## Security Notes

- Container runs as non-root `vscode` user
- Docker-in-Docker requires privileged mode
- API keys should be set via environment variables, not committed
- Workspace is mounted for persistence across sessions

## Troubleshooting

### Docker not available

If Docker is not available in the container:

1. Ensure Docker Desktop is running
2. For Windows/Mac: Enable "Expose daemon on tcp://localhost:2375"
3. Restart the container

### MCP servers not working

Ensure Docker is running and accessible:

```bash
docker ps
```

If you see an error, restart the container.

### Port conflicts

If ports are already in use:

```bash
# Check what's using the port
netstat -an | grep 4280

# Change ports in docker-compose.yml if needed
```

## Further Reading

- [VS Code DevContainers](https://code.visualstudio.com/docs/remote/containers)
- [.NET in Containers](https://docs.microsoft.com/en-us/dotnet/core/docker/)
- [Azure Static Web Apps](https://docs.microsoft.com/en-us/azure/static-web-apps/)
