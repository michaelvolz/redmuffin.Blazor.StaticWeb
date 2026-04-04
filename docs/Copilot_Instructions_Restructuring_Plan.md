---
title: Copilot Instructions Restructuring Plan
date: 2025-08-05
---

## Super Plan: Restructuring the Copilot Instructions File

### Overall Goals and Principles

- **Purpose**: Restructure the file to make it more modular, prioritized, and AI-friendly. This reduces errors by ensuring critical rules are prominent, code-specific details are isolated (to avoid overwhelming the general file), and important sections (like Testing) are self-contained for easy reference.
- **Key Benefits**:
  - Easier for AI to parse without missing details.
  - Prioritization ensures "Critical" rules (e.g., zero build warnings) are addressed first.
  - Separation prevents the main file from becoming bloated while keeping related info grouped (e.g., all Testing in one file).
  - Total files: 1 main file + ~4-6 section-specific files (based on analysis: Testing (including Mocking), C#, .NET, PowerShell, UI/Styling – combined where logical to avoid fragmentation).
- **Sorting by Priority**: Within each file/section, organize content as:
  1. Critical rules (e.g., zero build warnings, strict async usage).
  2. Important rules (e.g., logging with LoggerMessage, null checks).
  3. Best practices/general guidelines (e.g., whitespace rules, file naming).
  - If no explicit priority is stated, infer based on emphasis in the original (e.g., "Strict rules" = Important).
- **File Structure Overview**:
  - **Main File: copilot-instructions.md** (streamlined version of original):
    - Retains general, non-code-specific rules (e.g., Project Structure, Security, File Organization, Markdown Standards, Tooling, Important Directives like pre-commit testing).
    - Sorted by priority: Start with Critical overarching rules, then Important, then others.
    - References to section files (e.g., "For Testing details, see testing-copilot-instructions.md").
    - Reduced size by ~60% via extraction and deduplication.
  - **Extracted Section Files** (at same level, e.g., in .github/ folder):
    - Each file starts with a brief intro (e.g., "This file contains extracted rules for [Section] from the main copilot-instructions.md").
    - Strictly separate: No overlap with main file. If a rule has code-specific examples, move it here.
    - Naming: Unique shortnames (e.g., "testing-copilot-instructions.md" for all Testing rules, keeping them together as a cohesive unit).
    - References: Only if non-confusing (e.g., "For related security rules, see main copilot-instructions.md").
    - Specific files based on analysis:
      - **testing-copilot-instructions.md**: All Testing rules (TDD, TestScope, categorization, TUnit assertions, and Mocking strategies – extracted entirely, including general principles and mocking, as per instruction to keep important info together and merge Mocking with Testing).
      - **csharp-copilot-instructions.md**: C#-specific coding standards (e.g., async calls with ConfigureAwait(false), null checks, usings, member order, IDisposable, logging with LoggerMessage, whitespace, analyzer rules).
      - **dotnet-copilot-instructions.md**: .NET-specific rules (e.g., dependency injection with constructor injection and [Inject], project configuration for .NET 9 Blazor WASM/Azure Functions).
      - **powershell-copilot-instructions.md**: PowerShell-specific rules (e.g., scripting for automation, start-script.md references).
      - **uistyling-copilot-instructions.md**: UI/Styling rules (e.g., Zurb Foundation, SCSS, accessibility – extracted as it's somewhat code-specific to Blazor).
      - (If needed, combine low-volume sections to avoid too many files; Mocking is now merged into Testing.)
- **Handling Overlaps/Edge Cases**:
  - Duplication: Avoid entirely via strict separation (e.g., if a general rule has a C# example, move the whole rule to csharp file).
  - Cross-References: Minimal; only add if it aids clarity without looping (e.g., no more than 1-2 per file).
  - File Format: All in Markdown, with sections, bullet points, and tables for readability (matching original style).
- **Validation Steps**: After restructuring, ensure no information loss by cross-checking against original. Test for AI usability by simulating rule application.
- **Potential Risks**: Fragmentation could confuse if references are overused; mitigated by minimal references and self-contained files. Total effort: Medium, as it involves careful extraction.

This plan ensures ALL information is preserved, with better organization and no mistakes from overload.

## Migration Document: Sequential Tasks

This is a simple, precise list of tasks to execute the super plan. Each task is standalone but sequential (do not skip). Notes are added only where needed for dependencies or clarity. Once you approve, I'll proceed to implement (using tools like edit for file changes).

1. **Backup Original File**: Create a copy of `.github/copilot-instructions.md` named `.github/copilot-instructions-original.md` to preserve the source. (Note: Dependency - ensures we can reference it during extraction.)

2. **Analyze and Categorize Content**: Read the entire original file and create a temporary mapping document (e.g., in-memory or a temp file) listing all rules/sections, their priorities (Critical/Important/General), and assignment to main file or extracted files. Flag any overlaps for resolution. (Note: This is foundational; spend time here to avoid info loss.)

3. **Create Main File Skeleton**: Edit `.github/copilot-instructions.md` to a blank skeleton with prioritized sections (e.g., Critical Rules first). Populate only with general rules from the mapping (e.g., Project Structure, Security, File Organization, Markdown, Tooling, Important Directives). Remove all code-specific and extracted sections.

4. **Extract Testing Section**: Create new file `.github/testing-copilot-instructions.md`. Copy ALL Testing-related content from original (e.g., TDD, TestScope, categorization, TUnit) into it, sorted by priority. Add intro and minimal references if needed. Remove this content from main file.

5. **Extract C# Section**: Create new file `.github/csharp-copilot-instructions.md`. Copy C#-specific rules (e.g., async, null checks, usings, logging) from original, sorted by priority. Add intro and minimal references. Remove from main file.

6. **Extract .NET Section**: Create new file `.github/dotnet-copilot-instructions.md`. Copy .NET-specific rules (e.g., DI, project config for Blazor/Azure) from original, sorted by priority. Add intro and minimal references. Remove from main file.

7. **Extract PowerShell Section**: Create new file `.github/powershell-copilot-instructions.md`. Copy PowerShell-specific rules (e.g., scripting, automation) from original, sorted by priority. Add intro and minimal references. Remove from main file.

8. **Merge Mocking into Testing Section**: Edit the existing `.github/testing-copilot-instructions.md` file to add Mocking rules from original, sorted by priority and integrated into the appropriate section. Add intro notes if needed. Remove this content from main file. (Note: As per update, merge instead of creating a separate file to keep Testing and Mocking together.)

9. **Extract UI/Styling Section**: Create new file `.github/uistyling-copilot-instructions.md`. Copy UI/Styling rules from original, sorted by priority. Add intro and minimal references. Remove from main file.

10. **Clean Up and Deduplicate**: Review all files for duplicates or missed overlaps. Ensure main file is streamlined. Add cross-references sparingly (e.g., in main file: "See testing-copilot-instructions.md for details").

11. **Validate Completeness**: Compare all new files + main file against original to ensure 100% coverage (no lost info). Check priorities and sorting in each file.

12. **Final Polish**: Format all files for readability (e.g., consistent Markdown). Test by simulating AI rule application on a sample task.
