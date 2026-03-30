# DevContainer for redmuffin.Blazor.StaticWeb (Isolated)

A fully configured development container with .NET 9, Node.js, Docker-in-Docker, and all required tools for **secure, isolated development**.

## Security Model

This devcontainer provides **total isolation** from the Windows host:

- **No bind mount to Windows files** - workspace lives in Docker volume
- **Only SSH keys mounted** - read-only access to `~/.ssh` for git authentication
- **Repository cloned inside container** - fresh copy each session
- **opencode has zero access to Windows filesystem**

## Features

- **.NET 9 SDK** - All projects use .NET 9
- **Node.js LTS** - For Azure Static Web Apps CLI, Prettier, commitlint
- **Docker-in-Docker** - MCP servers and containerized tools
- **Azure Functions Core Tools** - Local API development
- **Git hooks** - Commit message validation
- **opencode** - AI assistant with full security boundary

## Prerequisites

### Windows Setup (Recommended)

1. **Docker Desktop with WSL2 backend**
   - [Download Docker Desktop](https://www.docker.com/products/docker-desktop/)
   - Enable WSL2 backend during installation
   - Enable WSL2 integration in Docker Desktop → Settings → Resources → WSL Integration

2. **DevContainer CLI**

   ```powershell
   npm install -g @devcontainers/cli
   ```

3. **PowerShell** 5.1+ or PowerShell Core

### macOS/Linux Setup

1. **Docker Desktop** or Docker Engine
2. **DevContainer CLI**: `npm install -g @devcontainers/cli`
3. **Terminal**: bash, zsh, or PowerShell

## Quick Start

### Option 1: Using DevContainer CLI (Recommended for Terminal)

This method provides the full security boundary and works with opencode:

```powershell
# One-time setup
npm install -g @devcontainers/cli

# Clone repo to a temp location (NOT inside the devcontainer!)
# You only need the .devcontainer folder, not the full repo
git clone git@github.com:michaelvolz/redmuffin.Blazor.StaticWeb.git temp-devcontainer
cp -r temp-devcontainer/.devcontainer ./
rm -rf temp-devcontainer

# Start devcontainer - repository is CLONED inside container
devcontainer up --workspace-folder .

# Run opencode (secrets injected automatically)
devcontainer exec opencode

# Or use helper script
.\scripts\opencode-secure.ps1

# When finished
.\scripts\devcontainer-down.ps1
```

> **Important**: You only need to copy the `.devcontainer` folder. The actual code is cloned INSIDE the container for isolation.

### Using for Multiple Repositories

This devcontainer can be copied to any repository. Volume names are automatically derived from the **folder name** using `${localWorkspaceFolderBasename}`:

#### Step 1: Clone to Correct Folder Name

Clone your repository with a kebab-case folder name (no dots):

```powershell
# Good: kebab-case folder name
git clone git@github.com:michaelvolz/redmuffin.Blazor.StaticWeb.git redmuffin-blazor-staticweb
cd redmuffin-blazor-staticweb

# Copy the .devcontainer folder
cp -r .devcontainer ../
cd ..
```

#### Step 2: Run Devcontainer

```powershell
# Folder name determines volume names automatically:
# redmuffin-blazor-staticweb-workspace
# redmuffin-blazor-staticweb-cache
# redmuffin-blazor-staticweb-npm
# redmuffin-blazor-staticweb-nuget

devcontainer up --workspace-folder .

# Or use helper script
.\scripts\opencode-secure.ps1
```

**Only ONE change needed**: Update `GIT_CLONE_URL` in `devcontainer.json` to match your repository.

```

devcontainer-cache → your-repo-name-cache
devcontainer-npm → your-repo-name-npm
devcontainer-nuget → your-repo-name-nuget

```

**post-create.sh** - Update the git clone URL:

```bash
git clone git@github.com:YOUR_USERNAME/YOUR_REPO.git .
```

redmuffin-workspace → your-repo-name-workspace
redmuffin.Blazor.StaticWeb → your-repo-name

```

**docker-compose.yml** - Replace:

```

devcontainer-cache → your-repo-name-cache
devcontainer-npm → your-repo-name-npm
devcontainer-nuget → your-repo-name-nuget

````

#### Step 2: One-Liner to Update Names

```powershell
# Run from your repo root (replace MYREPO with your repo name)
$REPO = "myrepo"
(Get-Content .devcontainer/devcontainer.json) -replace 'redmuffin-workspace',"$REPO-workspace" -replace 'redmuffin.Blazor.StaticWeb',$REPO | Set-Content .devcontainer/devcontainer.json
(Get-Content .devcontainer/docker-compose.yml) -replace 'devcontainer-cache',"$REPO-cache" -replace 'devcontainer-npm',"$REPO-npm" -replace 'devcontainer-nuget',"$REPO-nuget" | Set-Content .devcontainer/docker-compose.yml
````

#### Why This Matters

| Component        | Why Unique                                  |
| ---------------- | ------------------------------------------- |
| Workspace volume | Each repo needs its own isolated filesystem |
| Cache volumes    | NuGet/npm caches are repo-specific          |
| Container name   | Prevents conflicts when running multiple    |

The `opencode-secure.ps1` script automatically finds the correct container by project path label, so no changes needed there.

### Option 2: VS Code (GUI)

1. Open the project in VS Code
2. Install the **Remote - Containers** extension
3. Click the green button in the bottom-left corner
4. Select "Reopen in Container"

The container will build automatically on first open.

### Option 3: Docker Compose (Terminal Only)

```powershell
# Build and start the container
docker-compose -f .devcontainer/docker-compose.yml up -d

# Run commands inside container
docker exec -it redmuffin-devcontainer bash

# Run opencode
docker exec -it redmuffin-devcontainer opencode
```

## Secret Management

Secrets are managed via **VS Code DevContainer Secrets**:

### First Run Setup

1. When the container starts for the first time, VS Code will prompt for secrets
2. Enter the required secrets when prompted
3. Secrets are stored in Windows Credential Manager (or macOS Keychain/Linux libsecret)
4. Secrets are injected as environment variables inside the container

### Required Secrets

| Secret                   | Description                 | Get from                                                                   |
| ------------------------ | --------------------------- | -------------------------------------------------------------------------- |
| `BRAVE_API_KEY`          | Brave Search API Key        | [brave.com/search/api](https://brave.com/search/api/)                      |
| `CONTEXT7_API_KEY`       | Context7 API Key (optional) | [context7.com](https://context7.com)                                       |
| `RAINDROP_CLIENT_ID`     | Raindrop.io Client ID       | [app.raindrop.io](https://app.raindrop.io/settings/integrations/developer) |
| `RAINDROP_CLIENT_SECRET` | Raindrop.io Client Secret   | [app.raindrop.io](https://app.raindrop.io/settings/integrations/developer) |

### No .env Files Required

Unlike traditional development, you do **not** need to create `.env` files. Secrets are automatically injected from VS Code's secure storage.

## Ports

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
- opencode (AI assistant)

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

### Using opencode

```powershell
# Start opencode with full security
.\scripts\opencode-secure.ps1

# With arguments
.\scripts\opencode-secure.ps1 "create component Button"
```

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

## PowerShell Helper Scripts

Located in `scripts/`:

| Script                        | Purpose                                   |
| ----------------------------- | ----------------------------------------- |
| `opencode-secure.ps1`         | Starts container and runs opencode inside |
| `devcontainer-down.ps1`       | Stops the devcontainer                    |
| `Generate-CoverageReport.ps1` | Generates code coverage                   |
| `View-CoverageReport.ps1`     | Views coverage report                     |

### Creating an Alias

Add to your PowerShell profile (`$PROFILE`):

```powershell
# Open PowerShell profile for editing
notepad $PROFILE

# Add this line:
Set-Alias -Name opencode -Value "C:\path\to\scripts\opencode-secure.ps1"
```

Then simply use `opencode` from any PowerShell terminal.

## Security Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Windows Host                               │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Windows Credential Manager / SSH Keys               │   │
│  │  - BRAVE_API_KEY, CONTEXT7_API_KEY                  │   │
│  │  - RAINDROP_CLIENT_ID/SECRET                        │   │
│  │  - ~/.ssh (read-only mount)                         │   │
│  └─────────────────────────────────────────────────────┘   │
│                            │                                 │
│                            ▼ (read-only SSH keys only)       │
│  ┌─────────────────────────────────────────────────────┐   │
│  │         DevContainer (Ubuntu) Volume                 │   │
│  │  ┌─────────────────────────────────────────────────┐ │   │
│  │  │  opencode (AI Assistant)                        │ │   │
│  │  │  └─ MCP Servers (Docker containers)             │ │   │
│  │  │     - brave-search, fetch, sequentialthinking  │ │   │
│  │  └─────────────────────────────────────────────────┘ │   │
│  │  ┌─────────────────────────────────────────────────┐ │   │
│  │  │  .NET 9, Node.js, Azure Functions Tools        │ │   │
│  │  └─────────────────────────────────────────────────┘ │   │
│  │  ┌─────────────────────────────────────────────────┐ │   │
│  │  │  Repository (cloned from GitHub)                │ │   │
│  │  │  - NO access to Windows filesystem              │ │   │
│  │  └─────────────────────────────────────────────────┘ │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

## Security Notes

- Container runs as non-root `vscode` user
- Docker-in-Docker requires privileged mode
- **Zero secrets in repository** - all secrets via VS Code Secrets
- Secrets stored in OS credential manager, not on disk
- **Workspace is Docker volume** - isolated from Windows filesystem
- **Repository cloned inside container** - opencode cannot access Windows files
- **Only ~/.ssh is mounted** - read-only, for git authentication only

## Troubleshooting

### Docker not available

If Docker is not available in the container:

1. Ensure Docker Desktop is running
2. For Windows: Ensure WSL2 integration is enabled
3. For macOS: Enable "Expose daemon on tcp://localhost:2375"
4. Restart the container

### MCP servers not working

Ensure Docker is running and accessible:

```bash
docker ps
```

If you see an error, restart the container.

### Port conflicts

If ports are already in use:

```powershell
# Check what's using the port (Windows)
netstat -an | Select-String "4280"

# Or (Linux/macOS)
netstat -an | grep 4280

# Change ports in docker-compose.yml if needed
```

### First run prompts for secrets repeatedly

If VS Code keeps prompting for secrets:

1. Run "Dev Containers: Configure Secrets" from Command Palette
2. Rebuild the container: "Dev Containers: Rebuild Container"

## Further Reading

- [VS Code DevContainers](https://code.visualstudio.com/docs/remote/containers)
- [DevContainer CLI](https://code.visualstudio.com/docs/devcontainers/devcontainer-cli)
- [.NET in Containers](https://docs.microsoft.com/en-us/dotnet/core/docker/)
- [Azure Static Web Apps](https://docs.microsoft.com/en-us/azure/static-web-apps/)
- [SECURITY.md](./SECURITY.md) - Detailed security documentation
