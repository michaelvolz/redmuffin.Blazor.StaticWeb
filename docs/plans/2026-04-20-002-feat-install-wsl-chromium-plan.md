---
title: Install Chromium in WSL for Chrome DevTools MCP
type: feat
status: active
date: 2026-04-20
origin: docs/wsl-chrome-devtools-setup-2026-04-20.md
---

# Install Chromium in WSL for Chrome DevTools MCP

## Overview

Install Chromium browser directly in WSL (Arch Linux) and configure chrome-devtools-mcp to connect via localhost instead of attempting WSL→Windows networking bridge.

## Problem Frame

Current approach requires complex WSL→Windows networking (mirrored networking, port proxies) which is unreliable. Installing Chromium locally in WSL eliminates all networking complexity.

## Requirements Trace

- R1. Chromium installed and accessible via CLI in WSL
- R2. Chrome DevTools MCP connects to local Chromium on localhost:9222
- R3. OpenCode MCP config updated to use new local endpoint

## Scope Boundaries

- Does not replace Windows Brave (remains primary browser)
- Does not modify Windows-side configs (keeping for potential future use)
- Browser only used for DevTools MCP, not daily browsing

## Implementation Units

- [x] **Unit 1: Install Chromium in WSL**

**Goal:** Install Chromium browser via pacman

**Dependencies:** None

**Files:**

- System: `/usr/bin/chromium` (installed by pacman)

**Approach:**

- Check if already installed (pacman shows `[installed]` for chromium)
- If not installed, run `sudo pacman -S chromium`

**Test scenarios:**

- Test expectation: none — verification is CLI command success

**Verification:**

- ✅ `chromium --version` returns: `Chromium 147.0.7727.101 Arch Linux`

---

- [x] **Unit 2: Configure Chrome DevTools MCP to use Local Chromium**

**Goal:** Update OpenCode MCP config to connect to localhost:9222

**Dependencies:** Unit 1

**Files:**

- Modify: `~/.config/opencode/opencode.json`

**Approach:**

- Update MCP command to use `--browserUrl http://localhost:9222`
- Or use default (localhost is default if no browserUrl specified)

**Test scenarios:**

- Test expectation: none — verification is MCP connection

**Verification:**

- ✅ MCP connected successfully - user confirmed it works!

---

- [x] **Unit 3: Start Chromium with Remote Debugging**

**Goal:** Launch Chromium with remote debugging enabled

**Dependencies:** Unit 1

**Approach:**

- Start chromium with: `chromium --remote-debugging-port=9222`
- Or create startup script/shell alias

**Test scenarios:**

- Test expectation: none — verification is port listening

**Verification:**

- ✅ `curl localhost:9222/json` returns CDP endpoints

---

- [x] **Unit 4: Document Changes**

**Goal:** Update setup documentation with new configuration

**Dependencies:** Units 1-3

**Files:**

- Modify: `docs/wsl-chrome-devtools-setup-2026-04-20.md`

**Approach:**

- Add "Chromium in WSL" as new approach used
- Document undo steps

**Test scenarios:**

- Test expectation: none — documentation only

**Verification:**

- ✅ Document reflects current configuration

---

## Key Technical Decisions

- **Using localhost instead of Windows bridge:** Simplifies MCP connection, no networking hacks required
- **Using Chromium instead of Google Chrome:** Available in pacman, fully suitable for DevTools

## Risks & Dependencies

| Risk                                         | Mitigation                           |
| -------------------------------------------- | ------------------------------------ |
| Chromium not as performant as Windows Chrome | Acceptable for DevTools MCP use only |
| Need to manually update browser              | `sudo pacman -Syu` when needed       |

## Sources & References

- https://wiki.archlinux.org/title/Chromium
- https://chromium.googlesource.com/chromium/src/+/HEAD/docs/chromium_browser_vs_google_chrome.md
