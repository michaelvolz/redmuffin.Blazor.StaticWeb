---
applyTo: "**/*.{ps,ps1,psm}"
---

# Project coding standards

## General PowerShell Best Practices
- Always use `PascalCase` for function names (e.g., `Get-UserInfo`) and `camelCase` for variables.
- Follow the verb-noun convention in function names using approved verbs from `Get-Verb`.
- Write functions with `CmdletBinding()` and proper parameter blocks.
- Avoid aliases (e.g., use `Get-ChildItem`, not `gci`) for clarity and cross-platform compatibility.
- Use `Write-Verbose`, `Write-Output`, and `Write-Error` appropriately based on the context.

## Code Style and Structure
- Use Tabs for indentation�be consistent across scripts.
- Avoid one-liners for complex logic; prefer clarity over brevity.
- Use `Begin`, `Process`, `End` blocks when writing advanced functions.
- Prefer `param()` blocks with type constraints and default values.

## Security Best Practices
- Avoid hardcoding credentials; use `Get-Credential` or Windows Credential Manager.
- Always validate and sanitize input from users or external sources.
- Use `-ErrorAction Stop` when handling critical operations to catch and handle errors properly.
- Avoid invoking external executables without validating paths or input.

## Tooling & Modules
- Prefer using modules (`.psm1`) over large scripts when sharing functions.
- Group related functions into modules with a clear `Export-ModuleMember` section.
- Use comment-based help for all public functions (`.SYNOPSIS`, `.DESCRIPTION`, `.EXAMPLE`, `.PARAMETER`, `.OUTPUTS`).

## Testing and Validation
- Use Pester for testing PowerShell functions and scripts.
- Follow the Arrange-Act-Assert structure in Pester tests.
- Validate scripts with `Invoke-ScriptAnalyzer` and fix violations.

## Reusability and Maintainability
- Break large scripts into reusable functions or modules.
- Use `Try/Catch/Finally` for error handling instead of checking `$?` or `$LASTEXITCODE`.
- Comment and document non-trivial logic using inline comments.
- Avoid excessive pipeline chaining�use intermediate variables when needed.

## Clean Output
- Always return structured data (e.g., hashtables or custom objects), not raw strings.
- Avoid unnecessary `Write-Host` (use `Write-Output` or return values).
- Do not use `Write-Output` for logging�use `Write-Verbose` or `Write-Information`.

## Performance Considerations
- Use `[ordered]` and `[hashtable]` appropriately when working with key-value pairs.
- Avoid unnecessary loops; leverage the pipeline and built-in cmdlets like `ForEach-Object`.
- Use `Where-Object` and `Select-Object` efficiently to reduce memory and improve readability.