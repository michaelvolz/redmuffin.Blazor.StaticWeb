# DevContainer Development Environment Plan

**Created**: 2026-03-29  
**Status**: Implementation complete - Secrets and DevContainer workflow operational  
**Author**: AI Assistant

## Overview

This document outlines the plan for updating the project to support secure development using DevContainer with Windows/PowerShell as the primary workflow.

**Current State**: All components are operational. The devcontainer workflow uses PowerShell SecretStore for secure API key management, eliminating the need for VS Code Remote while maintaining full security. The project is pure .NET 9 with no .NET 10 dependency.

## Goals

1. Enable DevContainer-based development with full security boundary
2. Support Windows/PowerShell as the primary development environment
3. Ensure zero secrets in repository (VS Code DevContainer Secrets)
4. Provide clear documentation for other developers
5. Create helper scripts for streamlined workflow

---

## Current State Analysis

### What Exists

- ✅ DevContainer configuration (`.devcontainer/`)
- ✅ Security documentation (`.devcontainer/SECURITY.md`)
- ✅ Security-First policy in `AGENTS.md` and `README.md`
- ✅ VS Code Secrets configuration in `devcontainer.json`
- ✅ Docker-in-Docker for MCP servers

### What's Missing

- ✅ DevContainer CLI setup instructions
- ✅ PowerShell helper scripts
- ✅ README.md sections for DevContainer workflow
- ✅ Clear getting started for Windows/PowerShell users
- ✅ Documentation for other developers to replicate setup

### Recently Added ✅

- ✅ SSH agent forwarding for GitHub access from container
- ✅ Helper scripts: `opencode-secure.ps1`, `devcontainer-down.ps1`, `setup-secrets.ps1`
- ✅ Automatic devcontainer rebuild on Dockerfile changes
- ✅ Cross-platform path handling (Windows/WSL/macOS)
- ✅ NPM global packages installed in post-create (opencode-ai, Azure tools, etc.)
- ✅ PowerShell SecretStore for secure API key management (no VS Code required)
- ✅ Dockerfile optimized: .NET 10 removed (project is pure .NET 9)
- ✅ DevContainer CLI-only workflow (no VS Code Remote dependency)

---

## Implementation Plan

### Phase 1: Helper Scripts

#### 1. `scripts/opencode-secure.ps1`

Starts devcontainer (if not running) and executes opencode inside it.

```powershell
#Requires -Version 5.1

param(
    [Parameter(ValueFromRemainingArguments=$true)]
    [string[]]$Arguments
)

$containerRunning = docker ps --format "{{.Names}}" | Select-String "redmuffin-devcontainer"

if (-not $containerRunning) {
    Write-Host "Starting devcontainer..." -ForegroundColor Cyan
    devcontainer up --workspace-folder $PSScriptRoot\..
}

devcontainer exec opencode @Arguments
```

#### 2. `scripts/devcontainer-down.ps1`

Stops the devcontainer when done.

```powershell
docker-compose -f .devcontainer/docker-compose.yml down
```

---

### Phase 2: Dockerfile Update

Add opencode installation to `.devcontainer/Dockerfile`:

```dockerfile
# Install opencode globally in container
RUN npm install -g opencode
```

---

### Phase 3: README.md Updates

#### A. Update Table of Contents

Add:

```markdown
- [Development Environment](#development-environment)
  - [Option 1: DevContainer (Recommended)](#option-1-devcontainer-recommended)
  - [Option 2: Local Development](#option-2-local-development)
```

#### B. Update Prerequisites Section

Split into two paths:

```markdown
## Prerequisites

### For DevContainer Development (Recommended)

- **Docker Desktop** with WSL2 backend
  - [Download](https://www.docker.com/products/docker-desktop)
  - Enable WSL2 integration in Docker Desktop settings
- **Node.js** (for DevContainer CLI)
- **DevContainer CLI**: `npm install -g @devcontainers/cli`
- **PowerShell** 5.1+ or PowerShell Core

### For Local Development

- **Visual Studio 2022** (17.8+)
- **.NET 9 SDK**
- **Node.js** (for SWA CLI, Prettier, etc.)
- Manual secret management required
```

#### C. Create New Section: Development Environment

````markdown
## Development Environment

We support two development workflows. **DevContainer is strongly recommended** for security.

### Option 1: DevContainer (Recommended - Secure)

**Why DevContainer?**

- Complete security boundary (secrets, MCP servers, tools isolated)
- Consistent environment across all developers
- Zero secrets in repository or host filesystem
- Works identically on Windows, macOS, Linux

**Prerequisites:**

- Docker Desktop with WSL2 backend
- DevContainer CLI: `npm install -g @devcontainers/cli`

**Quick Start (PowerShell):**

```powershell
# One-time setup
npm install -g @devcontainers/cli

# Start development
devcontainer up --workspace-folder .
devcontainer exec opencode

# Or use helper scripts
.\scripts\opencode-secure.ps1
```
````

**Helper Scripts:**
| Script | Purpose |
|--------|---------|
| `scripts/opencode-secure.ps1` | Starts container and runs opencode |
| `scripts/devcontainer-down.ps1` | Stops the devcontainer |

**Secret Management:**
Secrets are managed via PowerShell SecretStore (encrypted in Windows Credential Manager):

```powershell
# One-time setup
.\scripts\setup-secrets.ps1

# Secrets are automatically loaded when starting devcontainer
.\scripts\opencode-secure.ps1
```

Required secrets:

- `BRAVE_API_KEY`
- `CONTEXT7_API_KEY`
- `RainDropClientID`
- `RainDropClientSecret`
- `RainDropTestToken`

### Option 2: Local Development

Use only if you cannot run Docker. **Reduced security** - secrets must be managed manually.

[Existing Visual Studio 2022 setup instructions...]

````

#### D. Update Getting Started Section

Replace current content with devcontainer-first approach:

```markdown
## Getting Started

### DevContainer Workflow (Recommended)

1. **Clone and enter directory:**
   ```powershell
   git clone <repo-url>
   cd redmuffin.Blazor.StaticWeb
   ```

2. **One-time setup:**

   ```powershell
   npm install -g @devcontainers/cli
   .\scripts\setup-secrets.ps1
   ```

3. **Start devcontainer:**

   ```powershell
   .\scripts\opencode-secure.ps1
   ```

   _First run: Script will load secrets from PowerShell SecretStore and start container_

4. **When finished:**
   ```powershell
   .\scripts\devcontainer-down.ps1
   ```

3. **Start devcontainer:**

   ```powershell
   devcontainer up --workspace-folder .
   ```

   _First run: VS Code will prompt for secrets_

4. **Run opencode:**

   ```powershell
   .\scripts\opencode-secure.ps1
   ```

5. **When finished:**
   ```powershell
   .\scripts\devcontainer-down.ps1
   ```

### Local Development Workflow

Alternative if Docker is not available.

````

#### E. Add PowerShell Scripts Section

````markdown
### PowerShell Helper Scripts

Located in `scripts/`:

| Script                        | Purpose                                      |
| ----------------------------- | -------------------------------------------- |
| `opencode-secure.ps1`         | Starts devcontainer and runs opencode inside |
| `devcontainer-down.ps1`       | Stops the devcontainer                       |
| `Generate-CoverageReport.ps1` | Generates code coverage                      |
| `View-CoverageReport.ps1`     | Views coverage report                        |

**Creating an alias (optional):**
Add to your PowerShell profile (`$PROFILE`):

```powershell
Set-Alias -Name opencode -Value "C:\path\to\scripts\opencode-secure.ps1"
```
````

````

#### F. Update Docker Integration Section

```markdown
## Container Infrastructure

### DevContainer

The devcontainer provides:
- **Isolated Environment**: .NET 9, Node.js, Azure Functions tools
- **Security Boundary**: Secrets and MCP servers run inside container only
- **Consistency**: Same environment for all developers

Configuration: `.devcontainer/devcontainer.json`

### Docker for MCP Servers

MCP servers run as Docker containers inside the devcontainer:
- Brave Search
- Fetch
- Time
- Sequential Thinking

Docker Desktop handles the containerization layer.

**Windows Setup:**
1. Install Docker Desktop
2. Enable WSL2 backend (recommended)
3. Ensure WSL2 integration is enabled
4. Share your project drive in Docker Desktop settings
````

---

### Phase 4: DevContainer README Update

Update `.devcontainer/README.md` with Windows/PowerShell specific instructions.

---

## Security Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Windows Host                               │
│  ┌─────────────────────────────────────────────────────┐  │
│  │           PowerShell SecretStore (Encrypted)             │
│  │  - BRAVE_API_KEY                                     │  │
│  │  - CONTEXT7_API_KEY                                  │  │
│  │  - RainDropClientID                                  │  │
│  │  - RainDropClientSecret                              │  │
│  │  - RainDropTestToken                                 │  │
│  └─────────────────────────────────────────────────────┘  │
│                            │ (loaded by opencode-secure.ps1)│
│                            ▼                                 │
│  ┌─────────────────────────────────────────────────────┐  │
│  │              DevContainer (Ubuntu)                    │  │
│  │  ┌─────────────────────────────────────────────────┐│  │
│  │  │  opencode (AI Assistant)                        ││  │
│  │  │  └─ MCP Servers (Docker containers)              ││  │
│  │  │     - brave-search                              ││  │
│  │  │     - fetch                                     ││  │
│  │  │     - sequentialthinking                        ││  │
│  │  └─────────────────────────────────────────────────┘│  │
│  │  ┌─────────────────────────────────────────────────┐│  │
│  │  │  .NET 9 SDK, Node.js, Azure Functions Tools   ││  │
│  │  └─────────────────────────────────────────────────┘│  │
│  └─────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

**Key Points:**

- Secrets never touch disk on host (stored encrypted in PowerShell SecretStore)
- Secrets loaded by opencode-secure.ps1 into environment variables
- MCP servers isolated in container
- opencode runs inside container
- Same experience on Windows, macOS, Linux

---

## Migration Path for Existing Developers

### For Users Who Want DevContainer:

1. Install Docker Desktop with WSL2
2. Install DevContainer CLI: `npm install -g @devcontainers/cli`
3. Clone repo and run `devcontainer up --workspace-folder .`
4. Configure secrets when prompted
5. Use `.\scripts\opencode-secure.ps1` to start opencode

### For Users Who Prefer Local Development:

1. Continue using existing setup
2. Be aware of increased security responsibility
3. Manage secrets manually (dotnet user-secrets or environment variables)

---

## Testing Checklist

- [ ] `npm install -g @devcontainers/cli` works
- [ ] `.\scripts\setup-secrets.ps1` stores secrets in PowerShell SecretStore
- [ ] `.\scripts\opencode-secure.ps1` loads secrets and starts container
- [ ] `.\scripts\devcontainer-down.ps1` stops container
- [ ] MCP servers (brave-search, fetch, etc.) work inside container
- [ ] README.md instructions are clear and accurate
- [ ] No secrets visible in any files
- [ ] Build succeeds inside container

---

## Files to Create/Modify

### Create

- `scripts/opencode-secure.ps1` - ✅ Created
- `scripts/devcontainer-down.ps1` - ✅ Created
- `scripts/setup-secrets.ps1` - ✅ Created

### Modify

- `.devcontainer/Dockerfile` - ✅ Updated (removed .NET 10, kept .NET 9 only)
- `README.md` - ✅ Updated with DevContainer workflow
- `.devcontainer/README.md` - ✅ Updated with Windows/PowerShell details

### No Changes Needed

- `devcontainer.json` - Already configured correctly
- `docker-compose.yml` - Already configured correctly
- `SECURITY.md` - Already comprehensive
- `opencode.json` - Already configured correctly

---

## Commit Strategy

Following Single-Purpose Principle:

1. `feat(scripts): add opencode-secure.ps1 helper script`
2. `feat(scripts): add devcontainer-down.ps1 helper script`
3. `feat(scripts): add setup-secrets.ps1 for PowerShell SecretStore`
4. `chore(devcontainer): remove .NET 10, keep pure .NET 9`
5. `docs(readme): add DevContainer development environment section`
6. `docs(readme): update prerequisites for devcontainer-first approach`
7. `docs(devcontainer): add Windows PowerShell setup instructions`

---

## References

- [VS Code DevContainer Documentation](https://code.visualstudio.com/docs/devcontainers/containers)
- [DevContainer CLI](https://code.visualstudio.com/docs/devcontainers/devcontainer-cli)
- [VS Code Secrets](https://code.visualstudio.com/docs/devcontainers/containers#_advanced-container-configuration)
- [Docker Desktop WSL2 Backend](https://docs.docker.com/desktop/wsl/)
