---
module: opencode
date: 2026-04-18
problem_type: build_error
component: tooling
severity: high
tags:
  - opencode
  - mcp
  - server
  - configuration
  - windows
  - spawn
  - chrome-devtools
applies_when:
  - OpenCode MCP servers configured locally and globally
  - Windows development environment
symptoms:
  - "sequential-thinking MCP server fails to load: Tool not found"
  - "chrome-devtools MCP spawn EINVAL error on Windows"
  - "MCP servers appear in opencode.json but do not initialize"
root_cause: config_error
resolution_type: config_change
---

## Problem

OpenCode MCP server configuration issues preventing tools from loading:

1. **sequential-thinking naming mismatch** - Local config used correct hyphenated name, global config used wrong name
2. **chrome-devtools.enabled missing** - Local and global configs missing the required `enabled: true` flag
3. **chrome-devtools spawn failure** - Node.js child_process spawn on Windows fails with EINVAL because `npx.cmd` requires `shell: true`

## Symptoms

- `The tool "sequential-thinking" could not be found` error when MCP attempts to load the tool
- `spawn EINVAL` error when chrome-devtools MCP server tries to spawn the browser launcher
- MCP servers present in configuration but not appearing in available tools list

## What Didn't Work

1. Verified opencode.json syntax was valid JSON - no parsing errors
2. Checked MCP server packages were installed - both sequential-thinking and chrome-devtools packages existed
3. Attempted alternative server entry points - paths were correct
4. Tried different npx command formats - both `npx` and full path approaches failed

## Solution

### 1. Fix sequential-thinking naming in global config

The npm package is `@modelcontextprotocol/server-sequential-thinking` (with hyphen).

**Global config (~/.config/opencode/opencode.json) - was WRONG:**

```json
"sequentialthinking": {
  "type": "local",
  "command": ["npx", "-y", "@modelcontextprotocol/server-sequential-thinking"]
}
```

**Fixed to:**

```json
"sequential-thinking": {
  "type": "local",
  "command": ["npx", "-y", "@modelcontextprotocol/server-sequential-thinking"],
  "enabled": true
}
```

### 2. Add enabled flag to chrome-devtools configs

Both local and global configs were missing the required `enabled: true` flag.

**Local opencode.json:**

```json
"chrome-devtools": {
  "type": "local",
  "command": ["node", "scripts/mcp/chrome-devtools-launch.mjs"],
  "enabled": true
}
```

**Global config:**

```json
"chrome-devtools": {
  "type": "local",
  "command": ["npx", "-y", "opencode-chrome-devtools"],
  "enabled": true
}
```

### 3. Fix chrome-devtools-launch.mjs spawn on Windows

Node.js `child_process.spawn()` on Windows requires `shell: true` when using `npx` because `npx.cmd` is a cmd.exe batch file.

**scripts/mcp/chrome-devtools-launch.mjs - was WRONG:**

```javascript
const child = spawn("npx", ["-y", "chrome-devtools-mcp"], {
  stdio: "inherit",
});
```

**Fixed to:**

```javascript
import { spawn } from "node:child_process";

const platform = () => process.platform;
const isWindows = platform() === "win32";

const child = spawn("npx", ["-y", "chrome-devtools-mcp"], {
  stdio: "inherit",
  shell: isWindows, // Required on Windows - npx.cmd needs cmd.exe shell
});
```

## Why This Works

1. **Naming** - The MCP server names in config must match the npm package entry point name. The `@modelcontext protocol/server-sequential-thinking` package exposes `sequential-thinking` (with hyphen), not `sequentialthinking`. OpenCode validates names against available MCP tools and silently skips mismatches.

2. **enabled flag** - OpenCode requires `enabled: true` for MCP servers to initialize. Without it, the server is parsed but never activated.

3. **Windows spawn** - On Windows, `npx` is a cmd.exe batch file (`npx.cmd`). Node.js `spawn()` without `shell: true` cannot execute cmd batch files - it passes the command directly to the OS which doesn't understand `.cmd` files. Setting `shell: true` invokes cmd.exe to process the batch file correctly. Linux/macOS use shell scripts which spawn handles natively.

## Prevention

> **Note (2026-06-05):** The `shell: true` pattern below is correct for general npx-on-Windows
> spawning but was superseded for chrome-devtools-mcp specifically. The sfw-protected
> `.cmd` shims use `<nul` to prevent sfw from hanging on non-TTY stdin — this redirect
> breaks the MCP JSON-RPC protocol. The current architecture uses direct `node` execution
> with a globally-installed `chrome-devtools-mcp` on Windows and the Omarchy npx wrapper
> on WSL/Linux. See the cross-platform MCP launcher doc for details.

1. **Always verify MCP server names** - Check the exact npm package entry point name (usually matches the package name with hyphens preserved)

2. **Always include enabled: true** - Required flag for all MCP server configurations

3. **Use shell: true for Windows npx spawn** - Any Node.js script spawning `npx` on Windows must use `shell: true` in spawn options

4. **For MCP servers that need stdin** - The `shell: true` pattern does not help when the npx invocation goes through sfw-protected `.cmd` shims that redirect stdin to NUL. Use direct `node` execution with a global npm install for Windows MCP servers that require stdin-based protocols.

5. **Test MCP servers after config changes** - Run OpenCode with verbose logging or check available tools list after configuration edits
