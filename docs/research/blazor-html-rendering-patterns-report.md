# Blazor HTML Rendering Patterns: State-of-the-Art Report (2026)

**Date**: 2026-05-24
**Scope**: ASP.NET Core 9/10 Blazor (latest at time of research)
**Sources**: learn.microsoft.com (primary), github.com/dotnet/aspnetcore, community blogs (Jon Hilton, Chris Sainty, Andrew Lock)

---

## 1. Blazor's HTML Output Model

### How `.razor` Becomes DOM HTML

At **compile time**, the `.razor` markup is transformed into procedural C# logic (a `BuildRenderTree` method). At **runtime**, this C# builds a _render tree_ — an in-memory representation describing elements, text, and child components. The render tree is then applied to the browser's DOM via one of two paths:

| Rendering Mode              | DOM Application Mechanism                                                                                                      |
| --------------------------- | ------------------------------------------------------------------------------------------------------------------------------ |
| **Static SSR**              | Render tree is serialized to HTML string on the server and sent as the HTTP response. No interactivity.                        |
| **Interactive Server**      | Render tree is sent as a sequence of JavaScript DOM-patching instructions over the SignalR WebSocket.                          |
| **Interactive WebAssembly** | Render tree is applied to the DOM by the .NET WebAssembly runtime running in the browser.                                      |
| **Auto**                    | Starts as Interactive Server, then transitions to Interactive WebAssembly on subsequent visits after the bundle downloads.     |
| **Prerendering**            | All interactive modes prerender statically first (HTML string), then "rehydrate" the component tree when interactivity begins. |

**Source**: [ASP.NET Core Razor components | Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/?view=aspnetcore-10.0), [Render modes](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-10.0)

### DOM Diffing and Patching

Blazor's renderer uses a diff-based approach conceptually similar to React's virtual DOM. After an event handler or state change triggers `StateHasChanged()`:

1. The component re-executes its build logic, generating a new render tree.
2. The renderer diffs the new tree against the previous tree.
3. Only changed elements/attributes/text are sent to the DOM.

**Implication**: Unlike server-rendered Razor Pages that produce a full HTML string replacement, Blazor interactive rendering preserves DOM state (focus, scroll position, input values) for unchanged elements.

### Implications for Accessibility, SEO, and Semantic HTML

- **SEO**: Static SSR and prerendering produce server-rendered HTML that search engines can crawl. Interactive-only content rendered after the page loads may not be indexed unless prerendering is enabled.
- **Accessibility**: Focus management across re-renders is controlled by `@key` (see §3) and `FocusOnNavigate` (see §9). Dynamic DOM patching means assistive technologies may not detect content changes unless live regions or focus management are used.
- **Semantic HTML**: Blazor does not alter your HTML elements — whatever elements you write in `.razor` markup are what appear in the DOM (aside from special components like `EditForm`, `Virtualize`). There is no automatic wrapper or shim.

### WebAssembly vs Server Rendering Differences

| Aspect                        | Interactive Server                         | Interactive WebAssembly                                                          |
| ----------------------------- | ------------------------------------------ | -------------------------------------------------------------------------------- |
| Where render tree is computed | Server (ASP.NET Core)                      | Browser (.NET WASM runtime)                                                      |
| DOM updates transmitted via   | SignalR WebSocket binary messages          | In-process DOM API calls                                                         |
| Network dependency            | Every UI interaction round-trips to server | Offline-capable after initial load                                               |
| HTML output to crawlers       | Prerendering produces static HTML          | Prerendering produces static HTML (hosted model) or no initial HTML (standalone) |
| `<script>` tag restrictions   | Same for both — not allowed in `.razor`    | Same for both — not allowed in `.razor`                                          |

---

## 2. XSS and HTML Injection — `MarkupString`

### The Mechanism

Blazor auto-encodes all C# strings rendered via standard Razor syntax (`@someString`). Special characters (`<`, `>`, `&`, `"`) are HTML-encoded, rendering them as visible text, not executable markup.

`@((MarkupString)rawHtml)` explicitly bypasses this encoding. The string is injected as raw HTML into the DOM.

### Microsoft's Stance

Microsoft's official documentation states:

> "Component authors can author components in C# without using Razor. The component author is responsible for using the correct APIs when emitting output. For example, use `builder.AddContent(0, someUserSuppliedString)` and _not_ `builder.AddMarkupContent(0, someUserSuppliedString)`, as the latter could create a XSS vulnerability."

**Source**: [Threat mitigation guidance for ASP.NET Core Blazor interactive server-side rendering](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/interactive-server-side-rendering?view=aspnetcore-10.0)

Additionally, `<script>` tags in `.razor` files generate **compile-time errors**. However, this protection is bypassed if `<script>` is injected via `MarkupString`.

### When Is `MarkupString` Safe?

- **Safe**: Rendering known, trusted HTML generated by the developer (e.g., preformatted content from a CMS with server-side sanitization, or static HTML templates).
- **Dangerous**: Any user-supplied or externally-sourced string without rigorous server-side sanitization.

### Comparison to `innerHTML`

`MarkupString` is functionally equivalent to JavaScript's `innerHTML` — both parse and render the string as HTML. The same XSS risks apply:

- Event handler attributes (`onerror`, `onclick`, `onmouseover`) execute.
- `<script>` tags execute (in interactive render modes, though blocked at compile time in `.razor`).
- `<iframe>`, `<object>`, `<embed>` can load external malicious content.

### Best Practice

Use `MarkupString` only with server-side-sanitized HTML content. Consider using a library like `HtmlSanitizer` (mganss/HtmlSanitizer) before casting to `MarkupString`. Never pass `AdditionalAttributes` values or form inputs directly to `MarkupString`.

**Community sources**: [amarozka.dev - How to Prevent XSS in Blazor](https://amarozka.dev/blazor-xss-protection-guide/), [Stack Overflow - Blazor XSS discussion](https://stackoverflow.com/questions/75236876/blazor-with-signalr-how-is-an-xss-or-other-attack-possible)

---

## 3. The `@key` Directive

### Purpose

`@key` controls how HTML elements and Blazor components are matched to model objects across re-renders. Without `@key`, Blazor preserves elements by their position (index) in the collection. With `@key`, Blazor preserves elements by the key's identity.

### Focus Preservation for Accessibility

The official documentation demonstrates this explicitly with the `Details` / `People` example:

- Without `@key`: inserting an item at index 0 shifts all existing `<input>` elements down by one position, so a user who had selected (and focused) "Person 3" now has focus on "Person 2" because focus follows the element at _index position 2_.
- With `@key="person"`: when a new person is inserted, existing `Details` component instances are preserved. Focus stays on the same person the user selected.

**Source**: [Retain element, component, and model relationships](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/element-component-model-relationships?view=aspnetcore-10.0)

### When Required

- **Always use `@key` when iterating a list** where items can be inserted, deleted, or reordered.
- Use `@key` when you want to forcibly destroy and recreate a subtree (changing the key value signals "this is a different thing").
- Use `@key` in `Virtualize` item templates to preserve scroll position across data refreshes.

### Scope

Keys are local to sibling elements within the same parent. Keys in different parent containers are independent.

### Performance Note

There is a small performance cost. Only use `@key` when it provides a benefit.

**Source**: [Same Microsoft Learn page as above](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/element-component-model-relationships?view=aspnetcore-10.0)

---

## 4. Component Boundaries vs. HTML Semantics

### The Problem

In raw HTML, certain elements require specific child elements:

- `<table>` requires `<thead>`, `<tbody>`, `<tr>`, `<td>`, `<th>`
- `<ul>` / `<ol>` require `<li>`
- `<select>` requires `<option>`
- `<dl>` requires `<dt>`, `<dd>`

A naive component wrapping these children could produce broken HTML if the component adds an intermediate wrapper element.

### Blazor's Solution: Components Have No Wrapper

**Blazor components do not insert extra wrapping `<div>` elements.** A component renders exactly the markup in its `.razor` file. If your component outputs `<tr>...</tr>`, it is a valid child of `<tbody>`.

Example (valid):

```razor
@* TableTemplate.razor — a templated table component *@
@typeparam TItem
<table class="table">
    <thead>
        <tr>@TableHeader</tr>
    </thead>
    <tbody>
        @foreach (var item in Items)
        {
            <tr @key="@item">@RowTemplate(item)</tr>
        }
    </tbody>
</table>
```

**Source**: [Blazor templated components](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/templated-components?view=aspnetcore-10.0), [Blazor University - Templating components with RenderFragments](https://blazor-university.com/templating-components-with-renderfragements/)

### Anti-Pattern

If you wrap content in a `<div>` inside a component that's expected to render as a `<tr>`, the HTML will be broken:

```html
<tbody>
  <div>
    <!-- Browser will hoist this out of <tbody>, breaking layout -->
    <tr>
      ...
    </tr>
  </div>
</tbody>
```

### Correct Pattern

Components that represent table rows, list items, etc. must output the exact required element as their root node. See the `Virtualize` section (§7) for table virtualization with `SpacerElement="tr"`.

---

## 5. `@attributes` Splatting and `AdditionalAttributes`

### Mechanism

`@attributes` splats a `Dictionary<string, object>` onto an HTML element as HTML attributes.

```razor
<input @attributes="InputAttributes" />

@code {
    private Dictionary<string, object> InputAttributes = new()
    {
        { "maxlength", "10" },
        { "placeholder", "Input placeholder text" },
        { "required", "required" }
    };
}
```

To accept arbitrary attributes from a parent component:

```razor
@code {
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }
}
```

**Source**: [ASP.NET Core Blazor attribute splatting and arbitrary parameters](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/splat-attributes-and-arbitrary-parameters?view=aspnetcore-10.0)

### Processing Order

`@attributes` are processed **right to left** (last to first), with the **first encountered attribute winning** for duplicates. This means:

- `<div @attributes="AdditionalAttributes" extra="5" />` → if parent passes `extra="10"`, the rendered output is `extra="5"` because the hardcoded `extra="5"` appears to the right (processed first).
- `<div extra="5" @attributes="AdditionalAttributes" />` → the splatted value from parent wins (`extra="10"`).

### XSS Risk with `AdditionalAttributes`

**YES, there is risk.** If user input reaches `AdditionalAttributes`, a malicious user could inject dangerous attributes:

- `onclick="alert('XSS')"`
- `onfocus="evil()"`
- `onerror="malicious()"`
- `style` (CSS injection)
- `href="javascript:..."`

Microsoft does not explicitly document this XSS vector in the splatting page, but the security documentation warns:

> "Don't write user-supplied data to the DOM by setting the `innerHTML` property of an element. Consider using Content Security Policy (CSP)."

**Source**: [Security - interactive server-side rendering](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/interactive-server-side-rendering?view=aspnetcore-10.0)

### Best Practice

Sanitize `AdditionalAttributes` before splatting. Strip event handler attributes (`on*`), `href` with `javascript:` protocol, and any attribute that could execute code. Use a CSP with `script-src` restrictions as defense-in-depth.

---

## 6. Blazor Form Components and Accessibility

### HTML Output of `EditForm`

`EditForm` renders as an HTML `<form>` element. It does not wrap content in additional elements. The form system includes:

- `AntiforgeryToken` — auto-added for `EditForm`, renders a hidden `<input>` field
- `DataAnnotationsValidator` — server-side validation component
- `ValidationSummary` — renders as a `<ul>` with `<li>` for each error
- `ValidationMessage<T>` — renders an inline `<div>` with validation error text

**Source**: [ASP.NET Core Blazor forms overview](https://learn.microsoft.com/en-us/aspnet/core/blazor/forms/?view=aspnetcore-10.0)

### Label Association

Blazor's `InputText`, `InputSelect`, etc. do **NOT** auto-generate `<label>` elements. You must provide `<label for="...">` manually.

The `id` on input components is **auto-generated** by Blazor (a GUID-like value), which makes hardcoding `for=""` values fragile. The recommended pattern is to use `id` parameter on the input component:

```razor
<label for="myInput">Name:</label>
<InputText id="myInput" @bind-Value="model.Name" />
```

### Validation Error Display and WCAG

- `ValidationSummary` renders as `<ul role="alert">` with `<li>` per error. This is screen-reader-friendly.
- `ValidationMessage<T>` renders inline error text. You should add `aria-describedby` to associate errors with their input.

**Microsoft Q&A confirms**: "Any attribute you add to a standard Blazor input component (that isn't a strict component parameter) gets 'splatted' (passed through) to the underlying HTML element."

**Source**: [Microsoft Q&A - Blazor ADA compliance](https://learn.microsoft.com/en-us/answers/questions/5736928/is-there-a-built-in-way-of-making-blazor-pages-and)

### What You Still Need to Do Manually

| Concern                         | Status                                                 |
| ------------------------------- | ------------------------------------------------------ |
| `<label for="...">`             | Must add manually                                      |
| `aria-describedby` on inputs    | Must add manually (pointing to `ValidationMessage` id) |
| `aria-required="true"`          | Must add manually or via `AdditionalAttributes`        |
| `required` attribute            | Set on model via `[Required]` data annotation          |
| `<fieldset>` / `<legend>`       | Must add manually                                      |
| `autocomplete` attribute        | Must add manually                                      |
| Error summary as `role="alert"` | `ValidationSummary` handles this                       |
| `tabindex` management           | Must manage manually                                   |

### Client-Side Validation Limitation

Client-side validation requires an active Blazor SignalR circuit. In static SSR forms, all validation happens on the server after submission.

---

## 7. `<Virtualize>` and Semantic HTML

### How Virtualize Renders

`Virtualize<TItem>` renders:

1. A **spacer `<div>` (or custom element)** above the visible items to simulate scroll offset
2. The visible item content
3. A **spacer `<div>` (or custom element)** below the visible items

**Source**: [ASP.NET Core Razor component virtualization](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/virtualization?view=aspnetcore-10.0)

### Table Row Virtualization

Use `SpacerElement="tr"` when virtualizing inside `<tbody>`:

```razor
<table>
    <thead style="position: sticky; top: 0; background-color: silver">
        <tr><th>Item</th><th>Another column</th></tr>
    </thead>
    <tbody>
        <Virtualize Items="fixedItems" ItemSize="30" SpacerElement="tr">
            <tr @key="context" style="height: 30px;">
                <td>Item @context</td>
                <td>Another value</td>
            </tr>
        </Virtualize>
    </tbody>
</table>
```

Without `SpacerElement="tr"`, the default `<div>` spacers inside `<tbody>` produce invalid HTML and may cause layout breakage.

### Layout Constraints

Virtualize works correctly only when:

- The scroll container has `display: block`, `display: table-row-group` (tbody), or `display: flex` with `flex-direction: column`
- Content items have identical height
- Content items have `display: block` (default for `div`) or `display: table-row` (default for `tr`)
- CSS does not interfere with spacer element width/height

### Keyboard Accessibility

The scroll container must be focusable for keyboard scrolling in Chromium-based browsers:

```razor
<div style="height:500px; overflow-y:scroll" tabindex="-1">
    <Virtualize Items="allFlights">...</Virtualize>
</div>
```

### New in .NET 10+

- Running average of measured item heights (improves accuracy)
- `AnchorMode` parameter for controlling viewport behavior (`Beginning`, `End`, `None`)
- `Virtualize.ItemComparer` for detecting prepends/appends with items provider

---

## 8. `<HeadContent>`, `<HeadOutlet>`, `<PageTitle>`

### Mechanism

These components provide a Blazor-native way to set `<head>` content from within Razor components:

```razor
<PageTitle>My Dynamic Title</PageTitle>
<HeadContent>
    <meta name="description" content="Page description">
    <link rel="canonical" href="https://example.com/page">
</HeadContent>
```

The `HeadOutlet` component (placed once in `App.razor` or `index.html`) is the render target where `PageTitle` and `HeadContent` output is rendered.

**Source**: [Control head content in ASP.NET Core Blazor apps](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/control-head-content?view=aspnetcore-10.0)

### SEO Implications

- **With prerendering/SSR**: The `<title>` and `<meta>` tags are present in the initial HTML response. Search engines see them. Good for SEO.
- **Without prerendering** (standalone WASM): The initial HTML may have a default title only. Dynamic titles set by components will only appear after WASM loads. This is problematic for SEO.
- **Dynamic title changes via SPA navigation**: When the router navigates to a new page, the `PageTitle` of the new component replaces the previous `<title>`. This happens via DOM manipulation. Search engine crawlers that don't execute JavaScript will not see the dynamic title changes.

### Pitfalls

1. **Default title required**: Always set a default `<PageTitle>` in `App.razor` (inside `<head>`) so pages have a title before components load.
2. **Not found page title**: Must be set via `<PageTitle>Not found</PageTitle>` in the NotFound component.
3. **WASM `head::after`**: In WebAssembly apps, `HeadOutlet` uses the pseudo-selector to _append_ to head content rather than replace it, preserving static content from `wwwroot/index.html`.

---

## 9. `<FocusOnNavigate>` — Navigation Focus Management

### Mechanism

```razor
<FocusOnNavigate RouteData="routeData" Selector="h1" />
```

After the router navigates to a new page, `FocusOnNavigate` moves focus to the first element matching the CSS selector (typically `h1`).

**Source**: [ASP.NET Core Blazor routing](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/routing?view=aspnetcore-10.0#focus-an-element-on-navigation)

### Accessibility Significance

This solves a critical SPA accessibility problem: after client-side navigation, screen reader users have no indication that the page changed unless focus is programmatically moved to the new content. Microsoft's docs state:

> "This is a common strategy for ensuring that a page navigation is announced when using a screen reader."

### Placement

`FocusOnNavigate` should be placed inside the `<Found>` context of the `Router` component in `Routes.razor`.

### Current Best Practice (2026)

The recommended target is the page's `<h1>` element. This is the pattern used in the Blazor project template and aligns with WCAG 2.1 Success Criterion 2.4.3 (Focus Order) and 2.4.5 (Multiple Ways).

---

## 10. `<ErrorBoundary>` — Error UI Accessibility

### Mechanism

`ErrorBoundary` catches exceptions thrown by child components and renders fallback content:

```razor
<ErrorBoundary>
    <ChildContent>
        <ComponentThatMayThrow />
    </ChildContent>
    <ErrorContent>
        <p role="alert">Something went wrong.</p>
    </ErrorContent>
</ErrorBoundary>
```

### Accessibility Considerations

1. **`role="alert"`**: The error content should include `role="alert"` so screen readers announce the error immediately when it appears.
2. **Recovery actions**: Error content should provide actionable recovery (e.g., a "Try Again" button) rather than just an error message.
3. **Focus management**: When an error boundary activates, focus does not automatically move to the error content. Consider moving focus to the error message programmatically.
4. **Default error content**: Blazor's default error UI is a minimal `<div>` with class `blazor-error-boundary`. You should provide custom `ErrorContent` with proper accessibility.

---

## 11. `<NavLink>` and Navigation Accessibility

### Mechanism

`NavLink` renders as an HTML `<a>` element. It toggles an `active` CSS class based on the current URL.

```razor
<NavLink class="nav-link" href="" Match="NavLinkMatch.All">
    Home
</NavLink>
```

**Source**: [ASP.NET Core Blazor navigation](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/navigation?view=aspnetcore-10.0)

### `aria-current`

**NavLink does NOT render `aria-current` automatically.** You must add it via `AdditionalAttributes`:

```razor
<NavLink href="/" Match="NavLinkMatch.All"
    AdditionalAttributes="@(new Dictionary<string, object> { { "aria-current", "page" } })">
    Home
</NavLink>
```

**Source**: [Stack Overflow - Render aria-current attribute with Blazor's NavLink](https://stackoverflow.com/questions/74340788/render-aria-current-attribute-with-blazors-navlink-component)

### `target="_blank"` and `rel`

Additional attributes pass through to the anchor tag, including `target`:

```razor
<NavLink href="external" target="_blank">External</NavLink>
<!-- Renders: <a href="external" target="_blank">External</a> -->
```

However, `rel="noopener noreferrer"` is **NOT added automatically**. You must include it:

```razor
<NavLink href="https://external.com" target="_blank"
    rel="noopener noreferrer">External</NavLink>
```

### `NavLinkMatch` Behavior

| Mode                            | .NET 9 and earlier                                     | .NET 10+                                                                                                                                                    |
| ------------------------------- | ------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `NavLinkMatch.All`              | Matches entire URL including query string and fragment | Matches entire URL _excluding_ query string/fragment by default. Use `AppContext` switch `EnableMatchAllForQueryStringAndFragment` to restore old behavior. |
| `NavLinkMatch.Prefix` (default) | Matches any prefix of the current URL                  | Same                                                                                                                                                        |

### CSS Isolation Gotcha

As of 2024, `NavLink` does not add CSS isolation attributes to its rendered `<a>` element (see [dotnet/aspnetcore#57160](https://github.com/dotnet/aspnetcore/issues/57160)). This means CSS isolation scoping selectors won't target the anchor. Use global CSS or inline styles for NavLink styling.

### Loop Variable Warning

When rendering `NavLink` inside a `for` loop, you must capture the loop variable locally:

```razor
@for (int c = 1; c < 4; c++)
{
    var ct = c;
    <NavLink href="@($"page-{ct}")">Page @ct</NavLink>
}
```

### External URLs

`NavLink` is designed for internal Blazor navigation. For external URLs, use a plain `<a>` tag.

---

## 12. General Blazor HTML Pitfalls and Known Issues

### Compile-Time Restrictions

1. **`<script>` tags in `.razor` files produce compile-time errors.** Scripts must be added via `wwwroot/index.html` or JavaScript interop. However, `<script>` injected via `MarkupString` bypasses this.

2. **CSS isolation attributes (`b-xxxxxxxxxx`) are added to elements in `.razor` files that have associated `.razor.css` files.** Child component elements do not inherit these attributes (intentional), and `NavLink`'s rendered `<a>` has a known bug where the attribute is missing.

### Validation and W3C Conformance

1. **Blazor's auto-generated `id` attributes**: Input components generate `id` values like `id="b-xxxxxxxxxx_fieldname"`. These are valid HTML5 IDs.

2. **Custom elements/data attributes**: Blazor markers like `_bl_1`, `_bl_2` (used internally for component tracking) are valid HTML5 custom attributes.

3. **Virtualize spacers**: The default spacer `<div>` elements inside `<tbody>` produce invalid HTML5 unless `SpacerElement="tr"` is used.

### ASP.NET Core 9/10 Known Issues

1. **`NavLink` CSS isolation bug** (dotnet/aspnetcore#57160): `NavLink` does not receive CSS isolation attributes on its rendered `<a>`.

2. **`table` + `InputText` bug** (dotnet/aspnetcore#50038): Interactive table cells with input components inside had rendering issues (workaround available).

3. **Streaming rendering limitation for POST**: Only DOM updates inside form handlers are streamed; `OnInitializedAsync` updates are only streamed for GET requests (dotnet/aspnetcore#50994).

4. **Static SSR client-side validation**: No built-in client-side validation for static SSR forms. Under consideration (dotnet/aspnetcore#51040).

5. **Enhanced navigation must be on the form element itself**: Cannot be set on ancestor elements. `data-enhance` must be on `<form>` directly.

6. **Non-serializable parameters**: Cannot pass `RenderFragment` or child content from a static parent to an interactive child component. Runtime `InvalidOperationException`.

### SEO Pitfalls

1. **No `<title>` without `PageTitle`**: If no `PageTitle` is set in any component, the page has no `<title>` tag.
2. **Dynamic `<meta>` tags require SSR**: Meta tags set via `HeadContent` in interactive-only components won't appear in search engine crawls.
3. **`<base>` tag required**: All Blazor apps need `<base href="/">` in `<head>` for correct URL resolution.

### Accessibility Checklist Summary

| Check                                        | Blazor Built-in?                             | Developer Must Do                       |
| -------------------------------------------- | -------------------------------------------- | --------------------------------------- |
| `<label for="...">` on form controls         | No                                           | Yes                                     |
| `aria-current` on active navigation link     | No                                           | Yes (via `AdditionalAttributes`)        |
| `aria-describedby` on inputs with validation | No                                           | Yes                                     |
| Focus on page navigation                     | `FocusOnNavigate` provided                   | Place correctly in router               |
| Focus preservation across list re-renders    | `@key` directive provided                    | Use correctly                           |
| `role="alert"` on dynamic errors             | No (error boundary fallback is raw)          | Yes                                     |
| `rel="noopener"` on `target="_blank"` links  | No                                           | Yes                                     |
| Alt text on images                           | No                                           | Yes                                     |
| Semantic list markup                         | No (but Virtualize supports `SpacerElement`) | Ensure correct spacer                   |
| Keyboard scroll in virtualized lists         | No                                           | Add `tabindex="-1"` to scroll container |
| `<html lang="...">`                          | No                                           | Set in `App.razor` / `index.html`       |

### Rendering Mode Accessibility Implications

| Render Mode                                        | Initial Focus After Nav                          | SEO Crawlability                    | AT Compatibility                    |
| -------------------------------------------------- | ------------------------------------------------ | ----------------------------------- | ----------------------------------- |
| Static SSR                                         | Full page load — natural focus at document start | Full                                | Excellent                           |
| Interactive Server (prerendered)                   | Client-side nav — `FocusOnNavigate` required     | Prerendered HTML is crawlable       | Good (with manual focus management) |
| Interactive WebAssembly (prerendered)              | Same as above                                    | Prerendered HTML is crawlable       | Good (with manual focus management) |
| Interactive WebAssembly (standalone, no prerender) | `FocusOnNavigate` required                       | Poor — crawlers may not see content | Requires careful focus management   |
| Auto                                               | Depends on current render tier                   | Prerendered HTML is crawlable       | Same as Server/WebAssembly per tier |

---

## References

1. [Blazor component rendering overview](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/rendering?view=aspnetcore-9.0)
2. [Blazor render modes](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-10.0)
3. [Security - interactive server-side rendering](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/interactive-server-side-rendering?view=aspnetcore-10.0)
4. [Security - Content Security Policy](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/content-security-policy?view=aspnetcore-10.0)
5. [Element/component/model relationships (@key)](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/element-component-model-relationships?view=aspnetcore-10.0)
6. [Attribute splatting and arbitrary parameters](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/splat-attributes-and-arbitrary-parameters?view=aspnetcore-10.0)
7. [Component virtualization](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/virtualization?view=aspnetcore-10.0)
8. [Control head content](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/control-head-content?view=aspnetcore-10.0)
9. [Blazor routing](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/routing?view=aspnetcore-10.0)
10. [Blazor navigation](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/navigation?view=aspnetcore-10.0)
11. [Blazor forms overview](https://learn.microsoft.com/en-us/aspnet/core/blazor/forms/?view=aspnetcore-10.0)
12. [Templated components](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/templated-components?view=aspnetcore-10.0)
13. [Hybrid security considerations](https://learn.microsoft.com/en-us/aspnet/core/blazor/hybrid/security/security-considerations?view=aspnetcore-10.0)
