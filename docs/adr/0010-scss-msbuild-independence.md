---
date: 2026-05-30
status: accepted
---

# CLI-Only SCSS/JS Toolchain — No MSBuild Involvement

SCSS compilation and JavaScript minification are handled entirely by CLI tools
(dart-sass, terser). No MSBuild targets, NuGet packages, or MSBuild task
runners are involved. The CI pipeline has zero CSS/JS tooling — pre-compiled
assets are committed to the repo.

## Decision

- **SCSS**: `sass` CLI (dart-sass) — expanded `app.css` for DevTools readability,
  compressed `app.min.css` for production.
- **JS minification**: `terser` CLI.
- **Dev watcher**: `sass --watch scss/app.scss:wwwroot/css/app.css` runs as a
  systemd user service alongside `dotnet watch`.
- **Production build**: `sass --style=compressed --no-source-map` one-shot,
  committed alongside `.scss` changes.
- **No MSBuild involvement**: No `BuildWebCompiler`, no `compilerconfig.json`,
  no Debug-Sass config, no `.targets` hooks for CSS.

## Considered Options

**Keep MSBuild SCSS compilation via BuildWebCompiler / WebCompiler2022.**
Rejected. These packages use MSBuild task runners that lock the generated CSS
files during `dotnet build`. When `dotnet watch` hot-reload tries to serve the
file, a lock conflict crashes the dev session. The crash is silent — `dotnet
watch` simply stops reloading, and the developer reloads a stale page
indefinitely.

**Use `dotnet-sass` NuGet package as an MSBuild hook.**
Rejected. Same underlying file-lock conflict. Any MSBuild task that writes to
`wwwroot/` during build will conflict with `dotnet watch`'s file watcher.

**Keep `Debug-Sass` as a separate build configuration.**
Evaluated and rejected. The Debug-Sass config conditionally ran the SCSS
compiler only in Debug builds. It worked but added complexity to every
`.csproj` and required a separate build configuration that could drift from
Release. CLI watchers are simpler and always active.

**Inline SCSS via Blazor CSS isolation (`.razor.css`).**
Rejected for global styles. CSS isolation is scoped to components — it cannot
replace shared variables, mixins, layout grids, or Foundation's utility classes.

**CI handles SCSS/JS compilation.**
Rejected. Adding dart-sass, terser, or npm to the CI runner for CSS/JS
compilation adds pipeline time and another potential failure point for a
task that takes ~0.45s locally. Pre-compiled assets committed to the repo
eliminate CI tooling entirely — CI runs `dotnet build -- publish -- swa deploy`
with zero asset processing.

## Consequences

- The `sass` watcher runs as a dedicated systemd user service named
  `redmuffin.Blazor.StaticWeb-sass-watcher`.
- Production `app.min.css` is committed alongside every `.scss` change
  (mandated by PRE-COMMIT VERIFICATION in AGENTS.md).
- CI has zero SCSS/JS tooling — `dotnet build -- publish -- swa deploy` only.
- SRI (Subresource Integrity) integrity hashes in `wwwroot/` can become stale
  after `dotnet watch` rebuilds the app without recompiling CSS. The fix is
  a full `dotnet build` restart — hot-reload alone is insufficient.
- dart-sass and terser are required local dev tools, documented in
  `rm-dev-toolchain` with cross-platform install commands.
- Foundation library files in `lib/foundation-sites/scss/` are never modified
  directly — selective `@include` calls in `app.scss` replace
  `foundation-everything()`.
