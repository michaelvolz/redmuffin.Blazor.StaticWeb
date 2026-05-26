---
date: 2026-03-30
title: "CodeQL Action v3 to v4 Migration"
tags: [ci-cd, security, github-actions]
problem_type: infrastructure
---

## Problem

The CodeQL security scanning workflow (`codeql.yml`) used `github/codeql-action/init@v3` and `github/codeql-action/analyze@v3`. CodeQL Action v3 runs on Node.js 20 and is scheduled for deprecation in December 2026. Continued use would trigger deprecation warnings and eventual workflow failure.

## Root Cause

Version lag — GitHub released CodeQL Action v4 on October 7, 2025 with a Node.js 24 runtime upgrade. The workflow was not updated to match.

## Solution

**Two-line version bump with zero functional changes:**

- `github/codeql-action/init@v3` → `@v4` (line 212)
- `github/codeql-action/analyze@v3` → `@v4` (line 240)

**No API changes** between v3 and v4 — same inputs, outputs, and configuration options.

**All custom workflow logic preserved:**

- `check_changes` job — detects documentation-only changes and skips analysis (38+ file skip patterns including `*.md`, `tests/**`, `.vscode/**`, `.gitattributes`, etc.)
- `docs_only_changed_job` — provides user-visible skip message
- Smart analysis triggering — runs on schedule always, on PR/push only when code files changed
- Dual-language matrix — `actions` and `csharp` with `build-mode: none`
- Weekly schedule — Wednesdays at 07:32 UTC
- Force push handling — falls back to `HEAD~1` comparison

**Testing approach:**

1. Documentation-only PR → verify analysis skipped
2. Code-change PR → verify both languages analyzed, SARIF uploaded
3. Merge to master → verify production workflow runs clean

**Risk:** Very Low — no breaking changes, purely a Node.js runtime upgrade (v20 → v24).

## Prevention

- Monitor the [GitHub Changelog](https://github.blog/changelog/) and [CodeQL Action Changelog](https://github.com/github/codeql-action/blob/main/CHANGELOG.md) for deprecation notices.
- Subscribe to the `github/codeql-action` repository release notifications.
- When a major version bump has no API changes, execute it immediately as a low-risk chore.
