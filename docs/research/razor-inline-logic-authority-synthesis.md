---
date: 2026-06-14
title: What Six Software Engineering Authorities Would Say About Inline C# in Razor Markup
tags: [architecture, blazor, razor, separation-of-concerns, research]
description: Synthesis of how Uncle Bob Martin, Martin Fowler, Kent Beck, Michael Feathers, John Ousterhout, and Sandi Metz would evaluate and restructure Blazor components that embed C# logic directly in .razor markup via @code blocks.
module: architecture
problem_type: design-pattern
---

## Research Question

What would six recognized software engineering authorities say about keeping C# logic inside `.razor` markup via `@if`, `@foreach`, and `@code` blocks in Blazor components?

## 1. Robert C. Martin ("Uncle Bob")

### What he would criticize

Inline `@if (order.Status == OrderStatus.Pending && user.Role == "Admin")` inside `.razor` markup violates three principles simultaneously:

1. **Single Responsibility Principle**: The `.razor` file now has two reasons to change — the markup structure and the business rule about which orders are editable. "A class should have only one reason to change" (Clean Code, Ch. 10).

2. **The Dependency Rule**: In Clean Architecture, source code dependencies must point inward. Business rules live in the inner circles (Entities, Use Cases). When you embed a rule like `user.Role == "Admin"` in the UI layer, you have inverted the dependency: the business rule now depends on the UI framework (Blazor/Razor compilation). That rule cannot be reused, cannot be tested in isolation, and will be duplicated across every component that needs it.

3. **Testability**: "The business rules can be tested without the UI, Database, Web Server, or any other external element" (Clean Architecture, Ch. 22). A `.razor` file with inline logic requires a Blazor render tree, bUnit, and the full component lifecycle just to exercise what is fundamentally a business rule.

### What structural pattern he would prescribe

The **Humble Object / Presenter** pattern from Clean Architecture (Ch. 23). Split the component into:

- A **Humble View** (the `.razor` file) that contains only markup, data binding, and the absolute minimum rendering logic (e.g., CSS class toggling). It receives a flat DTO/view model and renders it.
- A **Presenter** (a plain C# class) that takes raw data, applies all conditional logic, and produces the flat DTO that the view consumes.
- **Use Cases / Interactors** deeper in the architecture that contain the actual business rules, completely independent of any UI framework.

The view should have zero `@if` statements that encode business rules. It should have only rendering decisions like `@if (model.IsVisible)` where `IsVisible` was computed by the presenter or use case.

### Direct quote

> "They all have the same objective, which is the **separation of concerns**. They all achieve this separation by dividing the software into layers. Each has at least one layer for business rules, and another for interfaces." — _The Clean Architecture_ (2012)

> "You separate the UI from the business rules by passing simple data structures between the two. You don't let your controllers know anything about the business rules. ... The views do not know about the business objects. They just look in that data structure and present the response." — _Clean Coder Blog_ (2011)

---

## 2. Martin Fowler

### What he would criticize

Inline `@if` logic in `.razor` files violates **Separated Presentation** — Fowler's foundational principle that domain logic and presentation logic must live in different modules. An `@if` that checks a business condition (e.g., `order.CanBeCancelled()`) makes the view aware of domain semantics. The view should never call domain methods or know domain rules.

Even presentation logic (e.g., "show this panel when the user clicks expand") should be extracted when it becomes complex, because it makes the view untestable and couples business-rule display to a specific rendering framework.

### What structural pattern he would prescribe

**Presentation Model** (the pattern that later became MVVM). Fowler's definition:

- Create a `PresentationModel` class (a plain C# class) that holds all the state the view needs and exposes it as simple properties: `bool IsCancelButtonVisible`, `string StatusCssClass`, `IReadOnlyList<OrderRowViewModel> Rows`.
- The `.razor` view binds to these properties. It does not compute them.
- The Presentation Model coordinates with domain objects but the view never touches domain objects directly.
- The Presentation Model is fully unit-testable without any UI framework.

For simpler cases, **Passive View**: the controller/presenter does ALL the work of computing what the view should display; the view is a completely passive recipient.

> "The essence of a Presentation Model is of a fully self-contained class that represents all the data and behavior of the UI window, but without any of the controls used to render that UI on the glass. A view then simply projects the state of the presentation model onto the glass." — _Presentation Model_ (2004)

> "The driving reason to use Passive View is to enhance testability. With the view reduced to a dumb slave of the controller, you run little risk by not testing the view." — _Passive View_ (2006)

### Direct quote

> "Represent the state and behavior of the presentation independently of the GUI controls used in the interface." — _Presentation Model_ (2004)

---

## 3. Kent Beck

### What he would criticize

You cannot practice TDD on a `.razor` file with inline C#. The TDD cycle requires:

1. Write a failing test
2. Write the minimum code to make it pass
3. Refactor

A `.razor` file with `@code { ... }` is compiled into a generated C# class by the Razor compiler. You cannot instantiate it in a test harness without the Blazor rendering infrastructure. Even with bUnit, the test must go through the component lifecycle, render tree diffing, and parameter passing — a heavy, framework-coupled test. Beck's prerequisites for TDD include: "Predict inputs/outputs" and "Micro-tests yield macro-results." Inline razor logic fails both.

The Humble Object principle — which Beck helped popularize through his work with Ward Cunningham and the xUnit patterns community — says: extract all the logic you want to test into a plain object, and leave the framework-bound code so simple that it's "obviously correct" and doesn't need tests.

### What structural pattern he would prescribe

**Humble Object** (as formalized by Gerard Meszaros in xUnit Test Patterns, which Beck influenced). The pattern:

1. Create a plain C# class (`OrderListLogic`) that contains all the conditional logic, decision-making, and state transitions.
2. Test-drive this class with pure unit tests — no Blazor, no bUnit, no renderer.
3. The `.razor` component becomes the Humble Object: it receives the output of the logic class and renders it. It is so thin that it does not need unit tests (it is covered by integration/acceptance tests, or, as Beck says, "eyes should play a role of test runner" for pure rendering).

### Direct quote

> "If I write non-deterministic tests, if I write slow tests, if I write tests that are coupled to the structure of the system, if it takes me forever to write the test setup, then TDD will just be annoying. I think these are all software design issues, not testing issues." — _TDD Prerequisites_ (2023)

> "Your goal is to limit the time you spend debugging to the absolute minimum." — _Advanced TDD Workshop with Uncle Bob_ (recap, 2016)

---

## 4. Michael Feathers

### What he would criticize

Inline C# in `.razor` files creates code with **no seams**. Feathers defines a seam as:

> "A seam is a place where you can alter behavior in your program without editing in that place." — _Working Effectively with Legacy Code_, Ch. 4

When business logic is embedded in `@if (order.Total > 1000 && customer.IsPremium)` inside a `.razor` file, there is no seam. You cannot:

- Substitute a different discount rule without editing the `.razor` file.
- Test the discount rule in isolation — you must render the entire component.
- Sense (observe) the intermediate decision — you can only observe the final rendered HTML.

Feathers' concept of **sensing and separation** (Ch. 3) says you need to be able to (a) sense the values the code computes, and (b) separate the code from its dependencies to get it into a test harness. A `.razor` code block provides neither: the computed values are invisible except through the render tree, and the dependencies (component parameters, injected services, JS interop) are woven into the Blazor lifecycle.

Feathers would also note that a component with mixed markup and logic is a **seamless monolith** that resists incremental refactoring. If you later need to extract the discount logic into a shared service, you cannot do it safely without characterization tests that pin down the current behavior — and those tests are impossible to write without first extracting the logic.

### What structural pattern he would prescribe

**Object Seams** through dependency injection and extracted logic classes. Feathers identifies object seams as "the best choice in object-oriented languages" (Ch. 4, Seam Types). The prescription:

1. Extract every decision-making method from the `@code` block into a separate, injectable C# class (e.g., `IOrderPolicy`).
2. The `.razor` component depends on the interface, not the implementation.
3. In tests, substitute a test double that returns predetermined decisions, allowing you to test the rendering independently of the business rules.
4. Test the extracted `OrderPolicy` class with pure unit tests — no Blazor, no rendering.

Additionally, use **sensing variables** (his temporary technique) during extraction: introduce properties that expose computed decisions so you can write pin-down tests before refactoring.

### Direct quote

> "One of the things that nearly everyone notices when they try to write tests for existing code is just how poorly suited code is to testing. ... The only ways to end up with an easily testable program are to write tests as you develop it or spend a bit of time trying to 'design for testability.'" — _Working Effectively with Legacy Code_, Ch. 4

> "To create the Humble Dialog, we extract all the logic from the view component into a non-visual component that is testable via synchronous tests." — _The Humble Dialog Box_ (2002)

---

## 5. John Ousterhout

### What he would criticize

A `.razor` file that combines markup with `@if` business logic is a **shallow module**. Ousterhout defines module depth as:

```
Module Value = Functionality / Interface Complexity
```

The interface of a Blazor component includes its parameters, the injected services it requires, the events it raises, the render fragments it accepts, and the implicit contract of what markup it produces. A component with inline business logic has an interface nearly as complex as its implementation — every `@if` branch is effectively a documented (or undocumented) behavioral contract. You cannot understand the component's behavior from its interface alone; you must read the implementation.

Ousterhout would also flag **information leakage**: when business rules like "free shipping on orders over $50" are embedded in multiple `.razor` files, the rule leaks across the codebase. A change to the threshold requires editing multiple files, each in a different component context.

He would further identify this as **tactical programming** — the short-sighted approach of putting logic "where it's convenient" (next to the button that triggers it) rather than where it belongs architecturally. This accumulates complexity incrementally until the system becomes unchangeable.

### What structural pattern he would prescribe

**Deep modules** through **pulling complexity downward** (Ch. 8). The principle: "It is more important for a module to have a simple interface than a simple implementation."

For a Blazor component, the prescription is:

1. The `.razor` file should be a shallow shell — a thin adapter between the UI framework and a deep logic module.
2. Create a deep module (a C# class or set of classes) that encapsulates all the conditional logic, state management, and business rules. This module has a small, well-defined interface (a few methods with clear contracts) and a substantial implementation.
3. The `.razor` component's "interface" to the rest of the system becomes simple: it takes a few parameters, delegates to the deep module, and renders the result.
4. The deep module can be understood, tested, and modified independently of any UI framework.

The deep module approach also addresses Ousterhout's red flag of **repetition**: "If the same piece of code (or code that is almost the same) appears over and over again, that's a red flag that you haven't found the right abstractions."

### Direct quote

> "The best modules are deep: they have a lot of functionality hidden behind a simple interface. A deep module is a good abstraction because only a small fraction of its internal complexity is visible to its users." — _A Philosophy of Software Design_, Ch. 4

> "Shallow module: A shallow module is one whose interface is complicated relative to the functionality it provides. Shallow modules don't help much in the battle against complexity, because the benefit they provide (not having to learn about how they work internally) is negated by the cost of learning and using their interfaces." — _A Philosophy of Software Design_, Ch. 4

---

## 6. Sandi Metz

### What he would criticize

Inline C# logic in `.razor` files creates **duplicated conditional logic** across components, but more importantly, it creates a situation where developers feel pressure to extract the duplication prematurely — before they understand the right abstraction. Metz's key insight, drawn from decades of object-oriented design, is:

> "duplication is far cheaper than the wrong abstraction." — _The Wrong Abstraction_ (2016)

When a developer sees `@if (order.Status == "Pending")` repeated in three `.razor` files, the instinct is to extract it. But if the extraction is done before understanding _why_ the condition varies (is it always the same business rule? does it have the same future?) the resulting abstraction will be wrong. It will accumulate conditionals and parameters, becoming harder to understand than the original duplication.

Metz would also flag the **behemoth component** problem: components that start simple but accumulate conditionals one change at a time. She warns: "If important classes in your domain change often, get bigger every time they change, and are accumulating conditionals, stop adding to them right now."

### What structural pattern she would prescribe

**Shameless Green → Small Objects**: Metz's approach is to tolerate duplication until the pattern reveals itself, then extract small, focused objects that collaborate through message-passing.

1. **Start with "shameless green"**: Let the `@if` logic exist. Don't abstract it. Let the duplication accumulate until you have enough examples to see the real pattern. "When we see all of the duplicate elements in front of us, we have a much better chance of coming up with a proper name for that duplication."

2. **Extract small objects**: When the pattern is clear, extract each variant into its own small class. In Blazor terms, this means creating focused component variants or logic classes, each handling one case. Don't create a monolithic `OrderActionPolicy` with a dozen parameters — create `PendingOrderActions`, `ShippedOrderActions`, etc.

3. **Use message-passing (polymorphism) over conditionals**: Replace `@if (status == X)` with polymorphic dispatch. Inject the right strategy object for the current state. "Message sending makes it easy to change behavior by swapping in new parts."

4. **Resist the sunk-cost fallacy**: If an extracted abstraction becomes wrong, inline it back. "When dealing with the wrong abstraction, the fastest way forward is back."

### Direct quote

> "duplication is far cheaper than the wrong abstraction." — _The Wrong Abstraction_ (2016)

> "I resist the urge to abstract these similarities at this moment. While I understand the DRY principle, I know it's better to keep the duplication now, because I expect to uncover more information about this algorithm as I continue refactoring." — _All the Little Things_, RailsConf 2014

---

## Convergence: The Unified Prescription

All six authorities converge on the same structural answer, from different angles:

| Authority        | Starting Concern                  | Prescribed Pattern                                                     |
| ---------------- | --------------------------------- | ---------------------------------------------------------------------- |
| Uncle Bob        | Separation of concerns, SRP       | Humble Object / Presenter + Clean Architecture layers                  |
| Martin Fowler    | Separated Presentation            | Presentation Model / Passive View                                      |
| Kent Beck        | TDD testability                   | Humble Object (extract logic from framework-bound code)                |
| Michael Feathers | Seams for safe change             | Object seams via DI + extracted logic classes                          |
| John Ousterhout  | Module depth                      | Deep modules (simple interface, substantial hidden implementation)     |
| Sandi Metz       | Duplication vs. wrong abstraction | Small polymorphic objects; tolerate duplication until pattern is clear |

The concrete Blazor implementation that satisfies all six:

```
Component.razor        ← Humble View (markup only, binds to ComponentViewModel)
ComponentViewModel.cs  ← Presentation Model (flat properties, all display decisions pre-computed)
ComponentPolicy.cs     ← Deep logic module (business rules, conditionals, state transitions)
```

The `.razor` file should contain zero `@if` statements that encode business or presentation logic. It should only contain rendering decisions expressed as `@if (Model.IsVisible)` where `IsVisible` was computed elsewhere. The `@code` block, if present at all, should contain only lifecycle method calls that delegate to the ViewModel and trivial event handlers that forward to the policy class.

### The Razor @code litmus test

A `.razor` `@code` block passes the test if and only if every line in it could be replaced by a property on a ViewModel or a method call on an injected service, and the component's behavior would be unchanged. If removing a line from `@code` would change **what decision is made** (not just how it's rendered), that line should not be in the `.razor` file.
