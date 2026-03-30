# Security Guide for redmuffin.Blazor.StaticWeb

> **ZERO TOLERANCE**: This repository MUST NEVER contain a single secret. No exceptions.

## Overview

This document describes the security architecture for managing secrets in this project, including the devcontainer setup, MCP server configurations, and CI/CD pipelines.

---

## Core Principle: No Secrets in Files

**The repository must never contain any secrets in any file.** This includes:

- API keys or tokens
- Passwords or credentials
- Database connection strings
- Private keys (SSH, GPG, etc.)
- Session tokens or refresh tokens
- Any sensitive configuration values

### Why This Matters

Even in private repositories:

1. **Git history is forever** - Deleted secrets can be recovered from history
2. **Automated scanners exist** - GitHub, GitLab, and external services scan for secrets 24/7
3. **Insider threats** - Repository access doesn't mean secret access should be granted
4. **Supply chain attacks** - Secrets in repo can be used to attack downstream systems
5. **Compliance violations** - Many standards (SOC2, HIPAA, GDPR) require secrets management

---

## Secret Management Methods

### 1. VS Code DevContainer Secrets (Recommended for Local Development)

**Best for**: Individual developers working in VS Code

**How it works**:

1. VS Code prompts for secrets on first container start
2. Secrets stored in OS credential manager (macOS Keychain, Windows Credential Manager, Linux libsecret)
3. Secrets injected as environment variables inside container at runtime
4. Never written to disk, never in shell history

**Configuration** in `.devcontainer/devcontainer.json`:

```json
{
  "secrets": {
    "BRAVE_API_KEY": {
      "description": "Brave Search API Key",
      "documentationUrl": "https://brave.com/search/api/"
    },
    "CONTEXT7_API_KEY": {
      "description": "Context7 API Key (optional)",
      "documentationUrl": "https://context7.com"
    },
    "RAINDROP_CLIENT_ID": {
      "description": "Raindrop.io Client ID",
      "documentationUrl": "https://app.raindrop.io/settings/integrations/developer"
    },
    "RAINDROP_CLIENT_SECRET": {
      "description": "Raindrop.io Client Secret",
      "documentationUrl": "https://app.raindrop.io/settings/integrations/developer"
    }
  }
}
```

**How to add new secrets**:

1. Edit `.devcontainer/devcontainer.json` and add to `secrets` block
2. Rebuild the container (VS Code will prompt for the new secret)
3. Reference using `${env:SECRET_NAME}` in MCP configs

### 2. GitHub Repository Secrets (CI/CD Only)

**Best for**: GitHub Actions workflows

**How to configure**:

1. Go to Repository Settings → Secrets and variables → Actions
2. Add new repository secret
3. Reference in workflows using `${{ secrets.SECRET_NAME }}`

**Example workflow usage**:

```yaml
# .github/workflows/deploy.yml
jobs:
  deploy:
    env:
      Values__RainDropClientId: ${{ secrets.RAINDROP_CLIENT_ID }}
      Values__RainDropClientSecret: ${{ secrets.RAINDROP_CLIENT_SECRET }}
```

### 3. Environment Variables (MCP Configs)

**Best for**: MCP server configurations that need secrets at runtime

**Syntax**:

- In `.mcp.json`: `"env": { "KEY": "${env:VARIABLE_NAME}" }`
- In `opencode.json`: `"environment": { "KEY": "{env:VARIABLE_NAME}" }`

**Example MCP config**:

```json
{
  "servers": {
    "brave-search": {
      "command": "docker",
      "args": ["run", "-i", "--rm", "-e", "BRAVE_API_KEY", "mcp/brave-search"],
      "env": {
        "BRAVE_API_KEY": "${env:BRAVE_API_KEY}"
      }
    }
  }
}
```

### 4. Azure Key Vault (Production)

**Best for**: Production deployments requiring enterprise secret management

**How to use**:

```bash
# Install Azure CLI
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# Authenticate
az login

# Retrieve secret at runtime
export RAINDROP_CLIENT_SECRET=$(az keyvault secret show \
  --name raindrop-client-secret \
  --vault-name my-vault \
  --query value -o tsv)
```

### 5. .NET User Secrets (Local Development Only)

**Best for**: Quick local development without devcontainer

```bash
# Set a secret
dotnet user-secrets set "RainDrop:ClientId" "your-client-id"

# List secrets
dotnet user-secrets list

# Clear all secrets
dotnet user-secrets clear
```

**Note**: User secrets are stored in `~/.microsoft/userscrets/<project-hash>/secrets.xml` and should never be committed.

---

## What NOT To Do

### NEVER Do These

1. **Never hardcode secrets in any file**

   ```json
   // WRONG
   { "api_key": "sk-xxxxxxxxxxxx" }

   // CORRECT
   { "api_key": "${env:API_KEY}" }
   ```

2. **Never commit .env files with values**

   ```bash
   # WRONG - .env with real values
   GITHUB_TOKEN=ghp_xxxxxxxxxxxx
   BRAVE_API_KEY=BSAxxxxxxxxxxxx

   # CORRECT - .env.example with empty values (gitignored)
   GITHUB_TOKEN=
   BRAVE_API_KEY=
   ```

3. **Never use secrets in Dockerfile ARG or ENV**

   ```dockerfile
   # WRONG
   ARG API_KEY
   ENV API_KEY=$API_KEY

   # CORRECT - Pass at runtime only
   docker run -e API_KEY=$API_KEY myimage
   ```

4. **Never print secrets to logs or stdout**

   ```bash
   # WRONG
   echo "Token: $GITHUB_TOKEN"

   # CORRECT
   echo "Token configured: $([ -n "$GITHUB_TOKEN" ] && echo 'yes' || echo 'no')"
   ```

5. **Never share secrets via chat/email/slack**
   - Use secure secret sharing tools if necessary
   - Prefer rotating secrets rather than sharing

6. **Never store secrets in Docker image layers**
   - Even deleted secrets remain in image history
   - Use runtime injection or secrets management

---

## DevContainer Security Architecture

### Security Layers

```
┌─────────────────────────────────────────────────────────────┐
│                    Developer Workstation                     │
│  ┌───────────────────────────────────────────────────────┐  │
│  │              OS Credential Manager                      │  │
│  │  ┌─────────────────────────────────────────────────┐ │  │
│  │  │  VS Code Secrets Storage (Encrypted)             │ │  │
│  │  │  - BRAVE_API_KEY                                 │ │  │
│  │  │  - RAINDROP_CLIENT_ID                            │ │  │
│  │  │  - RAINDROP_CLIENT_SECRET                        │ │  │
│  │  └─────────────────────────────────────────────────┘ │  │
│  │                            │                           │  │
│  │  ┌────────────────────────┴───────────────────────┐  │  │
│  │  │  ~/.ssh (read-only mount)                        │  │  │
│  │  │  - Only for git authentication                   │  │  │
│  │  │  - Container CANNOT write to this                 │  │  │
│  │  └─────────────────────────────────────────────────┘ │  │
│  └───────────────────────────────────────────────────────┘  │
│                            │                                 │
│                            ▼ (environment variables only)    │
│  ┌───────────────────────────────────────────────────────┐  │
│  │              DevContainer (Ubuntu) Volume              │  │
│  │  ┌─────────────────────────────────────────────────┐ │  │
│  │  │  Repository (cloned inside container)            │ │  │
│  │  │  - NO access to Windows filesystem               │ │  │
│  │  │  - Volume persists across sessions               │ │  │
│  │  └─────────────────────────────────────────────────┘ │  │
│  │  ┌─────────────────────────────────────────────────┐ │  │
│  │  │  Environment Variables (In-Memory Only)         │ │  │
│  │  │  - $BRAVE_API_KEY                                │ │  │
│  │  │  - $RAINDROP_CLIENT_ID                           │ │  │
│  │  └─────────────────────────────────────────────────┘ │  │
│  │                            │                           │  │
│  │                            ▼                           │  │
│  │  ┌─────────────────────────────────────────────────┐ │  │
│  │  │  MCP Servers (Docker containers)                 │ │  │
│  │  │  - Receive secrets via environment               │ │  │
│  │  └─────────────────────────────────────────────────┘ │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### Security Properties

- **Total Windows isolation**: Workspace in Docker volume, NOT bind-mounted
- **Standalone devcontainer.json**: No docker-compose needed - uses Dev Container spec
- **Folder-based volume naming**: `${localWorkspaceFolderBasename}` creates unique volumes per repo
- **Repository cloned inside**: opencode cannot access any Windows files
- **SSH keys read-only**: Container can read keys but cannot modify them
- **Secrets never touch disk**: Environment variables are in-memory only
- **Isolation**: Each MCP server gets only the secrets it needs
- **No persistence**: Container restart clears all secret values
- **Audit trail**: VS Code tracks which secrets are configured

### Container Security Settings

```json
{
  "containerUser": "vscode",
  "securityOpt": ["seccomp=unconfined"],
  "privileged": true
}
```

**Rationale**:

- `containerUser: vscode` - Non-root execution
- `privileged: true` - Required for Docker-in-Docker (MCP servers)
- `securityOpt` - Required for seccomp restrictions with DinD

---

## Troubleshooting

### Secret Not Available in Container

1. Verify VS Code prompted for the secret on container start
2. Check `devcontainer.json` includes the secret definition
3. Rebuild container: "Rebuild Container" command in VS Code

### MCP Server Can't Find Secret

1. Verify secret is defined in `devcontainer.json`
2. Verify MCP config uses `${env:SECRET_NAME}` (not hardcoded)
3. Check environment: `echo $SECRET_NAME` in container terminal
4. Rebuild container if secret was recently added

### Docker-in-Docker Not Working

1. Verify Docker Desktop is running on host
2. Check `docker ps` works inside container
3. Verify container has `privileged: true` in devcontainer.json

### Accidentally Committed a Secret

**IMMEDIATE ACTIONS**:

1. **Rotate the secret** - Generate new key/token in the service
2. **Remove from git history**:

   ```bash
   # Using git-filter-repo (recommended)
   git filter-repo --path .env --invert-paths

   # Or using git-filter-branch
   git filter-branch --force --index-filter \
     'git rm --cached --ignore-unmatch .env' \
     --prune-empty --tag-name-filter cat -- --all
   ```

3. **Force push** (coordinate with team):
   ```bash
   git push origin --force --all
   git push origin --force --tags
   ```
4. **Audit access** - Check who cloned repo during exposure period
5. **Notify security team** if corporate environment

---

## Compliance and Standards

This project adheres to security best practices for:

- **SOC 2**: No secrets in code, proper access controls
- **GDPR**: Personal data (API keys) properly protected
- **PCI DSS**: Payment-related secrets never stored
- **HIPAA**: Health-related data properly secured

---

## Security Checklist

Before any commit or pull request:

- [ ] No API keys, tokens, or secrets in changed files
- [ ] No `password`, `secret`, `token`, `key`, `credential`, `auth` with visible values
- [ ] Config files use `${env:VAR}` or `${input:VAR}` syntax only
- [ ] `.gitignore` includes `.env`, `secrets/`, `*.local.json`
- [ ] New secrets use approved secret management methods
- [ ] Test configs use mock data, not real credentials

---

## Reporting Security Issues

If you discover a security vulnerability or secret exposure:

1. **Do not open a public GitHub issue**
2. **Email maintainer directly** (see repository contacts)
3. **Include**:
   - Description of the issue
   - File/line number where exposure occurred
   - Steps to reproduce
   - Potential impact assessment

---

## References

- [OWASP Secrets Management Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Secrets_Management_Cheat_Sheet.html)
- [GitHub Secret Scanning](https://docs.github.com/en/code-security/secret-scanning)
- [VS Code DevContainer Secrets](https://code.visualstudio.com/docs/devcontainers/containers#_advanced-container-configuration)
- [12-Factor App: Config](https://12factor.net/config)
