---
date: 2026-05-17
title: "Author Research: Shallow Methods, Code Surface Area, and Structural Quality"
tags:
  [
    research,
    quality-gates,
    depth,
    ousterhout,
    structural-quality,
    shallow-methods,
  ]
description: "Comprehensive research across six guiding authors on shallow methods, parameter bloat, wrong abstractions, and entanglement — forming the foundation for the Depth quality gate."
module: tools
problem_type: research
---

# Author Research: Shallow Methods, Code Surface Area, and Structural Quality

Research conducted to ground the **Depth** quality gate in established software
design principles. All six authors converge on the same structural concerns but
name them differently and emphasize different detection signals.

## 1. John Ousterhout — _A Philosophy of Software Design_

### Deep vs. Shallow Modules

Ousterhout's central concept. A **deep** module has high implementation cost but
low interface cost — it hides a lot behind a simple signature. A **shallow**
module has interface cost roughly equal to implementation cost — no net
cognitive reduction.

> "The best methods are those that provide a lot of functionality but have a
> very simple interface: they replace a large cognitive load (reading the
> detailed implementation) with a much smaller cognitive load (learning the
> interface)."

> "As methods get smaller and smaller there is less and less benefit to further
> subdivision. The amount of functionality hidden behind each interface drops,
> while the interfaces often become more complex."

### Entanglement

Ousterhout defines **entanglement** (called "conjoined" in APOSD):

> "Two methods are entangled if, in order to understand how one of them works
> internally, you also need to read the code of the other. If you've ever found
> yourself flipping back and forth between the implementations of two methods as
> you read code, that's a red flag."

> "When decomposed methods are entangled, they are harder to read than if they
> were not decomposed, and this defeats the whole purpose of decomposition."

### Detection Signal

If reading a method forces you to read another method (flipping back and forth),
the decomposition is wrong. His explicit signal: **if closing the code after
reading it is harder than it was before, don't extract.**

---

## 2. Michael Feathers — _Working Effectively with Legacy Code_

### Seam Identification

Feathers distinguishes **seams** (places where behavior can be changed without
editing) from cosmetic extractions. Extraction is justified when it creates a
genuine seam with a _different responsibility_ from the caller — not just a
_different line of code_.

### The ≥5 Lines Threshold

The `rm-guide-cleanup §2.1` rule — extraction requires ≥5 lines of **real
logic** — is grounded in Feathers' seam philosophy: if the extracted body is
too small, the interface cost (name, signature, parameters, call site) equals
or exceeds the implementation cost, creating a net-zero cognitive change.

### Detection Signal

**The call site is not simpler than the inline code would be.** If
`DoTheThing()` is less clear than the two lines it wraps, don't extract.

---

## 3. Robert C. Martin — _Clean Code_

### Small Methods

Clean Code 1st ed. advocated extreme decomposition with no "too small" guidance.
Martin later acknowledged: "It is certainly possible to over-decompose code."

In his debate with Ousterhout, Martin provided his own example:

```java
void doSomething() { doTheThing(); } // over-decomposed
```

Clean Code 2nd ed. is "more balanced" on method size.

### Parameter Count

> "The ideal number of arguments for a function is zero (niladic). Next comes
> one (monadic), followed closely by two (dyadic). Three arguments (triadic)
> should be avoided where possible. More than three (polyadic) requires very
> special justification — and then shouldn't be used anyway."

### Detection Signals

- Methods that are wrappers around a single call with no added logic
- Methods with >3 parameters ("avoided where possible")

---

## 4. Martin Fowler — _Refactoring_

### Inline Function

The inverse of Extract Function. Trigger: **"If the body of the function is as
clear as the name, inline it."**

Canonical example:

```javascript
// Before: shallow extraction
function getRating(driver) {
  return moreThanFiveLateDeliveries(driver) ? 2 : 1;
}
function moreThanFiveLateDeliveries(driver) {
  return driver.numberOfLateDeliveries > 5;
}

// After: inlined — the body IS as clear as the name
function getRating(driver) {
  return driver.numberOfLateDeliveries > 5 ? 2 : 1;
}
```

### Intention vs. Implementation

> "If you have to spend effort looking at a fragment of code to figure out
> _what_ it's doing, then you should extract it into a function and name the
> function after that 'what'."

### Long Parameter List

> "More than three or four parameters for a method." — this is a structural
> signal that the method's interface cost is too high relative to its
> implementation.

### Detection Signals

- Body is as clear as the name → inline it
- > 3-4 parameters → interface cost exceeds benefit

---

## 5. Sandi Metz — _Practical Object-Oriented Design_

### The Rule of Three

"Don't abstract until you've seen the duplication at least three times." The
first two occurrences are data points; the third confirms the pattern.

### Wrong Abstraction

> "Duplication is far cheaper than the wrong abstraction."

The wrong abstraction lifecycle:

1. Programmer A sees duplication, extracts an abstraction.
2. A new requirement arrives for which the abstraction is _almost_ right.
3. Programmer B adds a parameter and a conditional branch.
4. Loop → the abstraction becomes "a condition-laden procedure which interleaves
   a number of vaguely associated ideas."

### Recovery

> "When dealing with the wrong abstraction, _the fastest way forward is back_."
> Re-introduce duplication by inlining, then start anew.

### Detection Signals

- **Parameter count rising** — "If you find yourself passing parameters and
  adding conditional paths through shared code, the abstraction is incorrect."
- **Conditional proliferation** — the abstraction has become a switchboard
  (if/switch on parameter values inside extracted method)
- **Sunk cost pressure** — "The more complicated and incomprehensible the
  code... the more we feel pressure to retain it."

---

## 6. Kent Beck — _TDD by Example_, _Implementation Patterns_

### Private-to-Public Extraction

Beck's signal: **when the private method has a different rate of change from
its host class.** If you find yourself modifying a private method independently
of the class it lives in, extract it to its own class.

### Intention Revealing Message

From Smalltalk's `highlight` method (just a call to `reverse`): "The name of
the method was longer than its implementation — but that didn't matter because
there was a big distance between the intention of the code and its
implementation." A 1-line method is justified only when its name communicates
something the implementation doesn't make obvious.

### Detection Signal

If the method name is longer than its body AND the name doesn't reveal a
non-obvious intent, it's shallow.

---

## Industry Tool Thresholds (Context)

| Metric           | NDepend Hard | NDepend Extreme | SonarQube |
| ---------------- | ------------ | --------------- | --------- |
| Method LOC       | 20           | 40              | varies    |
| CC               | 15           | 30              | 10-15     |
| NbParameters     | 5            | —               | 7         |
| NbVariables      | 8            | 15              | —         |
| IL Nesting Depth | 4            | 8               | —         |

These are _upper_ bounds — industry tools flag methods that are too large, not
too small. No major tool detects shallow modules. This gate fills that gap.

---

## Summary: Convergent Author Signals

All six authors agree on one thing: **decomposition can go too far.** The
signals they independently identify form the foundation for the Depth gate:

| Signal                                  | Authors                            | Automatable                 |
| --------------------------------------- | ---------------------------------- | --------------------------- |
| Body ≤ ~4 LOC with single caller        | Ousterhout, Feathers, Fowler       | Yes                         |
| Parameter count > 3-4                   | Martin, Fowler, NDepend, SonarQube | Yes                         |
| if/switch on parameter values in helper | Metz, Ousterhout (entanglement)    | Yes                         |
| Call site not simpler than inline code  | Feathers                           | Proxy: LOC + n-params ratio |
| Name longer than body (Beck)            | Beck, Fowler                       | Heuristic — unreliable      |
