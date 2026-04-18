---
title: code-review-graph Integration for OpenCode
date: 2026-04-18
module: context-optimization
problem_type: developer_experience
component: tooling
severity: medium
applies_when:
  - Code reviews requiring token-efficient context
  - Understanding blast radius of changes
  - Analyzing code dependencies before refactoring
tags:
  - code-review-graph
  - knowledge-graph
  - mcp
  - token-efficiency
---

# code-review-graph Integration for OpenCode

## Context

AI coding assistants like OpenCode traditionally read large portions of codebase on every task. Even with context-mode providing sandboxed execution, the AI still receives excessive file content—often hundreds of files for a targeted change. This wastes tokens, slows responses, and reduces context quality for actual code reasoning.

**The gap**: No structural awareness of code relationships—functions call other functions, classes depend on interfaces, tests validate specific modules. Without this map, every query becomes a full codebase scan.

## Guidance

### Installation

```bash
pip install code-review-graph
```

### OpenCode Configuration

```bash
code-review-graph install
```

This single command:

1. Detects OpenCode as the active platform
2. Writes `.opencode.json` with MCP server configuration
3. Creates the knowledge graph on first run

**Resulting `.opencode.json`:**

```json
{
  "mcpServers": {
    "code-review-graph": {
      "command": "code-review-graph",
      "args": ["serve"],
      "type": "stdio",
      "env": []
    }
  }
}
```

### Building the Knowledge Graph

```bash
code-review-graph build
```

For the Blazor project, this produced:

- **271 files** parsed
- **1467 nodes**: 826 functions, 349 classes, 271 files, 21 tests
- **4100 edges**: 1961 calls, 1543 contains, 588 imports
- **23 communities** detected (architectural boundaries)

### Available MCP Tools (28 total)

| Tool                         | Purpose                                                |
| ---------------------------- | ------------------------------------------------------ |
| `build_or_update_graph_tool` | Build or incrementally update the graph                |
| `get_minimal_context_tool`   | Ultra-compact context (~100 tokens) — call this first  |
| `get_impact_radius_tool`     | Blast radius of changed files                          |
| `get_review_context_tool`    | Token-optimised review context with structural summary |
| `query_graph_tool`           | Callers, callees, tests, imports, inheritance queries  |
| `traverse_graph_tool`        | BFS/DFS traversal from any node with token budget      |
| `semantic_search_nodes_tool` | Search code entities by name or meaning                |
| `list_graph_stats_tool`      | Graph size and health                                  |
| `detect_changes_tool`        | Risk-scored change impact analysis for code review     |

### Blast Radius Example

For a changed file `.opencode/plugins/rtk.ts`:

```
get_impact_radius_tool(file: ".opencode/plugins/rtk.ts")
→ 5 files affected
→ 9 functions impacted
→ Shows exactly what breaks before reading any code
```

### Combined with context-mode

The token-efficient pattern:

1. Use MCP JSON tools (like `get_impact_radius_tool`) via `mcp_call_tool`
2. JSON response stays in subprocess via context-mode
3. Only the parsed result enters context
4. This is ~6.8× fewer tokens on reviews, up to 49× on daily coding tasks

## Why This Matters

1. **Token reduction**: Official benchmarks show 6.8× fewer tokens on reviews, up to 49× on daily coding tasks
2. **Precision**: Blast radius analysis tells you exactly what will break before you read any file
3. **Incremental updates**: Only re-parses files that actually changed—not the whole codebase
4. **Architectural insight**: Community detection surfaces natural code boundaries (layers, modules, bounded contexts)

## When to Apply

- **Code reviews**: Before reviewing a PR, query blast radius to understand impact
- **Refactoring**: When changing a function/class, know all dependents first
- **Onboarding**: Use `get_architecture_overview_tool` to understand project structure
- **Testing**: Query `get_affected_flows_tool` to find which tests exercise changed code
- **Large codebases**: Where full-project scanning becomes impractical

## Examples

### Review changes with blast radius

```
Before reading any files, call:
get_impact_radius_tool(file: "src/Service/UserService.cs")
→ Returns: { files: [...], functions: [...], tests: [...] }
→ Read only those, not the whole codebase
```

### Query callers of a method

```
query_graph_tool(query: "callers", target: "CalculateTotal")
→ Returns all functions that call CalculateTotal
```

### Find all tests covering changed code

```
query_graph_tool(query: "tests", target: "OrderProcessor")
→ Returns test files exercising OrderProcessor
```

### Get minimal entry point

```
get_minimal_context_tool(file: "Controllers/ApiController.cs")
→ Returns ~100 token summary of entry point
```

## Exclusions

Create `.code-review-graphignore` in repo root to exclude paths:

```
node_modules/
bin/
obj/
.vs/
*.generated.cs
```
