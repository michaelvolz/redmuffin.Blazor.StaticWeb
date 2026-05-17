---
date: 2026-05-17
status: accepted
---

# Depth: Structural Quality Gate Between Architecture and CRAP

The quality gates toolchain adds a sixth gate — **Depth** — positioned between
Architecture and CRAP in the execution pipeline. It detects structural code
problems that other gates miss: shallow methods (Ousterhout), parameter bloat
(Martin, Fowler), wrong abstractions (Metz), and entangled call chains
(Ousterhout). These are the signals our six guiding authors independently
identify as "decomposition gone too far" — the negative counterbalance to
CRAP's "extract this" signal.

Depth is a **peer** to CRAP, not subordinate. When they conflict, neither
auto-wins — the agent must find a refactoring that satisfies both.

## Considered Options

**Skip entanglement (Phase 1 only).** Rejected. Ousterhout's primary concern
is entanglement — shallow methods alone miss the worst structural problems.
Phase 1 includes a simple proxy (parameter count vs. expressiveness). Full
call-graph entanglement analysis follows in Phase 2.

**Depth above CRAP (auto-override).** Rejected. Auto-overriding CRAP would
block valid extractions that happen to create temporarily-shallow methods
during incremental cleanup. Human/agent judgment resolves conflicts.

**CRAP above Depth (advisory only).** Rejected. Would allow CRAP-driven
extraction to create shallow methods without pushback, defeating the gate's
purpose. Structural quality is not optional.

## Consequences

- Gate execution order changes: Architecture → **Depth** → CRAP → SCRAP →
  Mutation → Duplicates
- The Depth gate must be added to `AllCommand`, `Program.cs`, and the gates
  table in `tools/README.md`
- `rm-gates-cleanup §0` decision tree must include Depth as a peer signal
- The gate runs on all source files (not just test files, unlike SCRAP)
- Depth violations are composite-scored with weighted signals — no manual
  exclusion lists needed
