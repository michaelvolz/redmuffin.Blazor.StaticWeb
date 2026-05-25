---
name: rm-tdd
description: Triggers automatically for any task that involves writing new code with tests first or generating comprehensive tests for existing code or any test related work Enforces veteran TDD standards from Robert C Martin and Kent Beck with LLM optimized guardrails for planning vertical slicing one test at a time 100 percent meaningful coverage black box focus and maximum refactor safety
---

**OpenCode Skill: Ultimate Professional TDD Testing Mastery Protocol (LLM-Optimized)**

**Trigger Condition:**  
This skill activates automatically with full precedence whenever the task involves writing new code with tests-first TDD, generating comprehensive tests for existing code, improving tests, reviewing tests, or any test-related work. It overrides all other instructions for test generation or modification.

**Objective:**  
Produce the highest-quality, professional-grade tests that strictly follow veteran TDD recommendations from Robert C. Martin (Uncle Bob) and Kent Beck. Tests must function as clean, maintainable, first-class artifacts that serve as precise living documentation of observable behavior. They must enable massive internal code changes, complete refactors, algorithmic replacements, or architectural overhauls without any test breakage. They must achieve 100 % meaningful code coverage (statement, branch, and path) wherever technically feasible through disciplined, systematic design. The entire process is engineered exclusively for LLM agents in OpenCode; all human hand-coding flexibility has been removed in favor of rigid, repeatable guardrails that prevent LLM-specific failure modes.

**Mandatory LLM Execution Workflow (Follow Exactly – No Deviations):**

1. **Planning Phase (Always Execute First)**  
   Analyze the user request or supplied existing code. Identify all public interfaces, contracts, and observable behaviors. Create a concise prioritized list of behaviors to test, starting with core happy paths and critical logic. Present the proposed public API shape and prioritized behavior list back to the user and obtain explicit confirmation before writing any test or production code.

2. **Scenario Detection**
   - **Tests-First Pure TDD:** No production code exists → strictly obey the Three Laws of TDD for every behavior.
   - **Comprehensive Tests for Existing Code:** Production code already supplied → first write characterization tests that lock down current observable behavior exactly as it stands, then expand to full edge cases and error paths.

3. **Vertical Slicing / Tracer-Bullet Discipline (Non-Negotiable for LLMs):**  
   Never generate a large test suite horizontally in one pass. Work exclusively in small vertical slices: one observable behavior at a time. Each slice must be fully completed (test written, code made to pass, both refactored) before moving to the next. This prevents speculative tests, ensures tests are grounded in actual implemented behavior, and keeps you from outrunning your headlights.

4. **Strict Per-Behavior Incremental Cycle**  
   For each behavior in the prioritized list:  
   a. Write the minimal failing test (or characterization test for existing code).  
   b. Write the minimal production code sufficient to make that single test pass.  
   c. Refactor both production code and test code for cleanliness, readability, and removal of duplication.  
   d. Run the test suite to confirm green status.  
   e. Only then proceed to the next behavior.  
   Never refactor while any test is red. After the full set of behaviors is complete, perform a final whole-module refactor pass and re-verify coverage.

**Core Principles (Non-Negotiable – Adhere Strictly):**

1. **Three Laws of TDD (Robert C. Martin):**
   - Do not write any production code until a failing unit test exists.
   - Do not write more of a unit test than is sufficient to fail (compilation failures count).
   - Do not write more production code than is sufficient to pass the single failing test.

2. **F.I.R.S.T. Test Qualities:**
   - Fast, Independent, Repeatable, Self-Validating, Timely.

3. **Behavior-Driven Black-Box Focus (Essential for Refactor Safety):**  
   Test only observable public contracts: inputs, outputs, defined side effects, and declared exceptions. Never test private methods, internal data structures, specific algorithms, call orders, or implementation details. This guarantees that any internal rewrite leaves the entire test suite untouched.

4. **AAA (Arrange–Act–Assert) Structure:**  
   Every test must follow this exact pattern for clarity and consistency.

**Comprehensive Guidelines for Test Creation:**

1. **Test Naming Convention:**  
   Names must be full, human-readable sentences describing the behavior. Preferred formats:  
    should*[do_something]\_when*[condition]  
    given*[initial_state]\_when*[action]_then_[expected_result]  
   The name alone must allow any reader to understand the exact scenario without reading the body.

2. **Coverage Strategy (Target 100 % Meaningful Coverage):**  
   Achieve 100 % statement, branch, and path coverage by intentional design wherever feasible. Systematically cover:  
   • All happy paths (nominal success).  
   • Every edge and boundary value (boundary-value analysis).  
   • All equivalence classes of inputs.  
   • Zero, one, and many iterations for loops.  
   • Every conditional branch and decision point.  
   • All defined exception paths and error-handling behaviors.  
   • Invalid, null, empty, maximum, and minimum inputs.  
   • All contractual state transitions.  
   If coverage tools are available, generate the suite then add cases until 100 % is reached. Remove dead code rather than artificially covering it. Prioritize critical and complex paths first; extend to exhaustive coverage on subsequent slices.

3. **Assertions and Focus:**  
   One logical concept or assertion group per test. Use precise, domain-specific equality checks. Assert exclusively on public outputs, returned values, final contractual state, thrown exceptions, or observable side effects. Avoid vague or interaction-heavy assertions unless the contract explicitly requires them.

4. **Handling Dependencies and Test Doubles:**  
   Never use a mock when a real collaborator or in-memory fake would suffice. Never introduce test doubles inside system boundaries. Never use interaction verification when state verification captures the same contract. Never hardcode a dependency in production code that could be injected.

5. **Test Organization and Maintainability:**  
   Group tests by feature or public contract. Use test factories or builders for readable setup. Support parameterized tests for multiple data points. Keep setup and teardown minimal and explicit. Eliminate any test-order dependency. Refactor tests themselves whenever duplication appears.

**Common Pitfalls to Eliminate Completely (LLM-Specific Guardrails):**

- Horizontal slicing: writing many tests before any implementation.
- Over-generation of speculative or imagined behaviors.
- Tests coupled to implementation details that break on refactor.
- Duplication of production logic inside tests.
- Overuse of mocks for internal collaborators.
- Testing private members instead of exposing necessary public behavior.
- Skipping “simple” paths or error cases.
- Tests that require manual inspection or contain complex internal logic.
- Non-deterministic behavior or reliance on global state.
- Tests longer or more complex than the code they verify.

**Pseudocode Examples (Language-Agnostic – Adapt to Target Syntax):**

    test_should_calculate_sum_correctly_for_positive_numbers
        // Arrange
        calculator = new Calculator()
        input_numbers = [1, 2, 3]

        // Act
        result = calculator.calculateSum(input_numbers)

        // Assert
        assert result equals 6

    test_should_throw_illegal_argument_when_input_is_null
        // Arrange
        calculator = new Calculator()

        // Act & Assert
        expect exception IllegalArgumentException when
            calculator.calculateSum(null)

**Execution Mandate:**  
Follow every rule and workflow step above rigorously and without exception. Generate complete, self-contained test suites that cover the entire public surface of the module, class, or function. Verify coverage, readability, and refactor safety before finalizing any output. Align every test precisely to confirmed specifications or observed behavior. The resulting tests must be production-ready artifacts that veteran TDD practitioners would accept unchanged.

## Test Quality Verification

After writing tests, verify they actually catch logic errors.
See `rm-quality-gates` §4 for mutation testing workflow — the
per-survivor decision tree (equivalent / no coverage / weak test),
one-at-a-time TDD fix loop, and 100% kill rate standard.

## C# Refactoring Patterns

The Refactor step of red-green-refactor benefits from functional C#
patterns that directly reduce complexity and improve testability.
See `rm-guide-csharp-functional` for the full catalog — LINQ pipelines,
FrozenDictionary lookups, pattern-matching switch expressions, and
pure static methods eliminate branching that would otherwise require
extraction during later cleanup passes.
