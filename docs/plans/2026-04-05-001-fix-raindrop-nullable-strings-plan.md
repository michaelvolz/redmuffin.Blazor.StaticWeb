---
title: fix: Make RaindropItem string properties nullable to handle null API responses
type: fix
status: completed
date: 2026-04-05
---

# fix: Make RaindropItem string properties nullable to handle null API responses

## Overview

Fix deserialization exceptions in RaindropItem.cs by making string properties nullable when the Raindrop API may send null values.

## Problem Frame

The RaindropItem class has non-nullable string properties with string.Empty defaults. If the API sends null for these fields, System.Text.Json will throw an exception during deserialization, breaking the integration.

## Requirements Trace

- R1. RaindropItem deserialization should not throw on null string values from API
- R2. Maintain backward compatibility with existing non-null data

## Scope Boundaries

- Only modify string properties in RaindropItem.cs
- Do not change other types or add custom converters unless necessary

## Context & Research

### Relevant Code and Patterns

- Existing: src/redmuffin.Blazor.StaticWeb.Common/Raindrop/RaindropItem.cs uses non-nullable strings with defaults
- Pattern: .NET nullable reference types for optional API fields

### Institutional Learnings

- None directly relevant

### External References

- System.Text.Json best practices: Make properties nullable (string?) when APIs can return null to avoid JsonException
- .NET documentation on nullable annotations and deserialization

## Key Technical Decisions

- Change string to string? for properties that can be null from API
- Remove default values since nullable types can be null

## Implementation Units

- [x] **Unit 1: Update RaindropItem properties**

**Goal:** Make string properties nullable and remove defaults to handle null API responses

**Requirements:** R1, R2

**Dependencies:** None

**Files:**

- Modify: src/redmuffin.Blazor.StaticWeb.Common/Raindrop/RaindropItem.cs
- Test: tests/redmuffin.Blazor.StaticWeb.Common/RaindropItemTests.cs

**Approach:**

- Change string properties to string?
- Remove = string.Empty assignments

**Patterns to follow:**

- .NET nullable reference types for optional fields

**Test scenarios:**

- Happy path: Deserialize JSON with all non-null strings -> properties set correctly
- Edge case: Deserialize JSON with null strings -> properties set to null
- Error path: Deserialize invalid JSON -> throws appropriate exception

**Verification:**

- Unit tests pass for all scenarios
- No deserialization exceptions on null inputs

## Risks & Dependencies

| Risk                                         | Mitigation                                    |
| -------------------------------------------- | --------------------------------------------- |
| Breaking change if consumers expect non-null | Add null checks in consuming code if needed   |
| API behavior change                          | Monitor API responses for actual null sending |

## Sources & References

- Related code: src/redmuffin.Blazor.StaticWeb.Common/Raindrop/RaindropItem.cs
