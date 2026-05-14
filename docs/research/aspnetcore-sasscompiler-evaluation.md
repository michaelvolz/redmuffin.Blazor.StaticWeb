---
date: 2026-05-14
title: AspNetCore.SassCompiler Evaluation — Replacement for BuildWebCompiler2022
tags: [research, sass, scss, build-tools, migration, blazor-wasm, dart-sass]
description: Comprehensive evaluation of AspNetCore.SassCompiler as a BuildWebCompiler2022 replacement in a .NET 10 SDK / net9.0 Blazor WASM project using Foundation 6 SCSS.
---

## CORRECTION (2026-05-14)

**This evaluation recommended AspNetCore.SassCompiler as a NuGet package. We did not use it.**

Reasoning:

- AspNetCore.SassCompiler fires SCSS compilation on every `dotnet build` — wasteful.
- It has no watch mode for Blazor WASM — the `dotnet watch` integration is broken.
- It's SCSS-only — JS minification requires a separate tool anyway.

**What we did instead**: installed `dart-sass` directly (`sudo pacman -S dart-sass`) and use the `sass` CLI. No NuGet, no MSBuild, no `dotnet build -c Debug-Sass` hack. JS minification via `npx --yes terser`.

The `sass --watch` background process auto-compiles SCSS on save, `dotnet watch` detects the CSS changes, browser reloads. Zero manual compilation during development. Cross-platform.

See `rm-dev-tools` for the current toolchain reference.

## Summary Verdict

**Migration is feasible but not zero-cost.** AspNetCore.SassCompiler works with .NET 10 SDK targeting net9.0 Blazor WASM, compiles Foundation 6 SCSS correctly (with deprecation warnings that can be silenced), is cross-platform, and has active maintenance. Two significant gaps exist:

1. **No file watching in Blazor WASM** — `dotnet watch` won't recompile SCSS files on save. You must manually rebuild.
2. **No JavaScript compilation** — BuildWebCompiler2022 handles both SCSS and JS; this package is SCSS-only.

If those are acceptable, proceed. If not, alternatives should be explored.

---

## 1. .NET 10 SDK / net9.0 Compatibility

**Verdict: ✅ Compatible (HIGH confidence)**

- NuGet latest version: **1.99.0** (April 6, 2026)
- Targets: `net6.0` and `netstandard2.0`
- NuGet computes compatibility up to `net10.0`
- The package bundles platform-specific native Dart Sass executables (no Node.js dependency)
- The Blazor WASM sample in the repo targets `net8.0` but the MSBuild targets have no framework-specific restrictions
- 2.7M total NuGet downloads, active releases (8 releases in the last 6 months)

**Source:** NuGet Gallery shows "net10.0 was computed" as compatible; the repo's own Blazor WASM sample works.

---

## 2. Configuration Format & Migration Path

**Verdict: ⚠️ Different format; migration is manual but straightforward (HIGH confidence)**

**Current format (BuildWebCompiler2022 `compilerconfig.json`):**

```json
[
  { "inputFile": "scss/app.scss", "outputFile": "wwwroot/css/app.min.css" },
  { "inputFile": "scss/app.scss", "outputFile": "wwwroot/css/app.css" },
  {
    "inputFile": "scss/media-query-debugger.scss",
    "outputFile": "wwwroot/css/media-query-debugger.css"
  }
]
```

**New format (AspNetCore.SassCompiler `sasscompiler.json`):**

```json
{
  "Source": "scss",
  "Target": "wwwroot/css",
  "Arguments": "--error-css --style=expanded --source-map",
  "Compilations": [
    { "Source": "scss/app.scss", "Target": "wwwroot/css/app.min.css" },
    { "Source": "scss/app.scss", "Target": "wwwroot/css/app.css" },
    {
      "Source": "scss/media-query-debugger.scss",
      "Target": "wwwroot/css/media-query-debugger.css"
    }
  ],
  "Configurations": {
    "Debug": {
      "Arguments": "--error-css --style=expanded --source-map"
    },
    "Release": {
      "Arguments": "--no-error-css --style=compressed --no-source-map"
    }
  }
}
```

**Key migration notes:**

- Delete `compilerconfig.json` after migration
- Configuration goes in `sasscompiler.json` at project root (NOT nested under "SassCompiler" key)
- For Blazor WASM, `sasscompiler.json` is **required** (cannot use `appsettings.json`)
- The `Compilations` array handles multiple source→target pairs
- Configurations can be overridden per build configuration (Debug/Release)
- The `Arguments` field passes raw CLI flags to `dart-sass`; use `--style=compressed` for minification instead of separate .min output targets

**⚠️ sasscompiler.json conflict**: If you have both an RCL and a Blazor WASM app with `sasscompiler.json`, publish will fail with NETSDK1152. Workaround: add `<Content Remove="sasscompiler.json" />` + `<None Include="..." CopyToPublishDirectory="Never" />` to the RCL csproj (see issue #254).

---

## 3. Foundation 6 SCSS Compatibility

**Verdict: ✅ Compiles correctly; generates deprecation warnings (HIGH confidence)**

Foundation 6 uses extensive `@import` (old Sass syntax). Dart Sass 1.80+ deprecated `@import` in favor of `@use`/`@forward`. Key facts:

- **Dart Sass 1.80.0** (Oct 2024) started emitting **deprecation warnings** for `@import`
- **Dart Sass 2.0.0** does NOT remove `@import` — it introduces other breaking changes
- **Dart Sass 3.0.0** will remove `@import` — scheduled **no sooner than 2 years after 1.80.0** (i.e., ~late 2026 at the absolute earliest)
- **Foundation 6 still compiles fine** — only warnings are emitted, not errors
- The **sass-migrator** tool fails on Foundation 6's import patterns (see foundation-sites#15518)
- Foundation 6 upstream has no migration timeline yet

**Mitigation options (all supported via Arguments in sasscompiler.json):**

| Flag                           | Effect                                                  |
| ------------------------------ | ------------------------------------------------------- |
| `--silence-deprecation=import` | Silences @import warnings specifically                  |
| `--quiet-deps`                 | Silences warnings from dependencies (load-path imports) |
| `--quiet`                      | Silences ALL warnings (not recommended)                 |

**Recommended Arguments for Debug builds:**

```
--error-css --style=expanded --source-map --silence-deprecation=import --quiet-deps
```

**Confidence:** HIGH — confirmed by Sass official docs, Foundation issue tracker, and multiple community reports.

---

## 4. LibSass vs Dart Sass Output Differences

**Verdict: ⚠️ Minor; Foundation 6 output is functionally identical (MEDIUM confidence)**

**Known differences that may affect Foundation 6:**

1. **`@import` globs** — `@import "modules/*"` does NOT work in Dart Sass. Foundation 6 does not use glob imports in its core distribution (they use explicit imports). If your custom code uses globs, you must expand them manually.

2. **Division with `/`** — Dart Sass deprecates `/` as division outside `calc()`. Foundation 6 may use this pattern. If affected, use `math.div()` or wrap in `calc()`.

3. **Color function changes** — Dart Sass 1.79+ moved color functions to `sass:color` module. Foundation 6 uses global color functions which now emit deprecation warnings. Use `--silence-deprecation=color-functions` or add `@use "sass:color"`.

4. **Asset helpers** — `image-url()`, `asset-url()` don't work with Dart Sass (Ruby/Rails-specific). Not relevant for this project unless custom code uses them.

5. **CSS output** — Functionally identical for Foundation 6. Any differences would be in source map formatting or compressed whitespace, not in rendered styles.

**Recommendation:** After migration, diff the compiled CSS output against the current LibSass output to verify. Run `git diff` on `wwwroot/css/` after the first build.

**Confidence:** MEDIUM — differences are documented but Foundation 6-specific testing would require a local build comparison.

---

## 5. File Watching (dotnet watch)

**Verdict: 🔴 NOT supported for Blazor WASM (HIGH confidence)**

- The Sass watcher (`AddSassCompiler()` hosted service) is only supported in Blazor **Server** and ASP.NET Core MVC
- Blazor WASM issue: **#68** (open since April 2022, no PR in progress)
- The README explicitly states: "The sass watcher is currently not supported for Blazor WebAssembly projects"
- The MSBuild task compiles on **build and publish** — not on file save

**Practical impact:** During development, you must manually rebuild (`dotnet build`) after each `.scss` change to see updates. `dotnet watch` will restart on `.cs`/`.razor` changes but won't trigger SCSS recompilation.

**Workaround:** Use the `dotnet watch` hot reload with a separate terminal running a manual rebuild loop, or use the VS Code "Web Compiler 2022+" extension for watch support alongside AspNetCore.SassCompiler.

---

## 6. Minification

**Verdict: ✅ Supported via CLI arguments, not separate .min output (HIGH confidence)**

Dart Sass minification uses `--style=compressed`. There is no automatic `.min.css` suffix — you specify the output filename directly in the `Compilations` array.

**Example from sasscompiler.json:**

```json
{
  "Compilations": [
    { "Source": "scss/app.scss", "Target": "wwwroot/css/app.min.css" }
  ],
  "Arguments": "--style=compressed"
}
```

To have BOTH minified and expanded outputs, list them as separate compilation entries with different build configuration overrides in `Configurations.Debug` vs `Configurations.Release`.

---

## 7. Source Maps

**Verdict: ✅ Supported by default; highly configurable (HIGH confidence)**

Dart Sass generates source maps by default. Available flags:

| Flag                         | Behavior                                              |
| ---------------------------- | ----------------------------------------------------- |
| (default)                    | Generates separate `.css.map` file with relative URLs |
| `--no-source-map`            | Disables source maps                                  |
| `--source-map-urls=relative` | Relative paths (default)                              |
| `--source-map-urls=absolute` | Absolute `file://` paths                              |
| `--embed-sources`            | Embeds full SCSS source in map file                   |
| `--embed-source-map`         | Embeds source map in CSS file itself                  |

Vs. BuildWebCompiler2022 source maps: LibSass source maps are compatible with browser devtools. Dart Sass source maps follow the same v3 spec. No functional difference.

---

## 8. Performance

**Verdict: ✅ Dart Sass VM is comparable or faster than LibSass (MEDIUM confidence)**

- Dart Sass (native VM binary) is the primary implementation and actively optimized
- LibSass (C/C++) was historically fast but is now unmaintained
- The AspNetCore.SassCompiler package bundles native Dart Sass executables (NOT the JS port), so there is no Node.js overhead
- Cold-start: Dart Sass VM startup may be slightly slower than LibSass C library, but for typical SCSS compilation this is negligible (<100ms difference)
- Warm compilations in watch mode (not available for WASM): incremental

**Confidence:** MEDIUM — no direct benchmark found comparing the two in a .NET build context.

---

## 9. Cross-Platform Support

**Verdict: ✅ Linux, macOS, Windows (HIGH confidence)**

The NuGet package bundles native Dart Sass executables per platform:

- **Windows**: win-x64, win-arm64
- **macOS**: osx-x64, osx-arm64 (Apple Silicon)
- **Linux**: linux-x64, linux-arm64, linux-musl-x64

**Caveats:**

- Alpine Linux requires `apk add gcompat` for the Dart runtime
- The package auto-selects the correct native binary based on OS and architecture

**Confidence:** HIGH — confirmed by NuGet package structure and README.

---

## 10. MSBuild Integration

**Verdict: ✅ Automatic via NuGet PackageReference; no separate build config needed (HIGH confidence)**

- Adding the NuGet package automatically imports `.props` and `.targets` into the build
- SCSS compilation runs during **every build** (Debug and Release)
- Also runs during **publish** (`dotnet publish`)
- **No separate build configuration needed** — `Debug-Sass` can be retired

**MSBuild properties (add to .csproj if needed):**

| Property                      | Default | Purpose                                                                                               |
| ----------------------------- | ------- | ----------------------------------------------------------------------------------------------------- |
| `SassCompilerEnableBuildTask` | `true`  | Set to `false` to disable build-time compilation                                                      |
| `SassCompilerIncludeRuntime`  | `false` | Set to `true` to include Dart Sass binaries in Release output (needed for `ISassCompiler` at runtime) |

**Build integration vs. BuildWebCompiler2022:**

- BWC2022 requires a separate `Debug-Sass` build config and custom MSBuild target
- AspNetCore.SassCompiler **replaces** this — it hooks into the standard build pipeline automatically
- The `dotnet build -c Debug-Sass` pattern can be simplified to just `dotnet build`

**Known issues:**

- **#199**: "Multiple builds required" — some users report needing two builds for CSS to appear. Likely related to MSBuild incremental compilation cache. Workaround: clean build or touch a source file.
- **#213**: "Break build on SASS compilation errors" — by default, SCSS errors don't fail the build (error CSS is emitted instead). Use `--stop-on-error` in Arguments to fail the build on SCSS errors.

---

## 🔴 Blocker: No JavaScript Compilation

**Verdict: 🔴 AspNetCore.SassCompiler is SCSS-only (HIGH confidence)**

BuildWebCompiler2022 handles both SCSS compilation and JavaScript minification in `compilerconfig.json`. AspNetCore.SassCompiler does **NOT** compile or minify JavaScript.

Your current `compilerconfig.json` includes:

- `page-load-timing.js` → `page-load-timing.min.js`

**Solutions for JS minification:**

1. **Keep BuildWebCompiler2022 for JS only** (keep `compilerconfig.json` with only the JS entry, use `Debug-Sass` config for it)
2. **Use a separate JS bundler/minifier**: `BundlerMinifier.Core`, `WebOptimizer`, or a node-based tool
3. **Use MSBuild task for JS minification**: Write a custom target that calls a JS minifier

**Recommendation:** Option 1 is simplest — keep BWC2022 for JS only while migrating SCSS to AspNetCore.SassCompiler.

---

## Migration Steps (if proceeding)

1. **Install NuGet package:**

   ```
   dotnet add package AspNetCore.SassCompiler --version 1.99.0
   ```

2. **Create `sasscompiler.json`:**

   ```json
   {
     "Source": "scss",
     "Target": "wwwroot/css",
     "Arguments": "--error-css --style=expanded --source-map --silence-deprecation=import --quiet-deps",
     "Compilations": [
       { "Source": "scss/app.scss", "Target": "wwwroot/css/app.min.css" },
       { "Source": "scss/app.scss", "Target": "wwwroot/css/app.css" },
       {
         "Source": "scss/media-query-debugger.scss",
         "Target": "wwwroot/css/media-query-debugger.css"
       }
     ],
     "IncludePaths": ["scss"],
     "Configurations": {
       "Release": {
         "Arguments": "--no-error-css --style=compressed --no-source-map --silence-deprecation=import --quiet-deps"
       }
     }
   }
   ```

3. **Remove BuildWebCompiler2022 NuGet package** (or keep it for JS minification only).

4. **Remove `compilerconfig.json`** (or strip it to JS-only entries).

5. **Remove `Debug-Sass` build configuration** from `.csproj` if AspNetCore.SassCompiler replaces it fully.

6. **Remove any custom MSBuild SCSS compilation targets** from `.csproj`/`Directory.Build.targets`.

7. **Build and verify:**

   ```
   dotnet build
   ```

   Check `wwwroot/css/` for output files.

8. **Diff CSS output** against current LibSass output to catch any rendering differences.

9. **Update `.gitignore`** to exclude generated CSS files (optional — package recommends adding generated CSS to `.gitignore` since it's regenerated on publish).

10. **Test publish:**
    ```
    dotnet publish -c Release
    ```

---

## References

- Repo: https://github.com/koenvzeijl/AspNetCore.SassCompiler
- NuGet: https://www.nuget.org/packages/AspNetCore.SassCompiler
- Blazor WASM sample: https://github.com/koenvzeijl/AspNetCore.SassCompiler/tree/master/Samples/AspNetCore.SassCompiler.BlazorWasmSample
- Sass @import deprecation: https://sass-lang.com/documentation/breaking-changes/import/
- Dart Sass CLI docs: https://sass-lang.com/documentation/cli/dart-sass/
- Foundation 6 @import issue: https://github.com/foundation/foundation-sites/issues/15518
- Blazor WASM watcher issue: https://github.com/koenvzeijl/AspNetCore.SassCompiler/issues/68
- Publish conflict issue: https://github.com/koenvzeijl/AspNetCore.SassCompiler/issues/254
- NETSDK1152 workaround: https://github.com/koenvzeijl/AspNetCore.SassCompiler/issues/254
- LibSass deprecation: https://sass-lang.com/blog/libsass-is-deprecated/
