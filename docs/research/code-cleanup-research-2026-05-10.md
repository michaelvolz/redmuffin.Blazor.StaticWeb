---
date: 2026-05-10
tags: [research, cleanup, best-practices, blazor, csharp, testing, gates]
description: Raw research content for rm-guide-cleanup and rm-gates-cleanup skills. Covers .NET 9 / C# 13 conventions, Blazor WASM best practices, CRAP score, characterization tests, TDD for legacy code, test desiderata, and software design philosophy.
---

# Code Cleanup Research: rm-guide-cleanup & rm-gates-cleanup

## A) rm-guide-cleanup: Universal Code Quality Principles

### A1. .NET 9 / C# 13 Coding Conventions (Microsoft Docs)

**Source:** https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions

Key rules from official Microsoft conventions:

- **File-scoped namespaces** — use `namespace MySampleCode;` — most code files declare a single namespace.
- **Using directives outside namespace** — prevents name resolution ambiguity when new types are added to nested namespaces.
- **Four spaces for indentation**, no tabs. "Allman" brace style (opening/closing brace on own new line).
- **Line length**: limit to 65 chars for docs readability (adapt based on team standards).
- **`var` usage**: Use when type is obvious from right side (`new`, cast, literal). Do NOT use when type is not apparent.
- **Language keywords over runtime types**: `string` not `String`, `int` not `Int32`, `nint`/`nuint`.
- **Use `int`** rather than unsigned types. Easier interop with other libraries.
- **Collection expressions**: `string[] vowels = [ "a", "e", "i" ];` (C# 12+).
- **String interpolation**: `$"{name.Last}, {name.First}"`; `StringBuilder` for loops.
- **Raw string literals** (`"""..."""`) over escape sequences.
- **`Func<>` / `Action<>`** instead of defining delegate types.
- **Object initializers**: `new ExampleClass { Name = "Desktop", ID = 37414 }`.
- **`new()`** target-typed: `ExampleClass instance2 = new();`
- **Static members**: Call via class name `ClassName.StaticMember`. Don't qualify from derived class.
- **`using` declarations** (no braces): `using Font normalStyle = new("Arial", 10.0f);`
- **`&&` and `||`** (short-circuit) over `&` and `|`.
- **`required` properties** instead of constructors for forcing property initialization.
- **Pascal case** for record primary constructor params; **camel case** for class/struct primary constructor params.
- **Single-line comments** `//` for brief explanations. **XML comments** for public members.
- **Comment on separate line**, start with uppercase, end with period, one space after `//`.

**C# 13 new features (source: https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9/overview):**

- `params` collections — `params` now works with `Span<T>`, `ReadOnlySpan<T>`, and any collection with `Add` method.
- New `Lock` type and semantics — `System.Threading.Lock` class instead of `lock(object)`.
- Escape sequence `\e` for ESC character.
- `ref struct` types can implement interfaces.
- `ref struct` types as generic type arguments.
- Partial properties and indexers.
- Overload resolution priority attribute.
- Preview: `field`-backed properties.

```csharp
// params ReadOnlySpan<T> — zero allocation for callers
void Process(params ReadOnlySpan<int> values) { ... }
Process(1, 2, 3); // no array allocation!

// New Lock type (better than lock(object))
private readonly Lock _lock = new();
void DoWork() {
    using (_lock.EnterScope()) { /* critical section */ }
}
```

---

### A2. Blazor WASM Specific Best Practices

**Sources:**

- https://learn.microsoft.com/en-us/aspnet/core/blazor/components/lifecycle?view=aspnetcore-9.0
- https://learn.microsoft.com/en-us/aspnet/core/blazor/performance/?view=aspnetcore-9.0
- https://learn.microsoft.com/en-us/aspnet/core/blazor/components/synchronization-context?view=aspnetcore-9.0
- https://learn.microsoft.com/en-us/aspnet/core/blazor/components/component-disposal?view=aspnetcore-9.0

#### Lifecycle Order (each stage runs sync first, then async):

1. `SetParametersAsync` → 2. `OnInitialized` / `OnInitializedAsync` → 3. `OnParametersSet` / `OnParametersSetAsync` → 4. `ShouldRender` → 5. Render → 6. `OnAfterRender` / `OnAfterRenderAsync`

**Critical rules:**

- Blazor triggers a render as soon as it possibly can — it does NOT wait for long-running async methods.
- `OnInitializedAsync` — if it returns an incomplete `Task`, component MUST leave itself in a valid state for rendering in the synchronous portion.
- Components are **re-entrant** at any `await`. Lifecycle methods can be called before an async flow resumes. Always check for disposal after any await.
- Never call `StateHasChanged` in `Dispose`/`DisposeAsync` — renderer may already be torn down.

**Async in component lifecycle — cancellation pattern:**

```csharp
@implements IDisposable
@code {
    private CancellationTokenSource? _cts;

    protected override async Task OnInitializedAsync()
    {
        _cts = new();
        try
        {
            await LoadDataAsync(_cts.Token);
        }
        catch (OperationCanceledException) { /* component disposed */ }
    }

    protected override async Task OnParametersSetAsync()
    {
        _cts?.Cancel();
        _cts = new();
        await LoadDataAsync(_cts.Token);
    }

    public void Dispose() => _cts?.Cancel();
}
```

**Fire-and-forget with exception dispatch (Blazor .NET 8+):**

```csharp
private void SendReport()
{
    _ = Task.Run(async () =>
    {
        try { await ReportSender.SendAsync(); }
        catch (Exception ex) { await DispatchExceptionAsync(ex); }
    });
}
```

---

### A3. ConfigureAwait(false) Rules for Blazor WASM

**Sources:**

- https://learn.microsoft.com/en-us/aspnet/core/blazor/components/synchronization-context?view=aspnetcore-9.0
- https://github.com/dotnet/aspnetcore/issues/33583 (Steve Sanderson: "Blazor is a UI Framework... code needs to run on the synchronization context")
- https://stackoverflow.com/questions/64553991/using-configureawait-in-nested-async-methods

**The definitive rule set:**

| Context                                                       | Rule                                                                                                |
| ------------------------------------------------------------- | --------------------------------------------------------------------------------------------------- |
| **Blazor component code** (.razor, lifecycle, event handlers) | Do NOT use `ConfigureAwait(false)`. Code must resume on sync context for `StateHasChanged`.         |
| **Blazor WASM library/shared code** (services, non-UI code)   | Do NOT use `ConfigureAwait(false)`. WASM is single-threaded — there IS only one thread. No benefit. |
| **Blazor Server library/shared code** (services, callbacks)   | DO use `ConfigureAwait(false)`. Server has thread pool. Avoids capturing circuit's sync context.    |
| **General-purpose .NET libraries** (not Blazor-specific)      | DO use `ConfigureAwait(false)`. Prevents deadlocks in unknown hosting environments.                 |

**Why:** Blazor WASM runs in a single-threaded JS runtime per tab. `ConfigureAwait(false)` buys nothing — after await, you're back on the same thread anyway. However, in Blazor Server, the sync context is captured per circuit and failing to use `ConfigureAwait(false)` in non-UI code can cause thread starvation.

**Steve Sanderson's official answer (dotnet/aspnetcore#33583):** "No" — don't use `ConfigureAwait(false)` in Blazor components since Blazor is a UI framework.

---

### A4. Collection Abstraction Preferences

**Sources:**

- https://enterprisecraftsmanship.com/posts/ienumerable-vs-ireadonlylist/
- https://enterprisecraftsmanship.com/posts/which-collection-interface-to-use/

**Postel's Law applied to collections:**

- **Accept the most generic type** → `IEnumerable<T>` for input parameters (if you only iterate)
- **Return the most specific type** → `IReadOnlyList<T>` for return values (gives caller `.Count`, indexer)
- Fall back to `IEnumerable<T>` as return type only if lazy evaluation is intentional (streaming, deferred execution)

**When to use which:**

```csharp
// ACCEPTING parameters:
// Only need to loop? → IEnumerable<T>
public void ProcessItems(IEnumerable<string> items) {
    foreach (var item in items) { /* ... */ }
}

// Need count or index access? → IReadOnlyList<T>
public void ProcessItems(IReadOnlyList<string> items) {
    if (items.Count == 0) return;
    var first = items[0];
}

// RETURNING values:
// Return IReadOnlyList<T> when possible (most specific read-only)
public IReadOnlyList<User> GetUsers() => _users.ToList().AsReadOnly();

// Return IEnumerable<T> only for streaming/lazy (deferred execution)
public IEnumerable<User> GetAllUsers() {
    foreach (var user in LoadUsersFromDb()) yield return user;
}
```

**Avoid:** `IReadOnlyCollection<T>` — it's between IEnumerable and IReadOnlyList but all collections implementing it also implement IReadOnlyList, so there's no practical advantage.

**For private methods:** concrete types (`List<T>`, `Dictionary<TKey,TValue>`) are fine. OCP applies to public APIs.

---

### A5. C# Record Types Best Practices

**Sources:**

- https://www.infoworld.com/article/2336085/when-to-use-classes-structs-or-records-in-c-sharp.html
- https://enterprisecraftsmanship.com/posts/csharp-records-value-objects/
- https://khalidabuhakmeh.com/avoid-csharp-9-record-gotchas
- https://stackoverflow.com/questions/64816714/when-to-use-record-vs-class-vs-struct

**Use records for:**

- DTOs, API response/request models
- Value objects (DDD) — immutable, structural equality
- Data transfer between layers
- Keys, IDs, and other small immutable data types
- When you want value-based equality (not reference equality)

**Do NOT use records for:**

- Entity types with identity and mutable state
- Types requiring inheritance from a class (records can only inherit from records)
- Service classes with behavior
- Types where you need fine-grained encapsulation with backing fields

```csharp
// Good: DTO / Value Object
public record UserDto(string Name, string Email);
public record Money(decimal Amount, string Currency);

// Bad: Entity (should be class)
public record Order // Bad — entities have identity, mutable state
{
    public Guid Id { get; init; }
    public OrderStatus Status { get; set; }
}

// Good: Entity as class
public class Order
{
    public Guid Id { get; private set; }
    public OrderStatus Status { get; private set; }
    public void Ship() { Status = OrderStatus.Shipped; }
}

// Record struct for small, stack-allocated values
public readonly record struct Point3D(float X, float Y, float Z);
```

**Key gotchas:**

- Records have compiler-generated `ToString()`, `Equals()`, `GetHashCode()`, `Deconstruct()`, and `with` expressions.
- `with` creates a shallow copy — nested reference types still refer to the same objects.
- Records can inherit from other records, but NOT from classes, and classes can't inherit from records.
- `record struct` (C# 10+) gives value-type semantics with record features.

---

### A6. Microsoft.Extensions.Logging Structured Logging

**Source:** https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/source-generation

**The LoggerMessageAttribute (compile-time source generator) — THE standard for .NET 6+:**

```csharp
// Static pattern (recommended for most cases):
public static partial class Log
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Error,
        Message = "User {UserId} failed to login from {IPAddress}")]
    public static partial void UserLoginFailed(
        ILogger logger, string userId, string ipAddress);
}

// Instance pattern — uses ILogger field or primary constructor:
public partial class OrderService(ILogger<OrderService> logger)
{
    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "Order {OrderId} status changed to {Status}")]
    public partial void OrderStatusChanged(Guid orderId, string status);
}

// Dynamic log level (determine at runtime):
[LoggerMessage(EventId = 3001, Message = "Processing {ItemCount} items")]
public static partial void ProcessingItems(
    ILogger logger, LogLevel level, int itemCount);

// Extension method pattern (this ILogger):
[LoggerMessage(EventId = 0, Level = LogLevel.Critical, Message = "Could not open socket to {HostName}")]
public static partial void CouldNotOpenSocket(this ILogger logger, string hostName);
```

**Benefits over manual `LoggerMessage.Define`:**

- Zero boxing, zero allocations at runtime.
- Shorter, declarative syntax.
- Compile-time diagnostics (SYSLIB warnings for misuse).
- Unlimited parameters (Define supports max 6).
- Case-insensitive template name matching.
- Supports format specifiers: `{Value:E}`, `{Price:C}`.

**Rules:**

- Method must be `partial` and return `void`.
- Method name and parameter names must NOT start with underscore.
- No `params`, `scoped`, or `out` modifiers.
- If static, `ILogger` must be a parameter.
- If instance, `ILogger` is resolved from field or primary constructor.
- The first `ILogger`, `LogLevel`, and `Exception` in signature are treated specially.

**Structured logging — the state is preserved:**

```csharp
[LoggerMessage(0, LogLevel.Information, "Order {OrderId} placed by {CustomerName}")]
public static partial void OrderPlaced(ILogger logger, Guid orderId, string customerName);
// Logs: { "OrderId": "...", "CustomerName": "...", "{OriginalFormat}": "Order {OrderId} placed by {CustomerName}" }
```

**Redaction (Microsoft.Extensions.Telemetry):**

- Use `[PrivateData]` attributes on parameters.
- Register `builder.EnableRedaction()` and `builder.SetRedactor<StarRedactor>(MyTaxonomy.Private)`.
- Source-generated logger respects redaction automatically.

---

### A7. DI Best Practices for Blazor

**Sources:**

- https://learn.microsoft.com/en-us/aspnet/core/blazor/components/component-disposal?view=aspnetcore-9.0
- https://learn.microsoft.com/en-us/aspnet/core/blazor/components/synchronization-context?view=aspnetcore-9.0

**Service lifetimes in Blazor:**

| Lifetime      | Blazor WASM                             | Blazor Server                         | Notes                                                                                   |
| ------------- | --------------------------------------- | ------------------------------------- | --------------------------------------------------------------------------------------- |
| **Singleton** | App-wide (one per browser tab)          | App-wide (shared across ALL circuits) | Safe for stateless services. NEVER hold circuit/user state in singletons on Server.     |
| **Scoped**    | Same as singleton in WASM (one per app) | Per circuit (per user session)        | Default for DbContext on Server.                                                        |
| **Transient** | New instance per injection              | New instance per injection            | Never for `IDisposable`/`IAsyncDisposable` — DI container holds reference for disposal. |

**Disposal rules:**

- Components should NOT implement both `IDisposable` and `IAsyncDisposable`. If both exist, only async runs.
- Objects created in lifecycle methods must be null-checked in `Dispose` (they may not have been created if component was disposed early).
- No `StateHasChanged` in `Dispose`/`DisposeAsync`.
- `IAsyncDisposable.DisposeAsync` must not take a long time to complete.
- Always unsubscribe event handlers in `Dispose` (memory leaks when event-exposing object outlives the component).
- Lambda/anonymous handlers don't need explicit unsubscription IF the event-exposing object has shorter lifetime than the component.

**Singleton services with disposal — Blazor WASM:**

```csharp
// WASM: Singleton lives for app lifetime. Disposal at app shutdown is unreliable.
// Prefer stateless singletons.
builder.Services.AddSingleton<IWeatherService, WeatherService>();
```

**Never this pattern:**

```csharp
// NEVER: Transient disposable — DI container holds reference and can't dispose properly
builder.Services.AddTransient<IDisposableService, DisposableService>();
```

**JS interop disposal:**

- Dispose `IJSObjectReference` / `DotNetObjectReference` explicitly to avoid memory leaks.
- Trap `JSDisconnectedException` during disposal (circuit may be gone in Server).

---

### A8. Key .NET 9 Features for Code Quality

**Sources:**

- https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9/overview
- https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9/libraries

**Feature → Cleanup opportunity:**

| Feature                                  | What to Replace                                           | Example                                                  |
| ---------------------------------------- | --------------------------------------------------------- | -------------------------------------------------------- |
| `params ReadOnlySpan<T>`                 | `params T[]` (heap allocation)                            | `void Process(params ReadOnlySpan<int> values)`          |
| `Lock` class                             | `lock(object)`                                            | `using (_lock.EnterScope())`                             |
| `Guid.CreateVersion7()`                  | `Guid.NewGuid()` (for time-sortable IDs)                  | `Guid id = Guid.CreateVersion7();`                       |
| `CountBy` / `AggregateBy` (LINQ)         | `GroupBy().Select(g => new { g.Key, Count = g.Count() })` | `items.CountBy(x => x.Category)`                         |
| `PriorityQueue.Remove`                   | Re-create queue for priority updates                      | `queue.Remove(element, out _, out _)`                    |
| `TimeSpan.From*` (int overloads)         | Avoid double precision issues                             | `TimeSpan.FromSeconds(5)` vs `TimeSpan.FromSeconds(5.0)` |
| `ReadOnlySpan<char>.Split()` enumeration | `string.Split()` (allocations)                            | `foreach (var segment in span.Split(','))`               |
| `PersistedAssemblyBuilder`               | N/A (new capability)                                      | Save emitted assemblies with PDB support                 |
| `System.Threading.Lock`                  | `object` + `Monitor.Enter/Exit`                           | `private readonly Lock _sync = new();`                   |
| New `Base64Url` class                    | Manual base64url encoding                                 | `Base64Url.EncodeToString(bytes)`                        |

**`params ReadOnlySpan<T>` — the biggest performance win:**

```csharp
// Before: allocates array
void LogMessages(params string[] messages) { ... }
LogMessages("a", "b", "c"); // heap allocation!

// After: zero allocation
void LogMessages(params ReadOnlySpan<string> messages) { ... }
LogMessages("a", "b", "c"); // no heap allocation!
```

---

## B) rm-gates-cleanup: Quality Gates-Specific Cleanup

### B1. CRAP Score Formula and Reduction Strategies

**Source:** https://blog.ndepend.com/crap-metric-thing-tells-risk-code/

**Formula:**

```
CRAP(m) = CC(m)² × U(m)³ + CC(m)

where:
  CC(m) = cyclomatic complexity of method m
  U(m)  = percentage of method NOT covered by tests (0.0 to 1.0)
```

**Minimum score:** 1 (CC=1, 100% coverage)
**CRAP threshold:** > 30 = "CRAP-py" (risky to change)

**Key insights from the math:**

- Adding test coverage goes a LONG way (U is cubed — small coverage gains have big impact).
- A method with CC=10 needs only 42% coverage to be below 30.
- A method with CC=25 needs 80% coverage.
- **If CC > 30, NO amount of testing can make it non-CRAP-py** (must be refactored to reduce complexity).
- If CC ≤ 5, tests aren't required to be below threshold (but write them anyway).

**Reduction strategies (in priority order):**

1. **Add tests first** — fastest CRAP reduction. A complex method fully tested is still risky but much safer.
2. **Extract guard clauses** — transform nested ifs into early returns:
   ```csharp
   // Before: CC=4
   public void Process(Order order) {
       if (order != null) {
           if (order.IsValid) {
               if (order.Items.Any()) {
                   DoWork(order);
               }
           }
       }
   }
   // After: CC=2
   public void Process(Order order) {
       if (order is not { IsValid: true } || !order.Items.Any()) return;
       DoWork(order);
   }
   ```
3. **Extract methods** — move logical branches into their own methods:
   ```csharp
   // Before: CC=8 (multiple nested conditions)
   public decimal CalculatePrice(Order order) {
       // many if/else branches...
   }
   // After: Each method CC ≤ 3
   public decimal CalculatePrice(Order order) =>
       ApplyDiscounts(ApplyTaxes(CalculateSubtotal(order)));
   ```
4. **Use polymorphism** — replace switch/if-else chains with strategy pattern:
   ```csharp
   // Before: CC=6
   public decimal GetDiscount(Order o) => o.Type switch {
       OrderType.Standard => o.Total * 0.05m,
       OrderType.Premium => o.Total * 0.10m,
       OrderType.VIP => o.Total * 0.15m,
       _ => 0
   };
   // After: CC=1 + polymorphic dispatch
   public interface IDiscountStrategy { decimal Calculate(Order o); }
   ```
5. **Table-driven logic** — replace conditional chains with lookup tables.
6. **LINQ chains** — replace complex loops with declarative LINQ (reduces branching).

---

### B2. Characterization Tests (Golden Master Pattern)

**Sources:**

- Michael Feathers, "Working Effectively with Legacy Code"
- https://en.wikipedia.org/wiki/Characterization_test
- https://understandlegacycode.com/blog/characterization-tests-or-approval-tests/

**Definition:** A characterization test captures the CURRENT behavior of existing code, regardless of whether that behavior is "correct." It protects against unintended changes during refactoring.

**Steps:**

1. Identify the code to change/refactor.
2. Write tests that characterize its CURRENT behavior (not "correct" behavior).
3. Run these tests — they should pass (capturing existing behavior).
4. Now refactor — tests guard against regression.
5. After refactoring, add proper specification tests for new/changed behavior.

**In C# with ApprovalTests or Verify:**

```csharp
// Characterization test — captures output as golden master
[Test]
public async Task OrderProcessor_Characterization()
{
    // Arrange: diverse inputs covering all code paths
    var inputs = GenerateDiverseOrders(100);

    // Act: run existing code
    var outputs = inputs.Select(o => OrderProcessor.Process(o));

    // Assert: verify against golden master
    await Verifier.Verify(outputs)
        .UseDirectory("Snapshots");
}

// If golden master doesn't exist, it's created.
// If it exists and output differs, test fails — you changed behavior.
```

**Golden Master technique (for batch operations):**

1. Generate large set of diverse random inputs.
2. Run the legacy system with those inputs — record ALL outputs.
3. Save outputs as the "golden master" file.
4. After any change, re-run — if outputs differ, either the change was wrong or the golden master was capturing a bug.
5. For intentional behavior changes, update the golden master.

---

### B3. Test Quality: Meaningful vs Coverage Padding

**Key principles:**

**A meaningful test:**

- **Fails for exactly one reason** — the cause of failure is unambiguous (Kent Beck's "Specific" desideratum).
- **Tests behavior, not implementation** — "if I change behavior, the test should fail; if I refactor structure, the test should still pass."
- **Has a clear Arrange-Act-Assert structure** with meaningful names.
- **Tests one logical scenario per test method** (not one assertion — one SCENARIO).
- **Uses realistic data** — not `"foo"`, `"bar"`, `42` in every test.

**Coverage padding (what to avoid):**

- Testing trivial properties/getters (automatic properties don't need tests).
- Testing framework code (DI registration, routing).
- Tests without assertions (asserting `true == true`).
- Tests that only verify mocks were called without verifying behavior.
- Tests that duplicate each other (same logic, different data).

```csharp
// Coverage padding — low value:
[Test]
public void User_Name_Getter_ReturnsName() {
    var user = new User { Name = "John" };
    Assert.That(user.Name, Is.EqualTo("John"));
}

// Meaningful test — tests behavior:
[Test]
public async Task CreateOrder_WhenInventorySufficient_DecrementsStockAndReturnsOrder() {
    var inventory = new Inventory(new Dictionary<string, int> { ["SKU-1"] = 10 });
    var service = new OrderService(inventory);

    var order = await service.CreateOrderAsync("SKU-1", 3);

    Assert.That(inventory.GetStock("SKU-1"), Is.EqualTo(7));
    Assert.That(order.Status, Is.EqualTo(OrderStatus.Confirmed));
    Assert.That(order.Quantity, Is.EqualTo(3));
}
```

---

### B4. Uncle Bob's TDD Cycle Applied to Legacy Code

**The Red-Green-Refactor cycle for NEW code:**

1. **Red:** Write a failing test (compile error counts).
2. **Green:** Write the minimum code to pass.
3. **Refactor:** Clean up both test and production code.

**Applied to LEGACY code (the Feathers approach):**

1. **Identify change points** — find the seams where you can inject testability.
2. **Get the code under test (characterization)** — write tests capturing current behavior BEFORE changing anything.
3. **Make the change** — now you have a safety net.
4. **Refactor** — clean up after the change, tests still passing.

```csharp
// Legacy code — untested, tightly coupled:
public class ReportGenerator {
    public string Generate() {
        var data = Database.GetSalesData(); // static call, hard dependency
        // ... 200 lines of logic ...
        EmailSender.Send(report); // another static call
    }
}

// Step 1: Find seams & add characterization test
// Extract virtual method for the DB call
public class ReportGenerator {
    protected virtual SalesData GetSalesData() => Database.GetSalesData();
    // ... rest stays the same for now ...
}

// Characterization test subclass:
public class TestableReportGenerator : ReportGenerator {
    private readonly SalesData _data;
    public TestableReportGenerator(SalesData data) => _data = data;
    protected override SalesData GetSalesData() => _data;
}

// Step 2: Write characterization test before refactoring
[Test]
public void Generate_Characterization() {
    var gen = new TestableReportGenerator(TestData.SampleSales);
    var result = gen.Generate();
    await Verifier.Verify(result); // captures current output
}

// Step 3: Now refactor safely
// Step 4: Add proper DI, interface extraction, etc.
```

---

### B5. SCRAP: LOCAL Files and How to Fix Them

**LOCAL files** in the SCRAP model typically refer to files that have high Lines Of Code And (complexity) — overly large files with too many responsibilities.

**Fixes:**

1. **Extract helper classes** — move related methods into focused service/helper classes.
2. **Table-driven tests** — replace repetitive test methods with parameterized/tabled tests:

```csharp
// Before: Repetitive, LOCAL-smell test file
[Test] public async Task Test_Validation_EmptyName() { ... }
[Test] public async Task Test_Validation_EmptyEmail() { ... }
[Test] public async Task Test_Validation_InvalidEmail() { ... }
[Test] public async Task Test_Validation_NameTooLong() { ... }
// ... 20 more similar tests ...

// After: One parameterized test
[Test]
[MethodDataSource(nameof(ValidationScenarios))]
public async Task Validate_ReturnsExpectedError(string name, string email, string? expectedError)
{
    var result = await Validator.ValidateAsync(new(name, email));
    Assert.That(result.Error, Is.EqualTo(expectedError));
}

public static IEnumerable<(string, string, string?)> ValidationScenarios()
{
    yield return ("", "a@b.com", "Name is required");
    yield return ("John", "", "Email is required");
    yield return ("John", "invalid", "Invalid email format");
    yield return (new string('x', 101), "a@b.com", "Name too long");
}
```

3. **Extract test fixtures/contexts** — shared setup into reusable base/factory:

```csharp
// Shared test context
public class OrderTestContext : IAsyncDisposable
{
    public InMemoryDatabase Db { get; }
    public OrderService Service { get; }

    public static async Task<OrderTestContext> CreateAsync()
    {
        var ctx = new OrderTestContext();
        await ctx.Db.SeedAsync(TestData.StandardProducts);
        return ctx;
    }
}
```

---

### B6. Kent Beck's Test Desiderata

**Source:** https://kentbeck.github.io/TestDesiderata/

**The 12 properties (sliders — trade off, don't maximize all):**

| Property                  | Meaning                                               | C# Implication                                |
| ------------------------- | ----------------------------------------------------- | --------------------------------------------- |
| **Isolated**              | Tests don't affect each other's results               | No shared mutable state between tests         |
| **Composable**            | Test different dimensions separately, combine results | Test validation + calculation separately      |
| **Deterministic**         | Same input → same output (no flaky tests)             | Mock time sources, random seeds, external I/O |
| **Fast**                  | Tests run quickly                                     | Unit tests < 1ms, integration < 100ms         |
| **Writable**              | Cheap to write relative to production code            | Good test helpers, fixtures, builders         |
| **Readable**              | Reader understands motivation immediately             | Clear test names, given-when-then structure   |
| **Behavioral**            | Sensitive to behavior changes                         | Don't test implementation details             |
| **Structure-insensitive** | Doesn't break on refactoring                          | Test via public API, not internals            |
| **Automated**             | No human intervention to run                          | CI/CD pipeline, `dotnet test`                 |
| **Specific**              | Failure cause is obvious                              | One logical scenario per test                 |
| **Predictive**            | All passing = safe to deploy                          | Comprehensive scenario coverage               |
| **Inspiring**             | Passing tests builds confidence                       | Tests cover real-world edge cases             |

**Beck's core rule:** "No property should be given up without receiving a property of greater value in return."

```csharp
// Trade-off example: Structure-insensitive vs Specific
// Structure-insensitive test — won't break on refactor:
[Test]
public void CalculateDiscount_PremiumCustomerOver100_Applies12Percent()
{
    var result = Pricing.CalculateDiscount(CustomerTier.Premium, 150m);
    Assert.That(result, Is.EqualTo(18m)); // 12% of 150
}

// Bad (structure-sensitive) — tests implementation detail:
[Test]
public void CalculateDiscount_CallsApplyPremiumRate() {
    var mock = new Mock<IRateProvider>();
    mock.Setup(x => x.ApplyPremiumRate(150m)).Returns(18m);
    // test breaks if you rename ApplyPremiumRate or change the method signature
}
```

---

### B7. Dave Farley's "Modern Software Engineering" — Fast Feedback

**Source:** https://www.davefarley.net/

**Core principles:**

1. **Fast feedback loops** — from seconds (TDD) to minutes (CI) to hours (CD). Tighter feedback = faster learning.
2. **Small changes** — each change should be as small as possible. Small changes are easier to understand, test, and revert.
3. **Continuous Delivery** — always be in a releasable state. This is the ultimate feedback mechanism.
4. **Manage complexity** via modularity, cohesion, separation of concerns.
5. **TDD as a design tool** — not just for testing, but for driving good design.

**Applied to cleanup:**

- Every cleanup change should pass tests immediately.
- Commit after each small, focused improvement.
- If a cleanup change breaks many tests, it's too large — split it.
- Automated gates (CRAP, SCRAP) provide rapid feedback on code quality.

---

### B8. John Ousterhout's "A Philosophy of Software Design"

**Source:** https://www.mattduck.com/2021-04-a-philosophy-of-software-design.html

**Key principles:**

1. **Deep modules** — modules should have simple interfaces but powerful functionality. "The best modules are those that provide powerful functionality behind a simple interface."

2. **Shallow modules are a red flag** — complex interface with minimal functionality. This is the opposite of what we want.

3. **Information hiding** — hide complexity inside modules. Expose only what callers need.

4. **"Different layers, different abstractions"** — each layer should introduce a new abstraction, not just pass through calls.

5. **Strategic vs tactical programming** — invest in design now to reduce long-term complexity.

6. **Complexity is incremental** — it accumulates one small dependency at a time. Fight it continuously.

7. **"Design it twice"** — consider at least two approaches before committing.

8. **Comments should describe things not obvious from the code** — Ousterhout disagrees with "comments are always failure" (Clean Code). Comments should explain _why_, not _what_.

```csharp
// Shallow module (red flag) — complex interface, trivial functionality:
public interface IUserRepository {
    User GetById(int id);
    void Save(User user);
    void Delete(int id);
    IEnumerable<User> GetAll();
    // ... 15 more pass-through methods to EF Core ...
}

// Deep module — simple interface, powerful behavior:
public interface IUserStore {
    Task<User?> FindActiveUserAsync(string email); // handles lookup, activation check, caching
    Task<Result> RegisterAsync(RegisterCommand cmd); // handles validation, dedup, save, event
}
```

---

### B9. Clean Code on Comments

**Source:** Robert C. Martin, "Clean Code" (Chapter on Comments)

**The canonical quotes:**

- "Don't comment bad code — rewrite it." — Brian W. Kernighan and P.J. Plaugher
- "Every use of a comment represents a failure to express yourself in code."
- "Comments are not 'pure good.' The proper use of comments is to compensate for our failure to express ourselves in code."
- "Don't Use a Comment When You Can Use a Function or a Variable"

**When comments ARE appropriate:**

- Legal notices (copyright, license).
- Informative comments (regex explanation, algorithmic rationale).
- Explanation of intent — WHY this approach was chosen.
- Clarification of obscure arguments or return values.
- Warning of consequences.
- TODO comments (temporary).
- Public API documentation (XML comments for IntelliSense).

**When comments are a code smell:**

- Redundant comments: `// Increment i` above `i++`.
- Mumbling comments that restate the obvious.
- Commented-out code — delete it (version control remembers).
- Journal comments (git log handles this).
- Position markers (`// ---------- END SECTION ----------`).
- Closing brace comments (`} // end if`).
- Attributions (`// Added by John on 2020-03-15`).

```csharp
// Bad — comment compensates for bad naming:
// Check if user is eligible for discount
if (user.RegistrationDate > DateTime.Now.AddYears(-1) && user.TotalPurchases > 1000)
    return true;

// Good — code explains itself:
if (user.HasBeenMemberForAtLeastOneYear && user.HasExceededMinimumPurchases)
    return true;

// Good — XML doc explains PUBLIC API:
/// <summary>
/// Calculates the effective tax rate based on jurisdiction and exemption status.
/// </summary>
/// <param name="jurisdiction">The tax jurisdiction code (ISO 3166-2).</param>
/// <param name="exemptions">Any applicable tax exemptions.</param>
/// <returns>The effective tax rate as a decimal (e.g., 0.0825 for 8.25%).</returns>
public decimal CalculateTaxRate(string jurisdiction, IReadOnlyList<TaxExemption> exemptions) { ... }

// Good — comment explains WHY, not WHAT:
// Use ArrayPool to reduce GC pressure — this method is in the hot path
var buffer = ArrayPool<byte>.Shared.Rent(4096);
```

---

## Summary of C# Examples by Cleanup Category

### How to reduce CRAP:

```csharp
// 1. Guard clauses
if (input is null) return;

// 2. Extract method
var subtotal = CalculateSubtotal(items);
var tax = ApplyTax(subtotal, rate);
return subtotal + tax;

// 3. LINQ over loops
return items.Where(i => i.IsActive).Sum(i => i.Price);

// 4. Switch expression (C# 8+)
var discount = tier switch {
    CustomerTier.Standard => 0.05m,
    CustomerTier.Premium => 0.10m,
    CustomerTier.VIP => 0.15m,
    _ => 0m
};
```

### How to fix structural issues:

```csharp
// Before: Service locator anti-pattern
var service = ServiceProvider.GetRequiredService<IOrderService>();

// After: Constructor injection
public class CheckoutHandler(IOrderService orderService) { ... }

// Before: Null checks everywhere
if (user != null && user.Address != null && user.Address.City != null) { ... }

// After: Nullable reference types + pattern matching
if (user?.Address?.City is { } city) { ... }
```

### How to handle async in Blazor:

```csharp
// Correct lifecycle async
protected override async Task OnInitializedAsync()
{
    _cts = new();
    try
    {
        Data = await ApiService.GetDataAsync(_cts.Token);
    }
    catch (OperationCanceledException) { }
}

// External event dispatch (from non-Blazor context)
private async Task OnNotify(string key, int value)
{
    await InvokeAsync(() =>
    {
        _lastNotification = (key, value);
        StateHasChanged();
    });
}
```
