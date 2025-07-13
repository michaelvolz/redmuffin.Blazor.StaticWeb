---
applyTo: '**/*.{ps1,psm1,psd1}'
description: 'Consolidated PowerShell scripting and cmdlet best practices for AI coding assistants.'
---

# PowerShell Scripting and Cmdlet Best Practices

## General Best Practices
- Use `PascalCase` for function names and public variables; `camelCase` for private variables.
- Follow the verb-noun convention in function names using approved verbs (`Get-Verb`).
- Avoid aliases in scripts; use full cmdlet names for cross-platform compatibility.
- Write functions with `CmdletBinding()` and proper parameter blocks.
- Use `Write-Verbose`, `Write-Output`, and `Write-Error` appropriately.

## Code Style
- Use tabs for indentation; maintain consistency.
- Use `Begin`, `Process`, `End` blocks for advanced functions.
- Opt for `param()` blocks with type constraints and default values.
- Opening braces on same line as statement; closing braces on new line.
- Use line breaks after pipeline operators.
- Comment and document non-trivial logic using inline comments.
- Avoid excessive pipeline chaining—use intermediate variables when needed.

## Security
- Avoid hardcoding credentials; prefer `Get-Credential`.
- Validate and sanitize all input from users or external sources.

## Tooling and Modules
- Use modules (`.psm1`) for function sharing; include `Export-ModuleMember`.
- Incorporate comment-based help with `.SYNOPSIS`, `.DESCRIPTION`, `.EXAMPLE`, `.PARAMETER`, `.OUTPUTS`.

## Testing and Validation
- Leverage Pester for testing; follow Arrange-Act-Assert structure.
- Use `Invoke-ScriptAnalyzer` to validate scripts.

## Parameter Design
- Use common parameter names (`Path`, `Name`, `Force`) following PowerShell conventions.
- Use `[switch]` for boolean flags; avoid `$true`/`$false` parameters.
- Use common .NET types; implement proper validation with `ValidateSet`, `ValidateNotNullOrEmpty`.
- Enable tab completion where possible.
- Use singular form unless always multiple; choose clear, descriptive names.

## Performance
- Prefer structured data returns, avoid raw strings.
- Avoid unnecessary loops; optimize `Where-Object`, `Select-Object`.
- Use `[ordered]` and `[hashtable]` appropriately when working with key-value pairs.
- Avoid collecting large arrays; use process block for streaming.
- Enable immediate processing for pipeline operations.

## Error Handling
- Implement `ShouldProcess` with appropriate `ConfirmImpact`; use `ShouldContinue()` for additional confirmations.
- Use `try/catch/finally` for error management instead of checking `$?` or `$LASTEXITCODE`.
- Set appropriate `ErrorAction` preferences; handle terminating vs non-terminating errors properly.
- Use meaningful error messages and `ErrorVariable` when needed.
- Avoid interactive designs; accept input via parameters and support automation.

## Documentation
- Include comment-based help for public functions.
- Ensure consistent formatting and clear, descriptive naming.

## Pipeline and Output
- Support `ValueFromPipeline` and `ValueFromPipelineByPropertyName` in cmdlets; document pipeline input requirements.
- Return rich objects, use `PSCustomObject` for structured data.
- Default to no output for action cmdlets; implement `-PassThru` switch for object return.
- Avoid `Write-Host` for data output; use `Write-Output` for data, `Write-Verbose` for details.
- Do not use `Write-Output` for logging—use `Write-Verbose` or `Write-Information`.
- Use `Write-Warning` for warning conditions; `Write-Error` for non-terminating errors.

## Examples
### Basic Function Example
```powershell
function Get-UserProfile {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Username,
        [Parameter()]
        [ValidateSet('Basic', 'Detailed')]
        [string]$ProfileType = 'Basic'
    )
    process {
        # Logic here
    }
}
```

### Advanced Cmdlet Example
```powershell
function New-Resource {
    [CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
    param(
        [Parameter(Mandatory = $true, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$Name,
        [Parameter()]
        [ValidateSet('Development', 'Production')]
        [string]$Environment = 'Development'
    )
    begin {
        Write-Verbose "Starting resource creation process"
    }
    process {
        try {
            if ($PSCmdlet.ShouldProcess($Name, "Create new resource")) {
                # Resource creation logic here
                Write-Output ([PSCustomObject]@{ Name = $Name; Environment = $Environment; Created = Get-Date })
            }
        } catch {
            Write-Error "Failed to create resource: $_"
        }
    }
    end {
        Write-Verbose "Completed resource creation process"
    }
}
```
