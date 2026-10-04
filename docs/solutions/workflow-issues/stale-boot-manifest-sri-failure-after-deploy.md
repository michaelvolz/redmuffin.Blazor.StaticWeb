---
title: Stale boot manifest SRI failure after deploy
date: 2026-10-04
category: workflow-issues
module: azure-swa-publish-pipeline
problem_type: runtime_error
component: tooling
symptoms:
  - Browser console shows "Failed to find a valid digest in the 'integrity' attribute" for Raindrop.Contracts.wasm, Home.wasm, redmuffin.Blazor.StaticWeb.wasm, redmuffin.Blazor.StaticWeb.Common.wasm, or AzureHealthCheck.Contracts.wasm
  - "mono_download_assets errors are followed by 'Failed to start platform'"
  - The site fails to boot only for returning visitors after a deploy, and only until a hard refresh
  - A fresh browser session always boots clean
root_cause: config_error
resolution_type: config_change
severity: high
retire_when: ".NET SDK ships default WASM boot-asset fingerprinting (framework files served under content-hashed URLs), or the minimum Safari rises to 16.4+ so BlazorFingerprintBlazorJs and WasmFingerprintAssets can re-enable. Check the fingerprint flags in src/redmuffin.Blazor.StaticWeb/redmuffin.Blazor.StaticWeb.csproj."
tags:
  [blazor, wasm, sri, integrity, caching, immutable, azure-swa, cache-busting]
---

# Stale boot manifest SRI failure after deploy

## Problem

After every CI/CD deploy of the Blazor WebAssembly site, returning visitors hit a boot failure with subresource-integrity (SRI) errors. The failure lasts as long as the browser keeps the cached copy — up to a year — and a hard refresh clears it. A fresh browser session never reproduces it.

## Symptoms

- Browser console: `Failed to find a valid digest in the 'integrity' attribute` for one or more framework assemblies, for example `Raindrop.Contracts.wasm`, `Home.wasm`, `redmuffin.Blazor.StaticWeb.wasm`, `redmuffin.Blazor.StaticWeb.Common.wasm`, or `AzureHealthCheck.Contracts.wasm`.
- `mono_download_assets` errors, then `Failed to start platform`.
- The failure appears only after a deploy, only for visitors who visited the site before the deploy, and no server-side error appears in the workflow logs.

## What Didn't Work

- Probing the deployed boot-manifest URLs under `/_framework/` (the two earlier .NET conventions `blazor.boot.json` and `dotnet.boot.json`): both return the SPA fallback `index.html` (Content-Type `text/html`). .NET 10 inlines the boot manifest into the deployed `/_framework/dotnet.js` script after the `/*json-start*/` marker, so no separate manifest file exists.
- Reproducing in a fresh browser session: fresh sessions always boot clean, because the failure lives only in returning visitors' cached `dotnet.js`.
- Searching the network log for a manifest fetch: the log shows no separate manifest request, which confirms the inlining.

## Solution

Split the cache header rules in `src/redmuffin.Blazor.StaticWeb/staticwebapp.config.json`:

- `/` and `/index.html` → `no-cache` (lines 19 and 25).
- `/_framework/dotnet.js` → `no-cache` (line 31).
- `/_framework/blazor.webassembly.js` → `no-cache` (line 37).
- `/_framework/*` stays `public, max-age=31536000, immutable` (line 43) — the fingerprinted assets under that pattern keep their year-long cache.

Fingerprinting context: `BlazorFingerprintBlazorJs=false` and `WasmFingerprintAssets=false` stay set in `redmuffin.Blazor.StaticWeb.csproj` (lines 41 and 42; the Safari version comment sits at line 40). Framework files therefore keep stable URLs, and cache headers are the only freshness lever for them.

Added `scripts/verify-boot-digests.ps1`: the script downloads `dotnet.js`, extracts the inlined boot manifest by brace matching from the `/*json-start*/` marker, hashes every listed asset with SHA-256, and compares each hash to the manifest digest. It checks every asset, then exits code 1 when any hash mismatched.

Wired the script as a CI gate: the `health_check` job in `.github/workflows/azure-static-web-apps-lively-cliff-0945be603.yml` (line 297) checks out the repo (line 305) and runs the probe against production after each deploy (line 353).

## Why This Works

The .NET 10 WASM runtime fetches each assembly with `cache: 'no-cache'`, so every browser gets the current assembly bytes after a deploy. The integrity check then compares those bytes against the digests in the boot manifest. With `immutable` on `dotnet.js`, a returning visitor's browser never revalidates `dotnet.js`, so its digests stay old while the assemblies turn new. The browser sees a digest mismatch and blocks boot.

Making `dotnet.js` revalidate aligns the digests with the freshly fetched assemblies. `immutable` remains correct for fingerprinted assets whose content never changes under a stable URL. The browser's bytes-vs-manifest mismatch during diagnosis was the decisive evidence: the hashes the visitor's browser computed matched the current CDN files, so only the cached `dotnet.js` was stale.

## Prevention

- Cache-coherence rule: every file served at a stable URL that participates in integrity verification must either revalidate on every request or never change content.
- Keep digest-carrying files out of `immutable` caching.
- The `health_check` probe fails CI when production serves boot digests that mismatch the served bytes, so a deploy that breaks returning visitors is caught immediately.
- Do not extend `immutable` beyond fingerprinted assets, and leave the fingerprint flags off until Safari 16.4+ is the minimum.

## Related Issues

- [Azure SWA cache and security headers](../performance-issues/azure-swa-cache-security-headers.md) — its `/_framework/*` immutable recommendation is superseded by the split in this document (refresh candidate).
- [Pre-compressed HTML content negotiation on SWA](pre-compressed-html-content-negotiation-swa.md) — same SWA publish-pipeline failure family.
- [Brotli compression not reaching Azure SWA production](brotli-compression-not-reaching-azure-swa-production.md) — same pipeline, same post-deploy health-check concern.
- [SDK 10 build on .NET 9 Azure SWA](../tooling-decisions/sdk-10-build-net9-azure-swa.md) — its live health check still fetches the URL `/_framework/blazor.boot.json`, which returns the SPA fallback under .NET 10 (refresh candidate).
