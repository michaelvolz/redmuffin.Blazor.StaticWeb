---
module: chrome-devtools-mcp
date: "2026-04-20"
problem_type: developer_experience
component: tooling
severity: medium
tags:
  - wsl2
  - chrome-devtools
  - arch-linux
  - omarchy
  - development-tooling
  - browser-automation
  - mcp
applies_when:
  - Using OpenCode in WSL2 on Arch Linux
  - Needing Chrome DevTools MCP for frontend testing
  - Running Omarchy-based Linux environment
---

# Chrome DevTools MCP in WSL2 (Arch Linux / Omarchy)

## Context

Chrome DevTools MCP is needed for frontend testing (visual verification, CDP automation), but connecting from WSL2 to a Windows browser (Brave) proved unreliable due to complex cross-OS networking. The initial approach tried various bridging solutions that all failed.

## What Didn't Work

| Approach                                                            | Why It Failed                                              |
| ------------------------------------------------------------------- | ---------------------------------------------------------- |
| WSL mirrored networking (`networkingMode=mirrored` in `.wslconfig`) | Complex setup, unreliable in practice                      |
| Port proxy (`netsh interface portproxy`)                            | Connection timeouts, Brave not responding correctly to CDP |
| `wsl-chrome-mcp-bridge` npm package                                 | Failed to spawn Windows executable from WSL (ENOENT)       |
| `@dbalabka/chrome-wsl` npm package                                  | Failed to find powershell.exe                              |
| `npx chrome-devtools-mcp`                                           | Supply chain risk (no 7-day release age filter)            |
| Custom launch script with Windows registry query                    | Worked but added unnecessary complexity                    |

## Solution

Install Chromium directly in WSL and use localhost connection. This eliminates all cross-OS networking complexity.

### 1. Chromium Already Available

Chromium is in Arch Linux pacman:

```bash
# Verify installation
chromium --version
# Output: Chromium 147.0.7727.101 Arch Linux
```

### 2. Create Launcher Script

File: `~/.config/opencode/scripts/mcp/chrome-devtools-start.mjs`

```javascript
#!/usr/bin/env node
/**
 * Cross-platform Chrome/Chromium launcher for DevTools MCP
 * Works on: Windows, macOS, Linux (including WSL)
 */

import { spawn, exec, execSync } from "child_process";
import { readFileSync } from "fs";

const PORT = 9222;

// Detect OS and return appropriate browser command
function getBrowserCommand() {
  const platform = process.platform;
  const isWsl = () => {
    try {
      return readFileSync("/proc/version", "utf8").includes("Microsoft");
    } catch {
      return false;
    }
  };

  if (platform === "win32" || (isWsl() && process.env.WSL_DISTRO)) {
    return {
      cmd: "start",
      args: [
        "",
        "chrome",
        `--remote-debugging-port=${PORT}`,
        "--incognito",
        "--no-first-run",
        "--new-window",
      ],
      shell: true,
    };
  }

  if (platform === "darwin") {
    return {
      cmd: "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
      args: [
        `--remote-debugging-port=${PORT}`,
        "--incognito",
        "--no-first-run",
        "--new-window",
      ],
      shell: false,
    };
  }

  // Linux/WSL - chromium (about:blank by default)
  if (platform === "linux" || isWsl()) {
    return {
      cmd: "chromium",
      args: [
        `--remote-debugging-port=${PORT}`,
        "--no-sandbox",
        "--disable-gpu",
        "--incognito",
        "--no-first-run",
        "--disable-extensions",
        "--new-window",
      ],
      shell: false,
    };
  }

  return {
    cmd: "google-chrome",
    args: [`--remote-debugging-port=${PORT}`],
    shell: false,
  };
}

// Kill existing Chrome on the debug port
async function killExisting() {
  return new Promise((resolve) => {
    exec(`curl -s localhost:${PORT}/json`, (err) => {
      if (!err) {
        console.log(`Chrome already running on port ${PORT}, reusing...`);
        resolve();
        return;
      }
      const platform = process.platform;
      if (platform === "win32") {
        exec("taskkill /F /IM chrome.exe 2>nul", () => resolve());
      } else if (platform === "darwin") {
        exec('pkill "Google Chrome"', () => resolve());
      } else {
        exec("pkill -9 chromium 2>/dev/null; pkill -9 chrome 2>/dev/null", () =>
          resolve(),
        );
      }
    });
  });
}

async function main() {
  await killExisting();

  const browser = getBrowserCommand();

  console.log(`Starting ${browser.cmd} on port ${PORT}...`);

  const child = spawn(browser.cmd, browser.args, {
    detached: true,
    stdio: "ignore",
    shell: browser.shell,
  });

  child.unref();

  await new Promise((r) => setTimeout(r, 2000));

  try {
    const result = execSync(`curl -s localhost:${PORT}/json`, {
      encoding: "utf8",
    });
    const pages = JSON.parse(result);
    console.log("Chrome running. Page:", pages[0]?.url || "unknown");
  } catch (e) {
    console.log("Started (verify with curl localhost:" + PORT + ")");
  }
}

main().catch(console.error);
```

### 3. Make Script Executable

```bash
chmod +x ~/.config/opencode/scripts/mcp/chrome-devtools-start.mjs
```

### 4. Install chrome-devtools-mcp Globally (7-day delay rule)

```bash
# Check version age - must be at least 7 days old
npm view chrome-devtools-mcp version time --json

# Install latest version that passes 7-day filter
sudo npm install -g chrome-devtools-mcp@0.21.0
```

Current version 0.21.0 was released 2026-04-01 (19 days ago), so it passes.

### 5. Configure MCP

File: `~/.config/opencode/opencode.json`

```json
"chrome-devtools": {
  "type": "local",
  "command": ["chrome-devtools-mcp"],
  "enabled": true
}
```

Note: Uses global install instead of `npx -y chrome-devtools-mcp` for supply chain security.

### 6. Launch and Verify

```bash
# Start chromium with remote debugging
node ~/.config/opencode/scripts/mcp/chrome-devtools-start.mjs

# Verify
curl localhost:9222/json
```

Expected output shows CDP endpoints with `ws://localhost:9222/...`

## Why This Works

1. **Local connection**: Chromium on localhost:9222 is simple and reliable — no network bridging
2. **Security**: Global npm install applies the 7-day release age filter from `.npmrc`
3. **Simplicity**: No `.wslconfig` modifications, no port proxies
4. **Cross-platform**: Same script works on Windows (via `start chrome`), macOS, and Linux

## System Changes Made (Can Be Undone)

| Change                         | Where                               | How to Undo                                 |
| ------------------------------ | ----------------------------------- | ------------------------------------------- |
| Mirrored networking            | Windows `C:\Users\flynn\.wslconfig` | Remove `networkingMode=mirrored` line       |
| Port proxy                     | Windows PowerShell (Admin)          | Run `netsh interface portproxy reset`       |
| Brave remote debugging         | Windows Brave settings              | Uncheck "Allow remote debugging"            |
| Global npm chrome-devtools-mcp | WSL                                 | `sudo npm uninstall -g chrome-devtools-mcp` |

## Alternative: Docker (Higher Security, Slower)

For maximum isolation, use Docker image `briffa/chrome-devtools-mcp`:

```bash
docker pull briffa/chrome-devtools-mcp
docker run -i --rm briffa/chrome-devtools-mcp
```

Trade-off: Container start overhead vs. full isolation.

## Related Documentation

- `docs/wsl-chrome-devtools-setup-2026-04-20.md` — Full setup documentation with all attempted approaches
- `docs/solutions/developer-experience/opencode-mcp-configuration-and-windows-spawn-fix-2026-04-18.md` — Windows-specific MCP spawn issues
