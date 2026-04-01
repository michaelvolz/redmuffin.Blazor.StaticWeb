---
name: dev-workflows
description: Dotnet process management (VS-owned vs agent-owned), port management, and web search tool selection. Use when managing dotnet processes, checking port availability, identifying VS-owned processes, or deciding which search tool to use.
invocable: false
---

# Development Workflows

## Dotnet Process Management (VS Running)

VS runs own dotnet processes. Cannot Ctrl+C. Kill specific PID only:

```powershell
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
