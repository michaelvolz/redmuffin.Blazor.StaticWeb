---
aliases: [cleanup-dev, cdev]
description: Kill dev server (5233), close Brave DevTools, verify cleanup
---

1. Close DevTools pages: `chrome-devtools_list_pages` → close each with `chrome-devtools_close_page`
2. Close Brave DevTools browser: find main process PID → `taskkill //PID <PID> //F`
   ```bash
   wmic process where "name='brave.exe'" get ProcessId,CommandLine | findstr "chrome-devtools-mcp"
   ```
   Kill the PID with `--user-data-dir=C:\Users\flynn\.cache\chrome-devtools-mcp\chrome-profile`. This ONLY closes the agent's Brave instance — your main Brave browser is unaffected.
3. Kill dev server: `netstat -ano | findstr :5233` → note LISTENING PID → `taskkill //PID <PID> //F`
4. Verify: `netstat -ano | findstr :5233` → only TIME_WAIT or nothing (no LISTENING)
5. Check orphans: `tasklist | findstr dotnet` → verify ports → kill non-VS processes only
6. Remove stray Windows artifacts: `rm nul` (dotnet log redirection can produce stray `nul` files in the working directory)

NOTE: TIME_WAIT = closed TCP connections, PID 0, NOT active processes. They self-clean in minutes. Only LISTENING = running server.
