---
date: 2026-05-15
title: Mutation Test Filter Auto-Discovery Design
tags: [mutation, testing, quality, architecture, performance]
description: Design doc for auto-discovering test classes from source files
  during mutation testing, enabling TUnit treenode-filter scoping that
  reduces mutation runtime from ~10 minutes to ~1 minute per file.
module: tools
problem_type: architecture
---

# Mutation Test Filter Auto-Discovery Design

## Problem

Mutation testing runs the full test suite (287 tests) per mutation site.
A file with 30 sites takes ~10+ minutes — unreliable in the agent bash tool
(timeout at 2-3 minutes) and slow even for user terminal use.

## Solution

**Auto-discover the matching test class** from the source file being mutated,
then pass a TUnit `--treenode-filter` to scope the test run to only tests
that exercise the mutated file.

For `Commands/CrapCommand.cs`:

1. Search the test project for `CrapCommandTests.cs` (naming convention match)
2. Extract class name: `CrapCommandTests`
3. Build TUnit filter: `--treenode-filter "/*/*/CrapCommandTests/*"`
4. Pass to `dotnet run` as additional arguments

Result: 7 tests instead of 287 — ~2 seconds per mutation site instead of ~20 seconds.
30 sites × 2s = ~1 minute (down from 10+ minutes).

## TUnit Filter Syntax

Confirmed via TUnit docs (tunit.dev/docs/execution/test-filters/) and empirical testing:

```
/Assembly/Namespace/Class/Test
```

Wildcards work at any level: `/*/*/CrapCommandTests/*` matches all tests in the
`CrapCommandTests` class regardless of assembly name or namespace.

`**` wildcard is only valid in the final segment per Microsoft.Testing.Platform.
`/*/*/CrapCommandTests/**` is also valid but equivalent to `/*/*/CrapCommandTests/*`.

## Architecture

```
MutateCommand
  │
  ├── --project (source file)
  ├── --test-project
  ├── --no-test-filter (NEW — override auto-filter)
  │
  ▼
MutateHandler.RunMutationCoreAsync
  │
  ├── TestClassDiscovery.Discover(sourcePath, testProjectPath)
  │   │  Returns: "CrapCommandTests" or null (no match)
  │   │
  │   └── Auto-filter enabled when:
  │       - --no-test-filter is NOT set
  │       - Discovery returns a class name
  │
  ▼
MutationRunner.RunAsync(sourcePath, sites, testProjectPath, timeoutFactor, testFilter?)
  │
  └── RunTestsAsync(projectPath, timeout, testFilter?)
       │
       └── If testFilter: appends -- --treenode-filter "/*/*/{testFilter}/*"
```

## Trade-offs

| Aspect                | Auto-filter ON (default)                           | `--no-test-filter` (override)    |
| --------------------- | -------------------------------------------------- | -------------------------------- |
| Speed                 | ~1 min for 30 sites                                | ~10 min for 30 sites             |
| Scope                 | Tests for mutated class only                       | All tests (integration coverage) |
| Use case              | 95% of fix-and-verify cycles                       | Final integrity check            |
| False confidence risk | Cross-file interaction tests may not catch mutants | Full confidence                  |

**Protocol**: Auto-filter is the default for fast iteration. Run `--no-test-filter`
once at the end of a mutation fix session to catch integration-level survivors
that cross-file tests would have caught.

## File Mapping Convention

Source file naming: `{Name}.cs`
Test file naming: `{Name}Tests.cs`
Test class naming: `{Name}Tests`

Discovery scans the test project directory tree for a `.cs` file matching
`{Name}Tests.cs`. The class name is extracted from the filename (no namespace
parsing required — wildcards handle it).

## Related

- [Mutation Testing Decision Tree](./mutation-testing-decision-tree-2026-05-14.md)
- [Mutation Execution Protocol](./mutation-execution-protocol-2026-05-15.md)
- `rm-gates-cleanup` skill §4 — Mutation Cleanup Workflow
