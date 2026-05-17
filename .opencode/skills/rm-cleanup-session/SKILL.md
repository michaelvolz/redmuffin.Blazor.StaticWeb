---
name: rm-cleanup-session
description: Master orchestrator for quality gates cleanup sessions. Loads all required cleanup skills at once. Use when starting a cleanup session, when user says "cleanup", "quality gates", "fix CRAP", "fix Depth", or wants to systematically eliminate quality gate violations.
---

Never begin cleanup without first loading all seven skills:

```
skill({ name: "rm-guide-cleanup" })
skill({ name: "rm-gates-cleanup" })
skill({ name: "rm-guide-csharp-features" })
skill({ name: "rm-tdd" })
skill({ name: "rm-code-philosophy" })
skill({ name: "rm-guide-testing" })
skill({ name: "rm-uncle-bob-martin-agentic-coding" })
```

rm-guide-cleanup defines code quality rules. rm-gates-cleanup defines
the protocols (structural-first order, functional catalog, mutation
decision tree). rm-guide-csharp-features is the pattern catalog.
rm-tdd governs test discipline. rm-code-philosophy governs architectural
judgment. rm-guide-testing provides test patterns.
