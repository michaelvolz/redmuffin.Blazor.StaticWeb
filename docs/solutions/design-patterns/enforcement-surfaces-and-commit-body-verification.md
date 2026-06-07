---
module: agent-instructions
date: 2026-06-07
problem_type: design_pattern
component: assistant
severity: high
tags:
  - enforcement-surfaces
  - negative-constraints
  - commit-body-verification
  - instruction-design
  - tool-call-gate
  - priming-hazards
  - survival-test
  - rana-2026
applies_when:
  - "Writing or editing agent instruction files (AGENTS.md, SKILL.md, agents)"
  - "Designing multi-step workflows where step N depends on step N-1 being correct"
  - "Adding a rule that must survive agent override pressure during generation"
  - "Debugging why an agent knows a rule but still violates it"
root_cause: inadequate_documentation
resolution_type: documentation_update
related_components:
  - documentation
  - development_workflow
---

# Agent Rule Enforcement: Channel Architecture

## Context

During an instruction-file optimization session for the commit workflow,
we observed a persistent class of agent violations that resisted every
conventional fix. The agent represented the constraint correctly when
interrogated — it could recite the rule verbatim and explain its
rationale. Yet during execution, it violated the rule at a stable rate.
Clarifying wording, adding emphasis, repeating the rule in multiple
locations, and increasing instruction budget all failed to reduce the
failure rate.

The root cause was not rule clarity. It was the **enforcement channel**.

Agent behavioral rules land in one of three channels, each with a
radically different failure profile. A rule placed in the wrong channel
will fail no matter how well-written it is. The solution is not to write
better rules — it is to move the rule to a channel whose failure surface
matches the task's risk tolerance.

This pattern was discovered during the commit protocol optimization. The
full case study and the thought-phase regulation mechanism that preceded
this generalization are documented in `agent-protocol-enforcement-
thought-phase-regulation.md` (best-practices/). That doc describes the
four-layer defense applied to the commit protocol. This doc generalizes
the underlying enforcement surfaces to any agent instruction.

## Guidance

### Enforcement Surface Taxonomy

Every agent rule lands in exactly one of three channels. The channel is
determined not by what the rule says, but by _how the agent verifies
compliance_.

**Channel 1: Tool-Call Gates (near-zero failure rate)**

A tool-call gate encodes the constraint as a required tool invocation
whose presence or absence is externally verifiable. The agent either
makes the call or it does not — there is no ambiguous middle state.

```
"The first tool call after 'commit' must be skill(name: 'rm-commit')."
```

Why this works: Rana (2026) found that override pressure is strongest
during token-generation steps where the model autoregressively selects
tokens. A tool-call decision exits generation mode and enters
tool-selection mode, where the model chooses from a discrete set of
available tools. The override mechanism that suppresses constraint
representations during token generation (4.4× weaker suppression signal
in layers 23-27 on failure runs) does not apply to tool selection in the
same way.

Observed failure rate: zero violations in 40+ commit batches after
moving constraint from cognitive channel to tool-call gate.

**Channel 2: Text-Output Constraints (~12.5% override failure)**

A text-output constraint governs what the agent writes in its prose
response. The constraint is represented in the agent's internal state
but the override mechanism can suppress that representation during
autoregressive token generation.

```
"Never describe the diff — explain WHY the change exists."
```

Rana (2026) demonstrated that on failure runs, the attention pattern for
constraint-related tokens is 4.4× weaker in layers 23-27 compared to
success runs. The model does not "forget" the constraint — it represents
it but fails to apply it during generation. This explains why adding
emphasis, repetition, or bolding does not reduce the failure rate: the
constraint representation is already present. The problem is suppression
during generation, not absence during comprehension.

**Channel 3: Cognitive Self-Checks (highest failure rate)**

A cognitive self-check asks the agent to introspect on its own output
and verify compliance. This is the most fragile channel because it
depends on the override mechanism not suppressing the constraint during
_two_ generation phases: first when producing the output, then again
when verifying it.

```
"Read your body. Does it describe the diff? If so, delete those
sentences."
```

If override pressure suppressed the constraint during initial
generation, it will almost certainly suppress it during self-verification
too — the same upstream mechanism is in play. Worse, the agent has no
external signal that it should even initiate verification. The step is
entirely internal, invisible to both the runtime and the human reviewer.

### The Tool-Call Gate Pattern

The structural fix for any multi-step workflow with an intermediate
output constraint is a three-phase gate. Separate construction,
verification, and execution by tool-call boundaries so that the
intermediate output is externally inspectable before the next phase
executes.

**Phase 1 — Construct:** Build the output text.

**Phase 2 — Verify (tool-call gate):** `Write-Output $text` to print to
stdout. The tool output forces external inspection — separating producer
mode from reviewer mode. Apply the survival test.

**Phase 3 — Execute (tool-call gate):** Write to temp file, then execute
from the file. Never pipe directly — `Write-Output $text | git commit
-F -` removes the verification gate. The temp-file detour is the entire
point.

```
# PowerShell
Write-Output $message                     # Phase 2: verify
Set-Content -Path "$env:TEMP\msg.txt" -Value $message
git commit -F "$env:TEMP\msg.txt"         # Phase 3: execute

# Bash
echo "$message"                           # Phase 2: verify
echo "$message" > /tmp/msg.txt
git commit -F /tmp/msg.txt                # Phase 3: execute
```

### The Survival Test

Mechanical phrase checks ("does the body contain the word 'add'?") are
fragile and gameable. The agent can describe a diff without using any
banned word. The survival test is a semantic check that works on any
body, for any constraint, without enumerating trigger phrases:

```
Delete every sentence that describes what was added, removed, or
changed. Does the why survive?
```

If the remaining text still explains _why_ the change exists, the body
passes. If deleting all description-of-change sentences leaves an empty
or nonsensical body, the body describes the diff and must be
regenerated.

The survival test is universal because it operates on meaning, not
vocabulary. A sentence like "The user needed X" survives because it
explains motivation. A sentence like "Added a null check in
ProcessPayment" fails because it describes what changed.

### Deletion Rule

Applied before the survival test. This is a mechanical pre-filter that
catches the most common failure mode with near-zero false positives:

```
If a sentence only names what was added, removed, or changed, it
describes the diff. Delete it. The body is not a release note.
```

Sentences the deletion rule catches:

- "Added null check to ProcessPayment." (names what was added)
- "Removed the legacy CustomerService facade." (names what was removed)

Sentences the deletion rule passes (the survival test must judge):

- "Null reference in ProcessPayment could crash the payment pipeline
  silently." (explains risk, not change)

### Two-Gate Architecture

Workflows where both comprehension AND application can fail need two
independent gates. The commit workflow is the canonical example:

**Gate 1 — Comprehension Gate** (global AGENTS.md step 0): ensures the
agent loads and understands the rules before beginning work.

```
"After loading rm-commit, print your understanding of the body
verification gate to stdout via Write-Output."
```

**Gate 2 — Application Gate** (rm-commit body verification): ensures the
constructed output meets the constraint before it becomes permanent.

```
"Write-Output $fullMessage"
"Read tool output. Verify body passes survival test."
"Only after verification: Set-Content temp-file; git commit -F temp-file"
```

### Priming Hazard Mitigation

Instruction files often include examples of what NOT to do, marked with
✗ or "BAD." Rana (2026) demonstrated that naming the forbidden pattern
activates its representation — and on 87.5% of violation runs, the
activated representation wins during token generation.

We observed this directly: examples showing a diff-describing body
(marked "BAD") primed the agent to produce diff-describing bodies
despite the rule being explicit. The agent was not confused about the
rule — it was primed.

Fix: structural separation. The "Message Construction" section shows
only correct patterns. The "Execution" section enforces via gates. The
forbidden pattern is never named, never exemplified. If the agent must
know what to avoid, encode it in the survival test — "does the why
survive?" — which captures the semantic constraint without naming the
forbidden tokens.

## Why This Matters

The three enforcement channels explain a phenomenon that otherwise
appears random: an agent that "knows" a rule yet violates it. Without
this framework, the natural response is to strengthen the rule — clearer
wording, more emphasis, repetition. These interventions target rule
clarity, which is not the failure mode. The result is frustrated users
and instruction files that grow without improving compliance.

The channel taxonomy provides a diagnostic tool: when a rule fails, ask
which channel it occupies. If it is a cognitive self-check, move it to
the tool-call channel. If it is a text-output constraint and the risk
tolerance is zero (commits, PRs, deployments), add a tool-call
verification gate around it. The fix is always structural — change the
channel, not the wording.

The survival test and deletion rule provide a verification mechanism
that works across semantic constraints without enumeration. Any
enumeration-based check is both incomplete and fragile — the agent can
describe a diff without using listed words. A semantic test based on
"does the why survive?" is complete and stable.

The priming hazard mitigation prevents instruction files from
self-sabotaging. Every "BAD" example in an instruction file is a
potential seed for the violation it warns against.

## When to Apply

Apply the tool-call gate pattern when ALL of these are true:

1. The agent must produce an intermediate text output that must satisfy
   a semantic constraint.
2. The cost of constraint violation is high (permanent record, downstream
   breakage, human rework).
3. The constraint has been violated in practice, or the violation risk is
   unacceptably high.
4. The agent can verify the constraint by reading its own tool output
   before proceeding.

Apply the two-gate architecture when the agent must ALSO load and
comprehend rules before beginning work (i.e., the rules live in an
external file the agent may skip loading).

Apply the survival test when the constraint is semantic ("explain why,
not what") rather than mechanical ("never say the word 'bug'").

Apply priming hazard separation when the instruction file currently
shows examples of the forbidden pattern — regardless of whether the
examples are marked BAD. Replace them with the survival test.

## Examples

### Commit Body Gate — Before → After

**Before (cognitive self-check — Channel 3, failure observed):**

Never describe the diff. The body must explain WHY.
Read your body. Does it describe what was added or changed?

BAD: Added null check to ProcessPayment.
GOOD: Null reference in ProcessPayment could crash silently.

The "BAD" example primes the forbidden pattern. The self-check has no
external trigger.

**After (tool-call gate — Channel 1, zero observed failures):**

The Message Construction section shows correct patterns only — no BAD
examples. The Execution section enforces via gates:

1. `Write-Output $fullMessage`
2. Apply survival test: delete diff-describing sentences. Does the why
   survive?
3. Only after passing: `Set-Content $tempFile; git commit -F $tempFile`

### Research Result Validation — Before → After

**Before (text-output constraint — Channel 2):**

Always include confidence levels with factual claims.

The agent knows the rule. On ~12.5% of responses, override pressure
suppresses it during generation.

**After (tool-call gate — Channel 1):**

1. Construct response with confidence levels.
2. `Write-Output $response`
3. Read output. Does every factual claim have a [HIGH]/[MEDIUM]/[LOW]
   annotation?
4. Only after verification: emit final response.

### Priming Hazard — Before → After

**Before (priming active):**

Never use Task.Result — it deadlocks.
BAD: var x = task.Result;
GOOD: var x = await task;

The BAD example names the forbidden pattern, activating its
representation. On violation runs, the activated representation wins
with 87.5% probability (Rana 2026).

**After (construction-only):**

Await all tasks. Never access .Result or .Wait().
After writing code, print to stdout and verify mechanically.

The forbidden pattern is named only in the verification rule (as a
mechanical check, not an example). The construction instruction shows
only the correct pattern.

## Related

- `docs/solutions/best-practices/agent-protocol-enforcement-thought-phase-regulation.md` — the foundational protocol enforcement pattern that preceded this generalization. Defines the four-layer defense (state table, thought-phase blocker, tool-call gate, failure table) applied to the commit protocol. This doc generalizes the underlying enforcement surfaces to any agent instruction.
- `docs/solutions/best-practices/skill-design-negative-constraints.md` — Zhang et al. (2026) research on why negative constraints outperform positive directives. Provides the design methodology for writing enforcement surface rules.
- `docs/solutions/best-practices/prd-template-2026-research-additions.md` — Rana (2026) priming hazard constraint from the PRD template research. Relevant because the priming hazard applies to writing enforcement surface rules.
