---
mode: 'agent'
description: 'Generating a Product Requirements Document (PRD)'
---
# Generating a Product Requirements Document (PRD)

## Goal

To guide an AI assistant in creating a detailed Product Requirements Document (PRD) in Markdown format, based on an initial user prompt. The PRD should be clear, actionable, and suitable for a junior developer to understand and implement the feature in a Blazor WebAssembly .NET 9 application.

## Process

1.  **Receive Initial Prompt:** The user provides a brief description or request for a new feature or functionality.
2.  **Ask Clarifying Questions:** Before writing the PRD, the AI *must* ask clarifying questions to gather sufficient detail. The goal is to understand the "what" and "why" of the feature, not necessarily the "how" (which the developer will figure out).
3.  **Generate PRD:** Based on the initial prompt and the user's answers to the clarifying questions, generate a PRD using the structure outlined below.
4.  **Save PRD:** Save the generated document as `PRD-XXX-ShortTitle.md` inside the `/tasks` directory, where:
    - `XXX` is a three-digit number that increments based on the highest numbered PRD file in the target folder
    - `ShortTitle` is a concise, descriptive title derived from the feature (e.g., `AuthSystem`, `PaymentModule`)
    - If no PRD files exist, start with `001`

## Clarifying Questions (Examples)

The AI should adapt its questions based on the prompt, but here are some common areas to explore:

*   **Problem/Goal:** "What problem does this feature solve for the user?" or "What is the main goal we want to achieve with this feature?"
*   **Target User:** "Who is the primary user of this feature?"
*   **Core Functionality:** "Can you describe the key actions a user should be able to perform with this feature?"
*   **User Stories:** "Could you provide a few user stories? (e.g., As a [type of user], I want to [perform an action] so that [benefit].)"
*   **Acceptance Criteria:** "How will we know when this feature is successfully implemented? What are the key success criteria?"
*   **Scope/Boundaries:** "Are there any specific things this feature *should not* do (non-goals)?"
*   **Data Requirements:** "What kind of data does this feature need to display or manipulate?"
*   **Design/UI:** "Are there any existing design mockups or UI guidelines to follow?" or "Can you describe the desired look and feel?"
*   **Edge Cases:** "Are there any potential edge cases or error conditions we should consider?"
*   **API Integration:** "Does this feature require API endpoints? Should they be implemented as Azure Functions?"
*   **Client-Side Storage:** "Does this feature need to persist data locally using browser storage?"

## PRD Structure

The generated PRD should include the following sections:

1.  **Introduction/Overview:** Briefly describe the feature and the problem it solves. State the goal.
2.  **Goals:** List the specific, measurable objectives for this feature.
3.  **User Stories:** Detail the user narratives describing feature usage and benefits.
4.  **Functional Requirements:** List the specific functionalities the feature must have. Use clear, concise language (e.g., "The system must allow users to upload a profile picture."). Number these requirements.
5.  **Non-Goals (Out of Scope):** Clearly state what this feature will *not* include to manage scope.
6.  **Design Considerations:** Link to mockups, describe UI/UX requirements using Zurb Foundation framework, mention relevant Blazor components/styles if applicable.
7.  **Technical Considerations:** Mention any known technical constraints, dependencies, or suggestions specific to Blazor WebAssembly .NET 9:
    *   Blazor component structure (`.razor` files with code-behind `.razor.cs`)
    *   Integration with existing feature-based architecture (`src/redmuffin.Blazor.StaticWeb/Features/`)
    *   Azure Functions API endpoints if needed (`src/redmuffin.Blazor.StaticWeb.Api/`)
    *   Client-side storage using `Blazored.LocalStorage` or `IJSRuntime`
    *   Use of `HttpClient` for API calls
    *   SCSS styling with Zurb Foundation using `@use` directives
    *   Feature-based SCSS organization under `wwwroot/scss/features/`
8.  **Success Metrics:** How will the success of this feature be measured? (e.g., "Increase user engagement by 10%", "Reduce support tickets related to X").
9.  **Implementation Notes:** Blazor-specific guidance:
    *   Component placement in feature directories
    *   Parameter binding and event callbacks
    *   Lifecycle methods (`OnInitializedAsync`, `OnParametersSetAsync`)
    *   State management approaches
    *   Testing considerations using TUnit framework with `[Test]` attribute
    *   Mocking with LightMock.Generator (NSubstitute deprecated)
    *   Code quality standards (StyleCop/Meziantou analyzers)
    *   Async/await patterns with `ConfigureAwait(false)`
10. **Open Questions:** List any remaining questions or areas needing further clarification.

## Target Audience

Assume the primary reader of the PRD is a **junior developer** familiar with Blazor WebAssembly and .NET 9. Requirements should be explicit, unambiguous, and leverage Blazor-specific patterns and conventions. Provide enough detail for them to understand the feature's purpose, core logic, and integration points within the existing application architecture.

## Technology Context

This PRD is for a Blazor WebAssembly .NET 9 application with the following characteristics:
*   **Frontend:** Blazor WebAssembly with Zurb Foundation for UI
*   **Backend:** Azure Functions (.NET 8) for API endpoints
*   **Testing:** TUnit framework with `[Test]` attribute (NOT NUnit/xUnit/MSTest)
*   **Mocking:** LightMock.Generator ONLY (NSubstitute deprecated)
*   **Architecture:** Feature-based organization under `src/redmuffin.Blazor.StaticWeb/Features/`
*   **Styling:** SCSS with Foundation framework using `@use` directives
*   **Storage:** Browser-based storage via `Blazored.LocalStorage` and `IJSRuntime`
*   **Build:** .NET 9 with WebAssembly optimizations (`WasmStripILAfterAOT=true`, `InvariantGlobalization=true`, `PublishTrimmed=true`)
*   **Code Quality:** Zero build warnings policy (except IL2111), StyleCop/Meziantou analyzers enforced
*   **Project Structure:** 
    - Main Blazor app: `src/redmuffin.Blazor.StaticWeb/`
    - Azure Functions API: `src/redmuffin.Blazor.StaticWeb.Api/`
    - Shared models/DTOs: `src/redmuffin.Blazor.StaticWeb.Common/`
    - Tests: `tests/redmuffin.Blazor.StaticWeb.Tests/` and `tests/redmuffin.Blazor.StaticWeb.Api.Tests/`

## Output

*   **Format:** Markdown (`.md`)
*   **Location:** `/tasks/`
*   **Filename:** `PRD-XXX-ShortTitle.md` (e.g., `PRD-003-AuthSystem.md`)

## Final instructions

1. Do NOT start implementing the PRD
2. Make sure to ask the user clarifying questions
3. Take the user's answers to the clarifying questions and improve the PRD
4. Ensure all technical considerations align with Blazor WebAssembly and the existing project structure
