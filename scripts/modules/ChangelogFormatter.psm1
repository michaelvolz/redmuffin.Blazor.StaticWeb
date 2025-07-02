# ChangelogFormatter.psm1
# Module for formatting changelog entries

function Get-FormattingConfig {
    <#
    .SYNOPSIS
    Loads the formatting configuration from the config file
    
    .PARAMETER ConfigPath
    Path to the configuration file
    
    .RETURNS
    Configuration object with formatting rules
    #>
    param(
        [Parameter(Mandatory = $false)]
        [string]$ConfigPath = "config/changelog-config.json"
    )
    
    try {
        if (Test-Path $ConfigPath) {
            $configContent = Get-Content $ConfigPath -Raw -Encoding UTF8
            $config = $configContent | ConvertFrom-Json
            return $config.formatting
        }
        else {
            Write-Warning "Configuration file not found at: $ConfigPath. Using default formatting."
            return $null
        }
    }
    catch {
        Write-Warning "Failed to load formatting config: $($_.Exception.Message). Using default formatting."
        return $null
    }
}

function Format-CommitEntry {
    <#
    .SYNOPSIS
    Formats a single commit entry according to the specified format
    
    .PARAMETER Commit
    The commit object with Hash and Message properties
    
    .PARAMETER Format
    The format string to use (default: "{message} ({hash})")
    
    .RETURNS
    Formatted string representing the commit entry
    #>
    param(
        [Parameter(Mandatory = $true)]
        [PSCustomObject]$Commit,
        
        [Parameter(Mandatory = $false)]
        [string]$Format = "{message} ({hash})"
    )
    
    # Escape any special Markdown characters in the commit message
    $escapedMessage = Escape-MarkdownText -Text $Commit.Message
    
    # Replace placeholders in the format string
    $formattedEntry = $Format -replace '\{message\}', $escapedMessage
    $formattedEntry = $formattedEntry -replace '\{hash\}', $Commit.Hash
    
    # Add date if available and format includes it
    if ($Commit.PSObject.Properties['Date'] -and $Format -match '\{date\}') {
        $formattedEntry = $formattedEntry -replace '\{date\}', $Commit.Date
    }
    
    return $formattedEntry
}

function Escape-MarkdownText {
    <#
    .SYNOPSIS
    Escapes special Markdown characters in text (minimal escaping for readability)
    
    .PARAMETER Text
    The text to escape
    
    .RETURNS
    Text with only critical Markdown special characters escaped
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text
    )
    
    # Only escape characters that would break the markdown structure
    # Remove most escaping for better readability in plain text
    $escapedText = $Text
    
    # Only escape backticks since they can break code formatting
    $escapedText = $escapedText -replace '`', '\`'
    
    return $escapedText
}

function Sort-CommitsChronologically {
    <#
    .SYNOPSIS
    Sorts commits in chronological order (newest first by default)
    
    .PARAMETER CommitList
    Array of commit objects to sort
    
    .PARAMETER Order
    Sort order: "descending" (newest first) or "ascending" (oldest first)
    
    .RETURNS
    Sorted array of commit objects
    #>
    param(
        [Parameter(Mandatory = $true)]
        [array]$CommitList,
        
        [Parameter(Mandatory = $false)]
        [ValidateSet("descending", "ascending")]
        [string]$Order = "descending"
    )
    
    # If commits have LineNumber property (from parsing), use that for ordering
    # Since git log --oneline outputs newest first, lower line numbers = newer commits
    if ($CommitList.Count -gt 0 -and $CommitList[0].PSObject.Properties['LineNumber']) {
        if ($Order -eq "descending") {
            # Newest first (lower line numbers first)
            return $CommitList | Sort-Object LineNumber
        }
        else {
            # Oldest first (higher line numbers first)
            return $CommitList | Sort-Object LineNumber -Descending
        }
    }
    else {
        # If no LineNumber, return as-is (assuming they're already in correct order)
        if ($Order -eq "ascending") {
            # Reverse the order for ascending
            [array]::Reverse($CommitList)
        }
        return $CommitList
    }
}

function Format-CategorySection {
    <#
    .SYNOPSIS
    Formats a category section with its commits
    
    .PARAMETER CategoryName
    Name of the category
    
    .PARAMETER CommitList
    Array of commits in this category
    
    .PARAMETER ConfigPath
    Path to the configuration file
    
    .RETURNS
    Formatted Markdown section for the category
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$CategoryName,
        
        [Parameter(Mandatory = $true)]
        [array]$CommitList,
        
        [Parameter(Mandatory = $false)]
        [string]$ConfigPath = "config/changelog-config.json"
    )
    
    if ($CommitList.Count -eq 0) {
        return ""
    }
    
    # Get formatting configuration
    $formattingConfig = Get-FormattingConfig -ConfigPath $ConfigPath
    $commitFormat = if ($formattingConfig -and $formattingConfig.commitFormat) { 
        $formattingConfig.commitFormat 
    } else { 
        "{message} ({hash})" 
    }
    
    # Sort commits chronologically
    $sortOrder = if ($formattingConfig -and $formattingConfig.sortOrder) { 
        $formattingConfig.sortOrder 
    } else { 
        "descending" 
    }
    $sortedCommits = Sort-CommitsChronologically -CommitList $CommitList -Order $sortOrder
    
    # Build the section
    $section = @()
    $section += ""  # Empty line before section
    $section += "### $CategoryName"
    $section += ""  # Empty line after heading
    
    foreach ($commit in $sortedCommits) {
        $formattedEntry = Format-CommitEntry -Commit $commit -Format $commitFormat
        $section += "- $formattedEntry"
    }
    
    return $section -join "`r`n"
}

function Format-ChangelogDocument {
    <#
    .SYNOPSIS
    Formats the complete changelog document
    
    .PARAMETER CategorizedCommits
    Hashtable with categories as keys and commit arrays as values
    
    .PARAMETER ConfigPath
    Path to the configuration file
    
    .RETURNS
    Complete formatted changelog as a string
    #>
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$CategorizedCommits,
        
        [Parameter(Mandatory = $false)]
        [string]$ConfigPath = "config/changelog-config.json"
    )
    
    # Load configuration for output settings
    $config = $null
    if (Test-Path $ConfigPath) {
        try {
            $configContent = Get-Content $ConfigPath -Raw -Encoding UTF8
            $config = $configContent | ConvertFrom-Json
        }
        catch {
            Write-Warning "Failed to load config for output settings: $($_.Exception.Message)"
        }
    }
    
    # Get title and description from config or use defaults
    $title = if ($config -and $config.output -and $config.output.title) { 
        $config.output.title 
    } else { 
        "# Changelog" 
    }
    
    $description = if ($config -and $config.output -and $config.output.description) { 
        $config.output.description 
    } else { 
        "All notable changes to this project will be documented in this file." 
    }
    
    # Start building the document
    $document = @()
    $document += $title
    $document += ""  # Empty line
    $document += $description
    $document += ""  # Empty line
    
    # Get category order from CategorizationModule
    Import-Module (Join-Path $PSScriptRoot "CategorizationModule.psm1") -Force
    $categoryOrder = Get-CategoryOrder -ConfigPath $ConfigPath
    
    # Add each category section in the specified order
    foreach ($categoryName in $categoryOrder) {
        if ($CategorizedCommits.ContainsKey($categoryName) -and $CategorizedCommits[$categoryName].Count -gt 0) {
            $section = Format-CategorySection -CategoryName $categoryName -CommitList $CategorizedCommits[$categoryName] -ConfigPath $ConfigPath
            if (-not [string]::IsNullOrWhiteSpace($section)) {
                $document += $section
            }
        }
    }
    
    # Add any categories not in the standard order (shouldn't happen, but just in case)
    foreach ($categoryName in $CategorizedCommits.Keys) {
        if ($categoryName -notin $categoryOrder -and $CategorizedCommits[$categoryName].Count -gt 0) {
            $section = Format-CategorySection -CategoryName $categoryName -CommitList $CategorizedCommits[$categoryName] -ConfigPath $ConfigPath
            if (-not [string]::IsNullOrWhiteSpace($section)) {
                $document += $section
            }
        }
    }
    
    return $document -join "`r`n"
}

function Test-MarkdownValidity {
    <#
    .SYNOPSIS
    Tests if the generated Markdown is valid
    
    .PARAMETER MarkdownContent
    The Markdown content to validate
    
    .RETURNS
    Boolean indicating if the Markdown is valid
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$MarkdownContent
    )
    
    # Basic Markdown validation checks
    $issues = @()
    
    # Check for unmatched brackets
    $openBrackets = ($MarkdownContent -split '\[').Count - 1
    $closeBrackets = ($MarkdownContent -split '\]').Count - 1
    if ($openBrackets -ne $closeBrackets) {
        $issues += "Unmatched square brackets detected"
    }
    
    # Check for unmatched parentheses
    $openParens = ($MarkdownContent -split '\(').Count - 1
    $closeParens = ($MarkdownContent -split '\)').Count - 1
    if ($openParens -ne $closeParens) {
        $issues += "Unmatched parentheses detected"
    }
    
    # Check for proper heading structure
    $lines = $MarkdownContent -split "`r`n"
    foreach ($line in $lines) {
        if ($line -match '^#+\s*$') {
            $issues += "Empty heading detected: '$line'"
        }
    }
    
    if ($issues.Count -gt 0) {
        Write-Warning "Markdown validation issues found: $($issues -join ', ')"
        return $false
    }
    
    return $true
}

# Export functions
Export-ModuleMember -Function Get-FormattingConfig, Format-CommitEntry, Escape-MarkdownText, Sort-CommitsChronologically, Format-CategorySection, Format-ChangelogDocument, Test-MarkdownValidity
