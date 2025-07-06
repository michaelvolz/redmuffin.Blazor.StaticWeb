# CategorizationModule.psm1
# Module for categorizing commit messages

function Get-CategorizationConfig {
    <#
    .SYNOPSIS
    Loads the categorization configuration from the config file
    
    .PARAMETER ConfigPath
    Path to the configuration file
    
    .RETURNS
    Configuration object with categorization rules
    #>
    param(
        [Parameter(Mandatory = $false)]
        [string]$ConfigPath = "config/changelog-config.json"
    )
    
    try {
        if (Test-Path $ConfigPath) {
            $configContent = Get-Content $ConfigPath -Raw -Encoding UTF8
            $config = $configContent | ConvertFrom-Json
            return $config.categories
        }
        else {
            Write-Warning "Configuration file not found at: $ConfigPath. Using default categorization."
            return $null
        }
    }
    catch {
        Write-Warning "Failed to load categorization config: $($_.Exception.Message). Using default categorization."
        return $null
    }
}

function Test-CommitCategory {
    <#
    .SYNOPSIS
    Tests a commit message against category patterns
    
    .PARAMETER Message
    The commit message to categorize
    
    .PARAMETER Patterns
    Array of regex patterns for the category
    
    .RETURNS
    Boolean indicating if the message matches this category
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message,
        
        [Parameter(Mandatory = $true)]
        [array]$Patterns
    )
    
    foreach ($pattern in $Patterns) {
        if ($Message -match $pattern) {
            return $true
        }
    }
    
    return $false
}

function Get-CommitCategory {
    <#
    .SYNOPSIS
    Determines the category of a commit message
    
    .PARAMETER Message
    The commit message to categorize
    
    .PARAMETER ConfigPath
    Path to the configuration file
    
    .RETURNS
    String representing the category name
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message,
        
        [Parameter(Mandatory = $false)]
        [string]$ConfigPath = "config/changelog-config.json"
    )
    
    # Load configuration
    $categories = Get-CategorizationConfig -ConfigPath $ConfigPath
    
    if ($categories) {
        # Use configuration-based categorization
        
        # Check Security first (highest priority)
        if ($categories.security -and 
            (Test-CommitCategory -Message $Message -Patterns $categories.security.patterns)) {
            return $categories.security.name
        }
        
        # Check Removed (breaking changes)
        if ($categories.removed -and 
            (Test-CommitCategory -Message $Message -Patterns $categories.removed.patterns)) {
            return $categories.removed.name
        }
        
        # Check Deprecated
        if ($categories.deprecated -and 
            (Test-CommitCategory -Message $Message -Patterns $categories.deprecated.patterns)) {
            return $categories.deprecated.name
        }
        
        # Check Added (new features)
        if ($categories.added -and 
            (Test-CommitCategory -Message $Message -Patterns $categories.added.patterns)) {
            return $categories.added.name
        }
        
        # Check Fixed (bug fixes)
        if ($categories.fixed -and 
            (Test-CommitCategory -Message $Message -Patterns $categories.fixed.patterns)) {
            return $categories.fixed.name
        }
        
        # Check Documentation
        if ($categories.documentation -and 
            (Test-CommitCategory -Message $Message -Patterns $categories.documentation.patterns)) {
            return $categories.documentation.name
        }
        
        # Check Testing
        if ($categories.testing -and 
            (Test-CommitCategory -Message $Message -Patterns $categories.testing.patterns)) {
            return $categories.testing.name
        }
        
        # Check Reverted
        if ($categories.reverted -and 
            (Test-CommitCategory -Message $Message -Patterns $categories.reverted.patterns)) {
            return $categories.reverted.name
        }
        
        # Check Changed (improvements/modifications)
        if ($categories.changed -and 
            (Test-CommitCategory -Message $Message -Patterns $categories.changed.patterns)) {
            return $categories.changed.name
        }
    }
    else {
        # Fallback to default categorization logic
        # Only match conventional commits as per commit standards
        
        # Added (new features)
        if ($Message -match '^feat(\(.*\))?:') {
            return "Added"
        }
        
        # Fixed (bug fixes)
        if ($Message -match '^fix(\(.*\))?:') {
            return "Fixed"
        }
        
        # Documentation
        if ($Message -match '^docs?(\(.*\))?:') {
            return "Documentation"
        }
        
        # Testing
        if ($Message -match '^test(\(.*\))?:') {
            return "Testing"
        }
        
        # Security
        if ($Message -match '^security(\(.*\))?:') {
            return "Security"
        }
        
        # Reverted
        if ($Message -match '^revert(\(.*\))?:') {
            return "Reverted"
        }
        
        # Deprecated (not in commit standards but keeping for compatibility)
        if ($Message -match '^deprecate(\(.*\))?:') {
            return "Deprecated"
        }
        
        # Removed (not in commit standards but keeping for compatibility)
        if ($Message -match '^remove(\(.*\))?:') {
            return "Removed"
        }
        
        # Changed (improvements/modifications)
        if ($Message -match '^refactor(\(.*\))?:' -or 
            $Message -match '^perf(\(.*\))?:' -or 
            $Message -match '^chore(\(.*\))?:' -or 
            $Message -match '^config(\(.*\))?:' -or 
            $Message -match '^ci(\(.*\))?:' -or 
            $Message -match '^style(\(.*\))?:') {
            return "Changed"
        }
    }
    
    # Default category for uncategorized commits
    return "Other Changes"
}

function Categorize-Commits {
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    <#
    .SYNOPSIS
    Categorizes an array of commits into groups
    
    .PARAMETER CommitList
    Array of commit objects to categorize
    
    .PARAMETER ConfigPath
    Path to the configuration file
    
    .RETURNS
    Hashtable with categories as keys and arrays of commits as values
    #>
    param(
        [Parameter(Mandatory = $true)]
        [array]$CommitList,
        
        [Parameter(Mandatory = $false)]
        [string]$ConfigPath = "config/changelog-config.json"
    )
    
    $categorizedCommits = @{}
    
    foreach ($commit in $CommitList) {
        $category = Get-CommitCategory -Message $commit.Message -ConfigPath $ConfigPath
        
        if (-not $categorizedCommits.ContainsKey($category)) {
            $categorizedCommits[$category] = @()
        }
        
        # Add category property to commit object
        $commit | Add-Member -NotePropertyName "Category" -NotePropertyValue $category -Force
        $categorizedCommits[$category] += $commit
    }
    
    return $categorizedCommits
}

function Get-CategoryOrder {
    <#
    .SYNOPSIS
    Returns the preferred order for displaying categories
    
    .PARAMETER ConfigPath
    Path to the configuration file
    
    .RETURNS
    Array of category names in display order
    #>
    param(
        [Parameter(Mandatory = $false)]
        [string]$ConfigPath = "config/changelog-config.json"
    )
    
    # Default order following Keep a Changelog standard (excluding Documentation)
    $defaultOrder = @(
        "Added",
        "Changed", 
        "Deprecated",
        "Removed",
        "Fixed",
        "Security",
        "Testing",
        "Reverted"
    )
    
    # Try to get order from config
    $categories = Get-CategorizationConfig -ConfigPath $ConfigPath
    if ($categories) {
        $configOrder = @()
        
        # Add categories in Keep a Changelog standard order
        if ($categories.added) { $configOrder += $categories.added.name }
        if ($categories.changed) { $configOrder += $categories.changed.name }
        if ($categories.deprecated) { $configOrder += $categories.deprecated.name }
        if ($categories.removed) { $configOrder += $categories.removed.name }
        if ($categories.fixed) { $configOrder += $categories.fixed.name }
        if ($categories.security) { $configOrder += $categories.security.name }
        if ($categories.documentation) { $configOrder += $categories.documentation.name }
        if ($categories.testing) { $configOrder += $categories.testing.name }
        if ($categories.reverted) { $configOrder += $categories.reverted.name }
        
        return $configOrder
    }
    
    return $defaultOrder
}

function Remove-UnwantedCategories {
    <#
    .SYNOPSIS
    Removes Documentation and Other Changes categories from categorized commits
    
    .PARAMETER CategorizedCommits
    Hashtable of categorized commits
    
    .RETURNS
    Hashtable with unwanted categories removed
    #>
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$CategorizedCommits
    )
    
    $filteredCategories = @{}
    
    foreach ($category in $CategorizedCommits.Keys) {
        # Skip Documentation and Other Changes categories
        if ($category -eq "Documentation" -or $category -eq "Other Changes") {
            Write-Host "Filtering out $($CategorizedCommits[$category].Count) commits from category: $category" -ForegroundColor Yellow
            continue
        }
        
        $filteredCategories[$category] = $CategorizedCommits[$category]
    }
    
    return $filteredCategories
}

# Export functions
Export-ModuleMember -Function Get-CategorizationConfig, Test-CommitCategory, Get-CommitCategory, Categorize-Commits, Get-CategoryOrder, Remove-UnwantedCategories
