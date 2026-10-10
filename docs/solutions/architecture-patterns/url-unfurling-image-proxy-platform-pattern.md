---
title: "URL-unfurling image re-hosting: the platform standard for third-party link previews"
date: 2026-10-10
last_updated: 2026-10-10
category: architecture-patterns
module: image-pipeline
problem_type: architecture_pattern
component: service_object
severity: medium
applies_when:
  - "A page displays cards for third-party links and the card needs the target page's own preview image."
  - "A proposal embeds another site's og:image or cover URL directly in an img element."
  - "A Blazor WASM or other SPA publishes og: or twitter: meta tags from client-side code."
  - "OpenGraph or meta-tag infrastructure is swept out as dead code before its consumer exists."
  - "The term image-proxy appears in a plan or review and its consume-side or publish-side scope must be clear."
tags:
  - image-pipeline
  - image-proxy
  - url-unfurling
  - og-image
  - social-previews
  - media-proxy
  - spa
  - cloudflare
---

## Context

The site shows cards for bookmarked articles, videos, and X posts. The card image for an X post comes from Raindrop's `Cover` field, which holds an X-internal address (`jf.x.com/images/media-preview/<tweetId>`), and the 2026-10-09 production case on `redmuffin.net/videos` showed that address refusing visitors with `HTTP 403` per request while the same URL answered `200` to a non-browser fetch. The full evidence lives in `docs/solutions/architecture-patterns/render-and-observe-image-url-resolution.md` (the anchor predecessor for the consume-side decision and the open image-proxy storage review).

On 2026-10-10 the question "how do LinkedIn, Instagram, Facebook handle this — do they proxy?" produced primary-source answers: every major platform runs the same pipeline, and none of them hotlinks third-party preview images. That evidence changes what an image-proxy on this site should be: a rebuild of the platform-standard unfurler, which the repo already started once and deleted.

The repo history: the OpenGraph infrastructure (a `GetOpenGraphImages` Azure Function that fetched pages and parsed meta tags with AngleSharp, plus services, models, and monitoring) existed in 2025-07 and was deleted on 2025-07-26 because the Articles UI consumed only Raindrop's `Cover` field (`docs/solutions/features/delete-opengraph-infrastructure.md`; PRD `aeb6409d` and commits `777ab827`, `0a0c71c4`, `2fb8e3ab`). The deletion judged consumers by what the UI called, and the fetch layer was the one part whose consumer was planned, never wired. (Grounding note from the 2026-10-10 session: the `GetOpenGraphImages` function files were deleted in `777ab827` together with the models, so the Function part of the sweep landed there; `0a0c71c4` removed the services, dependency references, and processing-state wiring.) Restoring it with the meta-tag-first order is the planned direction, named **image-proxy** in this project (the name the user fixed on 2026-10-10 for the whole pipeline: Function fetch, storage home, cached serving, meta-first order).

Scope split for the term image-proxy, because one doc in the corpus says the proxy direction is closed forever: **consume-side** means proxying or re-hosting third-party _cover assets_ (the render-and-observe doc's decision; "don't poke the bear" holds for X hosts). **Publish-side** means fetching a bookmark's _target page_, reading that page's own `og:`/`twitter:` declarations, and storing the declared image. The planned image-proxy is publish-side. See also `docs/solutions/features/simple-image-validation-system.md`, whose banner closes "the network layer" without a side qualifier; this learning reads that closure as consume-side, consistent with the Image-proxy entry in `CONCEPTS.md`.

## Guidance

**Run the platform-standard unfurling pipeline.** Every chat and social platform that renders link previews uses the same six steps (dev.to pipeline reference, read 2026-10-10):

1. Detect the URL in the content.
2. Fetch the page server-side with a named user agent (`facebookexternalhit`, `LinkedInBot`, `Discordbot`, `Slackbot`, `Twitterbot` are the known ones). Browsers are not involved; JavaScript never executes on the target page.
3. Parse the HTML head in priority order: `og:` tags, then `twitter:` tags, then `<title>`/`<meta name="description">`/favicon, then plain fallbacks.
4. Fetch and check the declared image (existence, dimensions, thumbnail).
5. Cache the result, keyed by URL, with a TTL.
6. Render the card.

**Re-host the image bytes under your own domain.** No platform serves a card image from the target site's host. Discord documents its own Media Proxy (`media.discordapp.net`), which re-hosts embed and attachment media and resizes or transcodes them (Discord community API reference, primary, 2026-10-10). Facebook pre-caches publisher images through its Sharing Debugger, scrapes every URL on a standard 24-hour update cycle, and asks for `og:image:width`/`og:image:height` so its crawler can render without re-downloading (developers.facebook.com Sharing best practices, updated 2022-02-23, read 2026-10-10). Cache TTLs observed in the platform corpus: Facebook about 24 hours, Twitter and LinkedIn about 7 days, Discord minutes, Slack per workspace.

**Never embed another site's og:image URL directly in an `<img>` element.** The direct embed rides that host's edge policy: Cloudflare bot management, access throttling, referrer checks, and link rot all apply, and the failure is per request and invisible in testing. The 2026-10-09 X case is the local proof; the platforms' re-hosting is the industry answer to the same fact.

**On the publish side, an SPA is invisible to crawlers until the head tags come from the server.** Unfurling bots execute no JavaScript, so `og:` tags rendered client-side never reach the platforms. Publish statically rendered head tags; a static host like SWA satisfies this when the tags are written into the served HTML by hand or at build time.

**Guard the unfurler like the platforms do.** Fetch with a timeout of a few seconds, cap redirect hops, allow-list only `http:`/`https:` schemes, and never fetch private or loopback addresses (SSRF). Cache aggressively; most pages change their meta tags rarely.

## Why This Matters

Following the re-host rule buys four things at once: correctness (the card image survives the source host's refusals, deletions, moves, and geo-fencing), privacy (visitors' IP addresses and referrers stay away from third parties), control (transcode to card size once, serve from your own cache), and cost (the fetch happens once per unique image, and the serving home carries the repeat traffic — the free-grant math lives in the anchor doc's quantified paragraph).

Skipping it produces the exact failure this repo hit: renders that work for some visitors and time-slices and `403` for most, with no application-side cure, because the verdict belongs to the third party's edge. Skipping the SSR rule produces silent preview failures that look random to authors: the page works in a browser and shows a bare link on every platform.

The false economy to avoid: deleting meta-tag infrastructure as "unused" while the consuming UI never shipped. The 2025-07-26 deletion removed a working fetch layer one wiring step away from the planned architecture, and rebuilding started from commit archaeology instead of from the code.

## When to Apply

- Any new card feature that needs a third-party page's own preview image: reach for the unfurler pipeline, and keep the re-host rule.
- Any review where an `<img>` binds directly to another site's `og:image` or an aggregator's hotlink address: the pattern says re-host instead, per the open storage-home review recorded in the anchor doc.
- Any SPA that publishes `og:`/`twitter:` tags: verify the tags are in the served HTML, not client-rendered.
- Any cleanup proposal that targets OpenGraph or meta-tag fetching code: check the consume-vs-publish scope and the restoration intent before deleting.
- The Acronym expansion/assumption banner for the platform behaviors: Discord and Facebook claims come from primary pages read 2026-10-10; LinkedIn and Slack TTLs come from two independent secondary sources; recheck TTLs before relying on exact values.

## Examples

**The platform evidence, compacted.** Discord: `https://media.discordapp.net/` re-hosts and transforms media; clients receive proxy URLs only. Facebook: the Facebook Crawler scrapes server-side with a 24-hour update cycle and the debugger can pre-cache an image before the first share. X: `Twitterbot` runs the same fetch, which is why an X link unfurls on other platforms even though X's own asset servers refuse outside hotlinks.

**The local proof of the failure class.** On 2026-10-09, `redmuffin.net/videos` logged ten `403` and one `404` against `jf.x.com/images/media-preview/<tweetId>` URLs while a non-browser fetch of the same URL minutes earlier returned `200` with 661,554 bytes of `image/png` from X's Cloudflare edge. Per `docs/solutions/architecture-patterns/render-and-observe-image-url-resolution.md`, the refusal travels with the request context, so no application code can change the edge's verdict; the re-host is the industry's structural answer.

**The deleted stack that was the same idea.** 2025-07-15/16 built `GetOpenGraphImages` as an Azure Function fetching and parsing pages server-side (AngleSharp); 2025-07-18 continued the work in the Articles page flow (`da17cc29`, `4204c890`); 2025-07-19 added the HTTP HEAD validator on top; 2025-07-26 removed the whole sweep because the UI consumed only `Cover`: PRD `aeb6409d`, then `777ab827` (the `GetOpenGraphImages` Function files, models, and API), `0a0c71c4` (services, dependencies), `2fb8e3ab` (monitoring and tests). The un-shipped consumer was the unfurler described above. A future image-proxy rebuild inherits the boundary from `docs/solutions/architecture-patterns/raindrop-module-io-extraction-client-only.md`: fetch layer in the Api project or another Functions host, DTOs in Common, no frontend-code relocations.

**Planned homes under the $0.00 bar.** After the 2026-10-10 free-tier pricing review, the leading unpaid home stores the converted pictures in visitors' browsers: the Function returns the image with an immutable `Cache-Control` header on a stable hash URL; the unpaid backup commits converted covers as app files under `src/redmuffin.Blazor.StaticWeb/wwwroot/`. The blob-store home (roughly $0.001 per month for 1,000 covers) is the first paid line and is ruled out under the current bar. The review is open; the numbers sit in the anchor doc's quantified paragraph, and no home is decided.
