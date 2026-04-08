---
title: feat: Add cross-platform Brave launcher for Chrome DevTools MCP
type: feat
status: active
date: 2026-04-08
---

# feat: Add cross-platform Brave launcher for Chrome DevTools MCP

## Overview

Add a Node.js wrapper script to launch chrome-devtools-mcp with the correct Brave browser executable path for each operating system (Windows and Linux), replacing the hardcoded Windows path in opencode.json.

## Problem Frame

The Chrome DevTools MCP server configuration in opencode.json hardcodes a Windows-specific path to the Brave browser executable (`C:\Program Files\BraveSoftware\Brave-Browser\Application\brave.exe`). This causes the MCP server to fail on Linux systems where Brave is installed at `/usr/bin/brave`. We need a cross-platform solution that automatically detects the OS and uses the appropriate Brave path.

## Requirements Trace

- R1. Chrome DevTools MCP server must work on both Windows 11 and Linux (Omarchy)
- R2. Use Brave browser specifically (not Chrome)
- R3. MCP tools must be optimally usable in OpenCode
- R4. Configuration should be maintainable without OS-specific branches

## Scope Boundaries

- Only affects the chrome-devtools MCP server configuration
- Does not change other MCP servers (brave-search, context7, sequentialthinking)
- Assumes Brave browser is installed on both platforms

## Context & Research

### Relevant Code and Patterns

- `opencode.json` contains MCP server configurations
- Other MCP servers use Docker or HTTP endpoints
- chrome-devtools-mcp package supports `--executablePath` flag

### Institutional Learnings

- Project uses Node.js for cross-platform scripting
- MCP servers are configured in opencode.json
- Environment variables are used for API keys

## Key Technical Decisions

- **Node.js wrapper script**: Use a simple Node.js script that detects OS and passes the correct executable path to chrome-devtools-mcp, rather than modifying the MCP package or using shell scripts
- **Platform detection**: Use Node.js `os.platform()` to distinguish Windows (`win32`) from Linux (`linux`)
- **Path mapping**: Map OS to known Brave installation paths
- **Fallback handling**: Include fallback to `/usr/bin/brave` for unknown platforms

## Implementation Units

- [ ] **Unit 1: Create cross-platform launcher script**

**Goal:** Create a Node.js script that launches chrome-devtools-mcp with OS-appropriate Brave executable path

**Requirements:** R1, R2, R4

**Dependencies:** None

**Files:**
- Create: `scripts/mcp/chrome-devtools-launch.mjs`

**Approach:**
- Use Node.js `os.platform()` for OS detection
- Map platforms to Brave executable paths
- Spawn chrome-devtools-mcp with `--executablePath` flag
- Forward stdio for MCP protocol

**Patterns to follow:**
- Simple, maintainable Node.js script
- Similar to other scripts in `scripts/` directory

**Test scenarios:**
- Happy path: Script runs on Linux and passes `/usr/bin/brave` to chrome-devtools-mcp
- Happy path: Script runs on Windows and passes `C:\Program Files\BraveSoftware\Brave-Browser\Application\brave.exe` to chrome-devtools-mcp
- Edge case: Script handles unknown platform by falling back to `/usr/bin/brave`
- Integration: OpenCode can start the MCP server and tools are available

**Verification:**
- Script exists and is executable
- Running the script manually shows chrome-devtools-mcp starting with correct executable path

- [ ] **Unit 2: Update opencode.json configuration**

**Goal:** Replace hardcoded Windows path with reference to the cross-platform launcher script

**Requirements:** R1, R3, R4

**Dependencies:** Unit 1

**Files:**
- Modify: `opencode.json`

**Approach:**
- Change the `command` array for chrome-devtools MCP server
- Use `["node", "scripts/mcp/chrome-devtools-launch.mjs"]`
- Remove the hardcoded `--executablePath` argument

**Patterns to follow:**
- Consistent with other MCP server configurations in opencode.json
- Uses Node.js which is already available in the environment

**Test scenarios:**
- Happy path: OpenCode starts chrome-devtools MCP server successfully on Linux
- Happy path: OpenCode starts chrome-devtools MCP server successfully on Windows
- Integration: Chrome DevTools tools are available and functional in OpenCode

**Verification:**
- opencode.json updated with new command
- OpenCode can load the MCP server configuration without errors

## System-Wide Impact

- **Interaction graph:** Only affects chrome-devtools MCP server initialization
- **Unchanged invariants:** Other MCP servers (brave-search, context7, sequentialthinking) remain unchanged

## Risks & Dependencies

| Risk | Mitigation |
|------|------------|
| Brave not installed at expected paths | Script includes fallback and clear error messages |
| Node.js version compatibility | Uses modern Node.js features available in project's Node version |
| MCP protocol compatibility | Script only forwards stdio, doesn't modify protocol |

## Documentation / Operational Notes

- Update any documentation mentioning chrome-devtools MCP setup to note cross-platform support
- No operational changes required

## Sources & References

- Related code: `opencode.json` MCP configuration
- External docs: chrome-devtools-mcp CLI options