---
title: Analysis of Useless or Counterproductive Rules
date: 2025-08-05
---

This file lists rules from the attached files that I judge to be redundant (because I would already apply them 100% based on my built-in knowledge of C# 13, .NET 9, TDD with TUnit/bUnit/LightMock.Generator, and general best practices) or counterproductive (e.g., they might overcomplicate simple tasks, enforce outdated/rigid patterns, or conflict with efficient AI reasoning). Only flagged rules are included, with brief reasoning for each.

## Redundant Rules (Already Applied Instinctively)

- **.codellm/rules/csharp-13-features.mdc**: I inherently use C# 13 features like primary constructors and collection expressions in appropriate contexts without needing explicit rules.
- **.codellm/rules/csharp-async-calls.mdc**: Using ConfigureAwait(false) in async calls (except in asserts) is a standard deadlock-prevention practice I always follow.
- **.codellm/rules/csharp-idisposable.mdc**: Implementing IDisposable and using 'using' statements for resource disposal is fundamental and automatic in my code suggestions.
- **.codellm/rules/csharp-member-order.mdc**: Ordering members (Fields → Properties → Constructors → Methods, by accessibility) is a default convention I adhere to for readability.
- **.codellm/rules/csharp-null-checks.mdc**: ArgumentNullException.ThrowIfNull for parameters is a core null-safety pattern I apply universally.
- **.codellm/rules/csharp-using-statements.mdc**: Proper organization and ordering of using statements (e.g., System first, alphabetical) is something I do by default to maintain clean code.
- **.codellm/rules/csharp-whitespace.mdc**: Rules like no trailing whitespace, brace placement, and no multiple blank lines are basic formatting I enforce instinctively.
- **.codellm/rules/dotnet-best-practices.mdc**: SOLID principles, exception handling, and modularity are ingrained in my .NET development knowledge.
- **.codellm/rules/dotnet-di-constructor-injection.mdc**: Mandatory constructor injection with null checks is a standard DI practice I always recommend.
- **.codellm/rules/dotnet-httpclient-injection.mdc**: Injecting HttpClient only when necessary to avoid bloat is obvious and part of efficient design.
- **.codellm/rules/dotnet-service-lifetimes.mdc**: Choosing appropriate service lifetimes (Scoped/Transient/Singleton) is fundamental DI knowledge I apply contextually.
- **.codellm/rules/general-best-practices.mdc**: Basics like modularity and exception handling are core to all my coding assistance.
- **.codellm/rules/local-testing.mdc**: Running local tests before changes is a standard part of my TDD workflow recommendations.
- **.codellm/rules/pre-commit-testing.mdc**: Ensuring tests pass before commits is inherent to any TDD process I advocate.
- **.codellm/rules/testing-naming.mdc**: Test naming like Component_Behavior_ExpectedOutcome is a common convention I use without prompting.
- **.codellm/rules/testing-structure.mdc**: Arrange-Act-Assert structure is the default for all tests I suggest.
- **.codellm/rules/testing-tunit-assertions.mdc**: Chaining assertions and using Assert.Multiple is basic TUnit practice I follow.
- **.codellm/rules/testing-workflow.mdc**: Red-Green-Refactor is the core TDD cycle I always promote.
- **.codellm/rules/uistyling-javascript-minimization.mdc**: Minimizing JavaScript in Blazor projects (favoring C#) is a natural best practice for performance.
- **.codellm/rules/uistyling-performance.mdc**: Techniques like lazy loading and virtualization are standard UI optimizations I recommend.

## Counterproductive Rules (Potentially Harmful or Overly Rigid)

- **.codellm/rules/unclear-items.mdc**: This seems to handle ambiguous or unclear instructions, but it could encourage vagueness in responses or lead to inefficient back-and-forth, conflicting with my goal of clear, proactive problem-solving.

This list is based on my evaluation; other rules provide beneficial project-specific guidance that enhances my work without redundancy.
