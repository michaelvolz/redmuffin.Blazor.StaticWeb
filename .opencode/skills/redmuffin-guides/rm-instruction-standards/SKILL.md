---
name: rm-instruction-standards
description: >
  Governs LLM instruction files — AGENTS.md, SKILL.md, agents, CONTEXT.md.
  Covers negative-constraint principle, register conventions, placement
  rules, and instruction budget. Use when editing any instruction file.
---

## Scope

This skill governs every file that contains behavioral instructions for
an LLM. This includes:

- Global `~/.config/opencode/AGENTS.md`
- Local repo `AGENTS.md` files
- All `rm-*` SKILL.md files
- Agent definition files (`.md` in `agents/`)
- `CONTEXT.md` files containing behavioral rules (domain model sections
  with terminology and constraints)

This skill does NOT govern: OpenCode config files (`opencode.jsonc`,
`magic-context.jsonc`), implementation code, test files, or
documentation that is purely reference (no behavioral rules).

## The Great Filter: Non-Negotiable Constraints

These constraints override every other section in this skill. Never
weaken them during any restructuring:

1. **COMMIT AND PUSH RULES** — The state machine at the top of global
   AGENTS.md. DEFAULT state: never mention commits, never push (blocked
   at tool level). COMMIT_BATCH state: commit only when the user gives
   explicit instruction, permission expires when the working tree is
   clean. See global AGENTS.md §COMMIT AND PUSH RULES for the full
   text — it cannot be summarized without losing essential detail.

2. **Discussion vs Action** — Default is Discussion Mode. Questions,
   research, and feedback are discussion. Only explicit action
   instructions trigger work. Never chain report → action.

## The Negative-Constraint Principle

Research by Zhang et al. (2026) analyzed 25,532 rules across 679
instruction files with 5,000+ agent runs on SWE-bench Verified. The
finding is unambiguous:

**Positive directives actively hurt agent performance.** Rules like
"follow code style" or "write clean code" make agents worse. Negative
constraints like "do not refactor unrelated code" are the only
individually beneficial rule type.

The headline principle from the research: **Constrain what agents
must not do, never prescribe what they should.**

### Application to Every Instruction File

When writing any behavioral rule, always frame it as a prohibition:

| Weak (positive directive — harmful) | Strong (negative constraint — beneficial)             |
| ----------------------------------- | ----------------------------------------------------- |
| Use PascalCase for file names.      | Never use camelCase or snake_case for file names.     |
| Follow existing patterns.           | Never invent new patterns when existing ones suffice. |
| Write clean, maintainable code.     | Never add dead code or unused abstractions.           |
| Prefer early returns.               | Never nest if-else when early return is possible.     |
| Keep methods small.                 | Never write a method longer than 30 lines.            |

### Two Exceptions

Negative constraints do not apply to:

1. **Imperative workflow steps.** `Load rm-commit. Stage the files.` is
   a positive imperative — but it is a procedural step, not a behavioral
   rule. Procedural steps are exempt from the negative-constraint
   principle.

2. **Declarations of fact.** `Default: Discussion Mode.` defines a state
   or fact. It is not prescribing behavior — it is declaring a
   condition. Declarations are exempt.

### Context Priming Effect

The same research found that random rules help as much as expert-curated
ones, suggesting rules work partly through context priming rather than
specific instruction content. This does not mean rules are useless — it
means the mere presence of domain-specific context improves performance.
Write rules that are true to your domain, not generic platitudes.

## Tone & Register Conventions

Every instruction file must use a consistent register and mood. The LLM
weights instructions differently based on how they are framed —
inconsistency dilutes compliance.

### Three Registers (Who Is Speaking)

| Register                       | Example                              | Use for                                                                                    |
| ------------------------------ | ------------------------------------ | ------------------------------------------------------------------------------------------ |
| **Second person (`you`)**      | `You must never bypass this filter.` | Behavioral rules, constraints, trigger-action rules, operational instructions              |
| **First person (`I` / `me`)**  | `When I say "research," I expect...` | Defining how you interpret the user's words. The `I` is the user speaking through the file |
| **First person plural (`we`)** | `We are pair programming.`           | Framing the relationship model, setting collaborative context                              |

Never use third person about yourself (the LLM). `The agent must never
commit` creates psychological distance — you read it as describing a
theoretical agent, not yourself. All behavioral rules addressing the LLM
use `you`.

### Three Moods (How the Instruction Is Framed)

Zhang et al. (2026) found that negative-constraint phrasing is the only
individually beneficial rule type in coding-agent settings. When writing
any behavioral rule, use prohibition ("Never X", "Do not X").

| Mood            | Example                              | Compliance weight                                        |
| --------------- | ------------------------------------ | -------------------------------------------------------- |
| **Imperative**  | `Load rm-commit. Stage the files.`   | Highest. Unambiguous command (for procedural steps only) |
| **Obligation**  | `You must never bypass this filter.` | High. States a non-negotiable rule                       |
| **Declaration** | `Default: Discussion Mode.`          | Medium. Defines a state or fact                          |

Never use: hortative (`You should consider...`), permissive (`You may
want to...`), polite detours (`Please try to...`). These reduce
compliance weight — the LLM treats them as optional. Positive behavioral
directives (`Follow code style`, `Write clean code`) actively harm
performance — never write them.

### Reference Material

Tables, schemas, file paths, and code examples use no person. They are
data, not instruction.

### Application Rule

When writing a rule, decide its function first:

- Behavioral rule → `you` + negative constraint
- Procedural step → `you` + imperative
- User definition → `I` (user speaking through the file)
- Relationship framing → `we`
- Reference → no person

## Instruction Budget & Progressive Disclosure

### The Budget Problem

Two separate findings inform the budget:

1. **HumanLayer (2025):** Frontier thinking LLMs can follow approximately
   150-200 instructions with reasonable consistency. The OpenCode system
   prompt already consumes ~50 instructions. Skills add more. The
   AGENTS.md file must stay within the remaining budget. Degradation is
   uniform — a bloated file degrades every rule, not just the later ones.

2. **Zhang et al. (2026):** No degradation was observed up to 50 rules
   when those rules are coding-specific constraints. This suggests the
   degradation threshold depends on rule type — negative constraints
   are more resistant to budget pressure than positive directives.

The safe approach: keep AGENTS.md under 300 lines. Keep individual
skills under 500 lines. Prefer fewer, stronger rules over many weak ones.

### Progressive Disclosure

Do not put every instruction the LLM might need into AGENTS.md. Instead:

1. **AGENTS.md is the landing page.** It contains only universally
   applicable, always-needed rules. Target: under 300 lines.

2. **Skills are the detail layer.** Step-by-step workflows, command
   references, decision logic, and domain-specific conventions live
   in skills loaded on demand.

3. **References, not copies.** When a rule needs supporting detail,
   point to the skill that has it (`See rm-commit §Commit Shape`),
   do not duplicate the content.

4. **Periphery bias matters.** LLMs give highest weight to instructions
   at the very beginning and very end of the prompt. Place the most
   critical rules at the top of AGENTS.md. Place reference material
   at the bottom.

### Context Utilization Sweet Spot

HumanLayer's advanced context engineering research found that keeping
context window utilization in the 40-60% range produces the best
results. Beyond 60%, degradation accelerates. This applies to
AGENTS.md: the file should fill at most 60% of the remaining instruction
budget after the system prompt.

## Instruction File Architecture — What Goes Where

### Placement Decision Tree

Before adding any rule, answer in order:

1. **Does this rule apply to every repo on this system?**
   - Yes → global `~/.config/opencode/AGENTS.md`
   - No → continue

2. **Is this rule specific to one project?**
   - Yes → local repo `AGENTS.md`
   - No → continue

3. **Does this rule need more than 3 lines to be useful?**
   - Yes → create or update a skill, add a 1-line reference in AGENTS.md
   - No → add directly to the correct AGENTS.md

### When to Extract to a Skill

Extract a rule to a skill when it needs any of:

- Step-by-step workflow instructions
- Multiple commands or examples
- Decision logic (if X then Y)
- Reference tables or data schemas
- Content that changes frequently and would bloat AGENTS.md
- Content that applies to specific operations, not every session

### When to Split a Skill

Split a skill when:

- It exceeds 500 lines (models lose track of structure)
- It covers two distinct domains (e.g., commit formatting AND code review)
- One section is loaded far more often than the rest (split for budget)

### Cross-Skill References

When Skill A references Skill B, use the format: `See rm-skill-name
§section-name`. Never assume the LLM has Skill B loaded — the
reference is a pointer, not a dependency.

## Maintenance Workflow

### When to Restructure

Anthropic's guidance: "Treat CLAUDE.md like code — review it when things
go wrong, prune it regularly, and test changes by observing whether the
LLM's behavior actually shifts." Apply this to every instruction file:

- **Audit after every violation.** If the LLM breaks a rule, check whether
  the rule is too buried, ambiguous, or a positive directive that should be
  a negative constraint.

- **Blame the file, not the model.** If the LLM keeps doing something you
  prohibit, the file is too long or the rule is wrongly phrased. Rewrite
  the rule, do not repeat it.

- **The two-correction rule.** If you revise an instruction file twice and
  the problem persists, the file is structurally wrong. Rewrite the
  affected section from scratch rather than patching it a third time.

### Restructuring Workflow

When restructuring any instruction file:

1. **Read the full file** and any related files (global + local
   AGENTS.md, related skills). Understand current state.

2. **Identify scope.** Is this change system-wide or specific?

3. **Check for duplication.** Does this rule already exist elsewhere?
   Consolidate into one location. Delete the copy.

4. **Apply the decision tree.** Use the placement rules above.

5. **Remove, do not orphan.** If content moves, delete from the old
   location. No stale copies.

6. **Add references.** If content moves to a skill, leave a 1-line
   pointer: `See rm-skill-name §section.`

7. **Verify.** Check all affected files:
   - No lost rules
   - No duplication
   - All references resolve to real sections
   - Register is consistent (no third-person LLM references)
   - Tone matches the conventions above
   - Every behavioral rule is a negative constraint (or a procedural step)

## The Quality Cascade

HumanLayer's research identified a cascade: wrong instructions at the
AGENTS.md level affect every session, every plan, and every
implementation. A bad line in AGENTS.md has more leverage than a bad line
of code — it compounds across all future work.

The inverted leverage hierarchy:

- **Highest leverage:** AGENTS.md and skill files (affects every session)
- **Medium leverage:** Implementation plans and specs
- **Lowest leverage:** Individual code changes

This means instruction files deserve proportionally more review and
discipline than implementation code. Never auto-generate AGENTS.md.
Hand-craft every rule. Verify every rule by observing the LLM's behavior.

## Verification — Test Your Instructions

Anthropic's guidance: "Include tests, screenshots, or expected outputs
so Claude can check itself. This is the single highest-leverage thing
you can do." The parallel for instruction files:

After writing or changing a rule, verify it:

- Does the LLM follow this rule in the next session?
- Can you observe a behavioral difference?
- If you remove this rule, would the LLM make mistakes?

If a rule cannot be verified (no observable behavior change), it is
either already followed by the LLM without the rule (superfluous) or
too vague to be actionable (rewrite it).

## Common Mistakes

- **Writing positive behavioral directives.** "Follow code style"
  actively harms performance. Convert to negative constraints: "Never
  violate the code style conventions in rm-code-quality."

- **Duplicating global rules in local AGENTS.md.** The global file
  already applies. Do not repeat it.

- **Leaving dead references.** If a section moves, remove the old
  content. Do not leave "See above" pointing to nothing.

- **Using third person about the LLM.** "The agent" or "the model"
  creates distance. Always use "you."

- **Writing hortative rules.** "You should consider X" is not a rule.
  Use "You must X" or "Never X."

- **Qualifying a negative constraint with a judgment call.**
  "Never issue any potentially destructive command" lets the LLM decide
  what counts as destructive. `source` is a shell injection vector but
  the LLM judged it non-destructive because it was re-sourcing a config
  file. Drop the qualifier: "Never issue any command listed below." The
  table is the source of truth; do not let the LLM judge exceptions.

- **Adding rules that only apply to specific tasks.** Task-specific
  rules belong in skills, not in the always-loaded AGENTS.md.

- **Exceeding the instruction budget.** The AGENTS.md is loaded in
  every session. Every line costs compliance against all other rules.
  Be ruthless about what stays.

- **Patching a broken rule more than twice.** After two failed revisions,
  the section is structurally wrong. Rewrite from scratch.

- **Auto-generating AGENTS.md.** `/init` and similar tools produce
  generic rules that bloat the file. Hand-craft every rule based on
  observed LLM behavior.
