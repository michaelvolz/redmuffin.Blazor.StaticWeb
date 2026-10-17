---
date: 2026-10-17
version: 2.0.0
last_updated: 2026-10-17
purpose: >
  Governs the re-hosting of third-party Raindrop cover thumbnails in
  Cloudflare R2, with the full requirement history in order, the decided
  design, and the open verification items.
scope:
  - Cover storage home, fetch policy, negative-result policy, and serving path for Raindrop covers
exclude:
  - Publish-side og:image unfurling
  - Raindrop item caching and persistence
tags:
  - r2
  - cloudflare
  - thumbnails
  - azure-functions
  - caching
canonical_for:
  - cover-thumbnail-rehosting
---

# Cover thumbnail re-hosting in Cloudflare R2

This spec governs how Raindrop cover images are fetched once, stored in
Cloudflare R2, and served as immutable cached URLs. It records every
requirement change from the 2026-10-17 session in message order, so no
future session needs the chat history.

## Design Constraints

- **Viewpoint**: agent sessions that resume after compaction, and
  maintainers of the image pipeline.
- **What belongs**: the requirement history in order, the decided design
  with dates, verified pricing and evidence, and open verification items.
- **What does NOT belong**: publish-side unfurling design, Raindrop item
  cache mechanics, and chat narration or process recaps.

## Requirement history, in order

1. **Proxy or cache the thumbnails so users stop seeing authorization
   errors.** X covers on `/videos` refuse visitors with `403` while the
   page's own fetches succeed. Cost constraint: the site runs on the Azure
   Static Web Apps free tier; no added recurring cost. Traffic is
   demo-scale.
2. **Find a free tier for saving thumbnails and blobs.** Resolved to
   Cloudflare R2 Standard; Azure Blob Storage was ruled out (facts below).
3. **Analyze the Cloudflare solution and prepare it.** Added requirements:
   images reached from the origin as rarely as possible, cached at the
   Cloudflare edge, and served with optimal caching. The R2 subscription
   was accepted with a card on file; the bucket, the `r2.dev` public URL,
   and a bucket-scoped account API token exist; the keys are stored
   outside the repo and never enter it.
4. **Full automation replaced the ingest-script design.** New requirements:
   Raindrop links are added dynamically, so covers must appear as soon as
   possible with zero human interaction, forever. A one-time ingest script
   was rejected as a wasted second code path. The origin hosts and
   Cloudflare must never be hammered.
5. **The per-request Function extension was judged and adopted.** The
   existing `RaindropListVideos` / `RaindropListArticles` Functions now
   own the whole flow, so the script, the JSON mapping file, and the
   resolver wiring from the earlier design were all dropped.
6. **The negative-result policy was hardened.** A fixed 24-hour retry
   still hammers a refusing host, so a spaced retry curve that ends in a
   permanent tombstone replaced it (see next section).
7. **A manual retry lever was added.** A helper on the support page
   removes all tombstones at once, so the maintainer can re-trigger
   fetching after a month or so. Removal needs no other effect: with the
   tombstones gone, the next user request retries each cover through the
   normal curve from attempt one.
8. **The helper delegates to a new API Function.** The page sends one
   HTTP call to a new endpoint in the Api project, and the endpoint
   performs the R2 deletion. The page itself holds no R2 access.
9. **Request-time R2 access was eliminated.** The user required that a
   page load never consults R2 to check whether covers exist already.
   The marker moved into Raindrop itself: a processed item carries an
   additive tag, and untagged items are the only ones that touch R2 at
   request time. Original Raindrop values are never modified — the
   marker is an addition only — and the custom response DTO carries the
   cached URL as its own field, with the original `Cover` intact.

## Decided design

- **Storage.** Bucket `redmuffin-blazor-thumbnails`. Object key:
  `covers/<sha256-of-cover-url>.<ext>` — deterministic, so no mapping
  state exists. Uploaded with
  `Cache-Control: public, max-age=31536000, immutable`. Positive and
  negative results never change the key, so browser caches hold forever.
- **Fetch policy.** Cover URLs never change: the hash is deterministic
  from the cover URL, so the response carries the stable R2 URL forever.
  The list response carries each item's tags, and Raindrop holds the
  processing state:
  - A tagged item (`thumbnail-cached`) needs no state lookups at all. The
    Function computes the hash from the unchanged original cover,
    builds `covers/<hash>.<ext>`, and fills the DTO's additive
    `CachedThumbUrl` field; the item's `Cover` passes through
    untouched in both Raindrop and the response. Zero R2 access, zero
    origin access, on every instance including fresh, recycled, and
    scaled-out ones.
  - An untagged item gets one targeted marker check for its own hash
    only, never a bucket scan: an active tombstone answers the
    placeholder URL; a due tombstone retries through the curve; without
    a tombstone an exists-check answers hit or fetch. A miss triggers
    one origin fetch, bounded parallel (4–6 concurrent) under a
    per-hash in-process lock, then store and serve the R2 URL.
  - On success the upload lands first, then the tag write. A failed
    tag write leaves the item untagged, and the next request self-heals
    by finding the object and retagging — ordering never produces a
    lying tag.
  - A brand-new cover is therefore fetched once per unique cover, ever.
  - Accepted limitation: changing an item's cover URL after tagging
    leaves a lying tag whose hash has no object, so the placeholder
    shows and the maintainer removes the tag manually in Raindrop.
    Automatic detection would require the per-request existence checks
    this policy exists to avoid.
- **Stateless reality.** Azure Functions are stateless: instances
  recycle and scale out without warning. With the Raindrop-tag marker,
  the processing state travels with the items themselves, so no
  rehydration step exists and no request depends on memory. R2 keeps
  only the bytes and the tombstone markers with the retry clocks.
- **Negative policy with a stop condition.** Verdict-class failures
  (`403`, `404`, non-image content) retry once after one day, then write
  a tombstone. Transient-class failures (timeouts, `5xx`) retry after
  1 h, 6 h, 1 d, and 3 d, then write a tombstone. A tombstone is a
  permanent marker: the origin receives zero requests for that cover
  afterwards, and the item serves the Image Placeholder. Worst-case
  origin traffic for a permanently refusing cover is four requests in
  roughly 31 hours, then silence. Deleting the tombstone in the
  dashboard re-opens the key from attempt one. Tombstones live under
  their own key prefix (for example `negative/<hash>.json`), so the
  full set is listable and deletable.
- **Tombstone reset endpoint.** A new Function HTTP endpoint deletes
  every object under the tombstone prefix and reports the deleted
  count. Tombstoned items were never tagged, so no Raindrop cleanup
  happens. A button on a Debug-family sub-page calls the endpoint;
  the page holds no R2 access and needs no further interaction — the
  next user request retries the covers through the normal curve from
  attempt one.
- **Serving path.** The `r2.dev` public URL serves today. The end state
  is a custom domain `covers.redmuffin.net` after the DNS move: registrar
  stays Porkbun, DNS hosting moves to Cloudflare, every existing record
  is copied before the nameserver switch, and the apex keeps pointing at
  the Azure SWA.
- **Rejected alternatives, with reasons.** A one-time ingest script
  (separate path, useless after the first run). A committed `wwwroot/`
  cover set (needs a human step per new cover). Azure Blob (no free
  tier on the existing subscription). A pass-through proxy that streams
  origin bytes (concentrates traffic, spends Function bandwidth). A
  Cloudflare Grok plugin (setup convenience only; the runtime path never
  calls Cloudflare tooling). Writing resolution state back into
  Raindrop items was rejected in its full form (overwriting `cover`
  with the cached URL destroys the original address the hash derives
  from, mutates the user's bookmarks display, and couples a future
  `r2.dev`-to-custom-domain migration to a Raindrop rewrite). Under the
  zero-request-time-R2 requirement it was adopted in additive-only tag
  form (clarified 2026-10-17: original Raindrop values are never
  touched — the marker is an addition only): the `thumbnail-cached` tag
  marks processed items while `cover` keeps the original URL on both
  the Raindrop item and the response DTO, so the hash stays derivable
  and retries stay possible. The response DTO gains its own
  `CachedThumbUrl` field and passes `Cover` through unchanged.
  Tombstones stay in R2, so tagged and untagged items share one failure
  ledger and the reset endpoint needs no Raindrop access. The marker
  holds a boolean, and the hash itself is stored nowhere: deriving it
  from the preserved `cover` per request is pure CPU and cannot drift.
  Storing the hash was examined and rejected on field-semantics: a hash
  is data, and Raindrop has no custom data field — the only writable
  data slot is `note`, whose machine line user note edits would clobber
  and whose re-appending would fight the user. A boolean state is a
  label, which fits the tag field exactly. (`PUT /raindrops/{collection}`
  even appends tags atomically in bulk, and the existing `RaindropItem`
  DTO already maps `tags` out of the list responses the pipeline
  consumes.)

## Verified facts, 2026-10-17

- R2 free tier: 10 GB storage, 1M Class A writes, 10M Class B reads per
  month, zero egress fees, no time limit. A payment method on file is
  required to enable R2. Projected use: about 30 covers at 20 MB
  unconverted — orders of magnitude inside the free tier.
- Azure Blob Storage: no free tier on an existing pay-as-you-go
  subscription; free grants cover only brand-new free accounts.
- Origin-refusal evidence, 2026-10-09 production: one
  `jf.x.com/images/media-preview/<tweetId>` URL returned `200` (661 KB
  PNG) to a non-browser fetch and `403` to a visitor's `<img>` request
  minutes apart. The refusal travels with the request context.
- Upload tooling: `bunx wrangler r2 object put` supports `--cache-control`
  and works with an R2 API token; bun 1.4.3 is installed.
- The S3 endpoint shape is
  `https://<account-id>.r2.cloudflarestorage.com`. Access keys live in
  environment variables on the maintainer machine and are never
  committed.

## Open verification items

1. **The one-fetch experiment.** Fetch one X cover from the Azure
   Functions host and record the status. `200` green-lights the design; a
   `403` moves the fetch step to a Cloudflare Worker (Cloudflare IPs),
   with the storage and serving design unchanged.
   Implementation status: the probe endpoint exists
   (`src/redmuffin.Blazor.StaticWeb.Api/Functions/CoverFetchProbeFunction.cs`),
   locked to absolute-https URLs on `jf.x.com` only, with a 5 MB cap and a 20-second bound covering fetch and body read.
   The site-side entry point is `/debug/fetchprobe`: the call goes through
   the standard SWA `/api` proxy exactly like every other Function call
   (`AuthorizationLevel.Function`, host key injected by the proxy, no key in
   any URL — `docs/solutions/conventions/api-calls-reuse-configured-swa-auth-path.md`).
   The frontend-only dev host answers `/api/*` with an empty `200`, so the
   experiment produces its verdict only against a deployed build. After the
   verdict is recorded here, the probe files are removed.
2. **`r2.dev` is rate-limited and marked non-production**; edge caching
   is unavailable on it. The custom-domain move is the production end
   state.
3. **Whether SWA managed-Function responses count against the SWA
   bandwidth quota** — check in the Azure portal during implementation.
   This design keeps Function responses small by serving bytes from R2.
4. **WebP conversion is deferred.** V1 stores original bytes; conversion
   that shrinks fresh-view traffic roughly tenfold comes later.

## Related

- `docs/solutions/architecture-patterns/render-and-observe-image-url-resolution.md` - the no-proxy resolve-and-render decision this design extends to a fetch-once re-host.
- `docs/solutions/architecture-patterns/url-unfurling-image-proxy-platform-pattern.md` - the platform-standard re-hosting pattern and the earlier under-$0 review that preceded this decision.
- `docs/solutions/architecture-patterns/raindrop-module-io-extraction-client-only.md` - the Api-project deployment boundary the extended Functions respect.
