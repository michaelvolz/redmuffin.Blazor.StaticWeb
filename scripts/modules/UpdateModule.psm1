# UpdateModule.psm1
# Module for updating existing changelog files

function Get-ExistingChangelogEntries {
    <#
    .SYNOPSIS
    Extracts existing commit entries from a changelog file
    
    .PARAMETER FilePath
    Path to the existing changelog file
    
    .RETURNS
    Array of existing commit hashes found in the changelog
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath
    )
    
    if (-not (Test-Path $FilePath)) {
        Write-Host "No existing changelog found at: $FilePath" -ForegroundColor Yellow
        return @()
    }
    
    try {
        $content = Get-Content $FilePath -Raw -Encoding UTF8
        $existingHashes = @()
        
        # Regex pattern to match commit entries: message (hash) or [message] (hash)
        # Updated to support new format without brackets
        $pattern = '(?:^|\s)- (?:\[.+?\]|[^\(\[]+?)\s*\(([a-f0-9]{7,40})\)'
        $matches = [regex]::Matches($content, $pattern)
        
        foreach ($match in $matches) {
            $hash = $match.Groups[1].Value
            if ($hash -and $hash.Length -ge 7) {
                $existingHashes += $hash
            }
        }
        
        Write-Host "Found $($existingHashes.Count) existing entries in changelog" -ForegroundColor Cyan
        return $existingHashes
    }
    catch {
        Write-Error "Failed to read existing changelog: $($_.Exception.Message)"
        return @()
    }
}

function Test-CommitExists {
    <#
    .SYNOPSIS
    Tests if a commit already exists in the changelog
    
    .PARAMETER CommitHash
    The commit hash to test
    
    .PARAMETER ExistingHashes
    Array of existing commit hashes
    
    .RETURNS
    Boolean indicating if the commit already exists
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$CommitHash,
        
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [array]$ExistingHashes
    )
    
    # Check for exact match or if the new hash starts with any existing hash
    # (to handle cases where existing entries might have shorter hashes)
    foreach ($existingHash in $ExistingHashes) {
        # Skip empty strings
        if ([string]::IsNullOrWhiteSpace($existingHash)) {
            continue
        }
        
        if ($CommitHash -eq $existingHash -or 
            $CommitHash.StartsWith($existingHash) -or 
            $existingHash.StartsWith($CommitHash)) {
            return $true
        }
    }
    
    return $false
}

function Filter-NewCommits {
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    <#
    .SYNOPSIS
    Filters out commits that already exist in the changelog
    
    .PARAMETER CommitList
    Array of commit objects to filter
    
    .PARAMETER ExistingHashes
    Array of existing commit hashes
    
    .RETURNS
    Array of new commits that don't already exist
    #>
    param(
        [Parameter(Mandatory = $true)]
        [array]$CommitList,
        
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [array]$ExistingHashes
    )
    
    $newCommits = @()
    $duplicateCount = 0
    
    foreach ($commit in $CommitList) {
        if (-not (Test-CommitExists -CommitHash $commit.Hash -ExistingHashes $ExistingHashes)) {
            $newCommits += $commit
        }
        else {
            $duplicateCount++
            Write-Verbose "Skipping duplicate commit: $($commit.Hash)"
        }
    }
    
    Write-Host "Found $($newCommits.Count) new commits (skipped $duplicateCount duplicates)" -ForegroundColor Yellow
    return $newCommits
}

function Get-ChangelogSections {
    <#
    .SYNOPSIS
    Parses existing changelog to extract section content
    
    .PARAMETER FilePath
    Path to the existing changelog file
    
    .RETURNS
    Hashtable with section names as keys and content as values
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath
    )
    
    if (-not (Test-Path $FilePath)) {
        return @{}
    }
    
    try {
        $content = Get-Content $FilePath -Encoding UTF8
        $sections = @{}
        $currentSection = $null
        $sectionContent = @()
        
        foreach ($line in $content) {
            if ($line -match '^### (.+)$') {
                # Save previous section if it exists
                if ($currentSection) {
                    $sections[$currentSection] = $sectionContent -join "`r`n"
                }
                
                # Start new section
                $currentSection = $matches[1].Trim()
                $sectionContent = @()
            }
            elseif ($currentSection -and $line -match '^- ') {
                # Add entry to current section
                $sectionContent += $line
            }
        }
        
        # Save the last section
        if ($currentSection) {
            $sections[$currentSection] = $sectionContent -join "`r`n"
        }
        
        return $sections
    }
    catch {
        Write-Error "Failed to parse changelog sections: $($_.Exception.Message)"
        return @{}
    }
}

function Merge-ChangelogSections {
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    <#
    .SYNOPSIS
    Merges new categorized commits with existing changelog sections
    
    .PARAMETER ExistingSections
    Hashtable of existing sections
    
    .PARAMETER NewCategorizedCommits
    Hashtable of new categorized commits
    
    .PARAMETER ConfigPath
    Path to the configuration file
    
    .RETURNS
    Merged hashtable of all sections
    #>
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$ExistingSections,
        
        [Parameter(Mandatory = $true)]
        [hashtable]$NewCategorizedCommits,
        
        [Parameter(Mandatory = $false)]
        [string]$ConfigPath = "config/changelog-config.json"
    )
    
    # Import required modules
    Import-Module (Join-Path $PSScriptRoot "ChangelogFormatter.psm1") -Force -DisableNameChecking
    Import-Module (Join-Path $PSScriptRoot "CategorizationModule.psm1") -Force -DisableNameChecking
    
    $mergedSections = @{}
    
    # Get category order
    $categoryOrder = Get-CategoryOrder -ConfigPath $ConfigPath
    
    # Process each category in order
    foreach ($category in $categoryOrder) {
        # Skip Documentation and Other Changes categories
        if ($category -eq "Documentation" -or $category -eq "Other Changes") {
            continue
        }
        
        $existingEntries = if ($ExistingSections.ContainsKey($category)) { 
            $ExistingSections[$category] 
        } else { 
            "" 
        }
        
        $newEntries = ""
        if ($NewCategorizedCommits.ContainsKey($category) -and $NewCategorizedCommits[$category].Count -gt 0) {
            # Format new entries
            $formattedSection = Format-CategorySection -CategoryName $category -CommitList $NewCategorizedCommits[$category] -ConfigPath $ConfigPath
            
            # Extract just the entries (remove header and empty lines)
            $lines = $formattedSection -split "`r`n"
            $entryLines = $lines | Where-Object { $_ -match '^- ' }
            $newEntries = $entryLines -join "`r`n"
        }
        
        # Combine existing and new entries
        if (-not [string]::IsNullOrWhiteSpace($existingEntries) -and -not [string]::IsNullOrWhiteSpace($newEntries)) {
            $mergedSections[$category] = $newEntries + "`r`n" + $existingEntries
        }
        elseif (-not [string]::IsNullOrWhiteSpace($newEntries)) {
            $mergedSections[$category] = $newEntries
        }
        elseif (-not [string]::IsNullOrWhiteSpace($existingEntries)) {
            $mergedSections[$category] = $existingEntries
        }
    }
    
    return $mergedSections
}

function Save-LastProcessedCommit {
    <#
    .SYNOPSIS
    Saves information about the last processed commit for incremental updates
    
    .PARAMETER CommitHash
    Hash of the last processed commit
    
    .PARAMETER FilePath
    Path to save the tracking information
    
    .RETURNS
    Boolean indicating success
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$CommitHash,
        
        [Parameter(Mandatory = $false)]
        [string]$FilePath = ".changelog-state"
    )
    
    try {
        $stateInfo = @{
            lastProcessedCommit = $CommitHash
            lastUpdate = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        }
        
        $stateInfo | ConvertTo-Json | Out-File $FilePath -Encoding UTF8 -Force
        Write-Host "Saved last processed commit: $CommitHash" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Error "Failed to save last processed commit: $($_.Exception.Message)"
        return $false
    }
}

function Get-LastProcessedCommit {
    <#
    .SYNOPSIS
    Gets information about the last processed commit
    
    .PARAMETER FilePath
    Path to the tracking information file
    
    .RETURNS
    Hash of the last processed commit, or null if none found
    #>
    param(
        [Parameter(Mandatory = $false)]
        [string]$FilePath = ".changelog-state"
    )
    
    if (-not (Test-Path $FilePath)) {
        Write-Host "No previous state found - this appears to be the first run" -ForegroundColor Yellow
        return $null
    }
    
    try {
        $stateContent = Get-Content $FilePath -Raw -Encoding UTF8
        $stateInfo = $stateContent | ConvertFrom-Json
        
        Write-Host "Last processed commit: $($stateInfo.lastProcessedCommit) (updated: $($stateInfo.lastUpdate))" -ForegroundColor Cyan
        return $stateInfo.lastProcessedCommit
    }
    catch {
        Write-Warning "Failed to read last processed commit state: $($_.Exception.Message)"
        return $null
    }
}

function Update-ExistingChangelog {
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    <#
    .SYNOPSIS
    Updates an existing changelog with new commits
    
    .PARAMETER FilePath
    Path to the changelog file
    
    .PARAMETER NewCommits
    Array of new commit objects
    
    .PARAMETER ConfigPath
    Path to the configuration file
    
    .PARAMETER CreateBackup
    Whether to create a backup before updating
    
    .RETURNS
    Boolean indicating success
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        
        [Parameter(Mandatory = $true)]
        [array]$NewCommits,
        
        [Parameter(Mandatory = $false)]
        [string]$ConfigPath = "config/changelog-config.json",
        
        [Parameter(Mandatory = $false)]
        [switch]$CreateBackup
    )
    
    try {
        Write-Host "Updating existing changelog: $FilePath" -ForegroundColor Green
        
        # Get existing entries to prevent duplicates
        $existingHashes = Get-ExistingChangelogEntries -FilePath $FilePath
        
        # Ensure we have an array, not null
        if ($null -eq $existingHashes) {
            $existingHashes = @()
        }
        
        # Filter out commits that already exist
        $filteredNewCommits = Filter-NewCommits -CommitList $NewCommits -ExistingHashes $existingHashes
        
        if ($filteredNewCommits.Count -eq 0) {
            Write-Host "No new commits to add to changelog" -ForegroundColor Yellow
            return $true
        }
        
        # Create backup if requested
        if ($CreateBackup) {
            Import-Module (Join-Path $PSScriptRoot "FileGenerator.psm1") -Force -DisableNameChecking
            $backupPath = Backup-ExistingChangelog -FilePath $FilePath
            if (-not $backupPath) {
                Write-Warning "Backup creation failed, but continuing with update"
            }
        }
        
        # Import required modules
        Import-Module (Join-Path $PSScriptRoot "CategorizationModule.psm1") -Force -DisableNameChecking
        Import-Module (Join-Path $PSScriptRoot "ChangelogFormatter.psm1") -Force -DisableNameChecking
        
        # Categorize new commits
        $newCategorizedCommits = Categorize-Commits -CommitList $filteredNewCommits -ConfigPath $ConfigPath
        
        # Remove Documentation and Other Changes categories
        $filteredNewCategories = Remove-UnwantedCategories -CategorizedCommits $newCategorizedCommits
        
        # Get existing sections
        $existingSections = Get-ChangelogSections -FilePath $FilePath
        
        # Merge sections
        $mergedSections = Merge-ChangelogSections -ExistingSections $existingSections -NewCategorizedCommits $filteredNewCategories -ConfigPath $ConfigPath
        
        # Convert merged sections back to categorized commits format for the formatter
        $mergedCategorizedCommits = @{}
        foreach ($category in $mergedSections.Keys) {
            # Parse entries back to commit objects (this is a simplified approach)
            # In a real scenario, you might want to preserve the original commit objects
            $mergedCategorizedCommits[$category] = @()
            
            if (-not [string]::IsNullOrWhiteSpace($mergedSections[$category])) {
                $lines = $mergedSections[$category] -split "`r`n"
                foreach ($line in $lines) {
                    # Match both formats: "- [message] (hash)" and "- message (hash)"
                    if ($line -match '^- (?:\[(.+?)\]|(.+?)) \(([a-f0-9]{7,40})\)') {
                        $message = if ($matches[1]) { $matches[1] } else { $matches[2] }
                        $hash = $matches[3]
                        $mergedCategorizedCommits[$category] += [PSCustomObject]@{
                            Hash = $hash
                            Message = $message
                            Category = $category
                        }
                    }
                }
            }
        }
        
        # Write updated changelog
        Import-Module (Join-Path $PSScriptRoot "FileGenerator.psm1") -Force -DisableNameChecking
        $success = Write-ChangelogToFile -FilePath $FilePath -CategorizedCommits $mergedCategorizedCommits -ConfigPath $ConfigPath -CommitCount ($NewCommits.Count + $existingHashes.Count) -FilteredCount 0
        
        if ($success -and $filteredNewCommits.Count -gt 0) {
            # Save the last processed commit (first new commit, since they're in newest-first order)
            $null = Save-LastProcessedCommit -CommitHash $filteredNewCommits[0].Hash
        }
        
        return $success
    }
    catch {
        Write-Error "Failed to update changelog: $($_.Exception.Message)"
        return $false
    }
}

# Export functions
Export-ModuleMember -Function Get-ExistingChangelogEntries, Test-CommitExists, Filter-NewCommits, Get-ChangelogSections, Merge-ChangelogSections, Save-LastProcessedCommit, Get-LastProcessedCommit, Update-ExistingChangelog
