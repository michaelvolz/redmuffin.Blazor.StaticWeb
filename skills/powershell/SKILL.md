---
name: powershell
description: PowerShell script automation patterns, coverage scripts, and when to use scripts for complex or repetitive tasks.
invocable: false
---

# PowerShell

## When to Use Scripts

Use PowerShell scripts when:
1. **Scale**: Task spans numerous files or extensive code (e.g., renaming hundreds of variables)
2. **Repetition**: Actions are repeated across multiple locations
3. **Consistency**: Uniform standards are required
4. **Complexity**: Logic exceeds manual or basic tool capabilities
5. **Efficiency**: Automation significantly outperforms other methods

Reserve scripts for these scenarios, avoiding them for simple tasks.

## Scripts Location

`scripts/` for PowerShell automation

## Coverage Scripts

- `scripts/Generate-CoverageReport.ps1`
- `scripts/View-CoverageReport.ps1`

Outputs: HTML, XML, JSON, Cobertura to `coverage/`
