---
name: rm-coding-ci-github-actions-companion
description: redmuffin.Blazor.StaticWeb CI pipelines, deploy method, and step ordering. Load when editing this repo's .github/workflows/. General workflow method lives in rm-coding-ci-github-actions.
purpose: Repo-specific CI pipeline facts for redmuffin.Blazor.StaticWeb.
---

# rm-coding-ci-github-actions-companion

Companion to `rm-coding-ci-github-actions`: the global skill owns the
classification method, shared-pattern mechanism, and output schema; this
skill owns only this repo's pipelines. On a conflict between the two,
this skill wins for this repo.

## Workflows

- `.github/workflows/azure-static-web-apps-lively-cliff-0945be603.yml` —
  test, build, publish, deploy.
- `.github/workflows/codeql.yml` — weekly CodeQL scan
  (`cron: "32 7 * * 3"`), always executes (a pipeline-neutral codebase
  is not a vulnerability-free one).

## Deploy method

The only accepted deploy method is `shibayan/swa-deploy@v1`. It wraps the
same `StaticSitesClient` binary that `swa deploy` uses internally, with
zero npm dependency, automatic `~/.swa/deploy` binary caching, and no
Docker overhead. Never revert to `swa deploy` CLI or
`Azure/static-web-apps-deploy@v1` for deploy: the Azure action downloads
a 2GB Docker image every run versus a cached ~40MB binary, and Oryx
rebuilds from source, discarding Brotli compression and WASM trimming.
Pre-build once with `dotnet publish` and deploy the artifacts.

`app-location` must point directly at the directory containing
`index.html` — `bin/Release/publish/wwwroot`, never the publish root.
`staticwebapp.config.json` must be in the `wwwroot` publish directory
when custom routing is needed. The SWA CLI `--output-location`
parameter has no equivalent in this action.

Never use snake_case parameter names with `shibayan/swa-deploy@v1`: the
action uses kebab-case (`app-location`, `api-location`, `api-language`,
`environment-name`, `deployment-token`), and snake_case keys are silently
ignored. Outputs are also kebab-case:
`steps.deploy.outputs.deployment-url`, never `deployment_url`.

The sole exception is the `close_pull_request_job` teardown step, which
uses `Azure/static-web-apps-deploy@v1` with `action: "close"` — a
preview teardown API call, not a deployment. Azure automatic preview
cleanup is unreliable (orphaned previews when deploy finishes after the
close job runs, Issue #898); never skip this job.

`api-version` sets the Azure Functions runtime version for the managed
API. The API project targets `net9.0`, so the value `9.0` matches the
runtime.

```yaml
- name: Deploy to Azure Static Web Apps
  id: deploy
  uses: shibayan/swa-deploy@v1
  with:
    app-location: ${{ env.APP_LOCATION }}/bin/Release/publish/wwwroot
    api-location: ${{ env.API_LOCATION }}/bin/Release/publish
    api-language: dotnetisolated
    api-version: 9.0
    environment-name: production
    deployment-token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
```

## Step ordering

The main job (`Test, Build and Deploy`) executes in this order, with no
deviations:

1. Checkout (shallow, `fetch-depth: 1`)
2. WASM workload check + install (skip if present)
3. NuGet cache restore
4. `dotnet restore`
5. `dotnet build` (single build for both tests and publish — never split
   test and deploy into separate jobs; duplicate checkout/restore costs
   ~30s)
6. `dotnet run --project tests/...` (fail-fast gate — if tests fail, stop
   here; never `dotnet test` for TUnit, and never skip tests for
   SCSS-only changes since bUnit DOM assertions depend on CSS output)
7. **Gate on push**: `dotnet publish` (parallel: Blazor + API, full
   pipeline — never `--no-build`, which breaks .NET 10 asset
   fingerprinting; see `docs/research/blazor-wasm-trimming-gotchas.md`
   §Gotcha 3)
8. **Gate on push**: trimming verification (assembly count ≤60)
9. **Gate on push**: `shibayan/swa-deploy@v1`

Never run deploy-only steps on PR events — gate on
`github.event_name == 'push'`. Health checks curl production URLs and add
~15s of noise on PRs.

## Cache strategy

| Cache         | Key                                                     | Why                                                                |
| ------------- | ------------------------------------------------------- | ------------------------------------------------------------------ |
| NuGet         | `nuget-${{ hashFiles('**/Directory.Packages.props') }}` | `Directory.Packages.props` is the single source of package versions. |
| WASM workload | Skip install if `dotnet workload list \| grep -q wasm-tools` | The workload is present on 95%+ of runs (ubuntu-24.04 ships it). |
| Deploy binary | Handled automatically by `shibayan/swa-deploy@v1`      | The action caches `~/.swa/deploy` internally.                      |

Never cache on `packages.lock.json` — lock files were removed from the
repo. Never use `setup-dotnet` or `setup-node`: the Ubuntu 24.04 runner
ships .NET SDK 10.0.201 and Node.js 24.

## Requirements

Every requirement must be satisfied by every workflow change in this repo.

| ID  | Requirement                                                     | Verification                                                                           |
| --- | --------------------------------------------------------------- | -------------------------------------------------------------------------------------- |
| R1  | Tests pass before deploy — fail-fast                            | `dotnet run --project tests/...` exit code                                             |
| R2  | Blazor publish trimmed — wasm count ≤60                         | `find publish/wwwroot/_framework -name '*.wasm' -not -name 'dotnet.native.*' \| wc -l` |
| R3  | Brotli-compressed assets survive deploy                         | `curl -I \| grep content-encoding: br` (post-deploy)                                   |
| R4  | Single `dotnet build` per run                                   | Grep workflow for `dotnet build` — must appear exactly once                            |
| R5  | TUnit via `dotnet run`, never `dotnet test`                     | Grep workflow for `dotnet test` — must be absent                                       |
| R6  | `Directory.Packages.props` is single source of package versions | `dotnet restore` succeeds without lock file                                            |
| R7  | WASM workload present for Blazor publish                        | `dotnet workload list \| grep -q wasm-tools`                                           |
| R8  | Pipeline-neutral changes skip deploy, NOT tests                 | `check_changes` gate job with `if: should_skip != 'true'`                              |
| R9  | `close_pull_request_job` always present                         | Grep workflow for `action: "close"`                                                    |
| R10 | `fetch-depth: 0` in `check_changes` job                         | Read workflow checkout step                                                            |

## Repo never rules

- Never introduce npm into the CI pipeline — the repo has zero
  `package.json` files. Never hash `package.json` for caching and never
  run a background npm install without a `timeout` wrapper (postinstall
  scripts hung CI for 6 hours).
- Never leave one workflow with stale skip logic when the other is
  current. Two inline copies drifted within 3 commits (`.editorconfig`
  was in one but not the other).
- `.npmrc` is pipeline-relevant here even though `shibayan/swa-deploy@v1`
  never reads it: it enforces local supply chain hardening that must
  never drift silently.
- `.editorconfig` is pipeline-neutral here: every setting in this
  project's file controls analyzer severity and formatting only.
- `swa-cli.config.json` is pipeline-neutral (local serve config).
- `tools/**` and `scripts/**` are pipeline-neutral by policy: local
  development tooling and scripts CI never calls.

## Local testing

Validate with `act` (nektos/act) before pushing. Docker image (build
once, never commit a `Dockerfile`):

```bash
cat <<'DOCKERFILE' > /tmp/Dockerfile.dotnet-node
FROM mcr.microsoft.com/dotnet/sdk:10.0
RUN apt-get update && apt-get install -y --no-install-recommends nodejs npm
DOCKERFILE
docker build -t dotnet-sdk-node:10.0 -f /tmp/Dockerfile.dotnet-node /tmp
```

```powershell
act push \
  -W .github/workflows/azure-static-web-apps-lively-cliff-0945be603.yml \
  -P ubuntu-latest=dotnet-sdk-node:10.0 \
  --pull=false
```

Single job (build + tests only): add `-j test_and_build_job`.
Dry-run: add `-n`. The deploy step fails (no Azure token) and health
check skips (no production URL) — expected. API integration tests need
RainDrop secrets that exist only on GitHub
(`AssertionException: RainDropTestToken is null` locally). Never assume
local API test failures are workflow bugs. Never use `wrkflw` on
Windows — bash emulation cannot handle the deploy workflow's multi-line
`if/then` blocks.
