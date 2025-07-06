# FileGenerator.psm1
# Module for generating and writing changelog files

function New-ChangelogFile {
    <#
    .SYNOPSIS
    Creates a new changelog.md file in the specified location
    
    .PARAMETER FilePath
    Path where the changelog file should be created
    
    .PARAMETER Content
    The content to write to the changelog file
    
    .PARAMETER Encoding
    Text encoding to use (default: UTF8)
    
    .RETURNS
    Boolean indicating success
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        
        [Parameter(Mandatory = $true)]
        [string]$Content,
        
        [Parameter(Mandatory = $false)]
        [string]$Encoding = "UTF8"
    )
    
    try {
        Write-Host "Creating changelog file: $FilePath" -ForegroundColor Green
        
        # Ensure directory exists
        $directory = Split-Path $FilePath -Parent
        if (-not (Test-Path $directory)) {
            New-Item -ItemType Directory -Path $directory -Force | Out-Null
            Write-Host "Created directory: $directory" -ForegroundColor Yellow
        }
        
        # Write content to file
        $Content | Out-File -FilePath $FilePath -Encoding $Encoding -Force
        
        # Verify file was created successfully
        if (Test-Path $FilePath) {
            $fileSize = (Get-Item $FilePath).Length
            Write-Host "Successfully created changelog file ($fileSize bytes)" -ForegroundColor Green
            return $true
        }
        else {
            throw "File was not created successfully"
        }
    }
    catch {
        Write-Error "Failed to create changelog file: $($_.Exception.Message)"
        return $false
    }
}

function Test-FileWritePermission {
    <#
    .SYNOPSIS
    Tests if the current user has write permission to the specified path
    
    .PARAMETER FilePath
    Path to test write permission for
    
    .RETURNS
    Boolean indicating if write permission exists
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath
    )
    
    try {
        $directory = Split-Path $FilePath -Parent
        if (-not $directory) {
            $directory = "."
        }
        
        # Test by trying to create a temporary file
        $tempFile = Join-Path $directory "temp_write_test.tmp"
        "test" | Out-File -FilePath $tempFile -Encoding UTF8 -ErrorAction Stop
        Remove-Item $tempFile -ErrorAction SilentlyContinue
        
        return $true
    }
    catch {
        Write-Warning "No write permission for path: $FilePath - $($_.Exception.Message)"
        return $false
    }
}

function Get-ChangelogHeader {
    <#
    .SYNOPSIS
    Creates the standard changelog header
    
    .PARAMETER ConfigPath
    Path to the configuration file
    
    .RETURNS
    String containing the changelog header
    #>
    param(
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
            Write-Warning "Failed to load config for header: $($_.Exception.Message)"
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
    
    # Build header
    $header = @()
    $header += $title
    $header += ""  # Empty line
    $header += $description
    $header += ""  # Empty line
    $header += "*Generated on $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')*"
    $header += ""  # Empty line
    
    return $header -join "`r`n"
}

function Add-ChangelogMetadata {
    <#
    .SYNOPSIS
    Adds metadata to the changelog content
    
    .PARAMETER Content
    The changelog content
    
    .PARAMETER CommitCount
    Number of commits processed
    
    .PARAMETER FilteredCount
    Number of commits that were filtered out
    
    .PARAMETER ConfigPath
    Path to the configuration file
    
    .RETURNS
    Content with metadata added
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$Content,
        
        [Parameter(Mandatory = $false)]
        [int]$CommitCount = 0,
        
        [Parameter(Mandatory = $false)]
        [int]$FilteredCount = 0,
        
        [Parameter(Mandatory = $false)]
        [string]$ConfigPath = "config/changelog-config.json"
    )
    
    # Get emoji config
    Import-Module (Join-Path $PSScriptRoot "ChangelogFormatter.psm1") -Force -DisableNameChecking
    $emojiConfig = Get-EmojiConfig -ConfigPath $ConfigPath
    $autoGenEmoji = if ($emojiConfig -and $emojiConfig.autoGenerated) { 
        $emojiConfig.autoGenerated 
    } else { 
        "🤖" 
    }
    
    $metadata = @()
    $metadata += ""  # Empty line
    $metadata += "---"
    $metadata += ""  # Empty line
    $metadata += "*$autoGenEmoji This changelog was automatically generated from $CommitCount commits.*"
    
    if ($FilteredCount -gt 0) {
        $metadata += "*$FilteredCount commits were filtered out (merges, dependencies, formatting, etc.).*"
    }
    
    $metadata += ""  # Empty line
    
    return $Content + ($metadata -join "`r`n")
}

function Backup-ExistingChangelog {
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    <#
    .SYNOPSIS
    Creates a backup of an existing changelog file
    
    .PARAMETER FilePath
    Path to the existing changelog file
    
    .RETURNS
    Path to the backup file, or null if backup failed
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath
    )
    
    if (-not (Test-Path $FilePath)) {
        Write-Host "No existing changelog to backup" -ForegroundColor Yellow
        return $null
    }
    
    try {
        $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
        $backupPath = $FilePath -replace '\.md$', "_backup_$timestamp.md"
        
        Copy-Item $FilePath $backupPath -Force
        Write-Host "Created backup: $backupPath" -ForegroundColor Green
        
        return $backupPath
    }
    catch {
        Write-Error "Failed to create backup: $($_.Exception.Message)"
        return $null
    }
}

function Write-ChangelogToFile {
    <#
    .SYNOPSIS
    Writes the complete changelog to a file with all necessary formatting and metadata
    
    .PARAMETER FilePath
    Path where the changelog should be written
    
    .PARAMETER CategorizedCommits
    Hashtable containing categorized commits
    
    .PARAMETER ConfigPath
    Path to the configuration file
    
    .PARAMETER CreateBackup
    Whether to create a backup of existing file
    
    .PARAMETER CommitCount
    Total number of commits processed
    
    .PARAMETER FilteredCount
    Number of commits filtered out
    
    .RETURNS
    Boolean indicating success
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        
        [Parameter(Mandatory = $true)]
        [hashtable]$CategorizedCommits,
        
        [Parameter(Mandatory = $false)]
        [string]$ConfigPath = "config/changelog-config.json",
        
        [Parameter(Mandatory = $false)]
        [switch]$CreateBackup,
        
        [Parameter(Mandatory = $false)]
        [int]$CommitCount = 0,
        
        [Parameter(Mandatory = $false)]
        [int]$FilteredCount = 0
    )
    
    try {
        # Test write permissions first
        if (-not (Test-FileWritePermission -FilePath $FilePath)) {
            throw "No write permission for file: $FilePath"
        }
        
        # Create backup if requested and file exists
        if ($CreateBackup -and (Test-Path $FilePath)) {
            $backupPath = Backup-ExistingChangelog -FilePath $FilePath
            if (-not $backupPath) {
                Write-Warning "Backup creation failed, but continuing with file generation"
            }
        }
        
        # Import the ChangelogFormatter module
        Import-Module (Join-Path $PSScriptRoot "ChangelogFormatter.psm1") -Force -DisableNameChecking
        
        # Generate the formatted changelog content
        Write-Host "Formatting changelog content..." -ForegroundColor Yellow
        $changelogContent = Format-ChangelogDocument -CategorizedCommits $CategorizedCommits -ConfigPath $ConfigPath
        
        # Add metadata
        $finalContent = Add-ChangelogMetadata -Content $changelogContent -CommitCount $CommitCount -FilteredCount $FilteredCount -ConfigPath $ConfigPath
        
        # Validate the Markdown
        if (-not (Test-MarkdownValidity -MarkdownContent $finalContent)) {
            Write-Warning "Generated Markdown may have formatting issues"
        }
        
        # Write to file
        $success = New-ChangelogFile -FilePath $FilePath -Content $finalContent -Encoding "UTF8"
        
        if ($success) {
            Write-Host "Changelog successfully written to: $FilePath" -ForegroundColor Green
            
            # Display summary with emojis
            Import-Module (Join-Path $PSScriptRoot "ChangelogFormatter.psm1") -Force -DisableNameChecking
            $emojiConfig = Get-EmojiConfig -ConfigPath $ConfigPath
            $summaryEmoji = if ($emojiConfig -and $emojiConfig.summary) { 
                $emojiConfig.summary 
            } else { 
                "📊" 
            }
            
            $totalEntries = ($CategorizedCommits.Values | Measure-Object -Property Count -Sum).Sum
            Write-Host "$summaryEmoji Summary: $totalEntries entries across $($CategorizedCommits.Keys.Count) categories" -ForegroundColor Cyan
            
            foreach ($category in $CategorizedCommits.Keys) {
                $count = $CategorizedCommits[$category].Count
                if ($count -gt 0) {
                    $categoryEmoji = Get-CategoryEmoji -CategoryName $category -ConfigPath $ConfigPath
                    $entryText = if ($count -eq 1) { "entry" } else { "entries" }
                    Write-Host "  - $categoryEmoji $category`: $count $entryText" -ForegroundColor White
                }
            }
        }
        
        return $success
    }
    catch {
        Write-Error "Failed to write changelog to file: $($_.Exception.Message)"
        return $false
    }
}

function Test-ChangelogFileIntegrity {
    <#
    .SYNOPSIS
    Tests the integrity of a generated changelog file
    
    .PARAMETER FilePath
    Path to the changelog file to test
    
    .RETURNS
    Boolean indicating if the file is valid
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath
    )
    
    try {
        if (-not (Test-Path $FilePath)) {
            Write-Error "Changelog file does not exist: $FilePath"
            return $false
        }
        
        # Read the file content
        $content = Get-Content $FilePath -Raw -Encoding UTF8
        
        # Basic integrity checks
        $issues = @()
        
        # Check if file is empty
        if ([string]::IsNullOrWhiteSpace($content)) {
            $issues += "File is empty"
        }
        
        # Check for basic Markdown structure
        if ($content -notmatch '^#\s+') {
            $issues += "Missing main heading"
        }
        
        # Check for category sections
        if ($content -notmatch '###\s+') {
            $issues += "No category sections found"
        }
        
        # Import ChangelogFormatter for Markdown validation
        Import-Module (Join-Path $PSScriptRoot "ChangelogFormatter.psm1") -Force -DisableNameChecking
        
        # Validate Markdown
        if (-not (Test-MarkdownValidity -MarkdownContent $content)) {
            $issues += "Markdown validation failed"
        }
        
        if ($issues.Count -gt 0) {
            Write-Warning "Changelog file integrity issues: $($issues -join ', ')"
            return $false
        }
        
        Write-Host "Changelog file integrity check passed" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Error "Failed to test file integrity: $($_.Exception.Message)"
        return $false
    }
}

# Export functions
Export-ModuleMember -Function New-ChangelogFile, Test-FileWritePermission, Get-ChangelogHeader, Add-ChangelogMetadata, Backup-ExistingChangelog, Write-ChangelogToFile, Test-ChangelogFileIntegrity
