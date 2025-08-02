# Test Double Best Practices

## Introduction

This document outlines the findings from the analysis of current test double usage and provides best practice guidelines for naming and using test doubles in the codebase.

## Test Double Types and Naming Conventions

- **Mocks**: Used for behavior verification. Example naming: `Something_Mock`
- **Stubs/Fakes**: Used for state verification with predefined responses. Example naming: `Something_Stub`, `Something_Fake`
- **Spies**: Used for recording interactions while maintaining real functionality. Usually part of the main test class.
- **Dummies**: Used as placeholders to satisfy parameter requirements. Minimal implementation.

## Specific Examples and Best Practices

- **NavigationManagerMock**:
  - **Purpose**: Simplifies navigation testing with method interception
  - **Best Practice**: Suffix with `_Mock` for consistency
  - **Example**: `NavigationManager_Mock`

- **FailingHttpMessageHandler**:
  - **Purpose**: Simulates failing network conditions
  - **Best Practice**: Suffix with `_Stub`
  - **Example**: `FailingHttpMessageHandler_Stub`

- **TestLogger<T>**:
  - **Purpose**: Captures log messages for assertion
  - **Best Practice**: Despite name, acts as a Spy
  - **Example**: Should be `Logger_Spy` ("Test" prefix is redundant in test context)

## General Naming and Usage Guidelines

- **Consistent Suffix**: Always use suffixes (`_Mock`, `_Stub`, etc.) to clearly identify the role
- **Strategic Selection**: Use LightMock.Generator for 3rd party and external dependencies, custom mocks for internal components
- **Resource Management**: Utilize TestScope for fluent setup and disposal
- **Avoid NSubstitute**: Deprecated in favor of LightMock and custom implementations
- **Explicit Parameters**: Always specify all parameters when working with mocks to avoid common errors like CS0854
- **Partial Class Policy**: All test helpers should be placed inside the corresponding partial files, not separate helper files
- **Standards Compliance**: Follow set conventions to maintain clarity and ensure that all team members understand the test model

