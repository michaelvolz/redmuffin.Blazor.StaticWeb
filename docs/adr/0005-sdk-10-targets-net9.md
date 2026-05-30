---
date: 2026-05-30
status: accepted
---

# SDK 10 Builds net9.0 Targets for Azure SWA Compatibility

The repo uses the .NET 10 SDK (pinned at `10.0.100` in `global.json`) to build
all projects, but every deployable project targets `net9.0` because Azure Static
Web Apps managed Functions host only supports `dotnet-isolated:9.0` as the
maximum runtime version. Tools projects (`tools/src/` and `tools/tests/`) target
`net10.0` since they run locally and are never deployed.

## Considered Options

**Stay on .NET 9 SDK entirely.**
Rejected. Loses .NET 10 tooling improvements — newer Roslyn analyzers, MSBuild
performance fixes, and SDK-level features like `StaticWebAssetFingerprintPattern`
(though that specific feature is incompatible with net9.0 targeting). The 9.0
SDK is also no longer receiving non-security patches.

**Upgrade all projects to net10.0.**
Rejected. Azure SWA managed Functions host caps at `dotnet-isolated:9.0`.
Deploying a net10.0 Functions project causes a 500 error at runtime with no
build-time warning. Once SWA adds .NET 10 support, switching targets is a
one-line change per `.csproj`.

**Dual SDK installation (SDK 9 + SDK 10).**
Rejected. Adds CI complexity (two `setup-dotnet` steps), requires maintaining
two `global.json` files, and creates confusion about which SDK to use for what.

**SDK 10 with rollForward to build net9.0 (chosen).**
SDK 10 builds net9.0 targets natively with zero issues — the SDK supports all
previous target frameworks. The `wasm-tools` workload must be installed for
Blazor WASM publishing (`dotnet workload install wasm-tools-net9`), and the
`aspnet-runtime` 10.0 package is needed for `dotnet watch` on Linux.

## Consequences

- `global.json` at repo root pins SDK `10.0.100` with `rollForward: latestMinor`
  and `allowPrerelease: false`. No other `global.json` exists in the repo.
- Tools projects (`tools/src/`, `tools/tests/`) freely target `net10.0` — they
  run locally and are never deployed to Azure.
- CI (ubuntu-24.04) pre-installs SDK 10.0.201 — no `setup-dotnet@v5` step
  needed, saving ~68s of pipeline time.
- CI must run `dotnet workload install wasm-tools-net9` for Blazor WASM builds
  targeting net9.0. The `setup-dotnet@v5` `workloads` parameter installs the
  net10 variant, which is wrong.
- ASP.NET Runtime 10 (`aspnet-runtime 10.0.x`) is installed via system package
  manager, required by `dotnet watch` under SDK 10 on Linux.
- Switching to `net10.0` project targets is blocked on Azure SWA adding
  `dotnet-isolated:10.0` support.
