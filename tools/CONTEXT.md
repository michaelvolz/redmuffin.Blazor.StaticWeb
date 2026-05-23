# Tools Solution — Domain Language

## Quality Gates

**Gate**
A quality check that analyzes .NET source code and produces a gate result.
Each gate has an entry point in the CLI, an orchestrator that coordinates
execution, and one or more algorithm modules that perform the analysis.
Six gates exist: Architecture, Depth, CRAP, SCRAP, Mutation, Duplicates.

**Gate result**
The output of a gate: an exit code (0 = pass, 2 = violations found,
1 = error) plus a typed result model (ArchResult, DepthResult, etc.).

**Gate sequence**
The ordered execution of all gates: Architecture → Depth → CRAP →
SCRAP → Mutation → Duplicates. The sequence is fixed — structural gates
run first because structural fixes can eliminate CRAP violations.

**All command**
The CLI entry point that runs the full gate sequence. Not a gate itself —
it discovers projects, runs the coverage pipeline, executes each gate in
sequence, and collects their gate results.

**CLI layer**
System.CommandLine wiring. Handles argument parsing, flag registration,
and defaults. Each gate has a CLI class (e.g., ArchitectureCommand).

**Orchestrator layer**
Coordinates gate execution. Public static Run/RunAsync methods that
orchestrate the algorithm modules, produce the gate result, and return
the exit code. Each gate has an orchestrator class (e.g., ArchitectureHandler).

**Algorithm layer**
The analysis code behind each gate. Roslyn syntax tree walkers, Jaccard
similarity comparers, Cobertura XML parsers, mutation applicators.
Lives in the Analysis/ directory.

**Coverage pipeline**
Code coverage generation for CRAP and Mutation gates. Stages:
generate per-project Cobertura XML → merge into single file →
parse into in-memory coverage data → gates read line coverage percentages.

**Mutation site**
A location in source code where a mutation rule can apply.
Discovered by MutationRules via Roslyn syntax tree walking,
classified by coverage (covered or uncovered), then applied
by MutationApplicator.

**Mutation rule**
A specific code transformation (e.g., swap `+` for `-`, swap `>` for `<`).
Six categories: arithmetic, comparison, equality, boolean, conditional,
constant. Each rule discovers sites and applies the transformation.

**Mutation survivor**
A mutant that passes the test suite. Must be killed by writing
behavioral tests that detect the change. The only acceptable survivor
is an equivalent mutant (the transformed code is semantically identical).

**Visitor**
A Roslyn CSharpSyntaxWalker subclass that walks syntax trees to compute
metrics (CyclomaticComplexity), discover sites (MutationRules), or
detect patterns (DepthDetector). Named after the computation it performs.

**Coverage gap**
A method with high complexity but no genuine CRAP violation — classified
as a conductor method (delegation-only, covered) or switch dispatcher
(arm delegation, covered by integration tests). Not a FAIL; informational only.

## ConfigureAwait Fixer

**Fixer**
A console tool that loads the official Microsoft CA2007 analyzer,
runs it against source files, and automatically adds `.ConfigureAwait(false)`
to every `await` flagged by the analyzer.

**MSBuild workspace**
The Roslyn MSBuildWorkspace that loads a project file, resolves
NuGet packages, and creates a Compilation with full type information.
Used by the fixer to run the CA2007 analyzer with real project context.

**Formatter pipeline**
The OpenCode on-save hook that runs the fixer. Configured in
`opencode.jsonc` — the fixer runs transparently on every `.cs` file
save, before `dotnet build`.
