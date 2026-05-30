---
date: 2026-05-30
status: accepted
---

# NuGet / npm Supply Chain Security Hardening

Package registries are a primary vector for supply chain attacks. The project
adopts explicit hardening for both NuGet (primary .NET package source) and
npm (required by `@azure/static-web-apps-cli` and OpenCode plugins).

## Decision

### npm Hardening

Every `.npmrc` in the repo (and the global `~/.npmrc`) sets:

| Setting            | Value  | Rationale                                                                                      |
| ------------------ | ------ | ---------------------------------------------------------------------------------------------- |
| `ignore-scripts`   | `true` | Blocks postinstall supply chain attacks — the most common malicious-package vector             |
| `min-release-age`  | `1`    | Prevents typosquatting — a freshly published malicious fork cannot install for at least 1 day  |
| `engine-strict`    | `true` | Blocks incompatible Node.js versions that might have known vulnerabilities                     |
| `strict-peer-deps` | `true` | Prevents silent peer-dependency resolution that could introduce vulnerable transitive packages |

### NuGet Hardening

| Setting                              | Rationale                                                                                                         |
| ------------------------------------ | ----------------------------------------------------------------------------------------------------------------- |
| `RestorePackagesWithLockFile=true`   | Produces `packages.lock.json` with content hashes of every resolved package, auditable against the NuGet catalog  |
| Local-tools feed (`./tools/nupkgs/`) | Internal NuGet packages never leave the repo — no risk of accidental public NuGet publish                         |
| Dependabot as sole update mechanism  | All package updates come through Dependabot PRs with CI verification and human review — no ad-hoc `dotnet update` |

### What Was Deliberately Not Enabled

- **`RestoreLockedMode=true`**: Actively harmful for Blazor WASM. The SDK
  injects transitive packages that vary by SDK version — locked mode causes
  NU1004 build failures on every SDK roll. `RestorePackagesWithLockFile=true`
  provides auditability without the lockstep cost.

- **`fetch-retry-minTimeout` / `fetch-retry-maxTimeout`**: Network reliability
  tuning, not security controls. Neither OWASP nor npm-security-best-practices
  mention them.

## Considered Options

**Accept npm and NuGet defaults.**
Rejected. A compromised global `.npmrc` (e.g., from a malicious dotfiles
install) silently overrides all defaults. Explicit beats implicit for security
— every hardening setting is stated, not assumed.

**`min-release-age=7` or higher.**
Rejected. Blocks legitimate same-day releases — critical bug fixes and security
patches that publish on day 0 become unavailable. 1 day is the minimum
effective typosquatting window without hindering our own workflow.

**`ignore-scripts=false`.**
Rejected. Postinstall scripts are the single most exploited npm attack vector.
`@azure/static-web-apps-cli` survives `ignore-scripts=true` because it ships
pre-compiled with a `dist/` directory and has no postinstall lifecycle hook.

**`RestoreLockedMode=true` for "perfect" determinism.**
Tested and rejected. Blazor WASM projects have SDK-injected transitive
packages (AspNetCore.App.Ref, browser-wasm, etc.) whose resolved versions
vary by SDK version. Locked mode breaks on every SDK update, and as
documented, provides zero performance benefit — NuGet always walks the full
dependency graph regardless.

## Consequences

- `.npmrc` contains 14 explicit settings (not all security-related) with inline
  `# why:` comments explaining each one. No default is trusted.
- The local NuGet feed at `tools/nupkgs/` is consumed via `nuget.config`
  `<packageSourceMapping>` and is CI-safe (guarded by `Condition` on
  `$(CI)`/`$(GITHUB_ACTIONS)`).
- Dependabot is configured with auto-merge disabled — every PR includes CI
  verification and requires human review.
- `packages.lock.json` is committed and tracked. Lock file drift (NU1004)
  blocks builds and must be resolved before merging.
- Supply chain hardening is explicit, never implicit. Any future addition of
  a package registry or package management tool must have its own hardening
  configuration.
