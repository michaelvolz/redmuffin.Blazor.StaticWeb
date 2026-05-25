---
name: rm-guide-gates-development
description: Quality gates toolchain conventions, operational rules, gotchas, and development patterns for the tools/ solution. Use when working in the tools/ directory, editing tools/src/ or tools/tests/, or touching CRAP, SCRAP, Architecture, Depth, Mutation, or Duplicates gates.
---

# rm-guide-gates-development

## Critical Rules

1. **Never run from the repo root when building or testing the tools solution.**
   The repo root `global.json` pins SDK 9; the tools solution targets .NET 10
   (pinned in `tools/global.json`). Wrong directory produces `NETSDK1045`.
   Always `cd tools` first.

2. **Never use `dotnet test`.** TUnit + Microsoft.Testing.Platform in AOT mode
   discovers zero tests via `dotnet test`. The only working test command is:
   `dotnet run --project tests/redmuffin.Tools.QualityGates.Tests`

3. **Never suppress analyzer warnings with `#pragma`.** Warnings are signals
   that code is not following best practices. Fix the root cause properly.
   Single exception: genuine analyzer contradictions (e.g., CA1859 vs MA0016)
   with file-level suppression and documented reasoning per
   rm-guide-warnings §Decision Tree.

4. **Never inject dependencies into handler methods.** Handlers are
   `public static`. Test the Handler directly with its inputs, never the
   Command. No `InternalsVisibleTo`.

5. **Never mutate production code to kill a mutation survivor.** Only add
   tests or extract genuine seams (≥5 lines of replaceable behavior).
   Use the survivor decision tree in rm-quality-gates §4.

## Build and Test

```bash
# Build (from tools/)
dotnet build src/redmuffin.Tools.QualityGates --verbosity quiet

# Test
dotnet run --project tests/redmuffin.Tools.QualityGates.Tests

# Full verify cycle
dotnet clean src/redmuffin.Tools.QualityGates
dotnet build src/redmuffin.Tools.QualityGates --verbosity quiet
dotnet run --project tests/redmuffin.Tools.QualityGates.Tests
```

## Style Fixes

Run `dotnet format` from the tools solution root (not from repo root):

```bash
cd tools
dotnet format
```

Never use `--severity info` — that flag causes analyze-without-fix behavior.

## Quality Gates Happy Path

```bash
cd tools
dotnet run -- all   # auto-discovers projects from tools/redmuffin.Tools.slnx
```

Zero flags needed. The `all` command auto-generates coverage to
`/tmp/quality-gates-coverage.xml` and runs all six gates.

Specify a different solution:

```bash
dotnet run -- all --solution ../redmuffin.Blazor.StaticWeb.slnx
```

## TDD Pattern

- Write ONE failing test → minimal production code → refactor → next test.
- Test the Handler directly, never the Command.
- Plan document defines test scenarios per unit. Supplement missing categories
  (happy path, edge cases, error paths) before writing tests.

## TUnit Assertion API

Use `.IsEqualTo(n)`, `.Contains(str)`, `.IsLessThan(n)`,
`.Count.IsEqualTo(n)`. Never use `.HasCount()` or
`.SequenceEqual(features).IsTrue()` — these fail silently.

## Command/Handler Pattern

New gates follow `CrapCommand` / `CrapHandler` exactly:

- **Command**: `public static Command Create()` returns `Command` with options.
  `internal static int Execute(...)` wires the pipeline. Uses `SetAction`
  (never `SetHandler`).
- **Handler**: `public static` class. `Run()` with `TextWriter? output` param.
  Returns exit code (0=pass, 1=error, 2=threshold breach).
- **Options**: `public sealed record` in `Commands/`. Mutable options via
  `with` expressions.

## Analysis Pipeline Pattern

Every gate: Parser → Normalizer → Analyzer → Scorer → Recommender → Handler.

## Roslyn Patterns

- **CSharpSyntaxWalker subclass** for tree traversal (CRAP complexity,
  mutation discovery). Override specific `Visit*` methods.
- **CSharpSyntaxRewriter subclass** for tree mutation (SCRAP normalization,
  mutation application). Override `Visit(SyntaxNode?)`.
- **Per-file parsing** via `CSharpSyntaxTree.ParseText(source)`. Never use
  `MSBuildWorkspace`.
- **Span-based node identity.** Store `SyntaxNode` references and identify
  nodes by `Span` in rewritten trees. `CSharpSyntaxWalker` and
  `CSharpSyntaxRewriter` have incompatible base classes — use Span identity,
  never index counting.

## Adding a New Gate

1. Research the original Uncle Bob tool repo. Confirm existence, scope,
   thresholds, and CLI interface. Never build from memory.
2. Create: `Analysis/` classes, `Commands/XxxCommand.cs`,
   `Commands/XxxHandler.cs`, `Commands/XxxOptions.cs`, test files.
3. Wire into `Program.cs`: `rootCommand.Subcommands.Add(XxxCommand.Create());`
4. Hook into `AllCommand.cs` with appropriate flags.
5. Add test fixture projects under `tests/.../Fixtures/` if the gate needs
   real project execution.
6. Update the gates table and project structure in this README.

## Known Issues

- **`dotnet tool install`** broken with .NET 10 packaging
  (`DotnetToolSettings.xml` path). Smoke test with `dotnet run` instead.
- **Mutation runner tests** use `[NotInParallel]` because they modify shared
  fixture files. Keep this attribute on any test that mutates disk state.
- **CoverageGapDetector** automatically classifies infrastructure methods the
  coverage instrumenter cannot attribute (conductor methods at CC≤4, switch
  dispatchers at >50% coverage). These show `COVERAGE GAP` instead of `FAIL`
  in CRAP output. No manual exclusion lists, config files, or attributes
  needed.

## Project Structure

```
tools/
├── global.json                        .NET 10 SDK pin
├── redmuffin.Tools.slnx               Separate solution
├── src/redmuffin.Tools.QualityGates/
│   ├── Program.cs                     CLI root
│   ├── Commands/                      CLI wiring + handlers (one per gate)
│   ├── Analysis/                      Gate engines (CC, coverage, mutation, etc.)
│   └── Models/                        YAML config parsing
├── tests/redmuffin.Tools.QualityGates.Tests/
│   ├── Commands/                      Handler + composition tests
│   ├── Analysis/                      Per-gate unit tests
│   └── Fixtures/                      Coverage XML + MutationTarget project
└── quality-gates/                     Architecture rules config
```

## References

- `tools/README.md` — human-facing overview, gates table, usage, ADR links
- `rm-architecture-uncle-bob-martin` skill — Uncle Bob's full metric suite
- `rm-quality-gates` skill — cleanup workflows, survivor decision tree, Feathers extraction
- `rm-guide-code-quality` skill — universal code quality rules, seam extraction gates
- `rm-tdd` skill — TDD workflow enforced for all quality gates development
