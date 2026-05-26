---
title: "SDK 10 consolidation to build .NET 9 projects for Azure SWA"
date: 2026-05-12
category: tooling-decisions
module: build-configuration
problem_type: tooling_decision
component: tooling
severity: high
applies_when:
  - "Upgrading .NET SDK while targeting older runtime for cloud compatibility"
  - "Diagnosing Blazor WASM trimming or assembly bloat"
  - "Debugging CI pipeline restore/publish issues"
  - "Evaluating RestoreLockedMode for Blazor WASM projects"
tags:
  - dotnet
  - sdk-10
  - blazor-wasm
  - azure-swa
  - trimming
  - ci-pipeline
  - restore-locked-mode
  - wasm-tools
---

# SDK 10 consolidation to build .NET 9 projects for Azure SWA

## Context

.NET 10 SDK shipped, but Azure Static Web Apps managed Functions only support
.NET 9 (`apiRuntime: dotnet-isolated:9.0`). Two competing `global.json` files
(repo root pinned to SDK 9, `tools/` pinned to SDK 10) created confusion about
which SDK tooling was active. VSTest (`dotnet test`) was removed in SDK 10,
breaking CI. Blazor WASM trimming silently regressed under `--no-restore`
publish, producing 204 assemblies and a 7.5 MB framework download.
`RestoreLockedMode=true` caused CI failures with NU1004 when trimming packages
needed resolution. `TrimMode=full` crashed the Blazor app at runtime by
stripping reflection-based framework types with no build error.

Previous sessions investigated the trimming regression across three deploy
failures before the root cause (`--no-restore` starving the IL linker of
SDK-injected packages) was identified (session history). The full consolidation
required installing `wasm-tools` workload under SDK 10 to bridge the
`net9.0` WASM target gap, and replacing `dotnet test` with `dotnet run` for all
test execution (TUnit AOT, VSTest deprecated).

## Guidance

### Single `global.json` with SDK 10

Use one `global.json` at the repo root. The SDK compiles `net9.0` projects
without issue — it is a build toolchain, not a runtime. `wasm-tools` workload
bridges SDK 10 to `net9.0` WASM targets.

```json
{
  "sdk": { "version": "10.0.100", "rollForward": "latestMinor" }
}
```

All `.csproj` files keep `net9.0` targets:

```xml
<TargetFramework>net9.0</TargetFramework>
```

### Test with `dotnet run`

VSTest is removed in SDK 10. Use `dotnet run --project <test-project> -c Release`.
TUnit executes via `Program.cs`, identical behavior.

```yaml
- name: Run tests
  run: |
    dotnet run --project tests/redmuffin.Blazor.StaticWeb.Tests \
      -c Release --no-restore /p:CollectCoverage=false
```

### Single restore with trimming packages

A single `dotnet restore -p:PublishTrimmed=true` ensures the Blazor WASM
trimming packages (`Microsoft.NET.ILLink.Tasks`, `Microsoft.NET.Sdk.WebAssembly.Pack`)
are resolved. Both publish steps use `--no-restore`.

```yaml
- name: Restore dependencies
  run: dotnet restore -p:PublishTrimmed=true

# Blazor publish (later step):
- run: |
    dotnet publish ${{ env.APP_LOCATION }} \
      -c Release \
      -o publish \
      -p:PublishTrimmed=true \
      --no-restore \
      --nologo
```

### Lock file without locked mode

`RestorePackagesWithLockFile=true` for auditability. Never set
`RestoreLockedMode=true` — it blocks SDK-injected package resolution and
provides zero performance benefit (NuGet walks the full graph regardless).

```xml
<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
<!-- RestoreLockedMode NOT set -->
```

### TrimMode partial, not full

`TrimMode=partial` (the Blazor SDK default) is the correct setting.
`TrimMode=full` strips reflection-based framework types (DI, component routing,
serialization) and crashes the app at runtime with no build error.

### Verify trimming in CI

Build step validates `blazor.boot.json` assembly count:

```bash
ASSEMBLY_COUNT=$(jq '.resources.fingerprinting | keys | \
  map(select(endswith(".wasm"))) | length' blazor.boot.json)
if [ "$ASSEMBLY_COUNT" -gt 60 ]; then
  echo "ERROR: Trimming appears disabled — $ASSEMBLY_COUNT assemblies"
  exit 1
fi
```

Live health check confirms `linkerEnabled`:

```bash
BOOT=$(curl -f -s "https://redmuffin.net/_framework/blazor.boot.json")
echo "$BOOT" | jq -r '.linkerEnabled'  # must return "true"
```

## Why This Matters

Running two SDKs side-by-side creates a hidden compatibility gap: developers
test with one toolchain, CI builds with another. Consolidating to SDK 10
eliminates the gap and keeps the repo forward-compatible (when SWA adds .NET 10
Functions support, updating targets is one line per `.csproj`).

The trimming regression was silent — the app built and ran but served 7.5 MB of
framework assemblies on every page load. `--no-restore` on publish starves the
IL linker of its packages with no error or warning. `TrimMode=full` compounds
the confusion by producing a build that publishes cleanly but crashes at
runtime, indistinguishable from a correct build.

The net result: 204 assemblies became 49, framework download dropped from
7.5 MB to 47 KB (Brotli), and CI runtime decreased from 5m02s to 3m55s.

## When to Apply

- Upgrading the .NET SDK on a Blazor WASM project targeting Azure Static Web Apps
- `global.json` pins an SDK version and a newer SDK is available
- Trimming appears to produce no reduction in assembly count
- `RestoreLockedMode` causes NU1004 during restore
- `dotnet test` fails with "VSTest is not supported" after an SDK upgrade
- CI uses Oryx for builds (bypass it with `swa deploy` and pre-built output)

## Examples

**`global.json` — before (two files, two SDKs):**

```json
// repo root
{ "sdk": { "version": "9.0.200", "rollForward": "latestMinor" } }
// tools/ (separate file)
{ "sdk": { "version": "10.0.100", "rollForward": "latestMajor" } }
```

**`global.json` — after (one file, one SDK):**

```json
{ "sdk": { "version": "10.0.100", "rollForward": "latestMinor" } }
```

**CI test — before:**

```yaml
- run: dotnet test -c Release --no-restore
```

**CI test — after:**

```yaml
- run: |
    dotnet run --project tests/redmuffin.Blazor.StaticWeb.Tests \
      -c Release --no-restore /p:CollectCoverage=false
```

**Restore — before:**

```yaml
- run: dotnet restore
```

**Restore — after:**

```yaml
- run: dotnet restore -p:PublishTrimmed=true
```

**Blazor publish — before (untrimmed, silent failure):**

```yaml
- run: dotnet publish ${{ env.APP_LOCATION }} -c Release -o publish --no-restore
```

**Blazor publish — after (trimmed, verified, single restore):**

```yaml
- run: |
    dotnet publish ${{ env.APP_LOCATION }} \
      -c Release -o publish \
      -p:PublishTrimmed=true --no-restore --nologo
```

## Related

- `docs/research/dotnet-10-sdk-consolidation.md` — full migration path and
  SDK coexistence findings
- `docs/research/blazor-wasm-trimming-gotchas.md` — `--no-restore` and
  `TrimMode=full` failure modes in detail
- `docs/research/restore-locked-mode-placebo.md` — benchmarks and references
  for `RestoreLockedMode`
