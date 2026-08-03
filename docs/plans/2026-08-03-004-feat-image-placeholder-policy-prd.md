---
title: feat/image-placeholder-policy
date: 2026-08-03
status: done
---

## Problem

V1 promoted `IImageUrlResolver` and `IImagePlaceholderService` into Common so
page RCLs do not reference the host, but image implementations still live as
ad-hoc host Core registrations with an internal-only `IImageValidator`. V2
must settle Core vs module ownership and finish a single multi-feature image
policy beyond that abstraction enabler.

## Solution

Keep image **implementations in host Core** and **page-facing contracts in
Common**. Own the surface with one Core DI extension, internal collaborators,
mirrored host tests, and no `Modules/Image*` triad. Pages keep injecting only
Common image interfaces plus `IMediator` for Raindrop.

## Success Metrics

- Articles and Videos page projects reference image types only from
  `Common/ImagePlaceholder` (no `Core.ImagePlaceholder` usings).
- Host registers image services only via a single Core extension (no raw
  per-type `AddScoped` lines in `Program.cs` for this surface).
- `dotnet build` clean; host Core image test suite and Articles/Videos page
  suites green; each gated step green before the next.

## Key Technical Decisions

- **Core home, not Modules triad.** Image validation/placeholder is
  multi-page UI infrastructure (HTTP HEAD, localStorage cache, SVG), not a
  Mediator domain module. **Why:** roadmap V2 “Core vs module”; no use-case
  handlers; eager host DI matches all Raindrop page consumers. **Rejected:**
  `Modules/ImagePlaceholder` triad + lazy co-load — extra gate surface for no
  domain boundary.
- **Common stays page API.** `IImageUrlResolver` / `IImagePlaceholderService`
  remain in Common; `IImageValidator` and generation types stay Core-internal
  (or Core-only abstractions). **Why:** pages already mock Common only.
- **No Mediator for image IO in V2.** Pages keep UI helper injection for URL
  cache/populate. **Why:** V1 leave-behind and Raindrop PRD treated image as
  page/UI helper; V2 is ownership policy, not another Mediator vertical.
- **Gated steps, not one mega-PR.** Inventory/DI ownership → make collaborators
  internal + single extension → prove suites → update roadmap §6.4 V2 done.

## Modules & Seams

| Module | Path | Change | Test surface |
| ------ | ---- | ------ | ------------ |
| Common contracts | `src/redmuffin.Blazor.StaticWeb.Common/ImagePlaceholder/` | Keep page-facing interfaces; no Raindrop page usings of Core | Existing page mocks |
| Core image | `src/redmuffin.Blazor.StaticWeb/Core/ImagePlaceholder/` | Single `AddImagePlaceholderServices` (name final at implement); tighten visibility; keep impls here | `tests/.../Tests/Core/*Image*` / `*Placeholder*` |
| Host composition | `src/redmuffin.Blazor.StaticWeb/Program.cs` | Call Core extension only for this surface | Smoke via suite |
| Articles / Videos pages | `src/.../Pages/Articles/`, `Pages/Videos/` | No Core image usings; still inject Common interfaces | `tests/.../Pages/Articles.Tests`, `Videos.Tests` |
| Roadmap | `docs/specs/2026-08-03-riverbooks-modularization-roadmap-spec.md` | Mark §6.4 V2 done when AC pass; next V3 | Doc only |

## Testing Strategy

Prove at the Core service seam (validator, resolver, placeholder generation)
with existing host unit tests; prove pages still DI-mock Common interfaces
only. Do not add browser E2E for HEAD validation unless a regression needs
it. Do not re-test Raindrop Mediator in this vertical except as a green
gate if build pulls them in.

## Non-Functional Requirements

- No new eager product DLL for image (no Modules triad); host boot registration
  cost stays the same class as today.
- Background validation remains cache-first on first paint (existing
  `PopulateImageUrlCacheAsync` contract).
- Api Functions project untouched.

## Out of Scope

- Moving image into `Modules/` or new Contracts projects
- Mediator/Result wrapping of image validation
- Lazy-loading image implementation assemblies
- Debug / samples / Home-Auth verticals (V3+)
- Changing Raindrop load/refresh Mediator contracts
- Api project or deploy boundary changes
- SCSS/`_article-image-display` restyle

## Assumptions

- V1 image interface promotion in Common is already shipped and green.
- Articles/Videos only need `IImageUrlResolver` / `IImagePlaceholderService`
  from Common for production DI.
- LocalStorage/browser storage used by `ImageValidator` may stay concrete in
  Core for V2 (no new storage port) unless implementation hits a hard
  host-boundary conflict — then stop and re-plan.

## Acceptance Criteria

- [x] Decision recorded: image policy stays Core + Common (not Modules triad).
- [x] `Program.cs` registers image services only through one Core DI extension.
- [x] No Articles/Videos production code references
      `Core.ImagePlaceholder` namespaces.
- [x] Core collaborators that pages must not use are not part of the public
      page-facing API (validator/generation stay Core-owned).
- [x] `dotnet build` clean; host image-related tests green; Articles.Tests and
      Videos.Tests green.
- [x] Roadmap §6.4 V2 marked done; V3–V5 page homes already shipped as Pages
      RCLs (no further page-extract backlog on that table).
- [x] Each implementation big step verified green before the next starts.
