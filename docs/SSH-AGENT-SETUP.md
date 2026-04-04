---
title: SSH Agent Setup for DevContainer
date: 2026-03-30
---

This document describes how to configure SSH access for the devcontainer, enabling Git operations (push/pull) from within the container.

## Overview

The devcontainer uses a **secure key mounting approach** for SSH authentication:

- SSH keys are **mounted read-only** from the Windows host
- Keys are **copied into the container** with proper permissions at runtime
- A **container-local ssh-agent** is started to hold decrypted keys in memory
- Keys are **never stored in Docker images**

This approach balances security and convenience when using the DevContainer CLI on Windows.

## Architecture

```
Windows Host (Your Machine)
├── SSH Private Keys (C:\Users\<name>\.ssh\id_*)
└── DevContainer CLI
    └── Mounts .ssh directory READ-ONLY to /mnt/host-ssh
        └── Container setup-ssh.sh
            ├── Copies keys from /mnt/host-ssh to ~/.ssh/
            ├── Sets proper Unix permissions (600 for private keys)
            ├── Starts ssh-agent inside container
            └── Adds keys to container's agent (in memory only)
```

**Security Features:**

1. **Read-Only Mount**: Keys are mounted read-only from host
2. **Runtime Copy**: Keys only exist in running container (not in images)
3. **Proper Permissions**: Copied keys have Unix permissions 600 (owner only)
4. **Memory-Only Agent**: Decrypted keys held in memory by ssh-agent
5. **Ephemeral**: Keys disappear when container stops

## Prerequisites

### Windows Requirements

- Windows 10/11 with OpenSSH client installed
- SSH key pair generated and added to GitHub
- Keys stored in `C:\Users\<username>\.ssh\`

### Verify OpenSSH Installation

```powershell
# Check if OpenSSH is installed
Get-WindowsCapability -Online | Where-Object Name -like 'OpenSSH*'

# Should show:
# Name  : OpenSSH.Client~~~~0.0.1.0
# State : Installed
```

If not installed:

```powershell
# Install OpenSSH client
Add-WindowsCapability -Online -Name OpenSSH.Client~~~~0.0.1.0
```

## Quick Setup

### Step 1: Ensure SSH Keys Exist

Your SSH keys should be in the standard Windows location:

```
C:\Users\<username>\.ssh\
├── id_ed25519      (private key)
├── id_ed25519.pub  (public key)
└── config          (optional)
```

Generate keys if needed:

```powershell
# Generate Ed25519 key (recommended)
ssh-keygen -t ed25519 -C "your-email@example.com"

# Add to GitHub: https://github.com/settings/keys
```

### Step 2: Start DevContainer

```powershell
# Navigate to project
cd B:\redmuffin.Blazor.StaticWeb

# Start devcontainer
.\scripts\opencode-secure.ps1
```

The container will automatically:

1. Mount your `.ssh` directory read-only at `/mnt/host-ssh`
2. Copy SSH keys to container's `~/.ssh/` with proper permissions
3. Start ssh-agent inside the container
4. Add keys to the container's agent

### Step 3: Verify SSH in Container

```bash
# Inside the container, check SSH
ssh-add -l

# Should output:
# 256 SHA256:xxxxxxxxxxx michaelvolz@github.com (ED25519)

# Test GitHub connection
ssh -T git@github.com

# Should output:
# Hi username! You've successfully authenticated...
```

## How It Works

### Mount Configuration

The `.devcontainer/devcontainer.json` includes:

```json
"mounts": [
  "source=${localEnv:USERPROFILE}/.ssh,target=/mnt/host-ssh,type=bind,readonly"
]
```

This mounts your Windows `.ssh` directory read-only at `/mnt/host-ssh` inside the container.

### Automatic Setup Script

The `.devcontainer/setup-ssh.sh` script runs automatically when the container starts:

1. **Mounts Check**: Verifies SSH directory is mounted at `/mnt/host-ssh`
2. **Local Directory**: Creates `~/.ssh` directory in container with 700 permissions
3. **Copy Keys**: Copies private and public keys from mount to local directory
4. **Set Permissions**: Sets 600 on private keys, 644 on public keys
5. **Start Agent**: Launches ssh-agent and adds keys
6. **Persist**: Adds SSH agent environment to `.bashrc` for new shells

### Key Locations

**Windows Host (Source):**

- Private keys: `C:\Users\<username>\.ssh\id_*`
- Public keys: `C:\Users\<username>\.ssh\*.pub`
- Config: `C:\Users\<username>\.ssh\config`

**Inside Container (Runtime Copy):**

- Private keys: `~/.ssh/id_*` (permissions: 600)
- Public keys: `~/.ssh/*.pub` (permissions: 644)
- Config: `~/.ssh/config`
- Agent socket: `$SSH_AUTH_SOCK` (set automatically)

## Security Model

### Why Mount + Copy?

You might wonder why we mount keys read-only and then copy them, rather than just mounting them directly to `~/.ssh`. Here's why:

1. **Read-Only Mount**: The host directory is mounted read-only for security
2. **Agent Requires Write**: ssh-agent needs to create a socket file in the `.ssh` directory
3. **Copy Solution**: By copying to a local directory, we get:
   - Security of read-only source
   - Functionality of writeable destination
   - Proper Unix permissions (Windows ACLs don't translate well)

### Security Benefits

- **No Image Contamination**: Keys are never in Docker images
- **Isolated Copies**: Each container gets its own copy
- **Permission Enforcement**: Unix permissions ensure only owner can read private keys
- **Automatic Cleanup**: Keys disappear when container stops
- **No Network Exposure**: Keys never leave the local machine

## Troubleshooting

### Issue: "Host SSH directory not mounted"

**Cause:** SSH keys not in standard Windows location.

**Solution:**

Ensure keys are at `C:\Users\<username>\.ssh\`. If they're elsewhere:

```powershell
# Create the directory and move keys
mkdir -Force $HOME\.ssh
# Copy your keys to this location
```

### Issue: "No SSH keys found in mounted directory"

**Cause:** Keys have different naming or location.

**Solution:**

Check what keys exist:

```bash
# Inside container
ls -la /mnt/host-ssh/
```

The script looks for `id_*` files. If your keys have different names:

```bash
# Manually copy specific keys
cp /mnt/host-ssh/my_custom_key ~/.ssh/
chmod 600 ~/.ssh/my_custom_key
ssh-add ~/.ssh/my_custom_key
```

### Issue: "Permission denied (publickey)" from container

**Cause:** Keys copied but not added to agent, or wrong permissions.

**Solution:**

```bash
# Fix permissions
chmod 700 ~/.ssh
chmod 600 ~/.ssh/id_*
chmod 644 ~/.ssh/*.pub

# Restart agent
killall ssh-agent 2>/dev/null
eval "$(ssh-agent -s)"
ssh-add ~/.ssh/id_*

# Test
ssh -T git@github.com
```

### Issue: Passphrase prompt every time

**Cause:** ssh-agent not persisting between new shell sessions.

**Solution:**

The setup script automatically adds SSH agent configuration to `~/.bashrc`. Check if it's there:

```bash
# Check if already in .bashrc
grep "SSH_AUTH_SOCK" ~/.bashrc

# If missing, add manually:
cat >> ~/.bashrc << 'EOF'

# SSH Agent auto-start
if [ -z "$SSH_AUTH_SOCK" ]; then
    eval "$(ssh-agent -s)" > /dev/null
    ssh-add ~/.ssh/id_* 2>/dev/null
fi
EOF
```

### Issue: Git still asks for password

**Cause:** Repository using HTTPS instead of SSH.

**Solution:**

```bash
# Check remote URL
git remote -v

# If it shows https://, switch to SSH
git remote set-url origin git@github.com:username/repo.git

# Verify
git remote -v
```

## Comparison: Authentication Methods

| Method                            | Security  | Convenience | Setup              |
| --------------------------------- | --------- | ----------- | ------------------ |
| **Mount + Copy (This Setup)**     | Good      | High        | Automatic          |
| VS Code Remote + Agent Forwarding | Excellent | High        | Requires VS Code   |
| HTTPS + Credential Manager        | Good      | Medium      | Manual token setup |
| Manual Key Copy                   | Poor      | Low         | Manual             |

## Alternatives

### Option 1: VS Code Remote (Most Secure)

If you have VS Code installed:

1. Open project in VS Code
2. Install "Remote - Containers" extension
3. Press F1 → "Remote-Containers: Reopen in Container"
4. VS Code will automatically forward your SSH agent (no key copying needed)

### Option 2: HTTPS with Git Credential Manager

If you prefer not to use SSH:

```bash
# Switch to HTTPS
git remote set-url origin https://github.com/username/repo.git

# Git will use Windows Credential Manager for authentication
```

## Verification Checklist

Before using Git from the devcontainer:

- [ ] SSH keys exist in `C:\Users\<name>\.ssh\` on Windows
- [ ] Keys are added to GitHub (https://github.com/settings/keys)
- [ ] Container started and setup-ssh.sh ran successfully
- [ ] `ls ~/.ssh/` shows copied keys
- [ ] `ssh-add -l` shows loaded keys
- [ ] `ssh -T git@github.com` succeeds
- [ ] Can push/pull with `git push origin main`

## References

- [GitHub SSH Documentation](https://docs.github.com/en/authentication/connecting-to-github-with-ssh)
- [OpenSSH Windows Documentation](https://docs.microsoft.com/en-us/windows-server/administration/openssh/openssh_overview)
- [DevContainer SSH Agent Forwarding](https://code.visualstudio.com/docs/devcontainers/containers#_using-ssh-keys)
- [DevContainer CLI Issue #441](https://github.com/devcontainers/cli/issues/441) - SSH agent on Windows
