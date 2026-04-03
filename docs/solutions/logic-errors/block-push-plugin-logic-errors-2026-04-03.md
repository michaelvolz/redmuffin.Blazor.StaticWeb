---
title: "Fix block-push plugin logic errors and crash risk"
date: "2026-04-03"
category: "docs/solutions/logic-errors"
module: "opencode"
problem_type: "logic_error"
component: "tooling"
severity: "high"
root_cause: "logic_error"
resolution_type: "code_fix"
tags:
  - "opencode"
  - "plugin"
  - "block-push"
  - "null-safety"
  - "error-handling"
  - "code-duplication"
related_components:
  - "opencode"
  - "block-push-plugin"
---

# Fix block-push plugin logic errors and crash risk

## Problem

The `.opencode/plugins/block-push.js` plugin had critical code quality issues that could cause runtime crashes and block all bash command execution.

## Symptoms

- Code duplication between `detectGitRevert`/`isGitRevert` and `detectGitPush`/`isGitPush`
- Missing null checks could cause TypeError crashes when `output.args` is undefined
- No try-catch error handling - plugin could crash and block ALL bash commands

## What Didn't Work

- **Duplicated functions**: Having separate `detectGitPush()` and `detectGitRevert()` functions violated DRY principles. Any change to detection logic required editing two nearly identical functions.
- **No null guards**: Functions like `parseCommand()`, `tokenize()`, and `basename()` had no null/undefined checks, causing crashes on malformed input.
- **No error handling**: Without try-catch, any unhandled exception would crash the entire plugin, blocking all bash commands.

## Solution

### 1. Parameterized Functions

Created generic detection functions that accept the git subcommand as a parameter:

```javascript
function detectGitCommand(cmd, gitCommand) {
  const segments = parseCommand(cmd);
  for (const segment of segments) {
    if (isGitCommand(segment, gitCommand)) return true;
  }
  return false;
}

function isGitCommand(segment, gitCommand) {
  const tokens = tokenize(segment);
  if (tokens.length === 0) return false;

  const base = basename(tokens[0]);
  if (base === "git") {
    const sub = tokens.find((t) => !t.startsWith("-") && t !== "git");
    if (sub === gitCommand) return true;
  }

  // Check shell wrappers (bash, sh, zsh, cmd, powershell, pwsh)
  if (["bash", "sh", "zsh", "cmd", "powershell", "pwsh"].includes(base)) {
    const execArg = extractExecArg(tokens);
    if (execArg && detectGitCommand(execArg, gitCommand)) return true;
  }

  // Check script interpreters (python, node, ruby, perl, php)
  if (["python", "python3", "node", "ruby", "perl", "php"].includes(base)) {
    const execArg = extractExecArg(tokens);
    if (execArg && detectGitCommand(execArg, gitCommand)) return true;
  }

  return false;
}
```

### 2. Null Guards Added

```javascript
const cmd = output?.args?.command || ""; // Optional chaining

function parseCommand(cmd) {
  if (!cmd) return []; // Guard at entry
  // ...
}

function tokenize(cmd) {
  if (!cmd) return []; // Guard at entry
  // ...
}

function basename(path) {
  if (!path) return ""; // Guard at entry
  // ...
}
```

### 3. Try-Catch with Fail-Open Behavior

```javascript
try {
  const cmd = output?.args?.command || "";
  if (detectGitCommand(cmd, "push")) {
    throw new Error("BLOCKED by Policy: git push is restricted...");
  }
  if (detectGitCommand(cmd, "revert")) {
    throw new Error("BLOCKED by Policy: git revert is restricted...");
  }
} catch (err) {
  // Re-throw policy errors so they're enforced
  if (err.message.startsWith("BLOCKED by Policy:")) {
    throw err;
  }
  // Log other errors but don't block - fail open for safety
  console.error("BlockPushPlugin error:", err.message);
}
```

### 4. Backward Compatibility

Legacy wrapper functions were preserved:

```javascript
function detectGitPush(cmd) {
  return detectGitCommand(cmd, "push");
}

function detectGitRevert(cmd) {
  return detectGitCommand(cmd, "revert");
}

function isGitPush(segment) {
  return isGitCommand(segment, "push");
}

function isGitRevert(segment) {
  return isGitCommand(segment, "revert");
}
```

## Why This Works

1. **Parameterization eliminates duplication**: Now there's a single source of truth for git subcommand detection. Any future subcommand (e.g., "rebase", "reset") can be added by simply calling `detectGitCommand(cmd, "newcommand")`.
2. **Null guards prevent TypeError**: Using optional chaining (`output?.args?.command`) and early returns ensures functions handle undefined/null gracefully.
3. **Fail-open error handling**: Policy violations (BLOCKED by Policy:) are re-thrown to enforce restrictions, but unexpected errors are logged and don't block all commands. This prevents the plugin from becoming a single point of failure.

## Prevention

- **Code review**: Require reviewers to flag duplicated code patterns
- **ESLint rules**: Enable `no-duplicate-case` and custom rules to detect similar function patterns
- **Unit tests**: Add tests for null/undefined inputs to all parsing functions
- **Integration tests**: Verify plugin doesn't crash when given malformed input

## Related Issues

- Original plugin file: `.opencode/plugins/block-push.js`
- Commit: `74839f5` - fix(opencode): refactor block-push.js for reliability and DRY
