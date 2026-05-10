---
date: 2026-05-10
last_updated: 2026-05-10
tags:
  - quality-gates
  - crap
  - scrap
  - architecture
  - mutation
  - dupes
  - zero-defects
  - testing
  - code-cleanup
---

# Zero Quality Defects Strategy

## What Belongs in This File

- **Viewpoint**: Developer working on the quality gates toolchain, aiming
  for zero CRAP/SCRAP/Architecture/Mutation/Dupes violations as a
  pre-commit gate.
- **What belongs**: The taxonomy of CRAP violations (untested,
  structural, boilerplate), proven strategies for eliminating each, and
  the verification workflow.
- **What does NOT belong**: Specific method-by-method test
  implementations, tool configuration details (see tools/README.md), or
  non-quality-gate testing patterns (see rm-guide-testing).

---

## 0 — The Insight

CRAP violations fall into exactly three categories. Every single one is
eliminable without pragma directives, exclusion configs, or LLM-bypassable
mechanisms.

| Category        | Root Cause                                                           | Fix                                                                          | Example                     |
| --------------- | -------------------------------------------------------------------- | ---------------------------------------------------------------------------- | --------------------------- |
| **Untested**    | `public static` method has zero coverage because no test was written | Write the tests — the method is already testable by design                   | `CrapCommand.Execute(args)` |
| **Structural**  | Cyclomatic complexity is high because logic is lumped into one block | Split into sub-methods, each with lower CC                                   | `NormalizeNode` (CC=33)     |
| **Boilerplate** | System.CommandLine option construction inflates CC in `Create()`     | Extract options to static fields; `Create()` becomes pure assembly with CC=1 | `CrapCommand.Create()`      |

## 1 — Category: Untested

**Symptom**: CRAP scores of 20-72 with 0% coverage on `public static`
methods.

**Root cause**: Command/handler separation produced testable methods
(`Execute`, `RunAsync`, `HandleX`), but nobody wrote the tests. The
coverage tool runs against the test project, not the tool itself.

**Fix**: Write TUnit tests that call the `public static` method directly
with known inputs and assert exit codes/output. No mocking needed — these
methods take primitive/record inputs and return `int` exit codes.

**Affected methods** (examples):

- `CrapCommand.Execute()`, `ScrapCommand.Execute()`
- `MutateHandler.RunAsync()`, `AllCommand.ExecuteAsync()`
- `FindDuplicates()`, `ScanFile()`, `HasAnyFailure()`
- `WriteSummaryAsync()`, `LoadCoverage()`, `NormalizeSwitch()`

**Verification**: After tests are written and CRAP re-runs, all
previously 0% methods should have >80% coverage and CRAP < 8.

## 2 — Category: Structural

**Symptom**: Single methods with CC > 15 (e.g., `NormalizeNode` at
CC=33, `Decide` at CC=16, `ClassifyChannel` at CC=9).

**Root cause**: Large switch expressions or deeply nested conditionals.
These are correct code — just too much in one method.

**Fix**: Extract category-level sub-methods. A 33-branch switch becomes:

```csharp
static List<object> NormalizeNode(SyntaxNode node) => node switch
{
    ExpressionSyntax e  => NormalizeExpression(e),
    StatementSyntax s   => NormalizeStatement(s),
    MemberDeclarationSyntax m => NormalizeMember(m),
    _                   => WalkChildren(node),
};
```

Each sub-method has CC=10-15, and the dispatcher has CC=1. Each sub-method
can be tested independently.

**Verification**: CC per method < 15, CRAP < 8 for all.

## 3 — Category: Boilerplate

**Symptom**: `Create()` methods with CC=4-7 and 0% coverage. All are
`new Option<X>("--flag") { Description = "..." }` pattern repeated.

**Root cause**: System.CommandLine requires option definitions. Each
option adds a line. The analyzer counts each option definition toward CC.

**Fix**: Extract all options to `private static readonly` fields (as
`AllCommand` already does). Then `Create()` becomes pure assembly:

```csharp
public static Command Create()
{
    var command = new Command("foo", "Description")
    {
        OptionA, OptionB, OptionC, OptionD, OptionE, OptionF,
    };
    command.SetAction(parseResult => Execute(
        parseResult.GetValue(OptionA)!.FullName,
        parseResult.GetValue(OptionB),
        ...));
    return command;
}
```

CC=1 at any coverage = CRAP=1. Passes clean.

**Verification**: All `Create()` methods have CC=1 and CRAP < 2.

## 4 — Execution Order

When eliminating violations on a solution, follow this order for maximum
leverage:

1. **Boilerplate first** — extract options to static fields. Eliminates
   ~3 violations with minimal work.
2. **Structural second** — split large switches/conditionals. Eliminates
   the biggest single CRAP score (`NormalizeNode` at 249.2).
3. **Untested last** — write tests for all `public static` handlers and
   commands. This is the bulk of violations but each one is mechanical.

## 5 — Verification

After all three categories are eliminated, run all gates:

```bash
dotnet run --project src/redmuffin.Tools.QualityGates -- all \
  --project <path> --test-project <test-path> \
  --auto-coverage --arch-config <config> --dupes
```

Expected: CRAP=PASS, SCRAP=PASS, ARCH=PASS, DUPES=PASS.

If any gate fails, repeat the recursive quality loop: identify the worst
violation, apply the category-appropriate fix, re-run.

## Related

- `tools/README.md` — Quality gates toolchain reference
- `rm-gates-cleanup` skill — Step-by-step gate remediation workflows
- `rm-guide-testing` skill — Test patterns and conventions
