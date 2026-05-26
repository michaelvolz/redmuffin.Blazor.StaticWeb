---
date: 2026-04-03
title: "Changelog Automation System"
tags: [changelog, powershell, git]
problem_type: workflow
---

## Problem

Maintaining a changelog manually is time-consuming and error-prone. Developers forget to log changes, entries are inconsistent in format, and no single source of truth ties changelog entries back to their originating commits. The plain-text output was also difficult to visually scan — users couldn't quickly distinguish between different types of changes (features, fixes, docs, etc.) at a glance.

## Root Cause

There was no automated pipeline from Git commit history to a formatted `CHANGELOG.md`. All changelog entries were written by hand, leading to gaps, inconsistencies, and maintenance burden. Additionally, the initial generation script required a manually-created `git-commits.txt` file as input, adding unnecessary friction.

## Solution

`Update-Changelog.ps1` reads git commit history directly via `git --no-pager log --oneline`, filters out noise, categorizes changes, decorates output with emojis, and produces a well-formatted `CHANGELOG.md`.

### Filtering Rules

The script filters out non-essential commits:

- Package updates and dependency changes
- Dependabot commits
- Merge commits
- Documentation-only changes
- Code formatting and linting changes

### Categorization

Remaining commits are grouped into logical sections: Features, Bug Fixes, Improvements, and Breaking Changes.

### Git Integration

The script calls git directly — no intermediate file needed. Key details:

1. **Direct git integration** — script calls git internally; no pre-generated commit file required
2. **Backward compatibility** — the `-InputFile` parameter remains available (now optional) for users who prefer pre-generated commit files
3. **Error handling** — clear messages for git-not-installed, not-in-git-repo, and command-failure scenarios
4. **All existing functionality preserved** — filtering, categorization, output format, and parameters (`-OutputFile`, `-Update`, `-Preview`) unchanged

The `CommitParser` module was updated to handle direct git output.

### Output Format

Each entry: `[Commit message] ([commit hash])`, sorted in descending chronological order. The generator supports both one-time generation and incremental updates (appending new entries without overwriting).

A preview mode (`-Preview`) lets developers review filtered commits before generating final output.

### Emoji Decoration

Emojis appear at four levels throughout the changelog output:

1. **Category headers** — each section (Features, Bug Fixes, etc.) gets an emoji prefix (e.g., `### ✨ Features`)
2. **Commit entries** — each commit message is prefixed with a type-based emoji (e.g., `🚀 feat`, `🔧 fix`, `📚 docs`)
3. **Summary section** — stat lines get relevant emojis (e.g., `📊 Summary: 15 entries`)
4. **Header and footer** — main heading (`# 📋 Changelog`) and auto-generation note (`🤖 This changelog was automatically generated`)

**Category Emoji Mappings:** Added/Features → ✨, Changed → 🔄, Deprecated → 🚨, Removed → 🗑️, Fixed → 🐛, Security → 🔒, Performance → ⚡, Testing → 🧪, Documentation → 📚, Dependencies → 📦, Default → 📝

**Commit Type Emoji Mappings:** feat → 🚀, fix → 🔧, docs → 📚, style → 💄, refactor → ♻️, perf → ⚡, test → 🧪, build → 🏗️, ci → 👷, chore → 🔨, revert → ⏪, security → 🔒, deps → 📦

### Implementation Details

- UTF-8 encoding must be preserved throughout the PowerShell pipeline
- Existing emojis in commit messages are preserved (no double-emoji)
- Unmapped categories default to 📝
- Emoji mappings are not user-configurable (always enabled)
- An emoji legend is included at the top of the changelog for first-time readers

## Prevention

- CI/CD integration for automated changelog updates on release tags should be considered
- Use `-Preview` mode to review filtered commits before generating final output
- Test that generated `CHANGELOG.md` output is identical whether using direct `git log` or a pre-generated file with the same commits
- Test emoji rendering on GitHub, GitLab, and other platforms
- Cross-platform PowerShell compatibility (Windows and Linux) should be verified
- Keep emoji usage consistent and predictable — avoid overly playful choices
- Consider future date-range filtering via `--since`/`--until` if needed
- Cache git log output during `-Update` mode to avoid re-fetching the entire history
