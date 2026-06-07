---
date: 2026-06-07
title: "Naming Deep Modules and Service Variants: Process and Case Studies"
module: naming
tags:
  - naming
  - architecture
  - ousterhout
  - uncle-bob
  - domain-language
  - CONTEXT.md
  - service-architecture
problem_type: naming-and-architecture
difficulty: principle
---

# Naming Deep Modules and Service Variants

## Problem

Naming decisions in this project kept getting revisited. After multiple
rename cycles (`Pipeline/` → `Behaviors/` → split into
`PipelineBehaviors/` + root-level registration, and `Dummy`
→ `Local` → `Fallback` → `Demo` → `StandIn` → `Mimic`
→ `Synthetic`), it became clear that the initial naming process was
insufficient. Names were chosen for surface-level characteristics
(environment, mechanism, pattern name) rather than purpose and
constraints.

## The Naming Process

A naming decision is correct when it survives all four author lenses
without contradiction. Any name that fails one lens is wrong, even if
it passes the other three.

### Step 1: Identify the thing's PURPOSE, not its mechanism

Ask: "What does this thing DO for the reader/developer?" not "What
pattern does it implement?" or "Where does it run?"

- Wrong: "This directory contains Mediator pipeline behaviors" (mechanism)
- Right: "This directory contains cross-cutting infrastructure that
  extends Mediator's pipeline" (purpose)

- Wrong: "This service runs on localhost" (environment)
- Right: "This service returns generated data for UI iteration" (purpose)

### Step 2: Apply the four author lenses

| Author         | Question                                                           | Pass condition                                           |
| -------------- | ------------------------------------------------------------------ | -------------------------------------------------------- |
| **Uncle Bob**  | Does the name reveal intent?                                       | A reader knows what to expect without opening the file   |
| **Ousterhout** | Is the name deep or shallow?                                       | Deep names reveal the WHY, shallow names reveal the WHAT |
| **Fowler**     | Does the name describe the role in the pattern, not the mechanism? | The name survives a framework/library replacement        |
| **Kent Beck**  | Is the name the simplest thing that communicates?                  | No unnecessary words, no generic qualifiers              |

### Step 3: Test against known failure modes

| Failure mode                | Example                      | Why it fails                                                     |
| --------------------------- | ---------------------------- | ---------------------------------------------------------------- |
| **Environment name**        | `LocalHealthCheckService`    | Tells where it runs, not what it does                            |
| **Mechanism name**          | `Pipeline/`                  | Tells the pattern, not the content                               |
| **Generic qualifier**       | `DemoHealthCheckService`     | Says nothing specific                                            |
| **Taxonomy misuse**         | `DummyHealthCheckService`    | Claims a test-double category it does not fit                    |
| **Invented role**           | `FallbackHealthCheckService` | Describes selection logic, not service purpose                   |
| **Vague pattern name**      | `Behaviors/`                 | Could mean UI behaviors, lifecycle behaviors, pipeline behaviors |
| **Own-implementation risk** | `Mediator/`                  | Reader thinks it IS the Mediator library                         |

### Step 4: Refine iteratively until no lens objects

Each iteration identifies which lens objects and why. Fix the specific
objection, then re-test all four lenses. Do not stop until all four
pass.

## Case Study 1: Directory Structure for Mediator Infrastructure

### The constraint conflict

The `Common/` project needed a subdirectory for Mediator infrastructure.
The files it would contain:

| File                          | Type              |
| ----------------------------- | ----------------- |
| `LoggingBehavior`             | Pipeline behavior |
| `MediatorServiceExtensions`   | DI registration   |
| `ValidationBehavior` (future) | Pipeline behavior |
| `TelemetryBehavior` (future)  | Pipeline behavior |

Two different file types (behaviors + registration), no single term
covered both without compromise.

### Failed attempts

| Name         | Objection                                                    |
| ------------ | ------------------------------------------------------------ |
| `Pipeline/`  | Ousterhout: shallow — says mechanism, not content            |
| `Behaviors/` | Uncle Bob: does not reveal intent — what kind of behaviors?  |
| `Mediator/`  | Uncle Bob: reader thinks this IS our Mediator implementation |

### Resolution: split approach

Don't find a compromised term for two different types. Give each its
precise home:

```
Common/
├── MediatorServiceExtensions.cs     ← root level (DI entry point)
└── PipelineBehaviors/                ← subdirectory (only behaviors)
    ├── LoggingBehavior.cs
    ├── ValidationBehavior.cs
    └── TelemetryBehavior.cs
```

**Why each name works:**

- `PipelineBehaviors/`: "Pipeline" disambiguates from generic
  "Behaviors." It's a Mediator term of art (`IPipelineBehavior`).
  Only behaviors live here — no registration misfits.
- `MediatorServiceExtensions` at root: the DI entry point lives where
  someone looks first to understand "what does Common export?" It
  references all behaviors, so it's coupled to them but separate in
  type.

**Ousterhout check:** The directory is deep — simple interface (put
behavior files here), contains significant cross-cutting logic hidden
from consumers.

## Case Study 2: Naming the Development-Time Service

### The constraint conflict

The `ApiHealth` module needed a second implementation of
`IHealthCheckService` that returns representative data for UI
development, without requiring a live backend.

### The constraints (discovered iteratively)

1. **Not testing terminology** — The service lives in application code,
   not test projects. Names like Fake, Mock, Stub, Dummy belong in
   test projects (test doubles) and would mislead about the service's
   purpose.
2. **Not an environment name** — The service's purpose is providing
   data for design iteration, not "running on localhost." Names like
   Local, Dev, Development describe WHERE, not WHAT.
3. **Not an invented mechanism** — The selection logic is conditional
   (localhost → use this one), but the service's purpose is not "being
   the fallback." Names like Fallback, Backup, Standby describe the
   selection mechanism, not the service's function.
4. **Not generic** — Names like Demo, Design, Preview are too vague
   to differentiate this service from any other development-time
   artifact.
5. **Not a behavioral claim** — Names like Mimic, Emulate, Imitate
   suggest the service copies behavior of another class, but it does
   not — it returns its own generated data.

### The iteration

| Name                         | Why rejected                                                                                                  |
| ---------------------------- | ------------------------------------------------------------------------------------------------------------- |
| `DummyHealthCheckService`    | Meszaros: Dummy is passed but never used, returns null. Our service returns real data.                        |
| `LocalHealthCheckService`    | "Local" is environment, not purpose. Also implies localhost, not the data characteristic.                     |
| `FallbackHealthCheckService` | User caught this: invented the mechanism. The service IS never a fallback — it provides data for development. |
| `DemoHealthCheckService`     | Uncle Bob: generic, says nothing. "Demo" could mean anything.                                                 |
| `StandInHealthCheckService`  | Ousterhout: shallow. Describes the temporary nature, not what the service provides.                           |
| `MimicHealthCheckService`    | Uncle Bob: implies behavioral imitation that does not exist. The service does not mimic HTTP calls.           |

### The winner: `SyntheticHealthCheckService`

"Synthetic" captures the data characteristic: artificially generated to
simulate real data for development purposes.

**Four-lens check:**

- **Uncle Bob:** Reveals intent — "this returns synthetic (generated)
  health check data." Reader knows what to expect.
- **Ousterhout:** Deep — describes WHAT the service provides (data
  characteristic), not WHERE it runs or HOW it's selected.
- **Fowler:** Role in pattern — the service provides synthetic data
  for the UI to consume. Survives backend replacement.
- **Kent Beck:** Minimal, no unnecessary words, no testing vocabulary,
  no generic qualifiers.

### Establishing as a domain term

"Synthetic" was promoted from a one-off name to a project-wide domain
term defined in CONTEXT.md. Future implementations that substitute
generated data for a live backend follow the `Synthetic*` naming
convention (e.g., `SyntheticNewsFeedProvider`,
`SyntheticUserRepository`).

The CONTEXT.md entry distinguishes Synthetic from test doubles:

- Synthetic services live in **application code** for **development
  workflows** (UI iteration, design evaluation)
- Test doubles (Stubs, Fakes, Mocks, Spies) live in **test projects**
  for **automated testing**

This boundary prevents scope creep and keeps each concern in its
correct layer.

## Key Principles Extracted

1. **Split, don't compromise.** When a location would hold two types
   of files, give each its own precise home instead of forcing a
   compromised unified name. The split reveals the true structure.

2. **Name the data characteristic, not the selection logic.** A service
   that returns generated data should be named for what its DATA is
   (Synthetic), not for when it's selected (Local, Fallback).

3. **Testing terminology belongs in test projects.** Application code
   that provides substitute data for development workflows is not a
   test double. Using test-double names in application code creates
   confusion about which layer the code belongs to.

4. **Environment names describe WHERE, not WHAT.** A name that tells
   you where something runs (Local, Dev, Production) reveals location,
   not purpose. Intent-revealing names describe WHAT the thing provides
   (Synthetic, Live, Cached).

5. **All four author lenses must pass.** A name that passes Uncle Bob
   but fails Ousterhout will need renaming. A name that passes
   Ousterhout but fails Kent Beck is over-engineered. All four must
   agree.

6. **The naming process is iterative.** The `SyntheticHealthCheckService`
   name went through 7 iterations before all lenses passed. Each
   iteration fixed exactly one objection. No iteration introduced a
   new objection.

## References

- `~/.config/opencode/skills/redmuffin-guides/rm-code-quality/SKILL.md`
  §Our Guide Authors — trigger questions for each author lens
- `CONTEXT.md` §Synthetic — domain term definition
- `docs/plans/2026-06-06-001-feat-modular-monolith-first-module-prd.md`
  — PRD using PipelineBehaviors/ and SyntheticHealthCheckService
