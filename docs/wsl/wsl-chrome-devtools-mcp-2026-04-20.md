---
title: Chrome DevTools MCP in WSL2
problem_type: developer_experience
component: tooling
module: development_workflow
root_cause: environment_configuration
severity: high
tags:
  - wsl
  - chrome-devtools
  - mcp
  - omarchy
  - arch-linux
  - browser-automation
  - frontend-testing
symptoms:
  - unreliable devtools connection from wsl to windows browser
  - connection timeouts when running chrome-devtools-mcp
  - mirrored networking issues with .wslconfig
  - port proxy failures
---

## Solution: Chrome DevTools MCP in WSL2 (Arch Linux / Omarchy)

### Context

Chrome DevTools MCP is needed for frontend testing, but connecting from WSL to a Windows browser (Brave) proved unreliable due to complex networking setups involving `.wslconfig`, mirrored networking, and port proxies. Several approaches to bridge or proxy the connection failed to provide a stable setup.

**Failed approaches:**

- WSL mirrored networking with `.wslconfig` settings
- Port proxy from WSL gateway to Windows Brave
- npm packages attempting to bridge WSL and Windows Chrome (e.g., `wsl-chrome-mcp-bridge`, `@dbalabka/chrome-wsl`)
- Using `npx` to run `chrome-devtools-mcp` (poses supply chain risk)

### Guidance

Install Chromium locally in WSL and run the MCP manually with a launcher script.

**1. Install Chromium via pacman:**

```bash
sudo pacman -S chromium
```

**2. Create the launcher script at `~/.config/opencode/scripts/mcp/chrome-devtools-start.mjs`:**

```javascript
#!/usr/bin/env node

import { spawn } from "node:child_process";

const CHROMIUM_FLAGS = [
  "--remote-debugging-port=9222",
  "--no-sandbox",
  "--disable-gpu",
  "--incognito",
  "--no-first-run",
  "--disable-extensions",
  "--new-window",
  "about:blank",
];

const chromium = spawn("chromium", CHROMIUM_FLAGS, {
  detached: true,
  stdio: "ignore",
});

chromium.unref();
console.log("Chromium launched with remote debugging on port 9222");
```

**3. Make the script executable:**

```bash
chmod +x ~/.config/opencode/scripts/mcp/chrome-devtools-start.mjs
```

**4. Install chrome-devtools-mcp globally (7-day delay rule applies to npm global installs):**

```bash
sudo npm install -g chrome-devtools-mcp@0.21.0
```

**5. Configure the MCP in your settings:**

```json
{
  "mcpServers": {
    "chrome-devtools": {
      "command": "chrome-devtools-mcp"
    }
  }
}
```

### Why This Matters

- **Reliability**: Running Chromium locally in WSL eliminates all cross-OS networking complexity.
- **Security**: Using a global npm install instead of `npx` satisfies the 7-day release age filter.
- **Simplicity**: Avoids `.wslconfig` tweaks and port proxies entirely.
- **Performance**: Direct browser control without network bridging overhead.

### When to Apply

- When using Chrome DevTools MCP in WSL2 (Arch Linux / Omarchy) on Windows.
- When cross-OS browser connections are unstable.
- When you want a self-contained local development setup without external dependencies.
- When the codebase requires frontend testing via CDP.
