---
name: rm-cleanup-session
description: Master orchestrator for quality gates cleanup sessions. Loads all required cleanup skills at once. Use when starting a cleanup session, when user says "cleanup", "quality gates", "fix CRAP", "fix Depth", or wants to systematically eliminate quality gate violations.
---

Never begin cleanup without first loading all seven skills:

```
skill({ name: "rm-guide-cleanup" })
skill({ name: "rm-guide-quality-gates" })
skill({ name: "rm-review-heuristics" })
skill({ name: "rm-guide-csharp-features" })
skill({ name: "rm-guide-architecture" })
skill({ name: "rm-tdd" })
skill({ name: "rm-guide-testing" })
```

rm-guide-cleanup defines code quality rules. rm-guide-quality-gates
defines the protocols (structural-first order, functional catalog,
mutation decision tree). rm-review-heuristics defines structural
signals gates miss. rm-guide-csharp-features is the pattern catalog.
rm-guide-architecture loads Ousterhout and Uncle Bob Martin author
sub-skills during architecture work. rm-tdd governs test discipline.
rm-guide-testing provides test patterns.
