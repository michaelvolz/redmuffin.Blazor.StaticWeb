---
title: Test Categorization Rules - Partial Class Organization
date: 2025-08-03
project: redmuffin.Blazor.StaticWeb
author: AI Assistant
version: 1.0
description: Decision matrix for organizing tests into partial class files by category
tags: [testing, partial-classes, organization, blazor, tunit]
---

# Test Categorization Rules - Partial Class Organization

## Overview

Organize TUnit tests into partial class files to improve maintainability and clarity. Each test class (e.g., `HomeTests`) should be split into multiple partial class files based on test categories.

## Partial Class File Structure

For a test class named `HomeTests`, create these partial class files:

- **`HomeTests.cs`** - Main file with basic functionality tests
- **`HomeTests.EdgeCases.cs`** - Error handling and edge case tests
- **`HomeTests.Infrastructure.cs`** - Framework and system-level tests
- **`HomeTests.Behavior.cs`** - User interaction and workflow tests
- **`HomeTests.Helpers.cs`** - TestScope, mocks, and utilities (keep existing pattern)

## Decision Flow (Apply in Order)

### 1. **[TestClass].EdgeCases.cs** - Check FIRST

**Place test in EdgeCases partial class if ANY of these are true:**

- Test name contains: `Error`, `Exception`, `Fail`, `Invalid`, `Null`, `Empty`, `Timeout`, `Malformed`, `Corrupt`
- Test uses: `Assert.Throws`, `ThrowsAsync`, `SetException`, `HttpRequestException`, `InvalidOperationException`
- Test setup contains: `CreateFailing*`, `WithFailing*`, `SetupFailure`, `SetupException`, `SetupThrows`
- Test validates: Error messages, exception handling, fallback behavior, graceful degradation
- Test inputs: Null values, empty collections, invalid data, extreme values

### 2. **[TestClass].Infrastructure.cs** - Check SECOND

**Place test in Infrastructure partial class if ANY of these are true:**

- Test name contains: `Lifecycle`, `Logging`, `Cache`, `Auth`, `DI`, `JSInterop`, `Serializ`, `Disposal`, `Memory`, `Event`
- Test validates: `OnInitialized`, `OnParametersSet`, `OnAfterRender`, `StateHasChanged`, `Dispose`
- Test checks: Log entries, event IDs, authentication state, dependency injection, JS calls
- Test uses: `CascadingValue`, `AuthenticationState`, `JSInterop`, `LocalStorage`, cache services
- Test focuses on: Framework behavior, system integration, resource management

### 3. **[TestClass].Behavior.cs** - Check THIRD

**Place test in Behavior partial class if ANY of these are true:**

- Test name contains: `Click`, `Submit`, `Change`, `Interaction`, `Workflow`, `Concurrent`, `Multiple`, `Rapid`
- Test uses: `ClickAsync`, `ChangeAsync`, `TriggerEventAsync`, `MouseEventArgs`, `ChangeEventArgs`
- Test performs: User interactions, form submissions, button clicks, input changes
- Test validates: State transitions, user workflows, interactive behavior
- Test setup: Multiple operations, concurrent tasks, user simulation

### 4. **[TestClass].cs** (Main File) - DEFAULT

**Place test in main partial class if NONE of the above conditions are met:**

- Basic rendering tests
- Simple property validation
- Standard "happy path" scenarios
- Basic structure verification
- Default state validation

## Examples

**HomeTests.EdgeCases.cs:**

- `Should_Handle_Null_Input_Gracefully`
- `Should_Throw_ArgumentException_When_Invalid`
- `Should_Display_Error_When_API_Fails`

**HomeTests.Infrastructure.cs:**

- `Should_Log_Initialization_Events`
- `Should_Dispose_Resources_Properly`
- `Should_Handle_Authentication_State`

**HomeTests.Behavior.cs:**

- `Should_Submit_Form_When_Button_Clicked`
- `Should_Handle_Concurrent_Operations`
- `Should_Update_State_On_Input_Change`

**HomeTests.cs (Main File):**

- `Should_Render_Successfully`
- `Should_Display_Correct_Title`
- `Should_Have_Required_Elements`

## Code Structure

All partial class files should use the same namespace and class declaration:

## Override Rule

If a test could fit multiple categories, prioritize in this order:

1. EdgeCases (error/exception scenarios)
2. Infrastructure (framework/system concerns)
3. Behavior (user interactions)
4. Main (everything else)
