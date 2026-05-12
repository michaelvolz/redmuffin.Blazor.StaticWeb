---
title: Trimming Warnings Documentation
date: 2026-05-12
---

## Current Status

**0 trimming warnings.** All known IL2026 warnings resolved via source-generated
`RaindropJsonSerializerContext` (see `docs/research/blazor-wasm-trimming-gotchas.md` §3).

## Historical (Resolved)

### IL2026 — CreatorReferenceConverter (resolved 2026-05-12)

`CreatorReferenceConverter.cs` used generic `JsonSerializer.Deserialize<T>()`
and `JsonSerializer.Serialize<T>()` overloads which carry
`[RequiresUnreferencedCode]`. Fixed by adding `CreatorReference` to the
source-generated `RaindropJsonSerializerContext` and switching to
`JsonTypeInfo`-based overloads.

### IL2111 — LayoutView.Layout.set Warnings (2 instances)

**Location**: `App_razor.g.cs` (auto-generated file)
**Status**: Expected, safe to keep as warnings. These are framework-internal
reflection accesses preserved by the framework's own trimming configuration.
No user code involved.
