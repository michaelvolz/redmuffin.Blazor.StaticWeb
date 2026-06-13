---
date: 2026-06-12
last_updated: 2026-06-13
tags:
  - dotnet-10
  - csharp-14
  - blazor
  - wasm
  - upgrade
  - file-based-apps
  - fingerprinting
---

# .NET 10 & C# 14 Upgrade Opportunities for redmuffin.Blazor.StaticWeb

## What Belongs in This File

- **Viewpoint**: Developer maintaining a Blazor WebAssembly standalone app targeting
  net9.0, hosted on Azure Static Web Apps, waiting for SWA .NET 10 support.
- **What belongs**: Concrete .NET 10 / C# 14 features applicable to this codebase,
  with migration steps, examples, and pre-upgrade opportunities (things usable
  without changing the target framework).
- **What does NOT belong**: General .NET 10 release notes, features irrelevant to
  Blazor WASM standalone, Azure SWA infrastructure internals, migration guides for
  other project types (Blazor Server, Minimal API).

---

## 0 — Critical Viewpoint (READ FIRST)

This is a **pre-upgrade research guide**. Azure Static Web Apps does not support
.NET 10 for managed APIs as of June 2026 (Oryx #2702, SWA discussion #1719). We
cannot change `TargetFramework` to `net10.0` until SWA ships .NET 10 support, but
**C# 14 features can be used on the .NET 10 SDK** with `LangVersion=preview` even
while targeting `net9.0`. See §3 for what works now.

The features here are ranked by applicability to this repo:

- **IMPACT**: immediate developer productivity or user-facing performance gain
- **EFFORT**: lines of code to change or steps to execute
- **STATUS**: `now` (can use today), `upgrade` (needs target framework change), or
  `unsupported` (needs SWA .NET 10 support)

---

## 1 — Blazor Static Asset Fingerprinting & Compression

**What it is**: In .NET 10, the Blazor script (`blazor.web.js` for Web Apps,
`blazor.webassembly.js` for WASM standalone) is served as a static web asset with
SHA-256 content-based fingerprinting and Brotli/gzip compression applied at build
time. The old mechanism embedded these JS files inside DLLs. .NET 10 decouples them —
cache-busting happens via fingerprinted filenames, and compression cuts the payload
from ~200 KB to under 50 KB.

**For standalone Blazor WASM** (our project), fingerprinting replaces the
`#[.{fingerprint}]` placeholder in `wwwroot/index.html` at publish time:

```html
<!-- Before (marker in index.html) -->
<script src="_framework/blazor.webassembly#[.{fingerprint}].js"></script>

<!-- After (dotnet publish replaces the placeholder) -->
<script src="_framework/blazor.webassembly.958z1vx7fr.js"></script>
```

**What gets fingerprinted**: JavaScript modules only in standalone WASM. The
framework handles `blazor.webassembly.js` automatically. Custom JS files with
`#[.{fingerprint}]` markers need a matching `StaticWebAssetFingerprintPattern`
in the csproj. CSS fingerprinting via placeholders is **not supported** in
standalone WASM — it requires an ASP.NET Core host with `MapStaticAssets`.

**Three required pieces** for fingerprinting to work:

1. `<OverrideHtmlAssetPlaceholders>true</OverrideHtmlAssetPlaceholders>` in csproj
2. `<script type="importmap"></script>` in `<head>` (empty — SDK populates it)
3. `StaticWebAssetFingerprintPattern` with `Expression="#[.{fingerprint}]!"` (trailing `!` is required even though marker omits it)

```xml
<!-- In .csproj -->
<PropertyGroup>
  <OverrideHtmlAssetPlaceholders>true</OverrideHtmlAssetPlaceholders>
</PropertyGroup>
<ItemGroup>
  <StaticWebAssetFingerprintPattern Include="JSModule" Pattern="*.js"
    Expression="#[.{fingerprint}]!" />
</ItemGroup>
```

```html
<!-- In index.html <head> -->
<script type="importmap"></script>
```

**What we get**: SHA-256 integrity hashes in an import map, cache-busting
fingerprinted filenames (`page-load-timing.min.jfc0vlv5xk.js`), and Brotli
compression at build time. The browser caches fingerprinted files permanently
and re-requests only when content changes.

**Verified** (2026-06-12): `blazor.webassembly.js` (60 KB) → `.br` (17 KB,
72% reduction). `app.min.css` (106 KB) → `.br` (15 KB, 86% reduction).
`page-load-timing.min.js` (4 KB) → `.br` (2 KB, 55% reduction).

**IMPACT**: ⭐⭐⭐⭐⭐ (user-facing — faster startup for all visitors)
**EFFORT**: Trivial (three lines of config)
**STATUS**: `applied`

**CSS fingerprinting — custom solution**: Microsoft's built-in fingerprinting
only supports JS modules in standalone WASM. We added a custom MSBuild target
that SHA-256 hashes `app.min.css`, renames it to `app.min.{hash8}.css`, and
rewrites `index.html` references. `.br`/`.gz` companions are also renamed.

The target (`FingerprintCustomCssAssets` in the Blazor csproj) uses
`GetFileHash`, `Move`, and a `RoslynCodeTaskFactory` inline task for the HTML
rewrite. It runs `AfterTargets="Publish"` only on Release builds. No
external scripts, no PowerShell — pure MSBuild, cross-platform.

**Verified** (2026-06-12): `app.min.css` (106 KB) → `app.min.6113EDEC.css` +
`.br` (15 KB, 86% reduction). Both `<link rel="preload">` and `<link>`
references updated in published `index.html`.

### Preloaded Framework Assets

---

## 2 — Preloaded Framework Static Assets

**What it is**: In .NET 10, Blazor framework static assets (.NET runtime, assemblies,
JS) are preloaded by the browser via `<link rel="preload">` headers before the page
finishes parsing. In standalone WASM, assets download with high priority alongside
page rendering instead of after it. Combined with fingerprinting, this means the
browser always fetches fresh assets and does so earlier.

**IMPACT**: ⭐⭐⭐⭐ (faster initial page load, especially on slow connections)
**EFFORT**: Zero (automatic)
**STATUS**: `upgrade` (needs `TargetFramework=net10.0`)

---

## 3 — C# 14 Features (Usable Today)

**Key insight**: C# 14 features are controlled by `LangVersion`, not
`TargetFramework`. Setting `LangVersion=preview` in any `.csproj` that compiles
with the .NET 10 SDK enables C# 14 syntax, even when targeting `net9.0`. This
is how we can adopt C# 14 immediately.

### Extension Members — the headline feature

Replaces and supersedes the C# 3.0 `this`-parameter extension method pattern.
Supports extension properties, extension operators, and static extension members.

```csharp
// Before (C# 3-13): extension methods only, `this` parameter ceremony
public static class StringExtensions
{
    public static bool IsBlank(this string? value) =>
        string.IsNullOrWhiteSpace(value);
}

// After (C# 14): extension block — unified syntax for methods, properties, operators
extension for string?
{
    public bool IsBlank => string.IsNullOrWhiteSpace(this);
    public string? Truncated(int max) =>
        this is { Length: > 0 } s ? s[..Math.Min(s.Length, max)] : this;
}
```

**Where we can use this now**:

- `src/redmuffin.Blazor.StaticWeb.Common/` — extension methods on domain types
- Test helper extensions in `tests/`
- Any existing `static class` full of `this`-parameter methods

**IMPACT**: ⭐⭐⭐ (cleaner code, less ceremony, discoverable properties)
**EFFORT**: Per-file refactor of existing extension method classes
**STATUS**: `now` — set `LangVersion=preview` in `.csproj`, requires .NET 10 SDK

### `field` Keyword — property backing field access

```csharp
// Before: declare backing field, write boilerplate get/set
private string? _name;
public string? Name
{
    get => _name;
    set { _name = value; OnPropertyChanged(); }
}

// After: use `field` keyword — compiler synthesizes the backing field
public string? Name
{
    get => field;
    set { field = value; OnPropertyChanged(); }
}
```

**IMPACT**: ⭐⭐⭐ (eliminates backing field declarations for simple property logic)
**EFFORT**: Replace backing-field patterns in components and models
**STATUS**: `now`

### Null-Conditional Assignment (`?.=`)

```csharp
// Before
if (customer != null)
    customer.Order = newOrder;

// After
customer?.Order = newOrder;

// Also works with compound assignment
counter?.Total += amount;
```

**IMPACT**: ⭐⭐ (niche but cleans up null-guarded setters)
**EFFORT**: Find-and-replace `if (x != null) x.Prop = y` patterns
**STATUS**: `now`

### `nameof` on Unbound Generics

```csharp
// Before: need a dummy type argument
_ = nameof(List<int>); // "List"

// After: unbound generic
_ = nameof(List<>); // "List"
```

**IMPACT**: ⭐ (minor convenience for reflection/generic scenarios)
**STATUS**: `now`

### Modifiers on Lambda Parameters Without Types

```csharp
// Before: must specify type when using modifier
items.ForEach((ref int item) => item++);

// After: type inferred from delegate
items.ForEach((ref item) => item++);
```

**IMPACT**: ⭐⭐ (cleaner callbacks and LINQ chains)
**STATUS**: `now`

### User-Defined Compound Assignment Operators

```csharp
// Custom type can now define += behavior directly
public struct Counter
{
    public int Value { get; private set; }
    public void operator +=(int amount) => Value += amount;
}

// Usage
Counter c = new();
c += 5; // calls operator +=(int)
```

**IMPACT**: ⭐ (domain-specific — useful for accumulator/value-object types)
**STATUS**: `now`

### Partial Constructors & Events

```csharp
// Source generator declares the partial constructor signature
public partial class GeneratedComponent
{
    public partial GeneratedComponent(string name); // implementation elsewhere
}

// Generated code provides the implementation
public partial class GeneratedComponent
{
    public partial GeneratedComponent(string name) => Name = name;
}
```

**IMPACT**: ⭐⭐ (useful for source-generator-heavy code like Mediator.SourceGen)
**STATUS**: `now`

### Implicit Span Conversions

```csharp
// Before: explicit AsSpan() call
ReadOnlySpan<char> span = str.AsSpan();

// After: implicit conversion
ReadOnlySpan<char> span = str;
Span<byte> bytes = buffer[..8]; // implicit from array slice
```

**IMPACT**: ⭐⭐⭐ (less allocation ceremony, directly benefits performance-sensitive
parsing code)
**STATUS**: `now`

---

## 4 — File-Based Apps (`dotnet run app.cs`)

**What it is**: Run a single `.cs` file as a complete .NET application — no
`.csproj`, no solution, no project scaffolding. Use `#:` directives at the top of
the file to declare packages, SDKs, and MSBuild properties. Convert to a full
project with `dotnet project convert app.cs` when outgrowing a single file.

```csharp
#!/usr/bin/env dotnet
#:package CsvHelper@33.0.1
#:property TargetFramework=net10.0

using CsvHelper;
// ... full program here
```

Run with:

```
dotnet run process-data.cs input.csv -- --verbose
```

**What file-based apps replace in our workflow**:

| Old approach                                      | File-based app                                              |
| ------------------------------------------------- | ----------------------------------------------------------- |
| `tools/` projects with full `.csproj` scaffolding | Single `.cs` files with `#:` directives                     |
| Throwaway console apps for data transforms        | `dotnet run transform.cs data.json`                         |
| `scripts/` PowerShell for JSON/YAML processing    | C# scripts with System.Text.Json                            |
| Prototype services before creating a project      | Single-file ASP.NET Core with `#:sdk Microsoft.NET.Sdk.Web` |

**Directives**:

| Directive    | Purpose                    | Example                          |
| ------------ | -------------------------- | -------------------------------- |
| `#:package`  | NuGet package reference    | `#:package CsvHelper@33.0.1`     |
| `#:sdk`      | MSBuild SDK                | `#:sdk Microsoft.NET.Sdk.Web`    |
| `#:property` | MSBuild property           | `#:property LangVersion=preview` |
| `#:project`  | Project reference          | `#:project ../src/MyLib`         |
| `#:include`  | Multi-file (SDK ≥10.0.300) | `#:include ./**/*.cs`            |
| `#:exclude`  | Exclude from include       | `#:exclude ./**/*.Tests.cs`      |

**Conversion to full project**:

```
dotnet project convert app.cs
```

Creates a directory, scaffolds `.csproj`, translates `#:` directives to MSBuild,
copies code to `Program.cs`. Original file unchanged.

**IMPACT**: ⭐⭐⭐⭐⭐ (eliminates boilerplate for one-off tools, scripts, prototypes)
**EFFORT**: None (replace Select-String/sed workflows with C# scripts over time)
**STATUS**: `now` — requires .NET 10 SDK, works independent of target framework

**Limitations**:

- Single-file only in .NET 10 (multi-file support planned for .NET 11)
- `#:include` requires SDK 10.0.300+
- No IntelliSense for `#:` directives (planned)
- Not a replacement for the `tools/` Quality Gates solution (multi-project, needs
  full build infrastructure)

---

## 5 — Blazor UX & Developer Features (needs upgrade)

### QuickGrid Enhancements

- **RowClass parameter**: Apply CSS classes per row based on data
- **Close column options**: Users can dismiss/reorder columns via the column options UI

**IMPACT**: ⭐⭐
**STATUS**: `upgrade`

### Navigation Improvements

- **NavigateTo no longer scrolls to top** for same-page navigations (preserves scroll
  position on internal nav)
- **NavLinkMatch.All ignores query strings and fragments** — `/page` matches
  `/page?tab=2#section`
- **NotFoundPage parameter** on `Router` — declarative `NotFound` component,
  replaces imperative patterns

**IMPACT**: ⭐⭐⭐ (fixes subtle UX papercuts)
**STATUS**: `upgrade`

### JavaScript Interop

- **Construct JS objects with constructor parameters**: `await JSRuntime.InvokeAsync`
  with new overloads for JS class instantiation
- **Access JS properties**: direct property get/set on JS object references

**IMPACT**: ⭐⭐ (simplifies JS interop in components that call third-party JS
libraries)
**STATUS**: `upgrade`

### Persistent State Attribute

```csharp
// Before (.NET 9): imperative state persistence with manual serialization
@page "/profile"
@implements IDisposable

@code {
    private PersistingComponentStateSubscription? _subscription;
    private UserProfile? _profile;

    protected override void OnInitialized()
    {
        _subscription = ApplicationState.RegisterOnPersisting(PersistState);
        if (!ApplicationState.TryTakeFromJson<UserProfile>("profile", out var p))
            _profile = p;
    }

    private Task PersistState()
    {
        ApplicationState.PersistAsJson("profile", _profile);
        return Task.CompletedTask;
    }

    void IDisposable.Dispose() => _subscription?.Dispose();
}

// After (.NET 10): declarative — attribute handles serialization automatically
@page "/profile"

@code {
    [PersistentState("profile")]
    private UserProfile? Profile { get; set; }
}
```

**IMPACT**: ⭐⭐⭐⭐ (eliminates 25+ lines of boilerplate per stateful component)
**STATUS**: `upgrade`

### Reconnection UI Component

Blazor Web App template now includes a `ReconnectModal.razor` component for
displaying reconnection status. For standalone WASM, this is less critical (no
SignalR circuit to reconnect), but the component architecture is reusable.

### Blazor WASM Hot Reload

Enabled by default in Debug configuration. Modify code, see changes live without
restarting the app. Requires `TargetFramework=net10.0`.

### Environment in Standalone WASM

Environment is now set in `.csproj` instead of `appsettings.json`:

```xml
<PropertyGroup>
  <Environment>Development</Environment>
</PropertyGroup>
```

### InputHidden Component

New built-in component for hidden form fields — replaces `<input type="hidden">`
raw HTML in EditForms.

---

## 6 — EF Core 10 Features (API project)

| Feature                | What it does                                  | Use case                  |
| ---------------------- | --------------------------------------------- | ------------------------- |
| Complex Types          | Value objects as owned entity properties      | Address, Money types      |
| LeftJoin / RightJoin   | LINQ operators for SQL outer joins            | Complex query scenarios   |
| Named Query Filters    | Reusable, named filter predicates             | Multi-tenant, soft delete |
| ExecuteUpdate for JSON | Update individual JSON properties in a column | Partial document updates  |

**STATUS**: `upgrade` (API project is net9.0, blocked by SWA .NET 10 support)

---

## 7 — Other .NET 10 Runtime & SDK Improvements

### Post-Quantum Cryptography

`System.Security.Cryptography` adds ML-KEM, ML-DSA, SLH-DSA — FIPS 203/204/205
algorithms. Not immediately relevant to this repo (no crypto impl), but available
for future security features.

### NuGet Audit — Unused Package Removal

`dotnet restore` in .NET 10 can audit and remove unreferenced packages. Reduces
restore time and supply chain surface.

### Unified MSBuild Tasks

MSBuild tasks run on .NET itself (not .NET Framework). Eliminates the
Framework/Core task duplication that existed through .NET 9.

### TryGetValue with Index for OrderedDictionary

New overloads return the index as an `out` parameter — useful for ordered
collections.

### Numeric String Ordering

`StringComparer` now supports `NumericOrdering` — `"2"` and `"02"` compare equal.
Useful for filename sorting.

### Circular Reference Handling in System.Text.Json

`ReferenceHandler` can now be configured via `JsonSourceGenerationOptionsAttribute`
for source-generated serializers. Important for the `LZStringCSharp` integration
(JSON serialization in Blazor).

### WASM Feature Compatibility — Old Safari

.NET 8+ enables two WASM features and a JIT optimization by default.
Safari did not support any of them until version 16.4 (March 2023).
Devices capped at iOS 15 or macOS 12 — including iPhone 7 Plus — cannot
load the runtime with these features enabled.

All three must be disabled for compatibility. The default 2GB WASM
memory ceiling can also cause startup failures on iOS — iOS Safari may
reject WASM module instantiation when the stated maximum exceeds
available per-tab memory (dotnet/runtime#84638).

```xml
<WasmEnableSIMD>false</WasmEnableSIMD>
<WasmEnableExceptionHandling>false</WasmEnableExceptionHandling>
<BlazorWebAssemblyJiterpreter>false</BlazorWebAssemblyJiterpreter>
<EmccMaximumHeapSize>268435456</EmccMaximumHeapSize>
```

Disabling only SIMD and exception handling is not sufficient — the
JITerpreter's `do_jit_call` path does not handle the JS-based exception
fallback correctly (dotnet/runtime#95963). The memory ceiling reduction
is a documented iOS precaution (dotnet/runtime#84638). When the minimum
supported Safari reaches 16.4+, re-enable the three runtime properties
to restore throughput; the memory ceiling should remain reduced for iOS
compatibility.

**IMPACT**: ⭐⭐⭐ (blocks iOS 15 Safari)
**EFFORT**: Trivial (four properties)
**STATUS**: `applied` (all four set in Debug + Release)

---

## 8 — Upgrade Sequence (When SWA Supports .NET 10)

This is the recommended order when Azure SWA ships .NET 10 support:

1. **Install .NET 10 SDK** on all dev machines and CI runners (already done on
   ubuntu-24.04 CI runner — ships 10.0.201)
2. **Set `LangVersion=preview`** in `Directory.Build.props` — adopt C# 14 features
   incrementally (can start now)
3. **Change `TargetFramework` to `net10.0`** in all `.csproj` files (one-line
   change per project)
4. **Update NuGet packages**: `Microsoft.AspNetCore.*`,
   `Microsoft.Extensions.*` to 10.x versions
5. **Remove `@dependabot ignore` for major versions** — Dependabot will propose
   10.x updates
6. **Adopt fingerprint markers** in `wwwroot/index.html`:
   ```html
   <script src="_framework/blazor.webassembly#[.{fingerprint}].js"></script>
   ```
7. **Replace Blazor script reference** with `@Assets["_framework/blazor.webassembly.js"]`
   in `App.razor` (if applicable for standalone WASM)
8. **Add `ResourcePreloader`** component to `<head>` for preloaded assets
9. **Migrate extension method classes** to C# 14 extension blocks
10. **Replace backing-field properties** with `field` keyword where applicable
11. **Adopt `[PersistentState]` attribute** for component state — replaces
    `PersistingComponentStateSubscription` boilerplate
12. **Set environment in `.csproj`** instead of `appsettings.json` (if applicable)
13. **Update `global.json`** to require .NET 10 SDK
14. **Set `api-version: 10.0`** in the deploy workflow's `shibayan/swa-deploy@v1`
    step
15. **Run full CI pipeline** — build, tests, deploy, health check

---

## 9 — Feature Adoption Priorities

Ranked by value-to-effort ratio for our specific codebase:

| #   | Feature                                   |    Can use now?    | Impact     | Effort |
| --- | ----------------------------------------- | :----------------: | ---------- | ------ |
| 1   | Static asset fingerprinting + compression | No (needs upgrade) | ⭐⭐⭐⭐⭐ | Zero   |
| 2   | Preloaded framework assets                | No (needs upgrade) | ⭐⭐⭐⭐   | Zero   |
| 3   | File-based apps (`dotnet run app.cs`)     |      **Yes**       | ⭐⭐⭐⭐⭐ | Low    |
| 4   | `[PersistentState]` attribute             | No (needs upgrade) | ⭐⭐⭐⭐   | Medium |
| 5   | Extension members (C# 14)                 |      **Yes**       | ⭐⭐⭐     | Medium |
| 6   | `field` keyword (C# 14)                   |      **Yes**       | ⭐⭐⭐     | Low    |
| 7   | Implicit span conversions (C# 14)         |      **Yes**       | ⭐⭐⭐     | Low    |
| 8   | Navigation improvements                   | No (needs upgrade) | ⭐⭐⭐     | Low    |
| 9   | Null-conditional assignment (C# 14)       |      **Yes**       | ⭐⭐       | Low    |
| 10  | Partial constructors/events (C# 14)       |      **Yes**       | ⭐⭐       | Medium |
| 11  | Modified lambdas without types (C# 14)    |      **Yes**       | ⭐⭐       | Low    |
| 12  | QuickGrid enhancements                    | No (needs upgrade) | ⭐⭐       | Low    |
| 13  | JS interop constructor overloads          | No (needs upgrade) | ⭐⭐       | Medium |
| 14  | Compound assignment operators (C# 14)     |      **Yes**       | ⭐         | Low    |
| 15  | Post-quantum cryptography                 | No (needs upgrade) | ⭐         | N/A    |
| 16  | EF Core 10 features                       | No (needs upgrade) | ⭐         | Medium |

**Seven features we can use today** with the .NET 10 SDK + `LangVersion=preview` —
no target framework change required.

---

## 10 — Blockers

| Blocker                               | Status                           | Resolution path                                          |
| ------------------------------------- | -------------------------------- | -------------------------------------------------------- |
| Azure SWA managed API .NET 10 support | No timeline                      | Wait for Oryx #2766 release, track SWA discussion #1719  |
| .NET 8 EOL: November 10, 2026         | 5 months away                    | Ample time — .NET 9 EOL same date                        |
| .NET 9 EOL: November 10, 2026         | 5 months away                    | Must upgrade before this date or switch to BYO Functions |
| Dependabot 10.x proposals             | Blocked via `@dependabot ignore` | Remove ignore after SWA .NET 10 support                  |

---

## Related

- `docs/specs/2026-06-12-workflow-optimization-research-spec.md`
- `docs/research/restore-locked-mode-placebo.md`
