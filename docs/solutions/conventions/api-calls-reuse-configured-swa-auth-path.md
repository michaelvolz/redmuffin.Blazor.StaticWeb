---
title: API calls reuse the configured SWA auth path
date: 2026-10-17
category: conventions
module: azure-functions
problem_type: best_practice
component: azure_functions
severity: high
applies_when:
  - "A new frontend call, diagnostic button, or support tool needs to reach a Function endpoint."
  - "A Function trigger seems hard to call and a key-in-URL or auth-level change looks convenient."
  - "A temporary probe or demo endpoint is being wired into the site."
tags:
  [
    azure-swa,
    azure-functions,
    security,
    api-consumption,
    debug-pages,
    blazor-wasm,
  ]
---

# API calls reuse the configured SWA auth path

## Context

The 2026-10-17 fetch-probe work needed one more Function endpoint
(`CoverFetchProbe`) reachable from the site. The first suggested activation
flow told the maintainer to copy the function key out of the Azure portal and
append it as a `?code=` query parameter. That suggestion violated the site's
working security design, and the correction produced this rule.

Every HttpTrigger in the Api project carries `AuthorizationLevel.Function`.
The calls still work with no key in the client because of how Azure Static
Web Apps ships managed Functions: the SWA proxy for `/api/*` is the only
route in, and SWA injects its own host key server-side when it forwards the
request. The client sends a plain relative `/api/<FunctionName>` call and
nothing else. The two existing entry patterns both ride it:
`RaindropAPI` calls `/api/RaindropListVideos` and `/api/RaindropListArticles`
from module IO, and the Auth flow posts to `/api/ExchangeRaindropCode`.

A side observation from the same session explains why a missing API can look
deceptive during frontend-only local verification: the dev host answers
`/api/*` with `200` and an empty body when no Functions app runs behind it.
A `200` therefore proves nothing about the endpoint; the payload does.

## Guidance

1. Call every Function from the frontend as a plain relative `/api/<FunctionName>`
   request. No key, no header, no signed URL.
2. Keep `AuthorizationLevel.Function` on triggers. Never lower a level so a
   call gets easier; the SWA proxy injection is the mechanism, not the level.
3. Never copy a function key into a URL, a query string, or client code. That
   moves a server-side secret into a browsable, loggable, shareable location.
4. Wire diagnostic and support surfaces through the same path — a Debug-family
   page with a button that calls the endpoint like any other feature call.
5. Treat a local `200` on `/api/*` as "no Functions app behind this host" when
   the body is empty; verify the payload, not the status code.

## Why This Matters

The configured path keeps authorization server-side and invisible to clients.
A key pasted into a URL is a permanent leak in browser history, server logs,
and screen shares, and a lower authorization level silently widens the public
surface. Reusing the same mechanism keeps every endpoint auditable under one
rule, so a future reader can trust that any `/api/...` call in the frontend
is already authenticated the right way.

## Related

- `docs/specs/2026-10-17-cover-thumbnail-rehosting-spec.md` — the one-fetch
  experiment whose activation produced this rule's first application.
- AGENTS.md, "Azure Functions deployment boundary" — the accumulated repo rule
  this solution backfills with reasoning.
