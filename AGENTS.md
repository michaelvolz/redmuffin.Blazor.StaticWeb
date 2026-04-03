# AGENTS: Project Guide

## MANDATORY GLOBAL RULES

For every coding, architecture, refactoring, or review task:

- Immediately load the skill "strict-coding-standards" via the skill tool
- Strictly follow every rule in that skill. No exceptions.
- NEVER answer without reading the actual code first
- Research first: use Exa code search or web search before implementing unfamiliar APIs

## CRITICAL BOUNDARIES

- NEVER commit secrets, NEVER push to remote (HARD BLOCKED)
- ALWAYS `dotnet build --verbosity quiet` after C# changes
- ALWAYS `dotnet build -c Debug-Sass` after SCSS/JS changes
- ALWAYS `dotnet test` before commit
- ALWAYS check port 5233 free before `dotnet run`, redirect output to `logs/dotnet.log`
- NEVER kill dotnet processes without verifying they are not VS-owned

## STACK

| Technology      | Version     | Purpose        |
| --------------- | ----------- | -------------- |
| .NET            | 9.0         | Core framework |
| Blazor          | WebAssembly | Frontend       |
| Azure Functions | .NET 9      | Backend        |
| TUnit           | Latest      | Testing        |
| SCSS/Sass       | -           | Styling        |

## STRUCTURE

```
src/redmuffin.Blazor.StaticWeb/          # Frontend (Blazor WASM)
src/redmuffin.Blazor.StaticWeb.Api/      # Backend (Azure Functions)
tests/                                    # Tests (mirrors src)
.opencode/skills/                         # Custom skills (rm-* prefix) + vendor/
.opencode/commands/                       # Custom commands (rm-* prefix)
.opencode/agents/                         # Custom agents (rm-* prefix) + vendor/
docs/solutions/                           # Searchable knowledge store of past solutions (bugs, best practices, patterns), organized by category with YAML frontmatter (module, tags, problem_type). Relevant when implementing or debugging in documented areas.
```

## SKILL REFERENCES

| Skill                         | Trigger When...                                                        |
| ----------------------------- | ---------------------------------------------------------------------- |
| `rm-csharp-standards`         | Writing C# code, analyzer rules, LoggerMessage, async, design patterns |
| `rm-testing`                  | Writing tests, TUnit patterns, TestScope, mocking                      |
| `rm-dotnet`                   | .csproj, DI, build/test commands, Azure Functions, coverage            |
| `rm-dev-workflows`            | Process management, port 5233, search tool selection, Everything CLI   |
| `rm-ui-styling`               | Foundation CSS, SCSS, accessibility (WCAG 2.1 AA)                      |
| `rm-commit`                   | Committing, commit messages, conventional commits                      |
| `rm-security-secrets`         | API keys, tokens, passwords, MCP env vars, security config             |
| `rm-output-style`             | C# formatting, naming, C# 12/13, nullable types                        |
| `rm-markdown`                 | Writing markdown, MarkdownLint errors, documentation                   |
| `rm-nuget-manager`            | Adding/removing/updating NuGet packages                                |
| `rm-create-prd`               | Generating PRDs, requirements documents                                |
| `rm-generate-tasks`           | Task lists from PRDs, implementation plans                             |
| `rm-agent-markdown-optimizer` | "optimize for agents", "make agent-friendly"                           |
