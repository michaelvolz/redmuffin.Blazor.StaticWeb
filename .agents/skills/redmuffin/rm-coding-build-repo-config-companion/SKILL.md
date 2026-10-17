---
name: rm-coding-build-repo-config-companion
description: redmuffin.Blazor.StaticWeb build, test, publish, and device rules. Load when building, testing, or publishing that repo. General .NET build conventions live in rm-coding-build-repo-config.
purpose: Repo-specific build, publish, and device rules for redmuffin.Blazor.StaticWeb.
---

# rm-coding-build-repo-config-companion

Companion to `rm-coding-build-repo-config`: the global skill owns the
general routine, and this skill owns only what is unique to
redmuffin.Blazor.StaticWeb. On a conflict between the two, this skill
wins for this repo.

## WHEN TO LOAD

- Building, testing, or publishing redmuffin.Blazor.StaticWeb.
- Editing its publish, fingerprint, or device configuration.

## Build and test

- The pre-commit build and test gate lives in `AGENTS.md`; run it before
  every commit.
- SCSS compilation belongs to `rm-dev-toolchain`; never invoke sass
  outside its workflows because a second watcher corrupts output.

## Publish

- Publish through the full production build in `rm-dev-toolchain`; never
  publish with `--no-build` because fingerprinting needs the full
  pipeline.
- Keep the `#[.{fingerprint}]` placeholders in `index.html` intact;
  MSBuild replaces them during `dotnet publish`.
- Do not remove the custom `AfterTargets="Publish"` target in the Blazor
  `.csproj`; it fingerprints CSS, regenerates compressed files, and
  rewrites `index.html` because the Blazor SDK fingerprints no CSS in
  standalone WASM by design.

```html
<script src="_framework/blazor.webassembly#[.{fingerprint}].js"></script>
```

## Target frameworks

- Every project in the repo targets `net10.0`. SWA managed functions
  support `dotnet-isolated:10.0` (verified 2026-10-17), so the Api no
  longer holds a lower target. The `staticwebapp.config.json`
  `apiRuntime` and the workflow's `api-version` move together with any
  Api target-framework change. Never bump a target framework as part
  of a config edit.

## WASM mobile (iOS Safari)

- This repo defines no browser support floor, so no WASM compatibility
  property is policy. Before setting any property from
  `rm-coding-ui-blazor` → `references/dotnet10-blazor.md`, ask the repo
  owner — never apply them unprompted.
