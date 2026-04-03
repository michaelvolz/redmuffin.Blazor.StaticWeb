---
name: rm-dev-workflows
description: "Shortcut: rm:devops. Dotnet process management (VS vs agent-owned), port 5233 management, and web search tool selection. Use when managing processes, checking ports, identifying VS-owned processes, or deciding which search tool to use."
---

# Development Workflows

## Dotnet Process Management (VS Running)

VS runs own dotnet processes. Cannot Ctrl+C. Kill specific PID only:

```bash
netstat -ano | findstr :5233
taskkill /PID <PID> /F
```

Full stack: `pwsh Stop.ps1` (tracks PIDs in `.dev-session.pids`)

**Identifying VS-owned vs agent-owned processes:**

| Indicator             | VS-Owned Process                                         | Agent-Owned Process    |
| --------------------- | -------------------------------------------------------- | ---------------------- |
| Parent process        | `devenv.exe`                                             | Shell/bash             |
| Process count         | Multiple child processes                                 | Single process         |
| Started by            | Visual Studio launch                                     | Current agent session  |
| Safe to kill          | NEVER                                                    | YES                    |
| Identification method | `wmic process where ProcessId=<PID> get ParentProcessId` | Track PID from `nohup` |

## Web Search Decision Tree

| Query Type            | Tool                                           | Use Case                |
| --------------------- | ---------------------------------------------- | ----------------------- |
| Library/framework API | Context7 (`resolve-library-id` → `query-docs`) | .NET, Blazor, NuGet, JS |
| Keyword-specific      | `brave_web_search`                             | Errors, "how to"        |
| Vague/conceptual      | `websearch`                                    | "find library like X"   |
| Complex reasoning     | `sequentialthinking`                           | Architecture, debugging |

## Everything CLI (`es.exe`)

`es.exe` (voidtools Everything CLI) must be in the system PATH for all contributors. Use it for **instant filesystem searches** when you don't know where to look or need to search outside the workspace.

**Syntax:** `es.exe <search> [options]`

| Example                                            | Purpose                                     |
| -------------------------------------------------- | ------------------------------------------- |
| `es.exe "ext:cs redmuffin" -p`                     | Find .cs files matching "redmuffin" in path |
| `es.exe "ext:razor" -sort dm -n 10`                | 10 most recently modified .razor files      |
| `es.exe "filename:AGENTS.md" -p`                   | Find specific file by name anywhere         |
| `es.exe "path:redmuffin.Blazor.StaticWeb ext:sln"` | Find solution files in project              |

**When to use:** Prefer `glob`/`grep` for workspace-relative searches. Use `es.exe` when searching the entire filesystem, locating files outside the workspace, or when glob patterns are too slow for broad searches.

**Key options:** `-p` (match full path), `-n <num>` (limit results), `-sort dm` (sort by date modified), `-sort size` (sort by size), `/a-d` (files only), `/ad` (folders only)
