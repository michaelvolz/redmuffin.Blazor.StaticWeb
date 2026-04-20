---
title: WSL Chrome DevTools Setup Changes
date: 2026-04-20
tags: [wsl, chrome-devtools, mcp, networking]
---

# WSL Chrome DevTools Setup - System Changes

This document tracks all changes made to enable Chrome DevTools MCP from WSL2 to connect to Windows Brave/Chrome.

## Date

2026-04-20

## Changes Made

### 1. Windows Host (.wslconfig)

**File:** `C:\Users\flynn\.wslconfig`

**Added:**

```ini
[wsl2]
networkingMode=mirrored
```

**Purpose:** Make WSL share Windows network stack so localhost points to same interface.

**To undo:** Remove `networkingMode=mirrored` line.

---

### 2. Windows Host (Port Proxy)

**Command run in PowerShell (Admin):**

```powershell
netsh interface portproxy add v4tov4 listenport=9222 listenaddress=192.168.80.1 connectport=9222 connectaddress=127.0.0.1
```

**Purpose:** Forward WSL gateway IP (192.168.80.1) to Windows localhost:9222.

**To undo:**

```powershell
netsh interface portproxy reset
```

---

### 3. Windows Host (Brave Remote Debugging)

**Action:** Enabled remote debugging in Brave browser UI:

- Opened `brave://inspect/#remote-debugging`
- Checked "Allow remote debugging"

**Also tried (may have changed):**

```powershell
Start-Process "C:\Program Files\BraveSoftware\Brave-Browser\Application\brave.exe" -ArgumentList "-remote-debugging-port=9223","-user-data-dir=$env:LOCALAPPDATA\BraveDebug"
```

**To undo:** Uncheck the remote debugging option in Brave settings.

---

### 4. WSL - Mise Node Removal

**Files/Directories removed:**

- `~/.local/share/mise/installs/node/` - Removed entire node installation directory

**Purpose:** Remove mise-managed node to use system node via Pacman per DHH/Omarchy principles.

**To undo:** Reinstall mise node if needed (will reinstall via pacman hook automatically).

---

### 5. WSL - Mise Config

**File:** `~/.config/mise/config.toml`

**Changed from:**

```toml
[tools]
node = "latest"
```

**To:**

```toml
[tools]
# node = "latest"  # disabled - use system node via pacman

[settings]
disable_tools = ["node"]
```

**Purpose:** Disable mise from managing node, use system node.

**To undo:** Revert to original config.

---

### 6. WSL - Global npm chrome-devtools-mcp

**Installed:**

```bash
sudo npm install -g chrome-devtools-mcp
```

**Purpose:** Install chrome-devtools-mcp globally for system node.

**To undo:**

```bash
sudo npm uninstall -g chrome-devtools-mcp
```

---

### 7. OpenCode Config

**File:** `~/.config/opencode/opencode.json`

**Current chrome-devtools MCP config:**

```json
"chrome-devtools": {
  "type": "local",
  "command": [
    "npx",
    "-y",
    "chrome-devtools-mcp",
    "--browserUrl",
    "http://192.168.80.1:9222"
  ],
  "enabled": true
}
```

**Previous attempts (may need to undo):**

- Originally used custom launch script `~/.config/opencode/scripts/mcp/chrome-devtools-launch.mjs`
- Tried `wsl-chrome-mcp-bridge` package
- Tried `wsl-chrome-mcp-bridge` with `--chrome-path`

**To undo:** Restore original config with custom launch script if needed.

---

### 8. Launch Script

**File:** `~/.config/opencode/scripts/mcp/chrome-devtools-launch.mjs`

**Status:** Created/modified but currently NOT in use (using chrome-devtools-mcp directly).

**Purpose:** Was supposed to:

- Detect WSL
- Query Windows registry for Brave path
- Convert to WSL path
- Launch chrome-devtools-mcp

**To undo:** Can delete or restore from git if needed.

---

### 9. AGENTS.md

**File:** `~/.config/opencode/AGENTS.md`

**Added section:**

```markdown
## Path Configuration (All Configs)

- **Always use `~` as base path** for user home directories (works on Windows, WSL, and Linux)
- **Use forward slashes `/`** in all configuration paths, never backslashes
- Applies to: MCP tools, VS Code settings, shell configs, scripts, or any config referencing user folders
- Example: `~/.config/opencode/scripts/mcp/chrome-devtools-launch.mjs` not `C:\Users\flynn\...` or `/home/flynn/...`
```

**Purpose:** Document path conventions for cross-platform configs.

**To undo:** Remove the added section.

---

## Current Status (2026-04-20)

- ✅ Chrome DevTools MCP now working - using Chromium in WSL!
- Chromium installed via pacman (already present)
- MCP configured to connect via localhost:9222
- No WSL ↔ Windows networking required

---

### 10. WSL - Chromium in WSL (SOLUTION IMPLEMENTED)

**Installed:** Chromium already available in Arch pacman

**Verification:**

```bash
$ chromium --version
Chromium 147.0.7727.101 Arch Linux
```

**Started with remote debugging:**

```bash
chromium --remote-debugging-port=9222 --no-sandbox --disable-gpu &
```

**MCP config updated to localhost:**

```json
"chrome-devtools": {
  "type": "local",
  "command": [
    "npx",
    "-y",
    "chrome-devtools-mcp",
    "--browserUrl",
    "http://localhost:9222"
  ],
  "enabled": true
}
```

**To undo:**

1. Kill chromium process
2. Revert MCP config to use Windows bridge IP (192.168.80.1)
3. Optionally remove chromium: `sudo pacman -R chromium`

---

## References

- Chrome DevTools MCP: https://github.com/ChromeDevTools/chrome-devtools-mcp
- wsl-chrome-mcp-bridge: https://github.com/dijdzv/wsl-chrome-mcp-bridge
- @dbalabka/chrome-wsl: https://github.com/dbalabka/chrome-wsl
- WSL Mirrored Networking: https://learn.microsoft.com/en-us/windows/wsl/networking
