# PowerShell Copilot Instructions

This file contains extracted PowerShell-specific rules from the main copilot-instructions.md. For general rules, see copilot-instructions.md.

## Critical Rules

- **PowerShell Script Usage**: To address incomplete AI responses in large or complex codebases, use PowerShell scripts when:
  1. **Scale**: Task spans numerous files or extensive code (e.g., renaming hundreds of variables across files).
     - *Why*: Scripts save time on large tasks.
  2. **Repetition**: Actions are repeated across multiple locations (e.g., applying naming conventions).
     - *Why*: Automation reduces errors in repetitive tasks.
  3. **Consistency**: Uniform standards are required (e.g., enforcing file naming).
     - *Why*: Scripts ensure consistent application.
  4. **Complexity**: Logic exceeds manual or basic tool capabilities (e.g., dependency checks).
     - *Why*: Scripts handle intricate logic efficiently.
  5. **Efficiency**: Automation significantly outperforms other methods (e.g., analyzing large logs).
     - *Why*: Scripts improve speed and accuracy.
  Reserve scripts for these scenarios, avoiding them for simple tasks where manual methods suffice. If unsure, ask for clarification to ensure the best approach.

## Important Rules

- **Scripts Location**: `scripts/` for PowerShell automation.

## Best Practices/General Guidelines

- **Coverage Scripts**: Use `scripts/Generate-CoverageReport.ps1`, `scripts/View-CoverageReport.ps1`. Outputs HTML, XML, JSON, Cobertura to `coverage/`.
