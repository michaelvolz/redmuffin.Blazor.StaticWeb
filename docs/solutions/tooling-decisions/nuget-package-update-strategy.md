---
date: 2026-05-15
title: "NuGet package update strategy — Dependabot as sole automated mechanism"
tags:
  [
    nuget,
    dependabot,
    cpm,
    package-management,
    supply-chain,
    lock-files,
    strategy,
  ]
problem_type: strategy
module: ci-cd
---

# NuGet package update strategy

## Decision

**Dependabot is the sole automated mechanism for NuGet package updates.** No other bot (Renovate, etc.) runs alongside it. Manual tools (`scripts/Update-PackageVersions.ps1`, `dotnet list package --outdated`) are complementary for ad-hoc developer workflows but must not be used while Dependabot PRs are open.

## Lock file removal (2026-06-12)

`packages.lock.json` was removed from all projects. The research
(`docs/research/restore-locked-mode-placebo.md`) proved `RestoreLockedMode`
is a performance placebo — zero speed benefit. The remaining value (content
hash validation) did not justify the merge conflict cost on every package
update. NuGet 6.12+ resolver rewrite provides the actual restore performance
improvement.

`Directory.Packages.props` is the single source of package versions.
The §Lock file handling section below is historical — the conflict
resolution procedure no longer applies.

## Why Dependabot alone

### Microsoft endorsement

Microsoft's .NET Blog (August 2025) announced a complete rewrite of Dependabot's NuGet updater in native C# using NuGet client libraries and MSBuild APIs. The official position:

> "If you haven't enabled Dependabot for your .NET projects yet, now is a great time to start."

The rewrite delivered:

- **65% faster** test suite (26 min → 9 min)
- **94% success rate** (up from 82%)
- **Full Central Package Management support** — correctly handles `Directory.Packages.props`
- **Transitive dependency resolution** — promotes transitive deps to direct deps to fix CVEs using NuGet's "direct dependency wins" rule
- **Related package awareness** — updates entire `Microsoft.Extensions.*` families together to prevent version skew
- **global.json respect** — uses the exact SDK version specified, matching CI

### Community consensus

The r/dotnet community (2025-2026) is unanimous: **pick one bot and let it handle everything.** Running both Dependabot and Renovate produces duplicate PRs, conflicting lock files, and review fatigue. For GitHub-hosted repos, Dependabot is the natural first-party choice.

## Our Dependabot configuration

```yaml
# .github/dependabot.yml
version: 2
updates:
  - package-ecosystem: nuget
    directory: "/"
    schedule:
      interval: weekly
      day: monday
      time: "04:00"
    open-pull-requests-limit: 10
    cooldown:
      default-days: 7
      semver-major-days: 30
      semver-minor-days: 7
      semver-patch-days: 3
    labels:
      - "dependencies"
      - "dotnet"
    commit-message:
      prefix: "deps"
      include: "scope"
    groups:
      dotnet-dependencies:
        applies-to: version-updates
        patterns:
          - "*"
        update-types:
          - "minor"
          - "patch"
      dotnet-security:
        applies-to: security-updates
        patterns:
          - "*"
```

### What this means in practice

| Event                              | Behavior                                                                                                       |
| ---------------------------------- | -------------------------------------------------------------------------------------------------------------- |
| New minor/patch version released   | Dependabot waits for cooldown (7d minor, 3d patch), then opens **one grouped PR** with all qualifying updates  |
| New major version released         | Dependabot waits 30 days, then opens an individual PR (majors are not grouped — each deserves separate review) |
| CVE published against a dependency | Dependabot opens a security PR **immediately** (bypasses cooldown), in the `dotnet-security` group             |
| Package < 7 days old               | Dependabot skips it entirely — too risky (malicious packages typically detected within 24-72 hours)            |

### Why grouping matters

Before grouping: 5 patch updates → 5 PRs → 5 lock file changes → potential merge conflicts between them.

After grouping: 5 patch updates → 1 PR → 1 lock file change → no self-conflict.

Grouping is enabled for minor+patch (the high-frequency, low-risk updates) and disabled for major (each deserves focused review).

## Lock file handling (HISTORICAL — lock files removed 2026-06-12)

### Why we commit lock files

We commit `packages.lock.json` to enforce deterministic builds. Without committed lock files, transitive dependencies can change between restores — silently pulling in newly published package versions. The Christian Schneider supply chain research (January 2026) is explicit:

> "As long as lockfiles are committed and updates are gated, transitive dependencies won't change unless you explicitly accept an update."

### Merge conflicts are inherent

`packages.lock.json` is a derived file. Any two branches that modify packages produce a merge conflict in the lock file — regardless of whether Dependabot or a human made the change. This is true for every lock file system (npm, Yarn, Ruby, .NET).

### Resolution procedure

When a lock file conflict occurs (e.g., pulling master after a Dependabot PR was merged):

```bash
# Accept the incoming lock file, then regenerate
git checkout --theirs -- **/packages.lock.json
dotnet restore
git add **/packages.lock.json
```

`dotnet restore` regenerates the lock file against the current dependency graph. There is no manual conflict resolution — the file is always regenerated.

### Policy

Every `packages.lock.json` change must be committed alongside the change that caused it. Never ignore lock file drift. Never commit a lock file separately from its triggering change.

## Complementary tools (not replacements)

| Tool                                     | Role                                                                    | When                                                                       |
| ---------------------------------------- | ----------------------------------------------------------------------- | -------------------------------------------------------------------------- |
| **Dependabot**                           | Automated PR creation on schedule, with cooldown and security awareness | Runs weekly; security updates immediately                                  |
| **`scripts/Update-PackageVersions.ps1`** | Bulk-update all packages in one local command                           | "I want everything updated NOW" — but only when no Dependabot PRs are open |
| **`dotnet list package --outdated`**     | Quick terminal health check                                             | Ad-hoc: "are any packages stale?"                                          |
| **`dotnet list package --vulnerable`**   | Immediate vulnerability scan                                            | Ad-hoc security check between Dependabot cycles                            |

### The workflow rule

**Do not run `scripts/Update-PackageVersions.ps1` while a Dependabot PR is open.** Close or merge the Dependabot PR first. The script changes `Directory.Packages.props` and `packages.lock.json` — the same files Dependabot modifies. Running both simultaneously creates merge conflicts that neither tool can resolve automatically.

If you need a package updated immediately and Dependabot has an open PR for it: merge the Dependabot PR.

If you need a package updated immediately and Dependabot has NOT opened a PR yet (e.g., the package is still in its cooldown period): this is the one case where the script is appropriate, but document the cooldown bypass in the commit message.

## What we do NOT do

1. **Do not run two bots.** No Renovate alongside Dependabot. Pick one.
2. **Do not auto-merge dependency PRs.** CI verifies. Human reviews. Automated tests won't catch intentional backdoors in patch releases.
3. **Do not suppress Dependabot for convenience.** The cooldown is a supply chain defense. Bypassing it "just to update faster" defeats the purpose.
4. **Do not ignore lock file drift.** If `packages.lock.json` changes, commit it alongside the change. Never leave it unstaged.

## References

- [The new Dependabot NuGet updater: 65% faster with native .NET](https://devblogs.microsoft.com/dotnet/the-new-dependabot-nuget-updater/) — Microsoft .NET Blog, August 2025
- [Dependency cooldowns: a simple supply chain fix](https://christian-schneider.net/blog/dependency-cooldowns-supply-chain-defense/) — Christian Schneider, January 2026 (updated May 2026)
- [Defending against dependency supply chain attacks](https://wellarchitected.github.com/library/application-security/recommendations/managing-dependency-threats/) — GitHub Well-Architected, December 2025
- [Dependabot options reference](https://docs.github.com/en/code-security/reference/supply-chain-security/dependabot-options-reference) — GitHub Docs
- [Your dotnet outdated is outdated!](https://www.hanselman.com/blog/your-dotnet-outdated-is-outdated-update-and-help-keep-your-net-projects-up-to-date) — Scott Hanselman, November 2020
