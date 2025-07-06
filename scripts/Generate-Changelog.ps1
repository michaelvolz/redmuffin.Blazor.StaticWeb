<#
.SYNOPSIS
    Generate-Changelog.ps1 - Automated changelog generation from git commits

.DESCRIPTION
    This script generates a CHANGELOG.md file from git commit messages following conventional commit standards.
    It can read commits directly from git log or from a pre-generated file for backward compatibility.
    The script parses, filters, categorizes, and formats commits into a structured changelog.

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

.DEFAULT BEHAVIOR
    By default (when no -InputFile is specified), the script:
    1. Checks if git is installed and available
    2. Verifies the current directory is a git repository
    3. Ensures the repository has at least one commit
    4. Executes 'git --no-pager log --oneline' to retrieve commits
    5. Processes commits directly from git output
    
    For backward compatibility:
    - If -InputFile is specified, uses that file instead of git log
    - If 'git-commits.txt' exists in current directory, uses it automatically
    - The -CommitLimit parameter only works with git log, not file input

.KEY FUNCTIONS
    - Invoke-GitLog: Executes git log command and captures output
    - Test-GitAvailable: Checks if git is installed
    - Test-GitRepository: Verifies current directory is a git repo
    - Test-GitRepositoryHasCommits: Ensures repository has commits
    - Read-GitCommitsFromSource: Reads commits from file or git log
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
    -InputFile: Optional path to git-commits.txt file. If not specified, reads directly from git log
    -OutputFile: Path to output CHANGELOG.md (default: "CHANGELOG.md")
    -Update: Switch to update existing changelog instead of regenerating
    -Preview: Switch to preview changes without writing (not fully implemented)
    -CommitLimit: Optional limit for the number of commits to retrieve when using git log (default: 0 = no limit)

.EXAMPLE
    # Generate new changelog from git log
    .\Generate-Changelog.ps1

    # Update existing changelog with new commits from git log
    .\Generate-Changelog.ps1 -Update

    # Use custom input file (backward compatibility)
    .\Generate-Changelog.ps1 -InputFile "commits.txt" -OutputFile "CHANGELOG-v2.md"

    # Generate changelog from git log with custom output file
    .\Generate-Changelog.ps1 -OutputFile "CHANGELOG-v2.md"

    # Generate changelog from last 100 commits only
    .\Generate-Changelog.ps1 -CommitLimit 100

.REQUIREMENTS
    For default git log mode:
    - Git must be installed and available in system PATH
    - Must be run from within a git repository
    - Repository must have at least one commit
    - Git log output format: "hash message" (one line per commit)
    
    For file input mode:
    - Input file must exist and be readable
    - File format: one commit per line as "hash message"
    - No git installation required when using file input

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
# Main script for generating changelog from git commits (git log by default, or from file)

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateScript({ 
        if ($_ -and -not (Test-Path $_)) {
            throw "Input file not found: $_"
        }
        return $true
    })]
    [string]$InputFile,
    
    [Parameter()]
    [string]$OutputFile = "CHANGELOG.md",
    
    [Parameter()]
    [switch]$Update,
    
    [Parameter()]
    [switch]$Preview,
    
    [Parameter()]
    [ValidateRange(0, [int]::MaxValue)]
    [int]$CommitLimit = 0
)

# Define custom error messages for common git failures
$script:GitErrorMessages = @{
    NotInstalled = "Git is not installed or not available in the system PATH. Please install git or use the -InputFile parameter to specify a pre-generated commit file."
    NotRepository = "Current directory is not a git repository. Please run this script from within a git repository or use the -InputFile parameter to specify a pre-generated commit file."
    EmptyRepository = "Git repository is empty (no commits). Please make at least one commit before generating a changelog."
    InvalidArguments = "Invalid git log arguments. Please check the commit limit parameter."
    CommandFailed = "Git command failed. Please check your git installation and repository status."
    ParseError = "Failed to parse git commit. The commit format may be invalid."
}

# Import required modules
$ModulePath = Join-Path $PSScriptRoot "modules"
Import-Module (Join-Path $ModulePath "CommitParser.psm1") -Force -DisableNameChecking
Import-Module (Join-Path $ModulePath "FilterModule.psm1") -Force -DisableNameChecking
Import-Module (Join-Path $ModulePath "CategorizationModule.psm1") -Force -DisableNameChecking
Import-Module (Join-Path $ModulePath "FileGenerator.psm1") -Force -DisableNameChecking
Import-Module (Join-Path $ModulePath "UpdateModule.psm1") -Force -DisableNameChecking

function Get-GitTroubleshootingHelp {
    <#
    .SYNOPSIS
    Provides troubleshooting help for common git issues

    .DESCRIPTION
    Returns helpful suggestions based on the type of git error encountered.

    .PARAMETER ErrorType
    The type of git error encountered

    .RETURNS
    String containing troubleshooting suggestions
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ErrorType
    )

    switch ($ErrorType) {
        "NotInstalled" {
            return @"
To install git:
  - Windows: Download from https://git-scm.com/download/win or use 'winget install Git.Git'
  - After installation, restart your terminal and try again
"@
        }
        "NotRepository" {
            return @"
To fix this issue:
  - Ensure you're in the correct directory: cd /path/to/your/repo
  - If this is a new project, initialize git: git init
  - Or clone an existing repository: git clone <repository-url>
"@
        }
        "EmptyRepository" {
            return @"
To fix this issue:
  - Create your first commit: git add . && git commit -m "Initial commit"
  - Or use the -InputFile parameter with a pre-generated commit file
"@
        }
        default {
            return "Please check your git installation and repository configuration."
        }
    }
}

function Test-GitAvailable {
    <#
    .SYNOPSIS
    Checks if git is installed and available in the system PATH

    .DESCRIPTION
    Verifies that git executable can be found and executed.
    Returns $true if git is available, $false otherwise.

    .RETURNS
    Boolean indicating if git is available
    #>
    [CmdletBinding()]
    param()

    try {
        # Try to get git version
        $gitVersion = & git --version 2>&1
        
        if ($LASTEXITCODE -eq 0 -and $gitVersion -match "git version") {
            Write-Verbose "Git is available: $gitVersion"
            return $true
        }
        else {
            Write-Verbose "Git command failed or returned unexpected output"
            return $false
        }
    }
    catch {
        Write-Verbose "Git is not available: $($_.Exception.Message)"
        return $false
    }
}

function Test-GitRepository {
    <#
    .SYNOPSIS
    Checks if the current directory is a git repository

    .DESCRIPTION
    Verifies that the current directory is inside a git repository by checking for .git folder
    or using git rev-parse. Returns $true if in a git repo, $false otherwise.

    .RETURNS
    Boolean indicating if current directory is in a git repository
    #>
    [CmdletBinding()]
    param()

    try {
        # Use git rev-parse to check if we're in a git repository
        $gitStatus = & git rev-parse --is-inside-work-tree 2>&1
        
        if ($LASTEXITCODE -eq 0 -and $gitStatus -eq "true") {
            Write-Verbose "Current directory is inside a git repository"
            
            # Also get the root directory for information
            $gitRoot = & git rev-parse --show-toplevel 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-Verbose "Git repository root: $gitRoot"
            }
            
            return $true
        }
        else {
            Write-Verbose "Not in a git repository: $gitStatus"
            return $false
        }
    }
    catch {
        Write-Verbose "Failed to check git repository status: $($_.Exception.Message)"
        return $false
    }
}

function Test-GitRepositoryHasCommits {
    <#
    .SYNOPSIS
    Checks if the git repository has any commits

    .DESCRIPTION
    Verifies that the repository contains at least one commit.
    Returns $true if commits exist, $false if the repository is empty.

    .RETURNS
    Boolean indicating if repository has commits
    #>
    [CmdletBinding()]
    param()

    try {
        # Check if there are any commits
        $commitCount = & git rev-list --count HEAD 2>&1
        
        if ($LASTEXITCODE -eq 0 -and [int]$commitCount -gt 0) {
            Write-Verbose "Repository has $commitCount commits"
            return $true
        }
        elseif ($LASTEXITCODE -ne 0) {
            # Check for specific empty repo error
            if ($commitCount -match "does not have any commits yet|bad revision 'HEAD'") {
                Write-Verbose "Repository is empty (no commits)"
                return $false
            }
            else {
                Write-Verbose "Failed to check commit count: $commitCount"
                return $false
            }
        }
        else {
            Write-Verbose "Repository appears to be empty"
            return $false
        }
    }
    catch {
        Write-Verbose "Error checking repository commits: $($_.Exception.Message)"
        return $false
    }
}

function Invoke-GitLog {
    <#
    .SYNOPSIS
    Executes git log command to retrieve commit history

    .DESCRIPTION
    Runs 'git --no-pager log --oneline' to get commit history directly from the git repository.
    Returns the output as an array of strings.

    .PARAMETER CommitLimit
    Optional parameter to limit the number of commits retrieved

    .RETURNS
    Array of strings containing git log output (one commit per line)
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [int]$CommitLimit = 0
    )

    Write-Verbose "Starting git log command execution"
    Write-Verbose "CommitLimit parameter: $CommitLimit"

    try {
        # Build the git command
        $gitCommand = "git"
        # Include encoding option to ensure UTF-8 output
        $gitArgs = @("-c", "core.quotepath=false", "-c", "i18n.logoutputencoding=utf-8", "--no-pager", "log", "--oneline")
        
        # Add commit limit if specified
        if ($CommitLimit -gt 0) {
            $gitArgs += "-n"
            $gitArgs += $CommitLimit.ToString()
            Write-Verbose "Applied commit limit: $CommitLimit"
        }
        else {
            Write-Verbose "No commit limit applied - retrieving all commits"
        }

        Write-Verbose "Full git command: $gitCommand $($gitArgs -join ' ')"
        Write-Verbose "Current directory: $(Get-Location)"

        # Save current output encoding and set to UTF-8
        $originalOutputEncoding = [Console]::OutputEncoding
        try {
            # Set console output encoding to UTF-8 to handle special characters
            [Console]::OutputEncoding = [System.Text.Encoding]::UTF8

            # Execute git command and capture output
            $gitOutput = & $gitCommand $gitArgs 2>&1
            
            # Check for git command errors
            if ($LASTEXITCODE -ne 0) {
                # Parse specific git error scenarios
                $errorMessage = $gitOutput -join " "
                
                if ($errorMessage -match "not a git repository") {
                    throw "Not in a git repository. Please run this script from within a git repository."
                }
                elseif ($errorMessage -match "does not have any commits yet") {
                    throw "Git repository has no commits. Please make at least one commit before generating a changelog."
                }
                elseif ($errorMessage -match "ambiguous argument") {
                    throw "Invalid git log arguments. Please check the commit limit parameter."
                }
                else {
                    throw "Git command failed with exit code ${LASTEXITCODE}: $errorMessage"
                }
            }
        }
        finally {
            # Restore original output encoding
            [Console]::OutputEncoding = $originalOutputEncoding
        }

        # Convert output to array if it's a single string
        if ($gitOutput -is [string]) {
            $gitOutput = @($gitOutput)
        }

        # Check for empty repository (no commits)
        if ($gitOutput.Count -eq 0) {
            Write-Verbose "Git log returned no commits - repository appears to be empty"
            throw "No commits found in the repository. The repository appears to be empty."
        }

        Write-Verbose "Successfully retrieved $($gitOutput.Count) commits from git log"
        Write-Verbose "First commit: $($gitOutput[0])"
        Write-Verbose "Last commit: $($gitOutput[-1])"
        
        return $gitOutput
    }
    catch [System.Management.Automation.CommandNotFoundException] {
        throw "Git executable not found. Please ensure git is installed and available in the system PATH."
    }
    catch {
        # Re-throw with more context if not already a specific error
        if ($_.Exception.Message -notmatch "^(Not in a git repository|Git repository has no commits|No commits found|Invalid git log arguments|Git executable not found)") {
            throw "Failed to execute git log: $($_.Exception.Message)"
        }
        else {
            throw
        }
    }
}

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

function Read-GitCommitsFromSource {
    <#
    .SYNOPSIS
    Reads and parses git commits from either a file or git log

    .DESCRIPTION
    If FilePath is provided, reads from the file. Otherwise, executes git log directly.

    .PARAMETER FilePath
    Optional path to the git-commits.txt file. If not provided, uses git log.

    .PARAMETER CommitLimit
    Optional limit for the number of commits to retrieve when using git log

    .RETURNS
    Array of commit objects with Hash and Message properties
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$FilePath,
        [Parameter()]
        [int]$CommitLimit = 0
    )

    if ($FilePath) {
        Write-Host "Reading commits from file: $FilePath" -ForegroundColor Green
        return Read-GitCommits -FilePath $FilePath
    }
    else {
        Write-Host "Reading commits from git log..." -ForegroundColor Green
        
        try {
            # Get raw git log output
            $gitLogOutput = Invoke-GitLog -CommitLimit $CommitLimit
            
            if (-not $gitLogOutput -or $gitLogOutput.Count -eq 0) {
                throw "No commits found in git log"
            }
        }
        catch {
            # Re-throw with additional context if needed
            Write-Error "Failed to retrieve commits from git log: $($_.Exception.Message)"
            throw
        }

        Write-Host "Found $($gitLogOutput.Count) commits in git log" -ForegroundColor Yellow
        Write-Verbose "Starting to parse git log output..."

        # Parse each line
        $commits = @()
        $lineNumber = 0
        $skippedLines = 0
        $parseErrors = 0

        foreach ($line in $gitLogOutput) {
            $lineNumber++

            if ([string]::IsNullOrWhiteSpace($line)) {
                Write-Verbose "Skipping empty line at line $lineNumber"
                $skippedLines++
                continue
            }

            try {
                Write-Verbose "Parsing line ${lineNumber}: $line"
                $parsedCommit = Parse-CommitLine -CommitLine $line -LineNumber $lineNumber
                if ($parsedCommit) {
                    $commits += $parsedCommit
                    Write-Verbose "Successfully parsed commit: $($parsedCommit.Hash) - $($parsedCommit.Message.Substring(0, [Math]::Min(50, $parsedCommit.Message.Length)))..."
                }
            }
            catch {
                Write-Warning "Failed to parse line $lineNumber`: $line - $($_.Exception.Message)"
                $parseErrors++
                continue
            }
        }

        Write-Verbose "Parsing complete - Total lines: $lineNumber, Skipped: $skippedLines, Parse errors: $parseErrors"
        Write-Host "Successfully parsed $($commits.Count) valid commits" -ForegroundColor Green
        return $commits
    }
}

# Main execution
if ($MyInvocation.InvocationName -ne '.') {
    try {
        Write-Host "Starting Changelog Generation..." -ForegroundColor Cyan
        Write-Host "Output File: $OutputFile" -ForegroundColor White

        # Validate parameters
        if ($InputFile -and $CommitLimit -gt 0) {
            throw "Cannot use -CommitLimit with -InputFile. CommitLimit only applies when reading from git log."
        }

        # Determine source and read commits
        if ($InputFile -and $InputFile -ne "git-commits.txt") {
            # User explicitly specified an input file
            Write-Host "Input File: $InputFile" -ForegroundColor White
            $commits = Read-GitCommitsFromSource -FilePath $InputFile
        }
        elseif ((Test-Path "git-commits.txt") -and $InputFile -eq "git-commits.txt") {
            # Default file exists, use it for backward compatibility
            Write-Host "Input File: $InputFile (found existing file)" -ForegroundColor White
            $commits = Read-GitCommitsFromSource -FilePath $InputFile
        }
        else {
            # No input file specified or default file doesn't exist, use git log
            Write-Host "No input file specified or found, using git log directly" -ForegroundColor Yellow
            Write-Verbose "Switching to git log mode - no input file available"
            
            # Check if git is available
            Write-Verbose "Checking if git is available..."
            if (-not (Test-GitAvailable)) {
                throw $script:GitErrorMessages.NotInstalled
            }
            Write-Verbose "Git is available"
            
            # Check if we're in a git repository
            Write-Verbose "Checking if current directory is a git repository..."
            if (-not (Test-GitRepository)) {
                throw $script:GitErrorMessages.NotRepository
            }
            Write-Verbose "Confirmed: In a git repository"
            
            # Check if repository has any commits
            Write-Verbose "Checking if repository has commits..."
            if (-not (Test-GitRepositoryHasCommits)) {
                throw $script:GitErrorMessages.EmptyRepository
            }
            Write-Verbose "Repository has commits - proceeding with git log"
            
            if ($CommitLimit -gt 0) {
                Write-Host "Commit limit: $CommitLimit" -ForegroundColor Yellow
            }
            $commits = Read-GitCommitsFromSource -CommitLimit $CommitLimit
        }

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
        
        # Provide troubleshooting help for common git errors
        if ($_.Exception.Message -eq $script:GitErrorMessages.NotInstalled) {
            Write-Host "`nTroubleshooting:" -ForegroundColor Yellow
            Write-Host (Get-GitTroubleshootingHelp -ErrorType "NotInstalled")
        }
        elseif ($_.Exception.Message -eq $script:GitErrorMessages.NotRepository) {
            Write-Host "`nTroubleshooting:" -ForegroundColor Yellow
            Write-Host (Get-GitTroubleshootingHelp -ErrorType "NotRepository")
        }
        elseif ($_.Exception.Message -eq $script:GitErrorMessages.EmptyRepository) {
            Write-Host "`nTroubleshooting:" -ForegroundColor Yellow
            Write-Host (Get-GitTroubleshootingHelp -ErrorType "EmptyRepository")
        }
        
        exit 1
    }
}
