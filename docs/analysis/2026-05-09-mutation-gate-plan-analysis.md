---
date: 2026-05-09
title: Mutation Testing Gate — Flow Completeness & Edge Case Analysis
tags: [mutation-testing, quality-gates, analysis, edge-cases, roslyn]
problem_type: spec-review
---

## What Belongs in This File

Analysis of the mutation testing gate plan against the original clj-mutate
reference implementation and the existing Quality Gates codebase. Identifies
missing edge cases, state transitions, error paths, and integration gaps.

## User Flows

### Flow 1: Full Mutation Run (first time, no manifest)

1. User runs `quality-gates mutate --project src/Foo --test-project tests/Foo.Tests --lcov-file coverage.lcov.info`
2. Tool walks all `.cs` files via Roslyn, discovers mutation sites
3. Reads LCOV coverage, partitions sites into covered/uncovered
4. Runs baseline `dotnet test` to verify tests pass
5. If baseline fails → abort, exit 1
6. For each covered site in parallel (up to --max-workers):
   - Applies mutation via Roslyn SyntaxRewriter
   - Runs `dotnet test` with timeout
   - Classifies: killed (tests fail/timeout) or survived (tests pass)
   - Restores original source
7. Writes embedded manifest footer to mutated `.cs` files
8. Prints summary: X/Y killed (Z%), lists survivors
9. Exit 2 if any survivor, exit 0 if all killed

### Flow 2: Differential Run (manifest exists)

1. Same as Flow 1 steps 1-4
2. Extract embedded manifest from each `.cs` file
3. Compute module-hash for each file; if unchanged → skip file (0 mutations)
4. For files with changed module-hash: compute per-form hashes, identify changed forms
5. Run mutation only on sites in changed forms
6. Update manifest footer at end

### Flow 3: --scan (structural only)

1. Walk `.cs` files, discover mutation sites
2. Report total count, count on changed forms (if manifest exists)
3. Report mutation warning if count > threshold
4. No baseline test, no mutation execution, no manifest write
5. Exit 0

### Flow 4: Integrated via AllCommand

1. CRAP runs first (needs coverage)
2. Mutate runs second (needs coverage, baseline test passing)
3. SCRAP runs third
4. Architecture runs fourth
5. AllCommand combines exit codes, mutate exit 2 → overall exit 2

---

## Gaps

### Critical

**G1: Paradigm gap — Roslyn mutation discovery vs Clojure form-walking**

The clj-mutate mutation rules operate on Lisp atoms (`+`, `>`, `=`, `if`, `0`, `1`) within S-expressions. The C# port must redescover these semantics in Roslyn syntax trees. This is not a simple 1:1 translation:

- Clojure `+` at head position → C# `BinaryExpressionSyntax` with `PlusExpression` kind — but also `+=` compound assignment
- Clojure `inc`/`dec` → C# `PrefixUnaryExpressionSyntax` with `++`/`--`, or `x + 1` patterns
- Clojure `if`/`if-not` → C# `IfStatementSyntax` (negate condition, or swap then/else blocks)
- Clojure `when`/`when-not` has no direct C# equivalent; `if (cond) { ... }` patterns only
- Clojure `0`/`1` constants → C# numeric literals — but C# has many more numeric types (`0.0f`, `0m`, `0L`)

The spec says "arithmetic, comparison, equality, boolean, conditional, constant" — but does not specify which Roslyn syntax kinds map to each category. This is the single largest source of ambiguity.

**G2: Suppression rules are Clojure-specific and must be redesigned**

clj-mutate has four suppression predicates (`rand-comparison?`, `rand-nth-guard-form?`, `rand-nth-single-element-guard?`, `inside-rand-nth-literal?`) that are entirely Clojure-specific. These prevent false-positive mutations on certain Clojure idioms. The C# port needs its own set of suppression rules. Without them, every `if (list.Count > 0) list[0]` will generate a survivor when `>` is mutated to `>=` (off-by-one at boundary is equivalent). The plan mentions none of this.

**G3: Worker isolation for dotnet test is architecturally challenging**

clj-mutate creates worker directories under `target/mutation-workers/` and copies only the source file. Each worker runs `clj -M:spec` from its own directory, which only needs the mutated source file. `dotnet test` requires the entire project structure (`.csproj`, `obj/`, `bin/`, all source files). Copying the full project per worker is expensive. Running all workers against the same project directory introduces race conditions (multiple workers write the same source file). The plan does not address how to isolate `dotnet test` execution per worker.

**G4: AllCommand has zero mutation options — integration surface undefined**

AllCommand.cs currently accepts `--project`, `--test-project`, `--coverage-file`, `--arch-config`, `--changed`, `--verbose`. None of the mutation gate's 8+ options are wired. The mutation gate needs `--scan`, `--max-workers`, `--since-last-run`, `--mutate-all`, `--lines`, `--mutation-warning`, `--timeout-factor`, `--reuse-lcov`. The `CombineExitCodes` method takes 3 parameters (crap, scrap, arch) — needs a 4th.

**G5: Coverage format mismatch — CRAP uses Cobertura XML, mutation gate uses LCOV**

The existing CRAP gate parses Cobertura XML (`CoverageParser.cs`). The spec says mutation gate reads LCOV from coverlet. These are different formats from different coverlet invocations. Does the tool need two coverage files? Does it auto-convert? Does the user need to run coverlet twice with different output formats?

**G6: Manifest writes modify source files tracked by git — no handling specified**

clj-mutate embeds a manifest footer (`;; clj-mutate-manifest-begin ... ;; clj-mutate-manifest-end`) in every mutated source file. After `quality-gates all` completes, every C# source file will be modified (manifest only). This interacts with:

- `--changed` flag on other gates (CRAP, SCRAP) which run `git diff HEAD --name-only`
- Developer's working directory (staged changes, dirty tree in IDE)
- The pattern of "run gates, commit manifest changes" needs documentation

### Important

**G7: Multiple-file mutation vs single-file original**

clj-mutate operates on exactly one file (`src/.../file.clj`). The spec says "Discovers mutation sites in C# source files" (plural). How does the C# port handle multiple files?

- Mutate all `.cs` files in the project?
- Accept a file path argument like the original?
- Accept a directory and discover all `.cs` files?
- Skip generated files (`.g.cs`, `.Designer.cs`)?
  The manifest-per-file model from clj-mutate scales naturally to multiple files, but discovery and orchestration across a multi-file project needs definition.

**G8: Timeout classified as 'killed' — ambiguous semantics**

clj-mutate classifies timeout as `:killed`. The rationale is that a mutation that causes an infinite loop or deadlock is definitely a bad mutation. But for the quality gate, "killed" means "test detected the mutation." A timeout could mean "infinite loop in production code" (a good kill) or "test infrastructure timeout" (not a kill). The original doesn't distinguish. The C# port should at minimum log timeouts separately.

**G9: Baseline test command construction for dotnet**

clj-mutate uses `clj -M:spec --tag ~no-mutate` as default. The C# equivalent is `dotnet test <test-project>` — but which test project? The one passed to AllCommand? A separate `--test-project` option on the mutate subcommand? Does the baseline test include `--no-build` (assuming the project was already built by CRAP)?

**G10: Mutation application method — text replacement vs Roslyn rewrite**

clj-mutate uses regex-based text replacement (`token-pattern` → `str/replace-first`) on source lines. This is fragile even in Clojure. For C#, the correct approach is Roslyn's `SyntaxRewriter` which produces a new syntax tree and calls `.ToFullString()`. But the mutation index mapping (`find-mutations` and `apply-mutation` must walk identically) becomes complex with Roslyn — the rewriter doesn't use indices, it visits nodes. The plan says "rewriting source files" without specifying the mechanism.

**G11: Interrupted run recovery — backup restore**

clj-mutate creates `file.mutation-backup` before writing manifest. On next run, if backup exists, restores it and reports "Restored source from backup." The C# port needs this. Without it, a killed process leaves source files in an unknown state (possibly with a partial manifest, possibly with a mutant applied).

**G12: No coverage data → treat all sites as covered (policy decision)**

clj-mutate: "If coverage data is unavailable, all mutation sites are treated as covered." This is a deliberate policy — better to run extra mutations than skip potentially uncovered ones. The C# port must replicate this. But the AllCommand already requires `--coverage-file` (Required = true). If the mutate gate is run standalone (not via `all`), it needs its own coverage option with different semantics.

### Minor

**G13: --mutation-warning threshold default (50) may not fit C#**

clj-mutate warns if total mutations > 50 per file. C# files tend to be larger than Clojure files. A single C# controller class could easily have 100+ mutation sites. This threshold may need to be configurable per-project or raised by default.

**G14: No --verbose mode for per-mutant output in the AllCommand**

The existing `--verbose` flag on AllCommand shows detailed per-gate output for CRAP and SCRAP. The mutation gate needs to integrate with this — showing per-mutant progress when verbose, summary only when not.

**G15: Manifest footer in C# — need a comment format**

clj-mutate uses `;;` Clojure comments for the manifest. C# needs `//` or `/* */`. The manifest is a JSON blob — `/* clj-mutate-manifest-begin ... */` or `// clj-mutate-manifest-begin` followed by `//` prefixed JSON lines. The format must be parseable by both humans and the tool, and must not interfere with Roslyn parsing (Roslyn handles comments fine).

**G16: Exit code mapping for CombineExitCodes**

The existing convention: 0 = pass, 1 = error, 2 = threshold breach (FAIL). clj-mutate exits 0 on all-killed, but the Uncle Bob skill treats survivors as a failure that must be fixed. So the mutate gate should exit 2 when any mutant survives. The `CombineExitCodes` method needs extending.

---

## Questions

1. **Worker isolation**: How should `dotnet test` be isolated per worker? Copy the project tree to a temp directory per worker (slow), or run all workers sequentially against the same directory (no parallelism)? Default: sequential with `--max-workers 1` until a fast-isolation strategy is designed.

2. **Mutation file scope**: Should the C# port mutate one file (like the original), a directory, or all `.cs` files in the project? Default: accept a `--source-file` path, matching clj-mutate's single-file model. Multi-file can come later.

3. **Coverage format**: Should the mutate gate use the same Cobertura XML as CRAP, or should AllCommand require a separate `--lcov-file` option? Default: the mutate gate requires its own `--lcov-file` (matching the original's LCOV dependency), and AllCommand gains an optional `--lcov-file` option.

4. **Manifest suppression rules**: What C# patterns should suppress specific mutations? Example: `if (list.Count > 0)` where `>` → `>=` is equivalent at the boundary. Default: start with zero suppression rules and add them as false survivors are discovered during development.

5. **Timeout classification**: Should timeouts be classified as "killed" (matching the original) or as a separate "timeout" category that still counts as a kill for the pass/fail gate? Default: classify as killed but log as "TIMEOUT" (matching the original's `result-label` function).

---

## Recommended Next Steps

1. **Resolve G3 (worker isolation) before writing any code.** This is the architectural bottleneck. Prototype `dotnet test` isolation: can we build a shadow project that references the original's `obj/` output but has its own source files? Or does `dotnet test` with `--project` + file copy work?

2. **Define mutation rules for Roslyn syntax kinds** (G1). Write a mapping document: for each clj-mutate rule, what Roslyn `SyntaxKind` and `CSharpSyntaxNode` subclass it maps to. This surfaces all translation ambiguities.

3. **Add mutation gate scaffolding to AllCommand** (G4). Wire the `mutate` subcommand into `Program.cs`, add skeleton options to `AllCommand.cs`, extend `CombineExitCodes`. Do this first so the integration surface is visible.

4. **Confirm coverage file strategy** (G5). If the mutate gate gets its own `--lcov-file` option, the AllCommand needs one too. If it converts Cobertura → LCOV internally, only one file is needed.

5. **Address worker isolation strategy** (Q1, G3) and **mutation scope** (Q2) before deep implementation.
