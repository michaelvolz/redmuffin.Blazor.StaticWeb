---
date: 2026-05-14
title: Raindrop Item Presentation Helper — Feathers Extraction Pattern
tags:
  [extraction, feathers, duplication, raindrop, blazor, characterization-test]
description: Extraction of DisplayTitle and DisplayExcerpt from duplicated
  Videos and Articles code-behinds into a shared static helper class following
  the Feathers Seam Pattern with characterization tests.
module: raindrop
problem_type: duplication
---

# Raindrop Item Presentation Helper Extraction

## Problem

`Videos.razor.cs` and `Articles.razor.cs` contained near-identical `DisplayTitle`
and `DisplayExcerpt` methods — pure static functions duplicated across two
components. The Dupes gate flagged the 1.00 similarity between the code-behinds.

## Feathers Seam Pattern Applied

Following `rm-guide-cleanup` §2.1:

1. **Characterize**: Wrote 9 characterization tests capturing exact input→output
   behavior before touching production code
2. **Extract**: Created `RaindropItemPresentationHelper` static class with both
   methods as `public static`
3. **Test**: All 9 characterization tests pass against extracted code
4. **Verify**: Updated both components to use `@using static` import, verified
   build 0/0, 334/334 tests pass

## Extraction Details

| Method                                | Behavior                                     | Tests                                                             |
| ------------------------------------- | -------------------------------------------- | ----------------------------------------------------------------- |
| `DisplayTitle(PrunedRaindropItem?)`   | Returns title with 80-char truncation suffix | TitlePresent, TitleNull, TitleEmpty, TitleLongTruncated, ItemNull |
| `DisplayExcerpt(PrunedRaindropItem?)` | Returns excerpt with 200-char truncation     | ExcerptPresent, ExcerptNull, ExcerptEmpty, ItemNull               |

Both methods accept nullable items and handle all null guard scenarios.

## Component Integration

Both `.razor.cs` files and `.razor` files reference the static class:

```razor
@using static redmuffin.Blazor.StaticWeb.Features.Raindrop.Presentation.RaindropItemPresentationHelper
```

## What Was NOT Extracted

The larger orchestration methods (`LoadCachedDataAsync`, `FetchItemsAsync`,
`HandleRefreshClickAsync`) were evaluated against the extraction gates and
**rejected**:

- Only 2 occurrences — Metz Rule of Three not met
- Differ in state management (`_isLoading`, image cache clearing)
- Differ in error messages and log delegates
- Forcing a shared service would require 6+ parameters per method → shallow module

The one real seam already extracted: `RaindropBackgroundRefreshHelper.TryFetchFreshDataAsync`
(prior session).

## Verification

- Build: 0 errors, 0 warnings
- Tests: 334/334 pass
- Dupes gate: Videos/Articles structural similarity unchanged (1.00) — known
  intentional pattern, not actionable

## Related

- `docs/solutions/workflow-issues/multi-test-project-coverage-merge-2026-05-13.md` — CoberturaMerger for multi-project coverage
- `docs/research/mutation-testing-decision-tree-2026-05-14.md` — Kill rate protocol
