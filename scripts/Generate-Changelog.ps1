<#
.SYNOPSIS
    Generate-Changelog.ps1 - Automated changelog generation from git commits

.DESCRIPTION
    This script generates a CHANGELOG.md file from git commit messages following conventional commit standards.
    It's designed to parse, filter, categorize, and format commits into a structured changelog.

.ARCHITECTURE OVERVIEW
    The script follows a modular architecture with the following key components:
    
    1. **Module Imports** (lines 11-17)
       - CommitParser.psm1: Parses raw git commit lines into structured objects
       - FilterModule.psm1: Filters out non-essential commits (merge, deps, docs, etc.)
       - CategorizationModule.psm1: Categorizes commits by type (feat, fix, etc.)
       - FileGenerator.psm1: Handles file I/O and changelog formatting
       - UpdateModule.psm1: Manages incremental changelog updates
       
       Note: All modules use -DisableNameChecking to suppress warnings about non-approved verbs
    
    2. **Configuration** (config/changelog-config.json)
       - Defines filtering patterns for excluded commits
       - Maps commit types to changelog categories
       - Controls which patterns are enabled/disabled
       - Currently has documentation filtering disabled ("enabled": false)
    
    3. **Commit Standards** (.github/CommitStandars.instructions.md)
       - Defines conventional commit format: type(scope): description
       - Supported types: feat, fix, docs, style, refactor, perf, test, chore, etc.
       - Breaking changes indicated with '!' after scope
       - Detailed commit body separated by blank line

.KEY FUNCTIONS
    - Read-GitCommits: Reads and parses commits from input file
    - Parse-CommitLine: Extracts hash and message from each line
    - Filter-CommitsWithConfig: Removes non-essential commits based on config
    - Categorize-Commits: Groups commits by type into categories
    - Remove-UnwantedCategories: Excludes Documentation and Other Changes
    - Write-ChangelogToFile: Generates the final CHANGELOG.md
    - Update-ExistingChangelog: Appends new commits to existing changelog

.FILTERING LOGIC
    The script filters out:
    - Merge commits (pattern: ^Merge\s)
    - Dependabot commits (pattern: ^Bump\s.*dependabot)
    - Documentation commits (pattern: ^Docs?[:\s] - currently disabled)
    - Formatting commits (pattern: ^Format[:\s]|^style[:\(\s])
    - Dependency updates (pattern: ^chore\(deps?\)[:\s])
    
    Important: Filtering patterns use regex and must be properly escaped in config

.CATEGORIZATION MAPPING
    Commit types map to changelog categories:
    - feat/feature → Features
    - fix/bugfix → Bug Fixes
    - perf/performance → Performance
    - refactor → Refactoring
    - test/tests → Testing
    - build/ci → Build System
    - revert → Reverts
    - security → Security
    - breaking/! → Breaking Changes (highest priority)
    - docs → Documentation (filtered out in final output)
    - Other commits → Other Changes (filtered out in final output)

.OUTPUT FORMAT
    The generated CHANGELOG.md includes:
    - Header with generation timestamp
    - Summary of total commits and filtered count
    - Categorized sections with commit entries
    - Each entry shows: message (commit hash)
    - No brackets around messages (current format)

.MAINTENANCE NOTES FOR AI ASSISTANTS
    
    1. **When modifying filtering rules:**
       - Update patterns in config/changelog-config.json
       - Ensure regex patterns are properly escaped
       - Update corresponding tests in FilterModule.Tests.ps1
       - Check that Filter-CommitsWithConfig handles config correctly
    
    2. **When changing commit categories:**
       - Update categoryMapping in config/changelog-config.json
       - Modify regex patterns in CategorizationModule.psm1
       - Update test expectations in CategorizationModule.Tests.ps1
       - Ensure priority order matches requirements (breaking > feat > fix, etc.)
    
    3. **When updating changelog format:**
       - Modify Format-CategorySection in ChangelogFormatter.psm1
       - Update regex in Get-ExistingChangelogEntries if entry format changes
       - Adjust tests in ChangelogFormatter.Tests.ps1 and UpdateModule.Tests.ps1
    
    4. **Testing considerations:**
       - All modules have comprehensive Pester 5.x tests
       - Tests use real file operations, not just mocks
       - Empty arrays/collections need proper parameter attributes
       - String comparisons may need -Match instead of -Contain for multiline
    
    5. **Common pitfalls:**
       - Regex escaping: PowerShell strings need single backslash, not double
       - Empty commit lists: Parameters should have [AllowEmptyCollection()]
       - Config loading: Always check for null/missing properties
       - Output suppression: Some functions return booleans that need | Out-Null
    
    6. **Module dependencies:**
       - Modules must be imported in correct order
       - CommitParser is foundational, needed by all others
       - FilterModule and CategorizationModule are independent
       - UpdateModule depends on FileGenerator and ChangelogFormatter

.PARAMETERS
    -InputFile: Path to git-commits.txt file (default: "git-commits.txt")
    -OutputFile: Path to output CHANGELOG.md (default: "CHANGELOG.md")
    -Update: Switch to update existing changelog instead of regenerating
    -Preview: Switch to preview changes without writing (not fully implemented)

.EXAMPLE
    # Generate new changelog
    .\Generate-Changelog.ps1
    
    # Update existing changelog with new commits
    .\Generate-Changelog.ps1 -Update
    
    # Use custom input/output files
    .\Generate-Changelog.ps1 -InputFile "commits.txt" -OutputFile "CHANGELOG-v2.md"

.NOTES
    Author: Original development team
    Last Updated: See git history
    PowerShell Version: 5.1+
    
    This script is part of a larger build/release process and expects:
    - Git commits in specific format (hash message)
    - Conventional commit message standards
    - Proper module structure in ./modules directory
#>

# Generate-Changelog.ps1
# Main script for generating changelog from git-commits.txt

param(
    [string]$InputFile = "git-commits.txt",
    [string]$OutputFile = "CHANGELOG.md",
    [switch]$Update,
    [switch]$Preview
)

# Import required modules
$ModulePath = Join-Path $PSScriptRoot "modules"
Import-Module (Join-Path $ModulePath "CommitParser.psm1") -Force -DisableNameChecking
Import-Module (Join-Path $ModulePath "FilterModule.psm1") -Force -DisableNameChecking
Import-Module (Join-Path $ModulePath "CategorizationModule.psm1") -Force -DisableNameChecking
Import-Module (Join-Path $ModulePath "FileGenerator.psm1") -Force -DisableNameChecking
Import-Module (Join-Path $ModulePath "UpdateModule.psm1") -Force -DisableNameChecking

function Read-GitCommits {
    <#
    .SYNOPSIS
    Reads and parses git commits from the input file
    
    .PARAMETER FilePath
    Path to the git-commits.txt file
    
    .RETURNS
    Array of commit objects with Hash and Message properties
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath
    )
    
    Write-Host "Reading commits from: $FilePath" -ForegroundColor Green
    
    # Check if file exists
    if (-not (Test-Path $FilePath)) {
        throw "Input file not found: $FilePath"
    }
    
    # Read file content
    try {
        $content = Get-Content $FilePath -Encoding UTF8 -ErrorAction Stop
        if (-not $content) {
            throw "Input file is empty: $FilePath"
        }
        
        Write-Host "Found $($content.Count) commits in input file" -ForegroundColor Yellow
        
        # Parse each line
        $commits = @()
        $lineNumber = 0
        
        foreach ($line in $content) {
            $lineNumber++
            
            if ([string]::IsNullOrWhiteSpace($line)) {
                Write-Warning "Skipping empty line at line $lineNumber"
                continue
            }
            
            try {
                $parsedCommit = Parse-CommitLine -CommitLine $line -LineNumber $lineNumber
                if ($parsedCommit) {
                    $commits += $parsedCommit
                }
            }
            catch {
                Write-Warning "Failed to parse line $lineNumber`: $line - $($_.Exception.Message)"
                continue
            }
        }
        
        Write-Host "Successfully parsed $($commits.Count) valid commits" -ForegroundColor Green
        return $commits
    }
    catch {
        throw "Failed to read input file: $($_.Exception.Message)"
    }
}

# Main execution
if ($MyInvocation.InvocationName -ne '.') {
    try {
        Write-Host "Starting Changelog Generation..." -ForegroundColor Cyan
        Write-Host "Input File: $InputFile" -ForegroundColor White
        Write-Host "Output File: $OutputFile" -ForegroundColor White
        
        # Read commits
        $commits = Read-GitCommits -FilePath $InputFile
        
        # Filter out non-essential commits
        $filteredCommits = Filter-CommitsWithConfig -CommitList $commits
        $filteredCount = $commits.Count - $filteredCommits.Count
        Write-Host "Filtered out $filteredCount non-essential commits" -ForegroundColor Yellow
        
        # Ensure we have full paths
        $fullOutputPath = if ([System.IO.Path]::IsPathRooted($OutputFile)) {
            $OutputFile
        } else {
            Join-Path (Get-Location) $OutputFile
        }
        
        Write-Host "Full output path: $fullOutputPath" -ForegroundColor White
        
        if ($Update -and (Test-Path $fullOutputPath)) {
            Write-Host "Update mode: Appending new entries to existing changelog" -ForegroundColor Magenta
            
            # Update existing changelog
            $success = Update-ExistingChangelog -FilePath $fullOutputPath -NewCommits $filteredCommits -CreateBackup
        }
        else {
            Write-Host "Generate mode: Creating new changelog file" -ForegroundColor Magenta
            
            # Categorize commits
            $categorizedCommits = Categorize-Commits -CommitList $filteredCommits
            
            # Remove Documentation and Other Changes categories
            $filteredCategories = Remove-UnwantedCategories -CategorizedCommits $categorizedCommits
            
            # Update filtered count to include removed categories
            $removedFromCategories = 0
            foreach ($category in @("Documentation", "Other Changes")) {
                if ($categorizedCommits.ContainsKey($category)) {
                    $removedFromCategories += $categorizedCommits[$category].Count
                }
            }
            $totalFilteredCount = $filteredCount + $removedFromCategories
            
            # Generate new changelog file
            $success = Write-ChangelogToFile -FilePath $fullOutputPath -CategorizedCommits $filteredCategories -CreateBackup -CommitCount $commits.Count -FilteredCount $totalFilteredCount
            
            # Save last processed commit for future incremental updates
            if ($success -and $filteredCommits.Count -gt 0) {
                Save-LastProcessedCommit -CommitHash $filteredCommits[0].Hash
            }
        }
        
        if (-not $success) {
            throw "Changelog operation failed"
        }
        
        Write-Host "`nChangelog generation completed successfully!" -ForegroundColor Green
    }
    catch {
        Write-Error "Failed to generate changelog: $($_.Exception.Message)"
        exit 1
    }
}
