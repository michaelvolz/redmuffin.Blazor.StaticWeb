---
date: 2026-04-20
title: WSL Development Environment Setup Guide
tags: [wsl, development-environment, prerequisites]
description: Complete installation guide for setting up the redmuffin.Blazor.StaticWeb project in WSL
---

# WSL Development Environment Setup Guide

This guide covers setting up the development environment for [redmuffin.Blazor.StaticWeb](https://github.com/michaelvolz/redmuffin.Blazor.StaticWeb) in WSL (Windows Subsystem for Linux).

## ⚠️ Critical Requirement: .NET 9 Only

**This project requires .NET 9 SDK**. While .NET 10 can also be installed alongside .NET 9, this project **cannot build or run with .NET 10 alone**. The solution targets .NET 9 explicitly.

---

## Current Environment Status

### Already Installed

| Tool | Version | Status |
| ---- | ------- | ------ |
| GitHub CLI (gh) | 2.90.0 | ✅ Installed |
| Python | 3.14.4 | ✅ Installed |
| Node.js | 25.9.0 | ✅ Installed |
| npm | 11.12.1 | ✅ Installed |
| RTK (Rust Token Killer) | 0.37.1 | ✅ Installed |

### Needs Installation

| Tool | Install Order | Install Command |
| ---- | ------------ | ---------------- |
| .NET 9 SDK | 1 | See below |
| PowerShell | 2 | See below |
| @devcontainers/cli | 3 | `npm install -g @devcontainers/cli` |
| @azure/static-web-apps-cli | 4 | `npm install -g @azure/static-web-apps-cli` |
| prettier | 5 | `npm install -g prettier` |
| @commitlint/cli | 6 | `npm install -g @commitlint/cli` |
| @commitlint/config-conventional | 6 | `npm install -g @commitlint/config-conventional` |
| chrome-devtools-mcp | 7 | `npm install -g chrome-devtools-mcp` |
| cc-safety-net | 8 | `npm install -g cc-safety-net` |
| code-review-graph | 9 | `pip install code-review-graph` |
| dotnet-reportgenerator-globaltool | 10 | `dotnet tool install --global dotnet-reportgenerator-globaltool` |

---

## Installation Steps

### Step 1: Install .NET 9 SDK

**Critical: .NET 9 is required for this project.**

```bash
# Add Microsoft package repository
wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Update and install .NET 9 SDK
sudo apt update
sudo apt install -y dotnet-sdk-9.0

# Verify installation
dotnet --list-sdks
# Should show: 9.0.x and/or 10.0.x
```

**Important:** Both .NET 9 and .NET 10 can be installed side-by-side. This is supported and recommended.

### Step 2: Install PowerShell

```bash
# Download and install PowerShell
# For Ubuntu 24.04
wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

sudo apt update
sudo apt install -y powershell

# Verify
pwsh --version
```

### Step 3: Install Node.js Global Tools

```bash
# Install all required npm packages
npm install -g @devcontainers/cli
npm install -g @azure/static-web-apps-cli
npm install -g prettier
npm install -g @commitlint/cli @commitlint/config-conventional
npm install -g chrome-devtools-mcp
npm install -g cc-safety-net
```

### Step 4: Install Python Tools

```bash
# Install code-review-graph for AI-assisted code reviews
pip install code-review-graph

# Configure for OpenCode
code-review-graph install
code-review-graph build
```

### Step 5: Install .NET Global Tools

```bash
# Install ReportGenerator for code coverage reports
dotnet tool install --global dotnet-reportgenerator-globaltool
```

### Step 6: Install RTK

RTK (Rust Token Killer) is a CLI proxy that reduces LLM token consumption by 60-90% on common dev commands. Required for optimal OpenCode performance.

```bash
# Quick install script (Linux/Omarchy/WSL)
curl -fsSL https://raw.githubusercontent.com/rtk-ai/rtk/master/install.sh | sh

# Or manual installation
curl -L -o rtk.tar.gz https://github.com/rtk-ai/rtk/releases/latest/download/rtk-x86_64-unknown-linux-musl.tar.gz
tar -xzf rtk.tar.gz
chmod +x rtk
sudo mv rtk /usr/local/bin/rtk

# Verify installation
rtk --version

# Initialize RTK for OpenCode
rtk init -g --opencode

# Restart OpenCode for plugin to load
```

**What RTK Does:**
- Automatically compresses `git status`, `cargo test`, `pnpm list`, etc.
- Reduces token usage by 60-90% for development commands
- Works transparently - no changes needed to your workflow

---

## Verification Steps

After installation, verify everything is working:

```bash
# Verify .NET 9 SDK
dotnet --list-sdks
# Expected output should include 9.0.x

# Verify Node.js and npm
node --version
npm --version

# Verify PowerShell
pwsh --version

# Verify GitHub CLI authentication
gh auth status

# Verify npm global packages
npm list -g --depth=0

# Verify RTK
rtk --version
```

---

## Build and Test

Once all tools are installed, build and test the project:

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test
```

---

## Troubleshooting

### .NET 9 Not Showing Up

If after installing .NET 9, running `dotnet --list-sdks` only shows .NET 10:

1. Check what SDK versions are available:
   ```bash
   ls -la /usr/share/dotnet/sdk/
   ```

2. Ensure the .NET 9 package was installed correctly:
   ```bash
   dpkg -l | grep dotnet-sdk
   ```

3. If only .NET 10 is installed, reinstall .NET 9:
   ```bash
   sudo apt update
   sudo apt install --reinstall dotnet-sdk-9.0
   ```

### npm Global Packages Not Found

If global npm packages are not found, ensure your PATH includes npm global binaries:

```bash
# Add to ~/.bashrc or ~/.zshrc
export PATH="$(npm config get prefix)/bin:$PATH"

# Reload shell
source ~/.bashrc
```

### PowerShell Installation Fails

If PowerShell installation fails on WSL, ensure you have the correct repository added:

```bash
# Check if repository is added
cat /etc/apt/sources.list.d/microsoft-prod.list
```

---

## Next Steps

After setup, see the main [README.md](../README.md) for:
- DevContainer setup (recommended)
- Local development workflow
- Running the application
- PowerShell helper scripts
