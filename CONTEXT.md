# redmuffin.Blazor.StaticWeb

A Blazor WASM static web app with an Azure Functions serverless backend, powered
by Raindrop as its data source.

## Language

### Raindrop

**Raindrop**:
An external bookmarking service that provides the app's article and video data.
_Avoid_: bookmark provider, content source

**Raindrop Item**:
A single bookmark or content item fetched from Raindrop — the fundamental data
unit in the application. Stored in the Raindrop Items Cache.
_Avoid_: entry, record, bookmark

**Raindrop Code**:
An OAuth authorization code exchanged with Raindrop for an access token. Used
during authentication to allow the app to fetch the user's Raindrop items.

### Image Placeholder

**Image Placeholder**:
A generated placeholder image displayed before real external images finish
loading. Improves perceived performance and prevents layout shifts.
_Avoid_: loading skeleton, fallback image

**Placeholder Generation**:
The service that creates Image Placeholders. Takes configuration (dimensions,
colors, format) and produces an inline placeholder.

**Image Validation Cache**:
Validates external image URLs and caches the validation result to avoid
redundant HEAD requests on subsequent page loads.
_Avoid_: image checker, URL validator

### Cache

**Raindrop Items Cache**:
A browser-local LRU cache storing fetched Raindrop Items so the app survives
page reloads and avoids redundant API calls. Backed by Browser Storage.
_Avoid_: data store, item repository

**Cache Monitoring**:
Observes cache health, tracks hit/miss statistics, namespace-level usage, and
generates optimization recommendations.

**Browser Storage**:
An abstraction over the browser's localStorage API, used to persist cache
data, debug state, and user preferences client-side.
_Avoid_: local storage (the raw API), Blazored

### Infrastructure

**Azure Functions API**:
The serverless backend tier. Currently handles Raindrop OAuth code exchange
and proxied article/video listing. Uses PrefixedLogger for structured logging
with function-name prefixes. Will host additional non-Raindrop functions as
the app grows.
_Avoid_: backend (ambiguous — this is the API tier, not the data store)

**Warmup**:
Pre-populates caches on application startup to reduce first-load latency and
prevent the user from seeing empty states. Runs before the UI renders.
_Avoid_: preload, cache priming

**Page Load Speed**:
Performance metrics captured at page-level granularity and displayed to the
user in a debug overlay. Uses a configurable threshold for acceptable load
times.

**Production Delay Provider**:
Injects realistic network delays in production mode for authentic loading
UX. In local development (standalone WASM), a zero-delay version is used
so UI iteration is instant. See ADR-0001 for the standalone WASM mode.
_Avoid_: throttle, latency simulator

**SwaLauncher**:
A local end-to-end testing project that runs the full Blazor WASM + Azure
Functions chain end-to-end against real data and real functions. Seldom used
— reserved for final integration verification before deployment.
_Avoid_: E2E runner (it's specifically for the SWA + Functions chain)

### UI Features

**Articles Page**:
A page displaying Raindrop Items filtered to article-type content. Includes
its own article-specific services for enriched presentation.

**Videos Page**:
A page displaying Raindrop Items filtered to video-type content.

## Relationships

- **Raindrop** is the external data source for all **Raindrop Items**
- **Raindrop Items** are cached in the **Raindrop Items Cache** backed by
  **Browser Storage**
- **Image Placeholder** consults the **Image Validation Cache** before
  rendering to avoid redundant validation
- **Articles Page** and **Videos Page** display filtered **Raindrop Items**
- The **Azure Functions API** proxies **Raindrop** requests from the frontend
  (OAuth code exchange, article listing, video listing)
- **Warmup** primes the **Raindrop Items Cache** on app startup
- **SwaLauncher** exercises the full WASM → Functions → Raindrop chain

## Example dialogue

> **Dev:** "When the user opens the Articles Page, do we fetch Raindrop Items
> directly or go through the Azure Functions API?"
> **Domain expert:** "The frontend calls the Azure Functions API, which
> proxies the Raindrop request. That keeps the Raindrop access token
> server-side."
>
> **Dev:** "And the Warmup — does that also go through the Functions API?"
> **Domain expert:** "Yes. Warmup calls the Functions API for the initial
> Raindrop Items fetch, then populates the Raindrop Items Cache so the
> Articles Page renders with data already available."

## Flagged ambiguities

- "Raindrop" was overloaded in the codebase — used for the external service,
  the `IRaindropAPI` interface, the `RaindropAPI` implementation, and the
  `RaindropAPIFactory`. Resolved: CONTEXT.md uses "Raindrop" only for the
  external service. The API interface and factory are implementation details.
- "Cache" appears in multiple contexts (Raindrop Items Cache, Image
  Validation Cache) — these are distinct caches with different purposes.
  CONTEXT.md uses the full compound name for each.
