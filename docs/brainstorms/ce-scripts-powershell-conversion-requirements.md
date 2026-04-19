---
date: 2026-04-19
topic: ce-scripts-powershell-conversion
---

# CE Scripts PowerShell Conversion

## Problem Frame

Compound Engineering (CE) skills contain scripts in various languages (shell, Python, JavaScript) that are primarily Linux-oriented and not cross-platform compatible. This prevents CE skills from working natively on Windows 11 without Windows Subsystem for Linux (WSL), and creates inconsistency across platforms including Omarchy Linux (a special Linux remix by DHH). The goal is to enable native Windows 11 operation and create a unified PowerShell-based scripting environment across all supported platforms.

## Requirements

**Script Identification**

- R1. Catalog all CE scripts: shell scripts (.sh), Python scripts (.py), JavaScript scripts (.mjs)
- R2. Found session-history-scripts: discover-sessions.sh (Linux-specific), extract-metadata.py, extract-skeleton.py, extract-errors.py (cross-platform Python)
- R3. Found other CE scripts: worktree-manager.sh, resolve-base.sh, check-health (Linux-specific), Python/JS scripts (cross-platform)

**Conversion Strategy**

- R4. Convert Linux-specific shell scripts (.sh) to PowerShell (.ps1) using cross-platform PowerShell cmdlets
- R5. Keep Python and JavaScript scripts unchanged (cross-platform)
- R6. Update agent configurations to call PowerShell scripts instead of bash where needed

**Cross-Platform Compatibility**

- R7. Verify converted PowerShell scripts work on Windows 11 and Omarchy Linux
- R8. Ensure Python scripts run with available Python installations
- R9. Maintain script functionality and error handling across platforms

## Success Criteria

- All CE skills execute their scripts natively on Windows 11 without requiring WSL
- Unified PowerShell scripting environment enables consistent behavior across Windows 11 and Omarchy Linux
- No functional regression in script capabilities
- Scripts pass basic validation tests on both platforms

## Scope Boundaries

- Convert script implementations only; do not modify CE skill logic or workflows
- Focus exclusively on compound-engineering skills; exclude other OpenCode plugins
- Maintain existing script interfaces and expected outputs

## Key Decisions

- **Full PowerShell Rewrite**: Shell scripts will be fully rewritten in PowerShell rather than using wrappers, to ensure native performance and eliminate dependency on original scripts
- **Cross-Platform Priority**: Favor PowerShell Core features and .NET Standard APIs over platform-specific code
- **Python Preservation**: Keep Python scripts as-is unless they contain platform-specific dependencies

## Dependencies / Assumptions

- PowerShell 7+ is installed on both Windows 11 and Omarchy Linux
- .NET runtime is available on target platforms
- Node.js remains available for any JavaScript scripts that cannot be converted

## Outstanding Questions

### Deferred to Planning

- [Affects R3][Technical] What PowerShell cmdlets and .NET APIs provide equivalents for common Unix commands used in CE scripts?
- [Affects R4][Needs research] Which Python scripts (if any) contain Linux-specific dependencies that require conversion?
- [Affects R6][Technical] How to handle file permissions and ownership differences between Windows and Linux in PowerShell?

## Next Steps

-> /ce/plan for structured implementation planning
