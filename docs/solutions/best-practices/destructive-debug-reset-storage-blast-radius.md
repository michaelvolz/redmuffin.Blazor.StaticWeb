---
title: Destructive debug resets name their storage blast radius
date: 2026-10-09
category: best-practices
module: debug-pages
problem_type: best_practice
component: frontend
severity: medium
applies_when:
  - "A page or button clears browser storage for a whole app."
  - "A browser-storage writer gains a new key family whose keys the reset also destroys."
  - "A debug or support page is reachable only by a remembered URL."
tags: [localstorage, debug-pages, cache-reset, destructive-ux, blazor-wasm]
---

# Destructive debug resets name their storage blast radius

## Context

The app's recovery page `/debug/resetcache` clears the whole origin's localStorage with one `ClearAsync()` call. Session work surfaced three gaps around that scope: the page was reachable only by a remembered URL, the global error page had no link to recovery tooling, and the reset copy did not use to say that clearing also destroys Raindrop sign-in tokens, so a "cache" action silently signed the user out. The same pass mapped the writer inventory: `raindrop_cache_*` plus cache metadata from the Raindrop items cache, `raindrop_auth_code` and `raindrop_access_token` from the sign-in flow, and legacy `img_validation_*` logical keys that no current writer produces. Page-load timing writes no localStorage at all. Where pages touch storage directly, Blazored `ILocalStorageService` is the only injected abstraction; the app-owned persistence wrapper is gone.

The corpus already carried a warning in the other direction (session history): the OpenTelemetry plan's localStorage audit lists the auth keys as "Test OAuth flow (unused)" artifacts, while the Auth/Redirect flow writes them and the Cache Reset copy warns they log the user out. That audit table is a refresh candidate for `ce-compound-refresh`, together with its deleted `img:*` / `img_meta:*` rows and the `__browserstorage_index` entry from the deleted persistence wrapper.

The debug pages date back to August 2025 (session history), were moved into the lazy `src/redmuffin.Blazor.StaticWeb.Pages/Debug/` RCL in August 2026, and no prior session ever discussed their scope. The repo `.gitignore` needs explicit exemptions for that folder, because the folder name `Debug` matches the build-output ignore rule that hides any `debug` or `Debug` directory.

## Guidance

Inventory writers before shaping a destructive reset:

1. Enumerate every localStorage key family and its writer in code, and treat a family as real only when a writer writes it today. Orphans stay in the inventory as legacy data.
2. Put every family the clear destroys into the reset's warning copy, and state the user-visible consequence for each. The sign-out consequence is the one that surprises.
3. Separate destructive from read-only debug surfaces in copy and flow: `/debug/localstorage` only inspects, `/debug/resetcache` only clears.

The codebehind is one line, so the copy is where the protection lives. `CacheReset.razor.cs` counts entries with `LengthAsync()` and then calls `_localStorage.ClearAsync()`. The copy's warning list ("Raindrop article and video cache", "Raindrop sign-in tokens — you will need to sign in again after this", "All other cached application data") is the part users and auditors actually read, and the success message repeats the count plus the sign-in consequence.

Link the recovery surface to the failure surface. The global error fragment's Cache Reset button runs `NavigationManager.NavigateTo("/debug", forceLoad: true)`, because a full document reload escapes an error-boundary state and reboots the app; a soft route re-enters the crashed render. When the app can fail in one render tree, a recovery action only helps if it reboots.

## Why This Matters

A user who loses sign-in state they expected to keep judges reliability damage from the surprise, and the friction cost falls on the user. Naming the blast radius in copy turns a hidden hazard into an informed choice. The read-only/destructive split keeps an inspection page safe to click, and one index entry plus an error-fragment link keeps the recovery tooling reachable at the moment the user needs it most.

## When to Apply

- Adding any clear, reset, or eviction action over browser storage.
- Reviewing a new browser-storage writer: check the destructive surfaces' copy in the same pass.
- Auditing hidden pages: a page one drift away from nobody reaching it gets an index entry, `/debug` here, plus a link from the surface users see when the site breaks, the error fragment.

## Examples

From `src/redmuffin.Blazor.StaticWeb.Pages/Debug/CacheReset/CacheReset.razor.cs`:

```csharp
_itemsCleared = await _localStorage.LengthAsync().ConfigureAwait(false);
await _localStorage.ClearAsync().ConfigureAwait(false);
```

The warning list one screen earlier, from `CacheReset.razor`, names the blast radius by family instead of using a generic "clear cache" label.

## Related

- `docs/solutions/architecture-patterns/localstorage-caching-azure-function-calls-blazor-wasm.md` — the raindrop cache layer whose families a reset destroys.
- `docs/solutions/architecture-patterns/render-and-observe-image-url-resolution.md` — the removal that left `img_validation_*` keys legacy-only.
- `docs/solutions/developer-experience/opentelemetry-implementation-plan.md` — the only prior inventory in the corpus, now contradicted by the real writer map.
- `docs/solutions/architecture-patterns/riverbooks-client-end-to-end-modularization.md` — the module-side answer to the storage-wrapper question: the Raindrop module owns its storage port with cache policy in the module.
