---
date: 2026-06-14
title: Eliminating Inline C# from Blazor Razor Files — Patterns and Limits
tags:
  [blazor, razor, architecture, markup-purity, viewmodel, mvu, renderfragment]
description: Research into patterns that push C# logic out of .razor files — ViewModel pre-computation, discriminated unions, RenderFragment composition, MVU, and state machines. Includes the litmus test for what belongs in markup.
module: architecture
problem_type: design-pattern
---

## The Litmus Test

From the authority synthesis (Uncle Bob, Fowler, Beck, Feathers, Ousterhout, Metz):

> If removing a line from markup would change **what decision is made**
> (not just how it's rendered), that line doesn't belong in `.razor`.

**Allowed** — projects state, doesn't decide:

- `@PropertyName`, `@_field` — value references
- `@onclick="MethodName"` — event bindings (method name only)
- `@Body`, `@ChildContent` — render fragment references
- `<Child Param="_vm.Value" />` — passing state to children
- `@ref`, `@key`, `@attributes` — directive attributes
- `@page`, `@using`, `@namespace`, `@implements`, `@inherits` — directives

**Forbidden** — computes or decides:

- `@if`, `@foreach`, `@switch`, `@for`, `@while` — control flow
- `@code { }` — code blocks
- `@(condition ? "a" : "b")` — inline ternary decisions
- `@((bytes / 1024.0).ToString("F1"))` — inline computation
- `@_diagnostics.IsLocalStorageAvailable ? "success" : "alert"` — CSS class decisions
- `@onclick="() => DoSomething(x)"` — inline lambdas

---

## Pattern 1: Computed Properties (Baseline)

Move inline expressions to computed properties in `.razor.cs`.

```razor
@* BEFORE *@
@(_diagnostics.IsLocalStorageAvailable ? "✅ Available" : "❌ Not Available")

@* AFTER *@
@LocalStorageStatusText
```

```csharp
// Code-behind
private string LocalStorageStatusText => _diagnostics?.IsLocalStorageAvailable switch
{
    true => "✅ Available",
    false => "❌ Not Available",
    _ => "Unknown"
};
```

CSS class computation:

```razor
@* BEFORE *@
<div class="callout @(_diagnostics.IsLocalStorageAvailable ? "success" : "alert")">

@* AFTER *@
<div class="callout @AvailabilityCssClass">
```

Complex formatting:

```razor
@* BEFORE *@
@((_diagnostics.StorageInfo.QuotaBytes / (1024.0 * 1024.0)).ToString("F1")) MB

@* AFTER *@
@QuotaFormatted
```

---

## Pattern 2: `<When>` / `<ForEach>` Components (Control Flow Elimination)

Replace `@if` with a `<When Condition="boolProp">` component, `@foreach` with `<ForEach Items="list" Context="item">`.

The `<When>` component is infrastructure — permitted to contain `@if` internally:

```razor
@* When.razor *@
@if (Condition)
{
    @ChildContent
}
```

```csharp
// When.razor.cs
public partial class When
{
    [Parameter, EditorRequired] public bool Condition { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
```

Infrastructure components encapsulate control flow once. Every page-level component uses them declaratively.

---

## Pattern 3: ViewModel — Pre-Computed Display State

Fowler's Presentation Model: a plain C# record pre-computes every value the `.razor` needs. The `.razor` never computes — it projects.

```csharp
public sealed record ApiHealthViewModel(
    string StatusText,
    string StatusIconClass,
    string? EndpointUrl,
    string? ResponseTimeFormatted,
    string? ErrorDetail,
    bool HasError,
    bool IsLoading,
    bool HasData)
{
    public static readonly ApiHealthViewModel Empty = new(
        "No data", "icon-gray", null, null, null, false, false, false);

    public static ApiHealthViewModel Loading() => new(
        "Checking...", "icon-spinner", null, null, null, false, true, false);

    public static ApiHealthViewModel Success(string endpoint, double ms) => new(
        "Healthy", "icon-green", endpoint, $"{ms:F1} ms", null, false, false, true);

    public static ApiHealthViewModel Error(string endpoint, string detail) => new(
        "Unreachable", "icon-red", endpoint, null, detail, true, false, false);
}
```

```razor
@* .razor — only property references *@
<h1>@_vm.StatusText</h1>
<When Condition="_vm.HasError">
    <div class="callout alert"><p>@_vm.ErrorDetail</p></div>
</When>
<When Condition="_vm.HasData">
    <p>Endpoint: @_vm.EndpointUrl</p>
    <p>Response: @_vm.ResponseTimeFormatted</p>
</When>
```

The ViewModel is unit-testable without bUnit. Each factory method produces a complete, consistent state snapshot.

---

## Pattern 4: `<StateView<T>>` — Discriminated Union Page State

For data-fetching components, model page state as a discriminated union (`Loading | Errored | Data<T>`) and render via a generic `<StateView<T>>` component.

```csharp
public abstract record PageState<T>
{
    public sealed record Loading : PageState<T>;
    public sealed record Errored(string Message) : PageState<T>;
    public sealed record Data(T Value) : PageState<T>;
}
```

```razor
@* StateView.razor — one per project, handles all pages *@
@typeparam T

@switch (State)
{
    case PageState<T>.Loading:
        @LoadingTemplate
        break;
    case PageState<T>.Errored(var msg):
        @ErrorTemplate(msg)
        break;
    case PageState<T>.Data(var data):
        @DataTemplate(data)
        break;
}

@code {
    [Parameter, EditorRequired] public PageState<T> State { get; set; } = default!;
    [Parameter] public RenderFragment LoadingTemplate { get; set; } = @<p>Loading...</p>;
    [Parameter] public RenderFragment<string> ErrorTemplate { get; set; } = default!;
    [Parameter, EditorRequired] public RenderFragment<T> DataTemplate { get; set; } = default!;
}
```

Every page becomes declarative:

```razor
@* UserList.razor — zero decisions, pure composition *@
<StateView State="State">
    <LoadingTemplate><Spinner /></LoadingTemplate>
    <ErrorTemplate><ErrorAlert Message="@context" OnRetry="LoadAsync" /></ErrorTemplate>
    <DataTemplate><UserTable Users="@context" /></DataTemplate>
</StateView>
```

The `PageState<T>.Match()` extension method provides compile-time exhaustiveness for code-behind:

```csharp
public static TResult Match<T, TResult>(
    this PageState<T> state,
    Func<TResult> onLoading,
    Func<string, TResult> onErrored,
    Func<T, TResult> onData) => state switch
    {
        PageState<T>.Loading => onLoading(),
        PageState<T>.Errored(var msg) => onErrored(msg),
        PageState<T>.Data(var data) => onData(data),
        _ => throw new UnreachableException()
    };
```

---

## Pattern 5: RenderFragment from Code-Behind (Advanced)

For complex state-driven rendering, define `RenderFragment` delegates in `.razor.cs`. The `.razor` is a single line: `@BuildView()`.

```csharp
// Code-behind
private RenderFragment BuildView() => State switch
{
    PageState<T>.Loading => LoadingView,
    PageState<T>.Errored(var msg) => ErrorView(msg),
    PageState<T>.Data(var items) => RenderItems(items),
    _ => EmptyView
};

private RenderFragment LoadingView => @<div class="spinner">Loading...</div>;
private RenderFragment ErrorView(string msg) => @<p class="error">@msg</p>;

private RenderFragment RenderItems(IReadOnlyList<T> items) => __builder =>
{
    foreach (var item in items)
    {
        <ItemCard Model="item" />
    }
};
```

Use this only when the conditional structure is small enough that `@<...>` syntax in C# remains readable (under ~5 branches). For larger structures, prefer the `<StateView>` component or decomposition into separate components.

---

## Pattern 6: Static RenderFragment Factory

For shared UI snippets (empty states, spinners, error banners) that are stateless:

```csharp
// SharedFragments.razor.cs — one file, referenced project-wide
public partial class SharedFragments : ComponentBase
{
    public static RenderFragment Spinner => @<div class="spinner"></div>;

    public static RenderFragment<string> ErrorBanner => msg => @<div class="banner banner-error">@msg</div>;

    public static RenderFragment EmptyState(string message) => @<div class="empty-state"><p>@message</p></div>;
}
```

Consumption from any component:

```razor
@SharedFragments.ErrorBanner("Failed to load")
@SharedFragments.EmptyState("No results")
```

Zero per-component overhead — static `RenderFragment` delegates render without component lifecycle, parameter setting, or `StateHasChanged`.

---

## Pattern 7: State Machine Without Libraries

A single immutable state record, discriminated by type, with a switch expression in code-behind that computes the next state. The `.razor` binds to pre-computed properties only.

```csharp
// Code-behind
public abstract record TrafficLightState
{
    public sealed record Stop : TrafficLightState;
    public sealed record GetReadyToGo : TrafficLightState;
    public sealed record Go : TrafficLightState;
    public sealed record GetReadyToStop : TrafficLightState;
}

public partial class TrafficLight : ComponentBase
{
    protected TrafficLightState State { get; private set; } = new TrafficLightState.Stop();

    protected void Toggle() => State = State switch
    {
        TrafficLightState.Stop => new TrafficLightState.GetReadyToGo(),
        TrafficLightState.GetReadyToGo => new TrafficLightState.Go(),
        TrafficLightState.Go => new TrafficLightState.GetReadyToStop(),
        TrafficLightState.GetReadyToStop => new TrafficLightState.Stop(),
        _ => throw new ArgumentOutOfRangeException()
    };

    protected string CssClass => State switch
    {
        TrafficLightState.Stop => "red",
        TrafficLightState.GetReadyToGo => "red-yellow",
        TrafficLightState.Go => "green",
        TrafficLightState.GetReadyToStop => "yellow",
        _ => throw new ArgumentOutOfRangeException()
    };
}
```

```razor
@* .razor — zero decisions *@
<div class="traffic-light @CssClass">
    <button @onclick="Toggle">Next</button>
</div>
```

---

## Pattern Decision Matrix

| What you have                                                   | Use                                                           |
| --------------------------------------------------------------- | ------------------------------------------------------------- |
| `@(condition ? "a" : "b")`                                      | Computed property                                             |
| `@((x / y).ToString("F1"))`                                     | Computed property                                             |
| `@if (cond) { ... }`                                            | `<When Condition="boolProp">`                                 |
| `@foreach (var x in items)`                                     | `<ForEach Items="items" Context="x">`                         |
| `@if (x is null) / @else if (error) / @else` loading/error/data | `<StateView<T> State="state">`                                |
| Inline CSS class computation                                    | Computed property in code-behind                              |
| Complex multi-branch rendering                                  | `BuildView()` RenderFragment in code-behind                   |
| Shared stateless UI snippets                                    | Static `RenderFragment` factory                               |
| State machine with transitions                                  | Discriminated union record + switch expression in code-behind |

## When NOT to Push Further

The `RenderTreeBuilder` (raw `builder.OpenElement`/`CloseElement`) is a compiler target, not a human authoring surface. Never use it for components where the equivalent `.razor` markup is clear. The inflection point: if the C# rendering code is harder to read than the inline C# it replaces, you've gone too far. The `.razor` file should remain readable as a template — it describes structure, not logic.
