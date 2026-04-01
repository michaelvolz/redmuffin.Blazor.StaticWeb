---
name: commits
description: Conventional commit format and standards for this project.
invocable: false
---

CHECK_ORDER:

1. .gitignore staged? → NO: stage first
2. Dependencies committed? → NO: commit those first
3. One concern? → NO: separate commits
4. Body present? → NO: add body
5. Lock files changed? → YES: include them

FORMAT:
type(scope): subject (max 100)

Body explaining why.

TYPES: feat fix docs style refactor perf test build chore ci revert
SCOPES: blazor api ui deps build scripts ci docs opencode
NEVER: push, commit without approval, no body, mixed concerns
ALWAYS: one concern, explicit approval, lock files when changed
BREAKING: ! after scope + BREAKING CHANGE: in body

LOCK_PATHS:
src/**/packages.lock.json
tests/**/packages.lock.json
src/SwaLauncher/packages.lock.json
