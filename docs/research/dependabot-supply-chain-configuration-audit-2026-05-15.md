---
date: 2026-05-15
title: "Dependabot supply chain security audit and recommendations"
tags: [dependabot, supply-chain, security, nuget, cooldown, research]
problem_type: security-audit
module: ci-cd
---

# Dependabot supply chain security audit and recommendations

## Research sources

- **Christian Schneider** (security architect, DevSecOps consultant): [Dependency cooldowns: a simple supply chain fix](https://christian-schneider.net/blog/dependency-cooldowns-supply-chain-defense/) (Jan 2026, updated May 2026)
- **GitHub Well-Architected**: [Defending against dependency supply chain attacks](https://wellarchitected.github.com/library/application-security/recommendations/managing-dependency-threats/) (Dec 2025, updated Apr 2026)
- **GitHub Docs**: [Dependabot options reference](https://docs.github.com/en/code-security/reference/supply-chain-security/dependabot-options-reference)

## Context

Dependabot introduced the `cooldown` option in mid-2025. Between August 2025 and May 2026, at least seven major supply chain compromises hit popular ecosystems: Nx, axios, LiteLLM, Trivy, Checkmarx (twice), xinference, and TanStack/Shai-Hulud (42 packages in ~6 minutes). In every version-publishing case, the malicious releases were detected and pulled within hours. **Every downstream consumer with a 7-day cooldown was never exposed.**

## Current config: what's optimal

Our `.github/dependabot.yml` already implements the most impactful defense correctly:

| Setting                                    | Ours | Expert consensus                                                            |
| ------------------------------------------ | ---- | --------------------------------------------------------------------------- |
| `cooldown.default-days: 7`                 | ✓    | Schneider: "7-day cooldown" gold standard                                   |
| `cooldown.semver-major-days: 30`           | ✓    | Longer wait for breaking changes                                            |
| `cooldown.semver-minor-days: 7`            | ✓    | Standard safety window                                                      |
| `cooldown.semver-patch-days: 3`            | ✓    | Faster for fixes, still >2-day median detection                             |
| NuGet groups (minor+patch)                 | ✓    | Reduces PR noise; endorsed by Well-Architected                              |
| Lockfiles committed (`packages.lock.json`) | ✓    | Schneider's core point: without lockfiles, transitive deps bypass cooldowns |
| `schedule.interval: weekly`                | ✓    | Weekly strikes the balance between velocity and review burden               |

### Note on `--no-restore` and lockfiles

The CI restore step already runs `dotnet restore` which regenerates `packages.lock.json`. If lockfiles are committed and CI builds against them, no dependency can change without a Dependabot PR — the cooldown is enforced end-to-end.

## What we just improved

### 1. Explicit `applies-to` on groups

Added `applies-to: version-updates` to both NuGet and GitHub Actions groups. The implicit default was already `version-updates`, but explicit is safer — future Dependabot versions could change defaults.

### 2. Security updates group

Added a dedicated `dotnet-security` group with `applies-to: security-updates`. Security updates (CVE patches) bypass cooldown by design — as they should. A separate group makes critical patches visually distinct in the PR list.

Security updates bypass cooldown at the Dependabot level — a separate group gives human reviewers a clear signal: "This is a CVE fix, not a routine version bump."

## Beyond dependabot.yml: defense-in-depth layers

The dependabot.yml cooldown is one layer. GitHub Well-Architected recommends six layers. Here's where we stand:

### Layer 1: Disable package lifecycle scripts

**Status: ✓ Already applied.** Our `.npmrc` has `ignore-scripts=true`. The Well-Architected guide specifically calls out the Shai-Hulud attack, which used npm postinstall scripts for initial access. `ignore-scripts=true` blocks this vector.

**What we also have:** `save-exact=true`, `engine-strict=true`, `strict-peer-deps=true` (all recommended by Well-Architected as supplementary hardening).

**NuGet note:** NuGet does not have lifecycle scripts. This layer is npm-only, relevant for our `@azure/static-web-apps-cli` dependency.

### Layer 2: Dev containers for isolation

**Status: Not applied.** Development happens on bare-metal Linux. The Well-Architected guide recommends dev containers to sandbox dependency installs from the host filesystem, SSH keys, and cloud credentials.

**Near-future recommendation:** Evaluate `.devcontainer` configuration. Low priority for a single-developer project but worth having for onboarding.

### Layer 3: Signed commits

**Status: Not applied.** Commit signing with user interaction (passphrase, biometric, hardware key) prevents automated malware from creating commits. Relevant after the TanStack incident where the attacker created and published packages impersonating the maintainer.

**Near-future recommendation:** Enable GPG or SSH commit signing. Low effort, meaningful defense.

### Layer 4: Repository rulesets

**Status: Partially applied.** We have branch protection requiring PRs, but we don't enforce:

- Required status checks (Dependabot alerts, dependency review)
- Signed commits
- Code scanning results before merge

**Near-future recommendation:** Add dependency review action as a required status check. It blocks PRs that introduce known-vulnerable package versions. Single YAML file addition.

### Layer 5: Trusted publishing and attestation verification

**Status: Not applicable.** We don't publish NuGet packages. For consumption, NuGet does not yet have attestation verification equivalent to `npm audit signatures`. This layer is aspirational.

### Layer 6: Continuous monitoring

**Status: Partially applied.**

- Dependabot security updates: ✓ enabled
- Code scanning (CodeQL): ✓ enabled in CI
- Secret scanning: ✓ enabled (GitGuardian in CI)
- **Dependency review action**: **Missing** — recommended addition
- **Dependabot auto-triage rules**: **Missing** — dismisses low-risk alerts automatically
- **Dependabot vulnerability alerts**: Should be configured in repo settings (not dependabot.yml)

## Near-future recommendations by priority

### High priority (weeks not months)

1. **Add dependency review action to CI pipeline.** A single step in the workflow that runs on PRs and blocks high-severity vulnerabilities from being introduced.

   ```yaml
   - name: Dependency Review
     uses: actions/dependency-review-action@v4
     with:
       fail-on-severity: high
   ```

   Configure as a required status check in branch protection rules.

2. **Pin GitHub Actions to commit SHAs.** Schneider's key finding from the Trivy incident: version tags are mutable, commit SHAs are not. Our workflow uses `uses: actions/checkout@v4` — change to `uses: actions/checkout@<full-sha>`. Dependabot can auto-update these (we already have a GitHub Actions group).

3. **Enable commit signing.** Configure GPG or SSH signing and enforce via branch protection. Blocks automated commits from compromised tooling.

### Medium priority (months)

4. **Configure Dependabot auto-triage rules.** In repo settings, not `dependabot.yml`. Automatically dismiss:
   - Low-severity alerts for dev dependencies
   - Alerts for packages where the vulnerable code path is not reachable

5. **Evaluate workflow trigger security.** Audit all workflow triggers. The TanStack compromise used `pull_request_target` with fork-controlled code. Our workflows should use `pull_request` (limited permissions) by default, with `pull_request_target` only when explicitly needed and heavily sandboxed.

### Low priority (aspirational)

6. **Dev container configuration.** Add a `.devcontainer/devcontainer.json` for isolated development.
7. **NuGet package attestation.** Track as NuGet ecosystem matures.

## Summary

Our Dependabot configuration is ahead of most projects. The cooldown tiers are textbook-perfect per the leading expert on this topic. The improvements in this audit (explicit `applies-to`, security group) are clarifications, not corrections.

The remaining gaps are not in `dependabot.yml` itself but in the surrounding defense-in-depth layers — dependency review, commit signing, and Actions pinning. Each is a single-file change with meaningful security impact.
