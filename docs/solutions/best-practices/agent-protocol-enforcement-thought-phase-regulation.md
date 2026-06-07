---
date: 2026-06-07
title: "Instruction-Level Protocol Enforcement: Regulating the Thought Phase"
module: instruction-design
tags:
  - instruction-design
  - agent-protocol
  - thought-phase
  - commit-workflow
  - negative-constraints
  - behavior-modification
problem_type: behavioral-enforcement
difficulty: pattern
---

# Agent Protocol Enforcement via Thought-Phase Regulation

## Problem

Agents violate protocol steps (e.g., "load skill X before running git
commands") even when instructions prohibit the wrong action. Previous
explanations blamed the instruction text (not negative enough, too
buried, not bold enough). But the failure persisted through multiple
rewrites of increasing severity.

## Root Cause

Instructions only regulated the **observable action** (the tool call).
But the tool call is the output of a decision process that happens in
the invisible **thought phase**. By the time the agent issues a tool
call, the decision has already been made.

**Failure sequence observed across 5+ distinct sessions:**

```
User: "commit"
Agent thought: "Let me check what changed."
First tool call: git diff --stat
(Agent never loads the required skill)
```

The fix was adding a **thought-phase blocker** — regulating what the
agent thinks _before_ any tool call:

```markdown
Your first thought must be "load rm-commit" — not "what changed"
or "let me check." Analysis before loading is a violation. The
working tree is not your concern yet.
```

This targets the decision point (thought), not just the output (tool
call). The agent cannot "prepare" mentally before loading — the
instruction intercepts the thought before the action decisions starts.

## Mechanism

### Why Thought-Phase Regulation Works

Instructions that constrain tool calls only ("The first tool call must
be X") create a rule that activates _after_ the violating thought has
already occurred. The agent thinks "let me check what changed," decides
to run `git diff`, and only then checks "is this allowed?" — but it's
too late, the reasoning chain is already committed.

Thought-phase blockers intercept at the **intent formation** stage:

```
User: "commit"
┌─────────────────────────────────────────────────┐
│  1. Intent forms: "I need to check what changed" │ ← intercepted here
│  2. Reasoner validates intent                     │
│  3. Tool call selected                            │ ← old rule activates here
│  4. Tool call executed                            │
└─────────────────────────────────────────────────┘
```

When the instruction directly names the forbidden intent ("not 'what
changed' or 'let me check'"), the agent's attention mechanism
reframes the situation before committing to a reasoning path.

### The Four-Layer Defense

Successful protocol enforcement requires defense at every layer of
the decision stack. A single layer is insufficient — agents find
loopholes in any unconstrained layer.

| Layer             | What It Regulates     | Canonical Form                                          |
| ----------------- | --------------------- | ------------------------------------------------------- |
| **State table**   | Situation recognition | `COMMIT_BATCH: Your first action is to load rm-commit.` |
| **Thought-phase** | Intent formation      | `Your first thought must be X — not Y.`                 |
| **Tool-call**     | Action selection      | `The first tool call must be skill(name: "X"). Not Z.`  |
| **Step 0 + cost** | Confirmation          | `0. Load. Read. Cost is zero. Skip cost is rejection.`  |
| **Failure table** | Self-rationalization  | `"I'll check first, then load" → No, you won't.`        |

Each layer catches failures the previous layer misses:

- **State table** ensures the agent knows it's in a special protocol
  state, not normal conversation mode.
- **Thought-phase** prevents the "first analysis" habit from forming.
- **Tool-call** blocks visible violations as a safety net.
- **Step 0** provides a concrete action with cost comparison.
- **Failure table** pre-emptively debunks the rationalization the agent
  would use to justify skipping.

## The Memory Deception

A critical failure pattern: agents skip loading skills not because they
don't know the rules, but because they **believe they already know
them**. The loading step transitions from "informational" (learning
new rules) to "procedural" (following a known protocol) over time.

**The deception:**

1. Agent learns skill rules by loading and reading it.
2. Agent commits successfully many times using memorized rules.
3. Skill owner updates the skill (new body rules, new enforcement).
4. Agent does not load the updated skill — believes knowledge is current.
5. Agent violates new rules it never read.

**The fix:** Remove "knowledge" as a valid reason to skip loading.
Reframe loading as a **protocol handshake** — an act that signals
state transition to the agent itself, not merely information transfer.
Cost/benefit framing reinforces this:

> A fresh load takes one tool call. A rejected commit wastes a
> round-trip and requires manual recovery. The cost of loading is
> zero. The cost of skipping is a rejected commit.

## Precedents and Prior Art

### The Self-Judge Loop

The **self-judge loop** (documented in `self-judge-planning-docs-before-presenting.md`)
operates on a similar principle: require the agent to evaluate its own
work before presenting it to the user. The common insight is the
**invisible decision window** — whether in thought (this document) or
output evaluation (self-judge), the agent must process instructions
_before_ committing to an action, not _after_.

### Negative Constraints Research

Zhang et al. (2026, arXiv 2604.11088) found that negative constraints
("Never X") are the only individually beneficial rule type. This
finding extends to thought phases: frame thought-phase blockers as
"Never think about Y first" or "Your first thought must be X."

However, thought-phase regulation faces a **priming hazard** (Rana
2026, arXiv 2601.08070): naming the forbidden thought ("not 'what
changed'") primes that representation, which can increase its
activation. The mitigation is to **frame around the alternative
first** ("Your first thought must be X"), then name the forbidden
option second. The positive reframe anchors attention on the desired
thought, reducing the priming effect of the negation.

### Edit-Stage Firewall (Related Pattern)

The edit-stage firewall (AGENTS.md §The Edit-Stage Firewall) prevents
the agent from chaining `edit → stage` in the same message. It is a
mechanism-level rule (not thought-phase) that blocks a specific
behavior sequence. Together with thought-phase regulation, they form
a paired defense: thought-phase prevents the _intent_ to skip,
mechanism-level prevents the _action_ to chain.

## Application to Other Protocol Steps

The thought-phase regulation pattern is not limited to commit workflows.
It applies to any agent protocol where:

1. A specific action must happen before all others.
2. The agent tends to do something else "just to check" first.
3. The violation is costly enough to warrant defense-in-depth.

**Examples:**

- **Before implementing a new issue** — "Your first thought must be
  'read the issues doc' — not 'what files need to change.'"
- **Before running a destructive command** — "Your first thought must
  be 'what backup exists' — not 'confirm the command.'"
- **Before editing existing code** — "Your first thought must be
  'read the existing code first' — not 'how do I implement this.'"

## Verification

This document succeeds if:

- An agent operating under these instructions loads required skills
  before running any analysis commands.
- The first tool call after a protocol trigger (e.g., "commit") is
  the skill load, not a git command.
- Agents do not rationalize skipping with "I already know the rules"
  or "I'll load it after checking."
- The thought-phase blocker produces a visible change in reasoning:
  the first sentence of the agent's thinking mentions the required
  action, not the analysis.

## References

- Zhang, Y. et al. (2026). "Do Agent Rules Shape or Distort? Guardrails
  Beat Guidance in Coding Agents." arXiv 2604.11088.
- Rana, A. (2026). "Semantic Gravity Wells: Why Negative Constraints
  Backfire." arXiv 2601.08070.
- Gloaguen, T. et al. (2026). "Evaluating AGENTS.md." arXiv 2602.11988.
- AGENTS.md §The Two States. Global commit protocol state machine.
- AGENTS.md §The Edit-Stage Firewall. Mechanism-level behavior block.
- docs/solutions/workflow-issues/self-judge-planning-docs-before-presenting.md
