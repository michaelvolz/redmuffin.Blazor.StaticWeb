---
title: "SCSS Toolchain Migration to dart-sass CLI with Systemd Dev Server"
date: 2026-05-14
category: docs/solutions/tooling-decisions
module: build-configuration
problem_type: tooling_decision
component: tooling
severity: high
applies_when:
  - "Migrating from LibSass-based NuGet packages (BuildWebCompiler2022)"
  - "Setting up cross-platform SCSS compilation for .NET Blazor projects"
  - "Running long-lived dotnet processes from agent tools with timeouts"
  - "Coordinating multiple watchers (SCSS + dotnet) as a unified service"
tags:
  - "dart-sass"
  - "scss"
  - "build-toolchain"
  - "systemd-run"
  - "dotnet-watch"
  - "cross-platform"
  - "dev-server"
  - "TimeoutStopSec"
---

# SCSS Toolchain Migration to dart-sass CLI with Systemd Dev Server

## Context

The project used BuildWebCompiler2022 (NuGet), which wraps LibSass (EOL
Oct 2025). It was Windows-only, requiring `dotnet build -c Debug-Sass`
after every SCSS edit. On Linux, SCSS changes could not be compiled
locally. The bash tool's 120s timeout kills long-running `dotnet watch`
processes. Both problems needed solving together — a new compiler and a
way to run the dev server that survives tool timeouts.

## Guidance

### Part 1: SCSS Toolchain

**Install dart-sass** (Omarchy priority 1: pacman for system packages):

```bash
sudo pacman -S dart-sass          # Arch / Omarchy
winget install Sass.DartSass      # Windows 11
```

**Production build** (one-shot, ~0.45s):

```bash
sass --style=compressed --no-source-map \
  scss/app.scss:wwwroot/css/app.min.css
```

**Development watchers** — two in parallel, both tracking partials:

```bash
sass --watch scss/app.scss:wwwroot/css/app.css &                    # expanded
sass --style=compressed --watch scss/app.scss:wwwroot/css/app.min.css &  # compressed
```

Key finding: **file-syntax watchers track `@use`/`@import` partials
transitively.** Directory-mode watchers (`scss:wwwroot/css`) only
output `app.css` — never `app.min.css`.

Both files are always fresh. `app.min.css` is committed directly. CI
deploys it without any SCSS toolchain.

**What was removed:**

- `BuildWebCompiler2022` from `Directory.Packages.props`
- `compilerconfig.json`
- `Debug-Sass` build configuration from `.csproj`
- MSBuild swap target (replaced `app.css` → `app.min.css` in published
  index.html — hack, rejected)

**What stays unchanged:**

- CI pipeline (0 new dependencies — CSS is pre-compiled and committed)
- `index.html` always loads `app.min.css` (dev and prod use the same file)
- Build: 0 errors, 0 warnings. Tests: 325/325 pass.

### Part 2: Systemd Dev Server

**Prerequisites:**

```bash
sudo pacman -S aspnet-runtime    # SDK 10's dotnet-watch needs .NET 10 runtime
```

**Start** (returns immediately, survives bash timeout):

```bash
systemd-run --user \
  --working-directory=/home/flynn/Projects/redmuffin.Blazor.StaticWeb/src/redmuffin.Blazor.StaticWeb \
  -p TimeoutStopSec=1 \
  --unit=redmuffin.Blazor.StaticWeb-sass-dotnet-watch \
  bash -c 'sass --watch scss/app.scss:wwwroot/css/app.css & \
           sass --style=compressed --watch scss/app.scss:wwwroot/css/app.min.css & \
           dotnet watch --launch-profile https'
```

**Critical design rules:**

1. **`--working-directory`, not `cd &&`.** Bash background jobs (`&`) do
   not propagate `cd` to all children correctly. `systemd-run` sets CWD
   at the unit level — all children inherit identically.

2. **`TimeoutStopSec=1`.** `dotnet watch` ignores SIGTERM. Default 90s
   means `systemctl stop` hangs for 91 seconds. Setting it to 1s gives
   a ~2.3s total stop (1s wait + SIGKILL).

3. **Foreground `dotnet watch` (no `&`).** Backgrounding it causes `bash -c`
   to exit immediately, killing the service. Keep it as the foreground
   process; bash stays alive, service stays active.

4. **Never `dotnet build` while the dev server is running.** File locks
   from `dotnet watch` clash with MSBuild.

**Stop** (~2.3s):

```bash
systemctl --user stop redmuffin.Blazor.StaticWeb-sass-dotnet-watch.service
```

**Restart:**

```bash
systemctl --user stop redmuffin.Blazor.StaticWeb-sass-dotnet-watch.service && \
  systemctl --user reset-failed redmuffin.Blazor.StaticWeb-sass-dotnet-watch.service && \
  systemd-run --user [same start command]
```

**Check status:**

```bash
systemctl --user is-active redmuffin.Blazor.StaticWeb-sass-dotnet-watch.service
ss -tlnp | grep 5233
curl -s -o /dev/null -w '%{http_code}' http://localhost:5233/
```

**View logs:**

```bash
journalctl --user-unit=redmuffin.Blazor.StaticWeb-sass-dotnet-watch.service --no-pager -n 40
```

## Why This Matters

| Metric                   | Old (BWC)                             | New (dart-sass + systemd)           |
| ------------------------ | ------------------------------------- | ----------------------------------- |
| Compile time             | ~3s (MSBuild overhead)                | ~0.45s                              |
| SCSS compilation step    | Manual (`dotnet build -c Debug-Sass`) | Zero (auto on save)                 |
| Production CSS freshness | Manual (same build step)              | Zero (auto on save)                 |
| Stop time                | N/A (`fuser -k` hack)                 | ~2.3s (proper systemd)              |
| Platform                 | Windows only                          | Linux/macOS/Windows                 |
| Compiler lifecycle       | EOL (LibSass)                         | Active (Dart Sass v1.99+)           |
| Stale CSS risk           | Same as new (manual)                  | Zero (auto-generated, always fresh) |

## When to Apply

- Migrating any .NET project from LibSass-based NuGet SCSS compilation
- Setting up cross-platform development for Blazor WASM
- Running long-lived dev servers from agent tools with OS timeouts
- Coordinating multiple file watchers alongside a dev server

## Examples

**Full start command for this project:**

```bash
systemd-run --user \
  --working-directory=/home/flynn/Projects/redmuffin.Blazor.StaticWeb/src/redmuffin.Blazor.StaticWeb \
  -p TimeoutStopSec=1 \
  --unit=redmuffin.Blazor.StaticWeb-sass-dotnet-watch \
  bash -c 'sass --watch scss/app.scss:wwwroot/css/app.css & \
           sass --style=compressed --watch scss/app.scss:wwwroot/css/app.min.css & \
           dotnet watch --launch-profile https'
```

## Related

- `rm-dev-tools` skill — comprehensive toolchain reference with install commands
- `rm-scss` skill — SCSS conventions and Foundation philosophy
- `rm-dev-workflows` skill — dev environment process management
- `rm-cleanup` skill — session cleanup (kills dev server properly)
- [Dev Server Lifecycle for Blazor WASM](/docs/solutions/developer-experience/agent-optimized-site-startup-for-blazor-wasm-2026-04-04.md) — Windows/Start-Process approach
- [Bash Timeout Kills Long-Running Dotnet Processes](/docs/solutions/developer-experience/bash-timeout-kills-long-running-dotnet-processes-2026-04-04.md) — Windows PowerShell solution
- [Debug-Sass Lockfile Drift Prevention](/docs/solutions/build-errors/debug-sass-lockfile-drift-2026-04-02.md) — obsolete post-migration (add deprecation note)
