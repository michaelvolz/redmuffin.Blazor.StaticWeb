---
date: 2026-05-15
last_updated: 2026-07-22
title: "NuGet package update strategy — Dependabot as sole automated mechanism"
tags:
  [
    nuget,
    dependabot,
    cpm,
    package-management,
    supply-chain,
    strategy,
  ]
problem_type: tooling_decision
module: package-management
component: tooling
severity: medium
---

# NuGet package update strategy

## Decision

**Dependabot is the sole automated mechanism for NuGet package updates.** No other bot (Renovate, etc.) runs alongside it. Manual tools (`scripts/Update-PackageVersions.ps1`, `dotnet list package --outdated`) are complementary for ad-hoc developer workflows but must not be used while Dependabot PRs are open.

## Lock files removed (2026-06-12)

`packages.lock.json` was removed from all projects. `RestorePackagesWithLockFile` is **false** in `Directory.Build.props`. Research (`docs/research/restore-locked-mode-placebo.md`) showed locked mode was a restore-performance placebo; content-hash validation did not justify merge conflicts on every package update.

**`Directory.Packages.props` is the single source of package versions** under Central Package Management (CPM).

## Why Dependabot alone

### Microsoft endorsement

Microsoft's .NET Blog (August 2025) announced a complete rewrite of Dependabot's NuGet updater in native C# using NuGet client libraries and MSBuild APIs. The official position:

> "If you haven't enabled Dependabot for your .NET projects yet, now is a great time to start."

The rewrite delivered:

- **65% faster** test suite (26 min → 9 min)
- **94% success rate** (up from 82%)
- **Full Central Package Management support** — correctly handles `Directory.Packages.props`
- **Transitive dependency resolution** — can promote transitive deps to direct deps to fix CVEs using NuGet's "direct dependency wins" rule
- **Related package awareness** — updates entire `Microsoft.Extensions.*` families together to prevent version skew
- **global.json respect** — uses the exact SDK version specified, matching CI

### Community consensus

The r/dotnet community (2025-2026) is unanimous: **pick one bot and let it handle everything.** Running both Dependabot and Renovate produces duplicate PRs and review fatigue. For GitHub-hosted repos, Dependabot is the natural first-party choice.

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

| Event | Behavior |
| ----- | -------- |
| New minor/patch version released | Dependabot waits for cooldown (7d minor, 3d patch), then opens **one grouped PR** with all qualifying updates |
| New major version released | Dependabot waits 30 days, then opens an individual PR (majors are not grouped — each deserves separate review) |
| CVE published against a dependency | Dependabot opens a security PR **immediately** (bypasses cooldown), in the `dotnet-security` group |
| Package < 7 days old | Dependabot skips it entirely — too risky (malicious packages typically detected within 24-72 hours) |

### Why grouping matters

Before grouping: many patch updates → many PRs → review noise and serial merge pain.

After grouping: many patch updates → **one** PR → one review of the combined graph.

Grouping is enabled for minor+patch (high-frequency, low-risk) and disabled for major (each deserves focused review).

### CPM lockstep gaps Dependabot does not fix for you

Dependabot rewrites version properties and `PackageVersion` entries it knows about. It does **not** invent shared-property wiring.

| Gap | Failure mode | Mitigation in this repo |
| --- | ------------ | ----------------------- |
| Hardcoded sibling of a shared property group | **NU1605** when one package in the Components graph moves and another stays on an older patch | Bind the whole AspNetCore Components family — including **`Microsoft.AspNetCore.Components.Analyzers`** — to `$(MicrosoftExtensionsVersion)`. Never leave a literal `10.0.x` on Analyzers alone. |
| Duplicate `PackageVersion Update=` | Masks which declaration wins; confuses updater scripts | One `PackageVersion Include=` only |
| Transitive CVE (e.g. bunit → AngleSharp) while direct pin is safe | **NU1902** under TreatWarningsAsErrors if CPM only constrains direct refs | `CentralPackageTransitivePinningEnabled=true` so central `AngleSharpVersion` applies to transitive edges |

Incident write-up: `docs/solutions/build-errors/nu1605-components-analyzers-cpm-transitive-pin.md` (PR #234).

Dual TFM (API **net9.0** / Blazor **net10.0**) does **not** by itself forbid Microsoft.Extensions **10.x** package lines on net9 projects — incomplete family lockstep does.

## Complementary tools (not replacements)

| Tool | Role | When |
| ---- | ---- | ---- |
| **Dependabot** | Automated PR creation on schedule, with cooldown and security awareness | Runs weekly; security updates immediately |
| **`scripts/Update-PackageVersions.ps1`** | Bulk-update packages in `Directory.Packages.props` locally | "I want everything updated NOW" — only when no Dependabot PRs are open |
| **`dotnet list package --outdated`** | Quick terminal health check | Ad-hoc: "are any packages stale?" |
| **`dotnet list package --vulnerable`** | Immediate vulnerability scan | Ad-hoc security check between Dependabot cycles |

### The workflow rule

**Do not run `scripts/Update-PackageVersions.ps1` while a Dependabot PR is open.** Close or merge the Dependabot PR first. The script changes `Directory.Packages.props` — the same file Dependabot modifies. Running both simultaneously creates merge conflicts that neither tool can resolve automatically.

If you need a package updated immediately and Dependabot has an open PR for it: merge the Dependabot PR.

If you need a package updated immediately and Dependabot has NOT opened a PR yet (e.g., still in cooldown): the script is appropriate; document the cooldown bypass in the commit message.

## What we do NOT do

1. **Do not run two bots.** No Renovate alongside Dependabot. Pick one.
2. **Do not auto-merge dependency PRs.** CI verifies. Human reviews. Automated tests won't catch intentional backdoors in patch releases.
3. **Do not suppress Dependabot for convenience.** The cooldown is a supply chain defense. Bypassing it "just to update faster" defeats the purpose.
4. **Do not reintroduce `packages.lock.json` without a new ADR.** Locked mode was removed for merge-cost reasons; re-adding it needs evidence, not habit.
5. **Do not hardcode versions for packages that belong to a shared MSBuild version group** (especially Components.Analyzers next to Authorization/WASM).

## References

- [The new Dependabot NuGet updater: 65% faster with native .NET](https://devblogs.microsoft.com/dotnet/the-new-dependabot-nuget-updater/) — Microsoft .NET Blog, August 2025
- [Dependency cooldowns: a simple supply chain fix](https://christian-schneider.net/blog/dependency-cooldowns-supply-chain-defense/) — Christian Schneider, January 2026 (updated May 2026)
- [Defending against dependency supply chain attacks](https://wellarchitected.github.com/library/application-security/recommendations/managing-dependency-threats/) — GitHub Well-Architected, December 2025
- [Dependabot options reference](https://docs.github.com/en/code-security/reference/supply-chain-security/dependabot-options-reference) — GitHub Docs
- [Your dotnet outdated is outdated!](https://www.hanselman.com/blog/your-dotnet-outdated-is-outdated-update-and-help-keep-your-net-projects-up-to-date) — Scott Hanselman, November 2020
- `docs/solutions/build-errors/nu1605-components-analyzers-cpm-transitive-pin.md` — NU1605/NU1902 CPM lockstep + transitive pinning
