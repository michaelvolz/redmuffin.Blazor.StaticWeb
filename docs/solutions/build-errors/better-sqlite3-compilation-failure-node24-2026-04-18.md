---
title: Better-SQLite3 Compilation Failure on Node.js v24
date: 2026-04-18
category: build-errors
module: context-mode
problem_type: build_error
component: tooling
symptoms:
  - better-sqlite3 failed to compile native bindings on Node.js v24
  - FTS5 feature couldn't be enabled
root_cause: incomplete_setup
resolution_type: environment_setup
severity: medium
tags: better-sqlite3, node-js, compilation, fts5, native-bindings
---

# Better-SQLite3 Compilation Failure on Node.js v24

## Problem

context-mode's FTS5 feature failed to enable because better-sqlite3 could not compile its native bindings on Node.js v24, preventing the Insight dashboard from running.

## Symptoms

- Compilation errors when attempting to install or run context-mode with FTS5 enabled
- FTS5 functionality remained disabled despite configuration attempts
- Insight dashboard failed to start, with errors related to SQLite database operations
- Observable behavior: context-mode tools would fall back to non-FTS5 modes or fail entirely

## What Didn't Work

- Investigating PATH environment variables and adding Python/build tools (Visual Studio Build Tools, Python 3.x) - these were red herrings as the issue wasn't with missing build dependencies but rather Node.js version incompatibility
- Reinstalling dependencies multiple times - didn't resolve the compilation failure because the root cause was version-specific binary compatibility
- Checking system-level permissions and antivirus interference - these were not contributing factors

## Solution

Switch from Node.js v24 to Node.js v22 using a version manager like nvm or nvm-windows.

Before (Node.js v24):

```bash
node --version
npm install better-sqlite3
```

After (Node.js v22):

```bash
nvm install 22
nvm use 22
node --version
npm install better-sqlite3
```

After (Node.js v22):

```bash
nvm install 22
nvm use 22
node --version
# v22.x.x
npm install better-sqlite3
# Installation succeeds, FTS5 enables correctly
```

## Why This Works

better-sqlite3 relies on native Node.js APIs that changed between versions, making its compiled bindings incompatible with Node.js v24's runtime. Node.js v22 maintains API compatibility that better-sqlite3 was built against, allowing the native extensions to compile and load properly. This resolved the FTS5 initialization failures and enabled the Insight dashboard functionality.

## Prevention

- Use Node Version Manager (nvm) for all Node.js installations to easily test and switch versions
- Add Node.js version compatibility checks to CI/CD pipelines before dependency installation
- Maintain a compatibility matrix for critical native dependencies like better-sqlite3
- Include version-specific integration tests in the project:

```javascript
// package.json test script example
"scripts": {
  "test:compatibility": "node -e \"require('better-sqlite3')\""
}
```

```yaml
# .github/workflows/compatibility.yml example
name: Node Compatibility Check
on: [push, pull_request]
jobs:
  test:
    strategy:
      matrix:
        node-version: [20, 22]
    steps:
      - uses: actions/setup-node@v4
        with:
          node-version: ${{ matrix.node-version }}
      - run: npm install && npm run test:compatibility
```

## Related Issues

- [better-sqlite3 #1428: node 24 doesnt work](https://github.com/WiseLibs/better-sqlite3/issues/1428)
- [better-sqlite3 #1411: better-sqlite3 12.3.0 fails to build on Node.js latest (25)](https://github.com/WiseLibs/better-sqlite3/issues/1411)
- [better-sqlite3 #1437: NODE_MODULE_VERSION mismatch on Node 21.7.3](https://github.com/WiseLibs/better-sqlite3/issues/1437)
