---
title: OpenTelemetry Implementation Plan
module: developer-experience
tags:
  [
    opentelemetry,
    observability,
    gdpr,
    azure-functions,
    grafana-cloud,
    honeycomb,
  ]
problem_type: implementation-plan
date: 2026-05-28
---

# OpenTelemetry Implementation Plan

## Context

The project runs on Azure Static Web Apps (free tier) with Blazor WebAssembly
(.NET 10) frontend and Azure Functions isolated worker (.NET 9) backend. No
dedicated server infrastructure exists. The goal is to add distributed tracing,
metrics, and error tracking without introducing GDPR/ePrivacy compliance burden.

## Architecture Constraints

- Free Azure Static Web Apps tier (no SLA, no private endpoints, no server)
- Blazor WASM runs entirely in the browser — traditional OTel SDK does not work
- Azure Functions isolated worker (.NET 9) is the only server-side component
- No cookies, no Google Analytics, no tracking — intentionally banner-free
- localStorage usage is strictly functional (Raindrop item caches, Raindrop
  sign-in tokens, debug-page diagnostics probes)

## Free Tier Landscape (May 2026)

| Service                    | Free tier                                           | OTel native? | Verdict                                    |
| -------------------------- | --------------------------------------------------- | ------------ | ------------------------------------------ |
| Grafana Cloud              | 10K series, 50GB logs, 50GB traces, 14-day, 3 users | Full OTLP    | **Best fit**                               |
| Honeycomb                  | 20M events/month, 60-day, unlimited users           | Native OTel  | Best for tracing                           |
| New Relic                  | 100GB/month, 1 user, 8-day                          | Full OTLP    | Most volume, 1-user limit                  |
| Axiom                      | 500GB/month, 30-day                                 | OTLP         | Highest raw ingest                         |
| Azure Monitor/App Insights | 5GB/month free (Log Analytics)                      | OTLP preview | Tightest Azure integration, tiny free tier |
| Datadog                    | 14-day trial only                                   | Partial      | Not viable                                 |

**Decision: Grafana Cloud free tier** as backend. 50GB traces is generous,
14-day retention sufficient, OTLP ingestion native, dashboards excellent,
zero lock-in (OSS format).

## Implementation Plan

### Phase 1: Server-Side Only (Zero Browser Impact)

Add OpenTelemetry to Azure Functions API project. Send OTLP to Grafana Cloud.
No browser scripts, no cookies, no localStorage changes, no GDPR banner.

**Packages to add to API project:**

- `Microsoft.Azure.Functions.Worker.OpenTelemetry`
- `OpenTelemetry.Extensions.Hosting`
- `OpenTelemetry.Exporter.OpenTelemetryProtocol`
- `OpenTelemetry.Instrumentation.Http`

**What this covers:**

- HTTP trigger traces (request/response, status codes, latency)
- Dependency tracking (HTTP calls to Raindrop API, external services)
- Custom spans for business logic (token exchange, data fetching)
- Function execution metrics
- Error tracking with stack traces

**What this does NOT cover (Phase 1):**

- Browser-side performance (page loads, component render times)
- Client-side exceptions
- User interaction events

### Phase 2: Server-Side Proxy (Future)

Blazor WASM sends performance data to the project's own Azure Functions API.
API forwards to Grafana Cloud. No third-party scripts, no cookies, no consent.

**Pattern:**

```
Browser → /api/Telemetry (own API) → Grafana Cloud (OTLP)
```

**What this adds:**

- Browser performance metrics (page load, LCP, CLS via PerformanceObserver)
- Client-side error reporting
- Component render timing
- Still zero cookies — data flows through own API, not third-party JS SDK

**Why this works without consent:**

First-party API calls to your own backend do not trigger ePrivacy Article 5(3).
No third-party scripts are loaded. No storage is written to the user's device
beyond the existing functional localStorage (Raindrop item caches).

## GDPR/ePrivacy Analysis

### localStorage Usage Audit

**Key inventory re-snapshotted 2026-10-09** against the tree. The `/debug/localstorage`
inspector lists the actual keys of the browsing session at any time.

| Key pattern                                                   | Purpose                                                                                                                                              | GDPR impact                       |
| ------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------- |
| `raindrop_cache_*`, `raindrop_cache_*_metadata`               | Raindrop items cache (Videos, Articles)                                                                                                              | Functional — strictly necessary   |
| `raindrop_auth_code`, `raindrop_access_token`                 | Raindrop sign-in flow (`/redirect` page); written but never read back in this repo                                                                   | Functional — strictly necessary   |
| `__test_localStorage_*`, `__blazored_test_*`, `__json_test_*` | Debug-page diagnostics, written and removed within the same call                                                                                     | Ephemeral first-party diagnostics |
| `img_validation_*` (hashed)                                   | Legacy entries of the removed image validation layer; no writer since 2026-10-09; the whole-origin clear on `/debug/resetcache` is what removes them | Legacy residue                    |

**Verdict: No consent banner required.** All localStorage usage is first-party
functional storage. The ePrivacy Directive "strictly necessary" exemption
(Recital 49, Article 5(3)) covers storage that is required for the service
the user explicitly requested. No tracking, no profiling, no cross-site data.

### Server-Side Telemetry Impact

Server-side OpenTelemetry (Phase 1) sends data from Azure Functions to
Grafana Cloud. No browser-side changes. No cookies set. No consent needed.

### Proxy Pattern Impact (Phase 2)

Phase 2 sends browser performance data through the project's own API endpoint.
This is a first-party API call — same as any other HTTP request the app makes.
No third-party JavaScript. No cookies. No localStorage writes. No consent needed.

## Decision Log

- **Grafana Cloud over Honeycomb:** Grafana's free tier includes dashboards
  and alerting; Honeycomb excels at distributed tracing but has fewer free-tier
  visualization features.
- **Server-side first:** Avoids all GDPR complexity while delivering the
  highest-value telemetry (API performance, errors, dependencies).
- **Proxy over JS SDK:** Avoids the Application Insights JS SDK cookie issue
  entirely. The SDK writes session cookies by default; disabling them breaks
  session correlation. The proxy pattern sidesteps this completely.
- **No Application Insights:** The 5GB/month free tier is too small, and the
  JS SDK's cookie behavior creates GDPR compliance overhead.

## Open Questions

- Grafana Cloud account setup and OTLP endpoint configuration
- OTel Collector: hosted (Grafana Cloud's) vs self-managed (not needed for Phase 1)
- Sampling strategy for free tier (50GB traces — likely need head-based sampling)
- Custom span design for Azure Functions isolated worker
- Phase 2: Which browser metrics to expose via proxy (Web Vitals, custom timing)
