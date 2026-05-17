---
title: "feat: Add Depth structural quality gate between Architecture and CRAP"
type: feat
status: active
date: 2026-05-17
origin: docs/brainstorms/2026-05-17-depth-quality-gate-requirements.md
---

# feat: Add Depth Structural Quality Gate Between Architecture and CRAP

## Summary

Add a sixth quality gate — Depth — that detects structural code problems the
existing gates miss: shallow methods (Ousterhout), parameter bloat (Martin,
Fowler), wrong abstractions (Metz), and entangled call chains (Ousterhout).
Follows the established Command/Handler pattern, uses Roslyn walkers per the
existing `CyclomaticComplexity` pattern, and integrates into `AllCommand` as
an always-on gate running between Architecture and CRAP. Composite-scored with
weighted signals (3/2/1/2), threshold ≥3 FAIL. Exit codes: 0=pass, 1=error,
2=violations.

---

## Problem Frame

The toolchain's existing five gates cover risk, test quality, dependencies,
thoroughness, and redundancy — but none detect over-decomposition. CRAP rewards
extraction by splitting complexity across methods, creating an asymmetric bias.
The six guiding authors independently identify this gap (see
[origin document](../brainstorms/2026-05-17-depth-quality-gate-requirements.md)
for full context and [author research](../research/structural-depth-author-research-2026-05-17.md)
for primary-source quotes). No major industry tool detects shallow modules — this
gate fills that gap.

---

## Requirements

- R1. Detect shallow methods: private methods with LOC ≤ 4 and trivial bodies
  (no loops, no branching, just delegation/simple arithmetic). Single-caller
  filtering requires call-graph analysis — deferred to Phase 2.
- R2. Detect parameter bloat: methods with >4 parameters (Martin, Fowler, NDepend)
- R3. Detect wrong abstractions: helper methods with if/switch on formal parameters
  (Metz conditional proliferation signal)
- R4. Detect entanglement proxy: private helper methods with ≥3 parameters and
  side effects in body (assignment, field access, non-pure calls) — a rough
  signal that the caller and callee must be read together. Full call-graph
  entanglement analysis deferred to Phase 2.
- R5. Composite-score each method with weighted signals (shallow=3, wrong-abst=2,
  params=1, entanglement=2)
- R6. Classify methods: FAIL (≥3), WARN (=2), INFO (=1), CLEAN (=0)
- R7. Exit code 2 when any FAIL method found; 0 otherwise; 1 on tool error
- R8. Integrate into `AllCommand` as always-on gate between Architecture and CRAP
- R9. Text output format: `FAIL  File.cs:42  Method()  composite=4  [shallow(3) + params(1)]`

**Origin actors:** Agent running quality gates, Developer reviewing gate output
**Origin flows:** F1 (run all gates → Depth detects problem → agent reviews), F2 (CRAP + Depth conflict → agent finds third path)

---

## Scope Boundaries

- Depth runs on all C# source files (not just test files like SCRAP)
- Depth is enabled by default in `AllCommand` (like Duplicates, not like CRAP).
  Users can disable with `--no-depth`.
- Phase 1: shallow + parameter bloat + wrong abstraction + entanglement proxy
- Depth uses only built-in thresholds from author research — no external config file
  required
- No manual exclusion lists — composite scoring handles false-positive filtering

### Deferred to Follow-Up Work

- Phase 2: Full call-graph entanglement analysis replacing the Phase 1 proxy
- `--json` output mode (existing gates support it; add when needed)
- Name-body mismatch detection (Beck/Fowler — too subjective for Phase 1)

---

## Context & Research

### Relevant Code and Patterns

- `tools/src/redmuffin.Tools.QualityGates/Commands/CrapCommand.cs` — template for CLI wiring
- `tools/src/redmuffin.Tools.QualityGates/Commands/CrapHandler.cs` — template for handler
- `tools/src/redmuffin.Tools.QualityGates/Analysis/CyclomaticComplexity.cs` — Roslyn walker pattern
- `tools/src/redmuffin.Tools.QualityGates/Commands/AllCommand.cs` — orchestration integration point
- `tools/src/redmuffin.Tools.QualityGates/Program.cs` — subcommand registration

### Institutional Learnings

- ADR-0004: Depth positioned between Architecture and CRAP, peer priority
- Operational gotchas doc: `dotnet run --project`, not `dotnet test`; run from `tools/`
- rm-guide-cleanup §2.1: Feathers seam threshold (≥5 lines of real logic)
- CRAP-driven refactoring doc: extract-helper was rejected specifically because it
  would create shallow methods — the exact scenario Depth gates against

### External References

- [Author research: Shallow Methods and Structural Quality](../research/structural-depth-author-research-2026-05-17.md)
- [ADR-0004: Depth Quality Gate](../adr/0004-depth-structural-quality-gate.md)
- John Ousterhout, _A Philosophy of Software Design_ (2021)
- Sandi Metz, "The Wrong Abstraction" (2016)

---

## Key Technical Decisions

- **CrapCommand pattern, not ScrapCommand.** CrapCommand uses a separate
  `CrapAction` lambda with typed `Execute()` parameters — cleaner for a
  standalone gate than Scrap's `Func<ParseResult, int>`.
- **Handler returns exit code directly** (CrapHandler pattern), not a tuple
  (ArchHandler pattern). No separate output formatter needed for Phase 1 —
  Depth output is simpler than Architecture's text/JSON modes.
- **Per-file `CSharpSyntaxTree.ParseText()`**, not `MSBuildWorkspace`.
  Established pattern in `CyclomaticComplexity.cs` and `MutationDiscoverer.cs`.
- **Composite score computed in analysis layer** (`DepthDetector`), not in
  handler. Handler receives pre-scored results and only classifies against
  threshold.
- **`--no-depth` flag disables the gate** in `AllCommand`, following the
  `--no-duplicates` pattern. Gate is on by default.
- **TDD execution posture**: write one failing test → minimal production code
  → refactor. Test the handler directly with constructed data and `StringWriter`.
- **Command/Handler pattern enforced**: All classes `public static`, no
  `InternalsVisibleTo`. Test handlers directly, never commands.

---

## Open Questions

### Resolved During Planning

- CLI subcommand name: `depth` (full word, matches Architecture, Mutation, Duplicates)
- Gate execution order: Architecture → Depth → CRAP → SCRAP → Mutation → Duplicates
- FAIL threshold: composite ≥ 3

### Deferred to Implementation

- Exact method names for helper extraction in `DepthDetector` — discover during implementation
- Specific Roslyn node types for entanglement proxy — verify against real code

---

## Implementation Units

- U1. **DepthResult model and DepthDetector analysis engine**

**Goal:** Build the Roslyn-based analysis engine that walks all methods in a
source directory, detects the four signals, and produces composite-scored
`DepthResult` records.

**Requirements:** R1, R2, R3, R4, R5

**Dependencies:** None

**Files:**

- Create: `tools/src/redmuffin.Tools.QualityGates/Analysis/DepthDetector.cs`
- Create: `tools/src/redmuffin.Tools.QualityGates/Analysis/DepthResult.cs`
- Create: `tools/tests/redmuffin.Tools.QualityGates.Tests/Analysis/DepthDetectorTests.cs`
- Create: `tools/tests/redmuffin.Tools.QualityGates.Tests/Fixtures/depth-fixtures/ShallowMethod.cs`
- Create: `tools/tests/redmuffin.Tools.QualityGates.Tests/Fixtures/depth-fixtures/ParameterBloat.cs`
- Create: `tools/tests/redmuffin.Tools.QualityGates.Tests/Fixtures/depth-fixtures/WrongAbstraction.cs`
- Create: `tools/tests/redmuffin.Tools.QualityGates.Tests/Fixtures/depth-fixtures/MixedSignals.cs`
- Create: `tools/tests/redmuffin.Tools.QualityGates.Tests/Fixtures/depth-fixtures/DeepMethods.cs`

**Approach:**

- `DepthResult` — `public sealed record` with fields: `FilePath`, `MethodName`,
  `LineNumber`, `IsShallow`, `ParameterCount`, `IsWrongAbstraction`,
  `IsEntangled`, `CompositeScore`, `Signals` (list of signal names)
- `DepthDetector.Analyze(string projectPath)` — walks `.cs` files via
  `Directory.GetFiles("*.cs")` → per-file `CSharpSyntaxTree.ParseText` →
  `DescendantNodes().OfType<MethodDeclarationSyntax>()`
- Inner `CSharpSyntaxWalker` subclass for each signal or a single walker
  that collects all data in one pass:
  - **Shallow:** private, LOC ≤ 4, no `ForStatement`/`WhileStatement`/`DoStatement`/
    `IfStatement`/`SwitchStatement`/`TryStatement` in body
  - **Parameter bloat:** `parameterList.Parameters.Count > 4`
  - **Wrong abstraction:** body contains `IfStatement` or `SwitchStatement`
    whose condition references a formal parameter by name
  - **Entanglement proxy:** private, `parameterList.Parameters.Count ≥ 3`,
    method body has side effects (assignment, field access, non-pure calls)
- `CompositeScore = (isShallow ? 3 : 0) + (isWrongAbstraction ? 2 : 0) +
(paramBloat ? 1 : 0) + (isEntangled ? 2 : 0)`
- Returns `IReadOnlyList<DepthResult>` sorted by composite descending
- **Error handling:** Catch `CSharpParseException` per-file — log warning,
  skip file, continue with remaining files. Return exit code 0 if zero `.cs`
  files found in the project directory (no methods to analyze — not an error,
  matching ScrapCommand convention). Exit code 1 only on tool-level failures
  (directory not found, all files failed to parse after files were discovered).

**Execution note:** Write characterization tests first — parse known fixture
files with known structural problems, verify correct detection.

**Patterns to follow:**

- `CyclomaticComplexity.Analyze()` — per-file parse pattern
- `MutationDiscoverer.FindSites()` — walk-all-nodes pattern

**Test scenarios:**

- Happy path: Method with LOC=2, no branching, private → shallow detected (composite=3)
- Happy path: Method with 5 parameters → param-bloat detected (composite=1)
- Happy path: Method with if(param) branching → wrong-abstraction detected (composite=2)
- Happy path: Method that is shallow AND has param bloat → composite=4 (both signals)
- Happy path: Deep method (LOC=15, branching, public, 2 params) → CLEAN (composite=0)
- Edge case: Constructor with 5 params but public API → NOT shallow (public)
- Edge case: Single-line expression-bodied method (LOC=1) → shallow (composite=3)
- Edge case: Method with exactly 4 params → NOT param-bloat (threshold is >4)
- Edge case: Private method with LOC=4, no branching, called from single caller → shallow
- Error path: Directory not found → IOException caught, exit code 1

**Fixture files:**

The existing test `.csproj` wildcard (`<Content Include="Fixtures\**\*" CopyToOutputDirectory="PreserveNewest"/>`)
already covers the new `depth-fixtures/` directory — no `.csproj` modification needed.

_ShallowMethod.cs_ — a private 2-line method with no branching, single caller:

```csharp
public class ShallowTarget
{
    public void Caller()
    {
        var result = ShallowHelper(5);
        Console.WriteLine(result);
    }

    private int ShallowHelper(int x)
    {
        return x + 1;
    }
}
// Expected: ShallowHelper → shallow (LOC=1, private, no branching, ≤2 params) → composite=3 FAIL
```

_ParameterBloat.cs_ — a method with 5 parameters:

```csharp
public class BloatTarget
{
    public void Configure(string a, string b, int c, bool d, double e)
    {
        Console.WriteLine($"{a} {b} {c} {d} {e}");
    }
}
// Expected: Configure → param-bloat (5 params > 4) → composite=1 INFO
// Expected: Configure → NOT shallow (public, LOC>4 after formatting)
```

_WrongAbstraction.cs_ — a helper whose logic branches on a parameter value:

```csharp
public class AbstractionTarget
{
    public string Format(string value, string mode)
    {
        return ApplyMode(value, mode);
    }

    private string ApplyMode(string input, string mode)
    {
        if (mode == "upper")
            return input.ToUpper();
        return input;
    }
}
// Expected: ApplyMode → wrong-abstraction (if on formal param "mode") → composite=2 WARN
```

_MixedSignals.cs_ — a method that triggers shallow + param-bloat:

```csharp
public class MixedTarget
{
    private int Combine(int a, int b, int c, int d, int e)
    {
        return a + b;
    }
}
// Expected: Combine → shallow (LOC=2, private, no branching, ≤2 params used) AND
// param-bloat (5 params > 4) → composite=4 FAIL [shallow(3) + params(1)]
```

_DeepMethods.cs_ — methods that should all score CLEAN:

```csharp
public class DeepTarget
{
    public int Compute(int x, int y)
    {
        if (x > 0)
        {
            var temp = Process(x);
            return temp + y;
        }
        return y;
    }

    private int Process(int value)
    {
        var result = 0;
        for (var i = 0; i < value; i++)
        {
            if (i % 2 == 0)
                result += i;
        }
        return result;
    }

    public DeepTarget(int a, int b, int c, int d, int e)
    {
        // constructor — public, not flagged as shallow
    }
}
// Expected: Compute → CLEAN (LOC>4, branching, public) → composite=0
// Expected: Process → CLEAN (LOC>4, loops+branching, but private and called from one place)
//   Note: private but deep (real logic, not a wrapper) — the LOC≤4 gate correctly excludes it
// Expected: DeepTarget constructor → NOT shallow (public constructor, 5 params)
```

**Verification:**

- `DepthDetectorTests` pass. `DepthDetector.Analyze()` returns correct
  composite scores for all fixture files. No false positives on deep methods.

---

- U2. **DepthHandler output formatting and exit code logic**

**Goal:** Build the handler that takes `IReadOnlyList<DepthResult>` and produces
text output with per-method severity classification, and returns the correct exit code.

**Requirements:** R6, R7, R9

**Dependencies:** U1 (DepthResult model)

**Files:**

- Create: `tools/src/redmuffin.Tools.QualityGates/Commands/DepthHandler.cs`
- Create: `tools/tests/redmuffin.Tools.QualityGates.Tests/Commands/DepthHandlerTests.cs`

**Approach:**

- `public static class DepthHandler`
- `public static int Run(IReadOnlyList<DepthResult> results, int failThreshold = 3, TextWriter? output = null)`
  - output ??= Console.Out
  - Sort results by composite descending
  - For each: write `{severity}  {file}:{line}  {method}()  composite={score}  [signals]`
    where severity is FAIL (≥3), WARN (=2), INFO (=1)
  - Return `results.Any(r => r.CompositeScore >= failThreshold) ? 2 : 0`
- Exit code 1 only from the command layer (parse failures, directory not found),
  never from the handler

**Execution note:** Write failing tests first — construct `DepthResult` lists,
call `Run()`, verify exit code and output format.

**Patterns to follow:**

- `CrapHandler.Run()` — exact signature pattern (results list, threshold, TextWriter)
- `CrapHandlerTests` — test via `StringWriter` injection

**Test scenarios:**

- Happy path: Empty results → exit 0, output empty or summary-only
- Happy path: One FAIL method → exit 2, output shows `FAIL  ...`
- Happy path: Mixed FAIL + WARN + INFO → exit 2 (FAIL wins), all three severities in output
- Happy path: Only WARN + INFO → exit 0, both severities in output
- Happy path: Only INFO → exit 0, severity shown
- Edge case: Composite exactly 3 → FAIL (threshold is ≥3, inclusive)
- Edge case: Composite exactly 2 → WARN (not FAIL)
- Edge case: Default `failThreshold` parameter → 3 when not specified

**Verification:**

- `DepthHandlerTests` pass. Verified output format:
  ```
  FAIL  ShallowMethod.cs:10  ShallowHelper()  composite=4  [shallow(3) + params(1)]
  WARN  Abstraction.cs:8     ApplyMode()      composite=2  [wrong-abstraction(2)]
  INFO  Bloat.cs:5           Configure()      composite=1  [params(1)]
  ```
  Exit codes correct for all severity combinations.

---

- U3. **DepthCommand CLI wiring**

**Goal:** Build the CLI subcommand with `--project`, `--verbose` flags,
`Create()` factory, and `Execute()` entry point that validates paths and
calls `DepthDetector.Analyze()` then `DepthHandler.Run()`.

**Requirements:** R7

**Dependencies:** U1 (DepthDetector), U2 (DepthHandler)

**Files:**

- Create: `tools/src/redmuffin.Tools.QualityGates/Commands/DepthCommand.cs`
- Create: `tools/tests/redmuffin.Tools.QualityGates.Tests/Commands/DepthCommandTests.cs`

**Approach:**

- `public static class DepthCommand`
- Static readonly `Option<DirectoryInfo>` for `--project` (required), `Option<bool>` for `--verbose`
- `public static Command Create()` — returns `Command("depth", "Analyze structural depth: shallow methods, parameter bloat, wrong abstractions, and entanglement.")`
  - Uses `SetAction(parseResult => ...)` with typed extraction
- `public static int Execute(string projectPath, bool verbose = false)`
  - Validates `Directory.Exists(projectPath)` → if not, writes error, returns 1
  - Calls `DepthDetector.Analyze(projectPath)`
  - Calls `DepthHandler.Run(results)`
  - Returns handler exit code
- Verbose mode: writes per-file summary ("Analyzed N methods in file.cs")

**Execution note:** Write failing tests first — call `Create()` and verify
command structure, then call `Execute()` with fixture paths.

**Patterns to follow:**

- `CrapCommand.Create()` — exact pattern for option definitions and `SetAction`
- `CrapCommand.Execute()` — exact pattern for validation + analysis + handler chain

**Test scenarios:**

- Happy path: Create() returns Command with correct name ("depth")
- Happy path: Execute() with valid fixture path → calls detector + handler, returns exit code
- Happy path: Fixture with no depth problems → exit 0
- Happy path: Fixture with shallow method → exit 2, output shows FAIL
- Edge case: Missing directory → exit 1, error message written
- Edge case: Verbose mode → additional per-file output

**Verification:**

- `DepthCommandTests` pass. Command creates correctly. Execute() returns correct
  exit codes for clean and dirty fixtures. Error path returns 1.

---

- U4. **AllCommand integration and Program.cs wiring**

**Goal:** Integrate Depth into the `all` command pipeline and register the
`depth` subcommand in `Program.cs`.

**Requirements:** R8

**Dependencies:** U3 (DepthCommand)

**Files:**

- Modify: `tools/src/redmuffin.Tools.QualityGates/Commands/AllCommand.cs`
- Modify: `tools/src/redmuffin.Tools.QualityGates/Program.cs`
- Modify: `tools/tests/redmuffin.Tools.QualityGates.Tests/Commands/AllCommandTests.cs`

**Approach:**

**AllCommand.cs changes:**

- Add `--depth` / `--no-depth` option (following `--duplicates` / `--no-duplicates` pattern):
  ```csharp
  private static readonly Option<bool?> DepthOption = new("--depth")
  {
      Description = "Run the depth analysis gate. Enabled by default. Use --no-depth to disable."
  };
  ```
- Add `private static int RunDepth(string projectPath)` method that calls
  `DepthCommand.Execute(projectPath)`. Synchronous — Depth is pure Roslyn
  parsing with no I/O or process execution. Returns exit code directly.
- `projectPath` comes from `SlnxProjectDiscovery` — same resolved path that
  CRAP, Architecture, and Dupes gates use. No new discovery logic needed.
- Call `RunDepth` in `ExecuteAsync` between Architecture and CRAP.
  This requires reordering the existing `ExecuteAsync` pipeline: move
  `RunArchAsync` before `RunCrapAsync`, insert `RunDepth` between them.
  Current order is CRAP→SCRAP→Arch; new order is Arch→Depth→CRAP→SCRAP.
- Add depth exit code to `CombineExitCodes(Math.Max(...))` call
- Update `BuildSummaryLine` to include Depth status
- Add `DepthOption` to `Create()` command options list

**Program.cs changes:**

- Add `rootCommand.Subcommands.Add(DepthCommand.Create());` in alphabetical position
  (between `DupesCommand` and `MutateCommand` — alphabetically correct)

**AllCommandTests.cs changes:**

- Update all `BuildSummaryLine` test expectations to include Depth PASS/FAIL/ERROR
- Add test: `--no-depth` disables the gate
- Add test: Depth exit in pipeline generates correct combined exit code

**Execution note:** Write failing AllCommand tests that expect Depth in summary line
before adding the code.

**Patterns to follow:**

- `DupesOption` / `RunDupesAsync` — exact pattern for always-on gate with `--no-` toggle
- `CombineExitCodes` — existing pattern, just add another parameter

**Test scenarios:**

- Happy path: `all` command includes Depth in execution, summary line shows PASS/FAIL
- Happy path: Depth FAIL exits with 2, overall exit is 2
- Edge case: `--no-depth` skips Depth gate entirely, summary line omits Depth
- Edge case: Architecture ERROR (1) + Depth FAIL (2) + CRAP PASS (0) → overall 2
- Integration: `CommandIntegrationTests` includes Depth in gate enumeration

**Verification:**

- `AllCommandTests` pass. Summary line includes Depth. `--no-depth` works.
  `Program.cs` registers depth subcommand. `dotnet run -- depth --help` shows options.
  Smoke test: `cd tools && dotnet run -- all` shows Depth in the gates listing
  and produces a PASS or FAIL status.

---

- U5. **Documentation: README gates table, rm-gates-cleanup decision tree, SN-0040 update**

**Goal:** Update all documentation to reflect the new gate: README gates table,
`rm-gates-cleanup` conflict resolution, and SN-0040 sidenote.

**Requirements:** None (documentation)

**Dependencies:** U4 (integration complete)

**Files:**

- Modify: `tools/README.md` — add Depth row to gates table
- Modify: `.opencode/skills/rm-gates-cleanup/SKILL.md` — add Depth+CRAP conflict decision tree in §0
- Modify: `.opencode/skills/tools-guide/SKILL.md` — update known issues, project structure
- Modify: `docs/sidenotes/SN-0040.md` — mark converted with plan reference

**Approach:**

- README gates table: add Depth row between Architecture and CRAP rows
- `rm-gates-cleanup §0`: add "Depth + CRAP Conflict" subsection with the
  three-step decision tree from the brainstorm requirements doc
- `tools-guide`: add Depth to execution order, known issues
- SN-0040: `status: converted`, `converted-to: docs/plans/2026-05-17-001-feat-depth-structural-quality-gate-plan.md`

**Test expectation:** none — documentation only

**Verification:**

- README gates table has 6 rows in correct order
- `rm-gates-cleanup` references Depth gate and conflict resolution
- SN-0040 shows converted status
- Success criteria: run `dotnet run -- depth` against both the tools and main
  solutions. Phase 1 is successful if at least one real method is caught
  (shallow, wrong abstraction, or param bloat that CRAP misses) AND the
  false-positive rate on the tools solution is ≤ 5 FAIL results. This gates
  Phase 2 investment.

---

## System-Wide Impact

- **Interaction graph:** Depth runs between Architecture and CRAP in
  `AllCommand.ExecuteAsync`. No other gate depends on Depth output.
- **Error propagation:** Depth exit code 1 (tool error) prevents overall
  pass via `CombineExitCodes`, matching existing gate behavior.
- **State lifecycle risks:** None — Depth is read-only Roslyn analysis,
  no file mutation, no process execution.
- **API surface parity:** `DepthCommand.Execute()` follows `CrapCommand.Execute()`
  signature — consistent with all other gates.
- **Integration coverage:** `AllCommandTests` covers Depth integration;
  `CommandIntegrationTests` covers end-to-end gate execution against real code.
- **Unchanged invariants:** All existing gates continue to work identically.
  No existing tests modified (only `AllCommandTests` extended). No existing
  CLI flags changed. The `all` command default behavior adds Depth as always-on.

---

## Risks & Dependencies

| Risk                                                                                                     | Mitigation                                                                                                                                                                                                                                             |
| -------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Shallow detection flags legitimate public API methods                                                    | Shallow detection is limited to private methods (R1). Public API methods are excluded by design, not by composite scoring.                                                                                                                             |
| Shared private utility methods with LOC ≤ 4 flagged as shallow despite multiple callers                  | Known Phase 1 limitation — single-caller filtering deferred to Phase 2. Accept the noise; Phase 2 call-graph analysis will resolve.                                                                                                                    |
| Entanglement proxy has false positives                                                                   | Weight 2 contributes to composite scoring — can combine with other signals to produce FAIL. Full call-graph in Phase 2 will replace the proxy. Accept proxy noise for now; use --no-depth to suppress the gate if false-positive rate is unacceptable. |
| Wrong-abstraction detection flags valid parameterized helpers (e.g., `string FormatMessage(Severity s)`) | Acceptable — these ARE structural concerns per Metz. WARN, not FAIL. Human reviews.                                                                                                                                                                    |
| `AllCommandTests` must be updated for every new summary-line format                                      | Follow existing pattern — each test already expects a specific number of gate statuses.                                                                                                                                                                |

---

## Documentation / Operational Notes

- `tools/README.md` gates table updated with Depth row
- `docs/adr/0004-depth-structural-quality-gate.md` already exists
- `docs/brainstorms/2026-05-17-depth-quality-gate-requirements.md` already exists
- `docs/research/structural-depth-author-research-2026-05-17.md` already exists
- After implementation: capture operational knowledge via `ce-compound`

---

## Sources & References

- **Origin document:** [docs/brainstorms/2026-05-17-depth-quality-gate-requirements.md](../brainstorms/2026-05-17-depth-quality-gate-requirements.md)
- **ADR:** [docs/adr/0004-depth-structural-quality-gate.md](../adr/0004-depth-structural-quality-gate.md)
- **Research:** [docs/research/structural-depth-author-research-2026-05-17.md](../research/structural-depth-author-research-2026-05-17.md)
- **Sidenote:** [docs/sidenotes/SN-0040.md](../sidenotes/SN-0040.md)
- Related code: `tools/src/redmuffin.Tools.QualityGates/Commands/CrapCommand.cs`
- Related code: `tools/src/redmuffin.Tools.QualityGates/Analysis/CyclomaticComplexity.cs`
- Related code: `tools/src/redmuffin.Tools.QualityGates/Commands/AllCommand.cs`
