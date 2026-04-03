## Introduction/Overview

This document outlines the requirements for a PowerShell script named `DisplayWarnings.ps1`. The script is intended to improve development efficiency by isolating and displaying all warnings generated during the build process.

## Goals

- Run `dotnet clean` and `dotnet build` in the root directory of the repository.
- Collect and summarize all warnings in a human-readable format.
- Hide build output and only show concise warnings summary.
- Display warnings sorted by frequency.
- Highlight IL\* warnings in a different color at the end.
- Enhance user engagement with fancy formatting and emojis.

## User Stories

- **As a developer**, I want to see a summary of build warnings so that I can prioritize fixes without sifting through all output.
- **As a project manager**, I want to ensure that IL\* warnings are addressed separately and visibly.

## Functional Requirements

1. The script must run `dotnet clean` followed by `dotnet build` in the root directory.
2. It must suppress and hide the full build output.
3. It must capture warnings and format them in a summary, using emojis for clarity.
4. It should sort warnings by frequency and display IL\* warnings separately at the bottom with a softened color.

## Non-Goals (Out of Scope)

- The script will not fix the warnings; it merely reports them.
- It will not display errors or other messages.

## Design Considerations

- Use emojis to enhance readability in the console.
- Ensure compatibility with PowerShell 7.5.2 and Windows environment.
- Output formatting with color differentiation for IL\* warnings.

## Technical Considerations

- Must utilize PowerShell's capability to handle command execution and text processing.
- Optimize script for performance to reduce additional build time overhead.

## Success Metrics

- Reduction in time spent identifying and fixing build warnings.
- Increased visibility of recurring warnings.

## Implementation Notes

- The script will be placed in the `/scripts/` directory in the repository.
- Execution must be initiated at the project root for correct path context.

## Open Questions

- Are there specific formatting preferences for the emoji-enhanced output?
- Should there be options to filter or exclude certain warnings?
