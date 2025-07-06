# FilterModule.psm1
# Module for filtering commit messages

function Get-FilteringConfig {
    <#
    .SYNOPSIS
    Loads the filtering configuration from the config file
    
    .PARAMETER ConfigPath
    Path to the configuration file
    
    .RETURNS
    Configuration object
    #>
    param(
        [Parameter(Mandatory = $false)]
        [string]$ConfigPath = "config/changelog-config.json"
    )
    
    try {
        if (Test-Path $ConfigPath) {
            $configContent = Get-Content $ConfigPath -Raw -Encoding UTF8
            $config = $configContent | ConvertFrom-Json
            return $config
        }
        else {
            Write-Warning "Configuration file not found at: $ConfigPath. Using default patterns."
            return $null
        }
    }
    catch {
        Write-Warning "Failed to load configuration: $($_.Exception.Message). Using default patterns."
        return $null
    }
}

function Test-CommitAgainstPatterns {
    <#
    .SYNOPSIS
    Tests a commit message against an array of regex patterns
    
    .PARAMETER Message
    The commit message to test
    
    .PARAMETER Patterns
    Array of regex patterns to test against
    
    .RETURNS
    Boolean indicating if any pattern matches
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

function Test-MergeCommit {
    <#
    .SYNOPSIS
    Tests if a commit message is a merge commit
    
    .PARAMETER Message
    The commit message to test
    
    .RETURNS
    Boolean indicating if this is a merge commit
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message
    )
    
    $mergePatterns = @(
        '^[Mm]erge\s+',                    # "Merge branch" or "merge pull"
        '^[Mm]erge\s+branch\s+',           # "Merge branch 'feature'"
        '^[Mm]erge\s+pull\s+request\s+',   # "Merge pull request #123"
        '^[Mm]erge\s+remote-tracking\s+',  # "Merge remote-tracking branch"
        '^[Aa]uto-merge\s+',               # "Auto-merge"
        '\binto\s+\w+\s*$'                # Ends with "into branch"
    )
    
    foreach ($pattern in $mergePatterns) {
        if ($Message -match $pattern) {
            return $true
        }
    }
    
    return $false
}

function Test-DependabotCommit {
    <#
    .SYNOPSIS
    Tests if a commit message is from Dependabot
    
    .PARAMETER Message
    The commit message to test
    
    .RETURNS
    Boolean indicating if this is a Dependabot commit
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message
    )
    
    $dependabotPatterns = @(
        '^[Dd]ependabot',              # "Dependabot"
        '^[Bb]ump\s+.*\s+from\s+',     # "Bump package from x to y"
        '\bdependabot\[bot\]',         # "dependabot[bot]"
        '^[Aa]uto.*[Uu]pdate\s+',      # "Auto update dependencies"
        '^[Ss]ecurity\s+[Uu]pdate'     # "Security update"
    )
    
    foreach ($pattern in $dependabotPatterns) {
        if ($Message -match $pattern) {
            return $true
        }
    }
    
    return $false
}

function Test-DocumentationCommit {
    <#
    .SYNOPSIS
    Tests if a commit message is for documentation-only changes
    
    .PARAMETER Message
    The commit message to test
    
    .RETURNS
    Boolean indicating if this is a documentation-only commit
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message
    )
    
    $docPatterns = @(
        '^[Dd]ocs?\s',                     # "Docs: update readme"
        '^[Dd]ocumentation\s',             # "Documentation update"
        '^[Uu]pdate\s+[Dd]ocs?\b',         # "Update docs"
        '^[Aa]dd\s+[Dd]ocumentation\b',    # "Add documentation"
        '^[Ff]ix\s+[Dd]ocs?\b',            # "Fix docs"
        '\b[Rr]eadme\b.*[Uu]pdate',        # "README update"
        '\b[Rr]eadme\.md\b',              # "Update README.md"
        '^[Uu]pdate\s+[Rr]eadme\b',       # "Update README"
        '\b\.md\s+file\b',                # "Update .md file"
        '^[Dd]oc\s+[Ff]ix\b',             # "Doc fix"
        '^[Cc]omments?\s+[Uu]pdate',      # "Comments update"
        '^[Aa]dd\s+[Cc]omments?\b',       # "Add comments"
        '^[Uu]pdate\s+[Cc]omments?\b',    # "Update comments"
        '\b[Cc]omment\s+[Ff]ix\b',        # "Comment fix"
        '^[Tt]ypo\s+[Ff]ix\b',            # "Typo fix"
        '^[Ff]ix\s+[Tt]ypo\b'             # "Fix typo"
    )
    
    foreach ($pattern in $docPatterns) {
        if ($Message -match $pattern) {
            return $true
        }
    }
    
    return $false
}

function Test-FormattingCommit {
    <#
    .SYNOPSIS
    Tests if a commit message is for code formatting or linting changes
    
    .PARAMETER Message
    The commit message to test
    
    .RETURNS
    Boolean indicating if this is a formatting/linting commit
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message
    )
    
    $formattingPatterns = @(
        '^[Ff]ix\s+formatting\b',          # "Fix formatting"
        '^[Ff]ormat\s+code\b',             # "Format code"
        '^[Cc]ode\s+format\b',             # "Code format"
        '^[Ll]inting\s',                   # "Linting fixes"
        '^[Ff]ix\s+linting\b',             # "Fix linting"
        '^[Ll]int\s+fix\b',                # "Lint fix"
        '^[Rr]un\s+prettier\b',            # "Run prettier"
        '^[Pp]rettier\s+fix\b',            # "Prettier fix"
        '^[Ee]slint\s+fix\b',              # "ESLint fix"
        '^[Ff]ix\s+eslint\b',              # "Fix ESLint"
        '^[Ss]tyle\s+fix\b',               # "Style fix"
        '^[Ff]ix\s+style\b',               # "Fix style"
        '^[Ww]hitespace\s+fix\b',          # "Whitespace fix"
        '^[Ff]ix\s+whitespace\b',          # "Fix whitespace"
        '^[Ii]ndentation\s+fix\b',         # "Indentation fix"
        '^[Ff]ix\s+indentation\b',         # "Fix indentation"
        '^[Aa]uto\s+format\b',             # "Auto format"
        '^[Ff]ormatting\s+update\b',       # "Formatting update"
        '^[Cc]lean\s+up\s+code\b',         # "Clean up code"
        '^[Cc]ode\s+cleanup\b',            # "Code cleanup"
        '^[Rr]eformat\s+code\b',           # "Reformat code"
        '\b[Ss]tylecop\s+fix\b',           # "StyleCop fix" (for .NET)
        '\b[Rr]esharper\s+cleanup\b',      # "ReSharper cleanup" (for .NET)
        '^[Ff]ix\s+[Cc][Ss]\s+warnings?\b' # "Fix CS warnings" (C# compiler warnings)
    )
    
    foreach ($pattern in $formattingPatterns) {
        if ($Message -match $pattern) {
            return $true
        }
    }
    
    return $false
}

function Test-PackageUpdate {
    <#
    .SYNOPSIS
    Tests if a commit message indicates a package or dependency update
    
    .PARAMETER Message
    The commit message to test
    
    .RETURNS
    Boolean indicating if this is a package update commit
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message
    )
    
    $packagePatterns = @(
        '\bpackage\.json\b',           # Package.json updates
        '\bpackage-lock\.json\b',     # NPM lock file
        '\byarn\.lock\b',             # Yarn lock file
        '\bGemfile\.lock\b',          # Ruby lock file
        '\bComposer\.lock\b',         # PHP lock file
        '\bpoetry\.lock\b',           # Python poetry lock
        '\bPipfile\.lock\b',          # Python pipfile lock
        '\brequirements\.txt\b',      # Python requirements
        '\b[Uu]pdate.*[Dd]ependenc',   # "Update dependencies"
        '\b[Bb]ump.*[Vv]ersion\b',    # "Bump version"
        '\b[Uu]pgrade.*[Pp]ackage',   # "Upgrade package"
        '^[Uu]pdate\s+\w+\s+to\s+v?\d', # "Update package to v1.2.3"
        '\b[Dd]ependency\s+[Uu]pdate', # "Dependency update"
        '^[Cc]hore.*[Dd]eps?\b'       # "chore: update deps" or "dep"
    )
    
    foreach ($pattern in $packagePatterns) {
        if ($Message -match $pattern) {
            return $true
        }
    }
    
    return $false
}

function Filter-CommitsWithConfig {
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    <#
    .SYNOPSIS
    Filters out non-essential commit messages using configuration
    
    .PARAMETER CommitList
    Array of commit objects to filter
    
    .PARAMETER ConfigPath
    Path to the configuration file
    
    .RETURNS
    Array of filtered commit objects
    #>
    param (
        [Parameter(Mandatory = $true)]
        [array]$CommitList,
        
        [Parameter(Mandatory = $false)]
        [string]$ConfigPath = "config/changelog-config.json"
    )

    # Load configuration
    $config = Get-FilteringConfig -ConfigPath $ConfigPath
    $filteredCommits = @()

    foreach ($commit in $CommitList) {
        $exclude = $false
        
        if ($config -and $config.filteringRules) {
            # Use configuration-based filtering
            $rules = $config.filteringRules
            
            # Check merge commits
            if (-not $exclude -and $rules.mergeCommits -and $rules.mergeCommits.enabled -and 
                (Test-CommitAgainstPatterns -Message $commit.Message -Patterns $rules.mergeCommits.patterns)) {
                $exclude = $true
            }
            
            # Check dependabot commits
            if (-not $exclude -and $rules.dependabotCommits -and $rules.dependabotCommits.enabled -and 
                (Test-CommitAgainstPatterns -Message $commit.Message -Patterns $rules.dependabotCommits.patterns)) {
                $exclude = $true
            }
            
            # Check documentation commits
            if (-not $exclude -and $rules.documentationCommits -and $rules.documentationCommits.enabled -and 
                (Test-CommitAgainstPatterns -Message $commit.Message -Patterns $rules.documentationCommits.patterns)) {
                $exclude = $true
            }
            
            # Check formatting commits
            if (-not $exclude -and $rules.formattingCommits -and $rules.formattingCommits.enabled -and 
                (Test-CommitAgainstPatterns -Message $commit.Message -Patterns $rules.formattingCommits.patterns)) {
                $exclude = $true
            }
            
            # Check package updates
            if (-not $exclude -and $rules.packageUpdates -and $rules.packageUpdates.enabled -and 
                (Test-CommitAgainstPatterns -Message $commit.Message -Patterns $rules.packageUpdates.patterns)) {
                $exclude = $true
            }
        }
        else {
            # Fallback to default filtering if config not available
            if (-not $exclude -and (Test-MergeCommit -Message $commit.Message)) {
                $exclude = $true
            }
            if (-not $exclude -and (Test-DependabotCommit -Message $commit.Message)) {
                $exclude = $true
            }
            if (-not $exclude -and (Test-DocumentationCommit -Message $commit.Message)) {
                $exclude = $true
            }
            if (-not $exclude -and (Test-FormattingCommit -Message $commit.Message)) {
                $exclude = $true
            }
            if (-not $exclude -and (Test-PackageUpdate -Message $commit.Message)) {
                $exclude = $true
            }
        }

        if (-not $exclude) {
            $filteredCommits += $commit
        }
    }

    return $filteredCommits
}

function Filter-Commits {
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    <#
    .SYNOPSIS
    Filters out non-essential commit messages
    
    .PARAMETER CommitList
    Array of commit objects to filter
    
    .RETURNS
    Array of filtered commit objects
    #>
    param (
        [Parameter(Mandatory = $true)]
        [array]$CommitList
    )

    # Define regex patterns for exclusion
    $patterns = @(
        '^Merge\s',                  # Merge commits
        '^Dependabot\s',             # Dependabot commits
        '^Docs?[:\s]',               # Documentation changes (with colon or space)
        '^Fix formatting\s',         # Formatting changes
        '^Linting\s'                 # Linting
    )

    $filteredCommits = @()

    foreach ($commit in $CommitList) {
        $exclude = $false
        
        # Check general exclusion patterns
        foreach ($pattern in $patterns) {
            if ($commit.Message -match $pattern) {
                $exclude = $true
                break
            }
        }
        
        # Check for merge commits if not already excluded
        if (-not $exclude -and (Test-MergeCommit -Message $commit.Message)) {
            $exclude = $true
        }
        
        # Check for dependabot commits if not already excluded
        if (-not $exclude -and (Test-DependabotCommit -Message $commit.Message)) {
            $exclude = $true
        }
        
        # Check for documentation commits if not already excluded
        if (-not $exclude -and (Test-DocumentationCommit -Message $commit.Message)) {
            $exclude = $true
        }
        
        # Check for formatting commits if not already excluded
        if (-not $exclude -and (Test-FormattingCommit -Message $commit.Message)) {
            $exclude = $true
        }
        
        # Check for package updates if not already excluded
        if (-not $exclude -and (Test-PackageUpdate -Message $commit.Message)) {
            $exclude = $true
        }

        if (-not $exclude) {
            $filteredCommits += $commit
        }
    }

    return $filteredCommits
}

# Export functions
Export-ModuleMember -Function Filter-Commits, Filter-CommitsWithConfig, Test-PackageUpdate, Test-DependabotCommit, Test-MergeCommit, Test-DocumentationCommit, Test-FormattingCommit, Get-FilteringConfig, Test-CommitAgainstPatterns
